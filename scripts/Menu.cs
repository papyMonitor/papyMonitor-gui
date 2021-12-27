using Godot;
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using NLua;

public enum ComCtrl_e : byte
{
    // Commands from monitor to target;
    SetValue = (byte)'#',
    ReportAllValuesOn = (byte)'?',
    ReportAllValuesOff = (byte)'!',
    ReportValueOnce = (byte)'$',
    ExeCustomCommand = (byte)'@',

    // Messages from target to monitor
    IReportValue =        (byte)':',
    IReportTextConsole =  (byte)'>',
}

public class Menu : HBoxContainer
{
    // Classes instances
    private MainTop RInst;
 
    private Chart chartInst;
    private Label labelFooterInfoInst;
    private MenuButton fileMenuInst, openPortMenuInst, parametersInst;
    private HBoxContainer menuInst;
    private PopupMenu openPortMenuElementsInst, fileMenuElementsInst, parametersElementsInst;
    private ConsoleOut ConsoleInst;

    //
    private const int ID_OPEN_FILE = 0;
    private const int ID_EXIT = 1;

    private FileDialog LoadFileInst, SaveFileInst;
    private bool opensaveParamFile;

    private List<string> ParametersNames;
    private Int32 idParametersNamesSelected;
    private string ini;

    private bool serSendStop = false;

    private bool startupFirstSerialReceive = true;

    private UInt32 transmissionErrors = 0;

    private static Texture textOff = ResourceLoader.Load("res://icons/blackButton.png") as Texture;
    private static Texture textGreen = ResourceLoader.Load("res://icons/greenButton.png") as Texture;
    private static Texture textRed = ResourceLoader.Load("res://icons/redButton.png") as Texture;

    private UtilityFunctions helper;

    public override void _Ready()
    {
        // Seek class instances
        RInst = GetNode<MainTop>("/root/Main");

        chartInst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("Chart") as Chart;

        labelFooterInfoInst = RInst.FindNode("LabelFooterInfo") as Label;

        menuInst = RInst.FindNode("Menu") as HBoxContainer;

        openPortMenuInst = FindNode("MenuButton_OpenPort") as MenuButton;
        openPortMenuInst.Text = "Open port";
        openPortMenuElementsInst = openPortMenuInst.GetPopup();

        fileMenuInst = FindNode("MenuButton_File") as MenuButton;
        fileMenuElementsInst = fileMenuInst.GetPopup();

        parametersInst = FindNode("MenuButton_Parameters") as MenuButton;
        parametersElementsInst = parametersInst.GetPopup();

        LoadFileInst = RInst.FindNode("LoadFile") as FileDialog;
        LoadFileInst.Connect("file_selected", this, nameof(onLoadFileSelected));
        SaveFileInst = RInst.FindNode("SaveFile") as FileDialog;
        SaveFileInst.Connect("file_selected", this, nameof(onSaveFileSelected));

        ParametersNames = new List<string>();

        ConsoleInst = (GetNode<MainTop>("/root/Main")).FindNode("ConsoleOut") as ConsoleOut;

        /*
		* FILE MENU
		*/
        fileMenuElementsInst.AddItem("Open *.lua file", ID_OPEN_FILE);
        fileMenuElementsInst.AddSeparator();
        fileMenuElementsInst.AddItem("Exit", ID_EXIT);

        fileMenuElementsInst.Connect("id_pressed", this, nameof(MENU_onFilePressed));

        helper = new UtilityFunctions();
    }

    private void updatePortListToMenu()
    {
        // Scan all available serial ports and put on the first menu
        List<string> ports = new List<string>(RInst.ComPortInst.GetPorts());

        // Fill the port menu with all the ports found
        var idx = 0;

        openPortMenuElementsInst.Clear();

        if (!RInst.ComPortInst.IsOpen())
        {
            foreach (string port in ports)
                openPortMenuElementsInst.AddItem(port, idx++);
            openPortMenuElementsInst.Connect("id_pressed", this, nameof(MENU_onOpenPortPressed));
        }
    }

    private void updateParametersMenu()
    {
        ParametersNames.Clear();
        
        // Retreive all the parameters Names from ConfigData.Vars
        foreach (Int32 Data in RInst.ConfigData.Vars.Keys)
        {
            string param = RInst.ConfigData.Vars[Data].Parameter;
            if (param != "") 
            {
                // Check if this parameter is not already in the list
                // and add to list if not found
                if (!ParametersNames.Contains(param))
                    ParametersNames.Add(param);
            }
        }

        // Update the parameter menu with load/save buttons for each parameter type
        // First remove children of parametersElementsInst if any
		parametersElementsInst.Clear();
        for (Int32 idx=0; idx<ParametersNames.Count; idx++)
        {
            PopupMenu submenu = new PopupMenu();
            submenu.Name = ParametersNames[idx];
            submenu.AddItem("Load", idx*3);
            submenu.AddItem("Save", idx*3+1);
            submenu.AddItem("Send to device", idx*3+2);
            submenu.Connect("id_pressed", this, nameof(MENU_onParametersPressed));
            parametersElementsInst.AddChild(submenu);
            parametersElementsInst.AddSubmenuItem(ParametersNames[idx]+" ", ParametersNames[idx], idx);
        }
            

        if (ParametersNames.Count>0)
            parametersInst.Disabled = false; 
        else
           parametersInst.Disabled = true; 
    }

    private void MENU_onFilePressed(int id)
    {
        switch (id)
        {
            case ID_OPEN_FILE:
                opensaveParamFile = false;
                LoadFileInst.PopupCentered();
                break;                         

            case ID_EXIT:
                // Save the current file
                GetTree().Quit();
                break;
        }
    }

    private void MENU_onParametersPressed(int id)
    {
        idParametersNamesSelected = id/3;

        opensaveParamFile = true;

        if ( id % 3 == 0)
            LoadFileInst.PopupCentered();
        else
        {
            if ( id % 3 == 1)
            {   
                SaveFileInst.CurrentFile = ParametersNames[idParametersNamesSelected] + 
                        DateTime.Now.ToString("-yyyy-MM-dd-HH-mm-ss");
                SaveFileInst.PopupCentered();
            }
            else
                SendParametersValuesToDevice(ParametersNames[idParametersNamesSelected]);
        }
    }
    
    private void MENU_onOpenPortPressed(int id)
    {
        string portName = openPortMenuElementsInst.GetItemText(id);

        if (!RInst.ComPortInst.IsOpen())
        {
            // Port was closed, open it
            RInst.ComPortInst.Open(portName, RInst.ConfigData.Baudrate);

            if (RInst.ComPortInst.IsOpen())
            {
                startupFirstSerialReceive = true;
                ConsoleInst.Print(LogLevel_e.eInfo, portName + 
                    " openned @ " + RInst.ConfigData.Baudrate.ToString() + " bauds\n");
                openPortMenuInst.Text = "Port";
                openPortMenuElementsInst.Clear();
                openPortMenuElementsInst.AddItem("Close", 0);
            }
            else
                ConsoleInst.Print(LogLevel_e.eError, "Can't open port\n");
        }
        else
        {
            // Port was openned, close it            
            RInst.ComPortInst.Close();
            ConsoleInst.Print(LogLevel_e.eInfo, "Port closed\n");
            openPortMenuInst.Text = "Open port";
            updatePortListToMenu();
        }
    }

    private void SendParametersValuesToDevice(string ParamName)
    {
        ConsoleInst.Print(LogLevel_e.eInfo, "ParamName: " + ParamName + "\n");

        RInst.ComPortInst.SendParameters(ParamName);
    }

    private void onLoadFileSelected(string file)
    {
        // ini file
        if (!opensaveParamFile)
        {
            var iniFile = new Godot.File();

            if (iniFile.Open(file, Godot.File.ModeFlags.Read) != Error.Ok)
            {
                ConsoleInst.Print(LogLevel_e.eError, "File " + file + " not found, abording...\n");
            }
            else
            {
                ConsoleInst.Print(LogLevel_e.eInfo, "File " + file + " found\n");

                // Close port if user decide to change the config file and the file is found
                RInst.ComPortInst.Close();
                ConsoleInst.Print(LogLevel_e.eInfo, "Port closed\n");
                updatePortListToMenu();

                // Get all the iniFile content as string
                ini = iniFile.GetAsText();
                iniFile.Close();

                Lua state = new Lua ();
                RInst.ConfigData = new ConfigData_t(RInst, System.IO.Path.GetDirectoryName(file));

                state["cfg"] = RInst.ConfigData;
           
                // Parse to ConfigData here
                try
                {
                    state.DoString(ini);
                    // Display port menu
                    openPortMenuInst.Disabled = false;
                }
                catch(Exception ex)
                {
                    ConsoleInst.Print(LogLevel_e.eError, ex.Message + "\n");
                    // Bad file format, hide port menu
                    openPortMenuInst.Disabled = true;
                }

                ////////////////////////////////////////////
                // If parsing ini file was Ok, we display data
                ////////////////////////////////////////////
                if (RInst.ConfigData.ConfigOk)
                    RInst.Display();

                ////////////////////////////////////////////
                // Update the parameters menu
                // For the user, create as many load/save parameters NAME
                // as NAME in the Parameter fields
                ////////////////////////////////////////////            
                updateParametersMenu();
            }
        }
        // parameter file
        else
        {
            string param;
            var paramFile = new Godot.File();

            if (paramFile.Open(file, Godot.File.ModeFlags.Read) != Error.Ok)
            {
                ConsoleInst.Print(LogLevel_e.eError, "File " + file + " not found\n");
            }
            else
            {
                ConsoleInst.Print(LogLevel_e.eInfo, "File " + file + " found\n");

                // Get all the iniFile content as string
                param = paramFile.GetAsText();
                paramFile.Close();

                Lua state = new Lua ();
                ParametersGroups_t pars = 
                    new ParametersGroups_t(RInst, ParametersNames[idParametersNamesSelected]);

                state["par"] = pars;
            
                // Parse to pars here
                bool parseOk = true;
                try
                {
                    state.DoString(param);
                }
                catch(Exception ex)
                {
                    parseOk = false;
                    ConsoleInst.Print(LogLevel_e.eError, ex.Message + "\n");
                }

                if (parseOk)
                {
                    // Fill the related parameters fields of ConfigData
                    foreach (Int32 keyVar in pars.Vars.Keys)
                    {
                        // Check good matching of parameter
                        if (pars.Vars[keyVar].Parameter == RInst.ConfigData.Vars[keyVar].Parameter) 
                        {
                            // Check good matching of data array
                            if (pars.Vars[keyVar].Data.Count == RInst.ConfigData.Vars[keyVar].Data.Count)
                            {
                                foreach (Int32 keyData in pars.Vars[keyVar].Data.Keys)
                                {
                                    RInst.ConfigData.Vars[keyVar].Data[keyData].Value =
                                        pars.Vars[keyVar].Data[keyData].Value;
                                }
                            } 
                            else
                                ConsoleInst.Print(LogLevel_e.eWarning, 
                                    " Variable " + keyVar.ToString() + " has not the same number of data," +
                                    " value(s) not changed.\n");
                        }
                        else
                            ConsoleInst.Print(LogLevel_e.eWarning, 
                                " Variable " + keyVar.ToString() + " is not tagged with same parameter," +
                                " value(s) not changed.\n");
                    }

                    // Update the display fields
                    foreach (Int32 keyVar in RInst.ConfigData.Vars.Keys)
                    {
                        if (RInst.ConfigData.Vars[keyVar].Parameter == ParametersNames[idParametersNamesSelected])
                        {
                            foreach (Int32 keyData in RInst.ConfigData.Vars[keyVar].Data.Keys)
                            {
                                updateDisplayValue(keyVar, keyData);
                            }
                        }
                    }                                   
                }
            }
        }
    }

    private void onSaveFileSelected(string file)
    {
        bool parseOk = true;

        if (opensaveParamFile)
        {
            Lua state = new Lua ();
            ParametersGroups_t pars = 
                new ParametersGroups_t(RInst, ParametersNames[idParametersNamesSelected]);
  
            state["cfg"] = pars;

            // Parse to pars here
            try
            {
                state.DoString(ini);
            }
            catch(Exception ex)
            {
                parseOk = false;
                ConsoleInst.Print(LogLevel_e.eError, 
                "Failed to parse parameter, error: " + ex.Message + "\n");
            }
            
            if (parseOk)
            {
                List<string> parsText = new List<string>();
                //Create from scratch the .lua conf file
                foreach (Int32 varKey in pars.Vars.Keys)
                {
                    parsText.Add("---------------");
                    parsText.Add("par:Parameter({");
                    parsText.Add("    Parameter = \"" + pars.Vars[varKey].Parameter + "\",");
                    parsText.Add("    Name = \"" + pars.Vars[varKey].Text + "\",");
                    parsText.Add("    Type = \"" + pars.Vars[varKey].Type + "\",");
                    parsText.Add("    Index = " + varKey.ToString() + ",");

                    parsText.Add("    Data = {");
                    foreach (Int32 dataKey in pars.Vars[varKey].Data.Keys)
                    {
                        pars.Vars[varKey].Data[dataKey].Value = 
                            RInst.ConfigData.Vars[varKey].Data[dataKey].Value;
                        if ((VarType_e)RInst.ConfigData.Vars[varKey].Type[0] == VarType_e.Single)
                        {
                            NumberFormatInfo nfi = new NumberFormatInfo();
                            nfi.NumberDecimalSeparator = ".";
                            string val = Convert.ToDouble(pars.Vars[varKey].Data[dataKey].Value).ToString("0.000000", nfi);
                            parsText.Add("        { Value = " + val + " },");
                        }
                        else
                            parsText.Add("        { Value = " + pars.Vars[varKey].Data[dataKey].Value + " },");
                            
                    }
                    parsText.Add("    }"); 
                    parsText.Add("})");
                }

                // Save to disk
                System.IO.File.WriteAllLines(file, parsText);
            }
        }
    }

    public override void _Process(float delta)
    {
        if (RInst.ComPortInst.IsOpen())
        {
            if (!startupFirstSerialReceive)
            {
                if (RInst.ComPortInst.GetPendingBytes() > 0)
                    parseLines(RInst.ComPortInst.ReadPendingBytes());
            }
            else
            {
                // port.DiscardInBuffer();
                startupFirstSerialReceive = false;
            }
        }
    }

    string relicat = "";
    float debugFloat = 0.0f;

    string serDataCopy, serDataCopyCompleted;
    private void parseLines(string serData)
    {
        string line;
        Int32 startPos, endPos;
        char[] cars = ":MNOPQR".ToCharArray();

        serDataCopy = serData;

        startPos = 0;
        endPos = 0;

        serData = relicat + serData;

        serDataCopyCompleted = serData;

        // We don't use split() for an obscure raison of performance (see stackoverflow)
        while (endPos != -1 && startPos != -1)
        {
            startPos = serData.IndexOfAny(cars, startPos);

            if (startPos!=-1)
            {
                endPos = serData.IndexOf('\n', startPos);
                if (endPos!=-1)
                {
                    line = serData.Substring(startPos, endPos-startPos);
                    startPos = endPos;

                    // Get the first character witch is the line meaning
                    if (line.Length > 0)
                    {
                        processReportValue(line);
                    }
                }
            }
        }
        if (startPos!=-1)
            relicat = serData.Substring(startPos);
        else
            relicat = "";
    }

    private void processReportValue(string line)
    {
        object objValue = null;
        bool goUpdate = false;
        Int32 varIdx = -1;

        // Check if it's a console line
        if (line.Length > 1)
        {
            if (line[0] == '>') {
                // Yes, print on the console
                ConsoleInst.AddText(line.Substring(2) + "\n");

            } else {

                if (line.Length > 2)
                {
                    try
                    {
                        // It's a report value, retrieve the variable index
                        varIdx = int.Parse(line.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);

                        // Update the receiving timing per index
                        chartUpdateTiming(varIdx);

                        if (line.Length > 3)
                        {
                            VarType_e varType = (VarType_e)line.Substring(3, 1)[0];

                            objValue = helper.GetCOMValue(line, varType);

                            goUpdate = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        String ff = ex.ToString();
                        transmissionErrors++;
                        labelFooterInfoInst.Text = transmissionErrors.ToString();

                        // En attendant de debugger
                        chartUpdateTiming(0);
                    }
                    
                    if (goUpdate)
                        updateValue(varIdx, objValue);
                }  
            }
        }
    }



    private bool keepThisIndex = true;
    private Int32 indexKeeped = -1;
    private UInt32 elementsCounter = 0;
    private UInt32 lastElementsCounter = 0;
    // Timer for updating widget on field receiving
    private void chartUpdateTiming(Int32 varIdx)
    {
        if (keepThisIndex)
        {
            indexKeeped = varIdx;
            keepThisIndex = false;
            elementsCounter = 0;
            lastElementsCounter = 0;
        }
        else
        {
            elementsCounter++;
            if (varIdx == indexKeeped)
            {
                if (lastElementsCounter != elementsCounter)
                {
                    // Number of elements sampled has changed
                    chartInst.SetNbElementsSampled(elementsCounter);
                    lastElementsCounter = elementsCounter;
                }
                elementsCounter = 0;
            }
            // reset in case of indexKeeped unselected
            if (elementsCounter >= 256)
                keepThisIndex = true;
        }
    }

    private void updateValue(Int32 idx, object Value)
    {
        Int32 keyVar = -1;
        Int32 keyData = -1;

        // Search the key that contains this index in the dictionnary
        foreach(Int32 _keyVar in RInst.ConfigData.Vars.Keys)
        {
            Var_t var = RInst.ConfigData.Vars[_keyVar];

            if (Math.Abs(_keyVar-idx) < var.Data.Keys.Count)
            {
                    keyVar = _keyVar;
                    keyData = idx - _keyVar;
                    break;
            }
        }

        try
        {
            // Set the value in ConfigData
            RInst.ConfigData.Vars[keyVar].Data[keyData].Value = Value;

            updateDisplayValue(keyVar, keyData);
            
            // Add point to curve if required
            if (RInst.ConfigData.Vars[keyVar].Data[keyData].CanPlot)
                chartInst.AddElement(keyVar+keyData, Value);
        }
        catch (Exception ex)
        {
            String ff = ex.ToString();
            transmissionErrors++;
            labelFooterInfoInst.Text = transmissionErrors.ToString();
        }
    }

    private void updateDisplayValue(Int32 keyVar, Int32 keyData)
    {
        var Value = RInst.ConfigData.Vars[keyVar].Data[keyData].Value;

        // Refresh the on monitoring flag
        (RInst.ConfigData.Vars[keyVar].RootWigdet as widget).setOnMonitoring();

        // Display NOT boolean
        if (!RInst.ConfigData.Vars[keyVar].Data[keyData].BoolsOnU8)
        {
            if (RInst.ConfigData.Vars[keyVar].WidgetType == "normal")
            {
                widgetSimpleValue widgetSimpleValueInst = (widgetSimpleValue)RInst.ConfigData.Vars[keyVar].Data[keyData].Wigdet;
                LineEdit lineEditInst = widgetSimpleValueInst.FindNode("LineEdit_Value") as LineEdit;

                // Update value on the displayed field only if not in edit mode
                if (!RInst.ConfigData.Vars[keyVar].Data[keyData].OnEdit)
                {
                    // If float -> display with precision
                    if ((VarType_e)RInst.ConfigData.Vars[keyVar].Type[0] == VarType_e.Single)
                    {
                        NumberFormatInfo nfi = new NumberFormatInfo();
                        nfi.NumberDecimalSeparator = ".";
                        lineEditInst.Text = Convert.ToDouble(Value).ToString("n"+RInst.ConfigData.Vars[keyVar].Data[keyData].Precision, nfi);
                    }
                    else
                        lineEditInst.Text = Convert.ToDouble(Value).ToString();
                }
            }

            if (RInst.ConfigData.Vars[keyVar].WidgetType == "sliderH")
            {
                widgetSliderH widgetSliderHInst = (widgetSliderH)RInst.ConfigData.Vars[keyVar].Data[keyData].Wigdet;
                HSlider HSliderInst = widgetSliderHInst.FindNode("HSlider") as HSlider;

                // Update value on the displayed field only if not in edit mode
                if (!RInst.ConfigData.Vars[keyVar].Data[keyData].OnEdit)
                {
                    HSliderInst.Value = Convert.ToSingle(Value);
                }
            }  
            
            if (RInst.ConfigData.Vars[keyVar].WidgetType == "sliderV")
            {
                widgetSliderV widgetSliderVInst = (widgetSliderV)RInst.ConfigData.Vars[keyVar].Data[keyData].Wigdet;
                VSlider VSliderInst = widgetSliderVInst.FindNode("VSlider") as VSlider;

                // Update value on the displayed field only if not in edit mode
                if (!RInst.ConfigData.Vars[keyVar].Data[keyData].OnEdit)
                {
                    VSliderInst.Value = Convert.ToSingle(Value);
                }
            }                                       
        }
        // Display boolean
        else
        {
            // Refresh each bit
            for (byte bit = 0; bit < 8; bit++)
            {
                widgetBool widgetBoolInst = (widgetBool)RInst.ConfigData.Vars[keyVar].Data[keyData].Bits[bit].Wigdet;
                TextureRect texture = widgetBoolInst.FindNode("TextureRect_BitVal") as TextureRect;

                string color = RInst.ConfigData.Vars[keyVar].Data[keyData].Bits[bit].Color;

                bool bitValue = (Convert.ToByte(Value) & (1 << bit)) != 0;

                if (bitValue)
                {
                    if (color=="red")
                        texture.Texture = textRed;
                    else
                        texture.Texture = textGreen;
                }
                else
                    texture.Texture = textOff;
            }
        }
    }
}



