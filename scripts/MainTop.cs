using Godot;
using System;

public class MainTop : Panel
{
	// Classes instances
	public ComPort ComPortInst;

	private const Int32 MAX_SIGNALS_PER_COLUMN = 15;

	private Spatial visu3DInst;
	private VBoxContainer VBoxPlotInst;
	private HBoxContainer HBoxTabGroupsInst;
	private VBoxContainer VBoxVisu3DInst;

	private Chart ChartInst;
	private VBoxContainer ControlAreaInst;
	private VBoxContainer CMDSInst;

	private VBoxContainer SigPan1Inst;
	private VBoxContainer SigPan1Inst2;
	public ConsoleOut ConsoleInst;
	private Button clearConsoleInst;

	public ConfigData_t ConfigData;

	private static Texture textOff = ResourceLoader.Load("res://icons/blackButton.png") as Texture;
	private static Texture textGreen = ResourceLoader.Load("res://icons/greenButton.png") as Texture;
	private static Texture textRed = ResourceLoader.Load("res://icons/redButton.png") as Texture;

	private int cur_joy = -1;
	public Label joyPadNameInst;
	
	public override void _Ready()
	{
		// Class instance
		visu3DInst = FindNode("visu3D") as Spatial;
		VBoxPlotInst = FindNode("VBoxPlot") as VBoxContainer;
		HBoxTabGroupsInst = FindNode("HBoxTabGroups") as HBoxContainer;
		VBoxVisu3DInst = FindNode("VBoxVisu3D") as VBoxContainer;
		ChartInst = (FindNode("drawEngine") as VBoxContainer).FindNode("Chart") as Chart;
		ControlAreaInst = (FindNode("drawEngine") as VBoxContainer).FindNode("controlArea") as VBoxContainer;
		joyPadNameInst = ControlAreaInst.FindNode("JoyPadName") as Label;
		CMDSInst = FindNode("Commands") as VBoxContainer;
		ConsoleInst = FindNode("ConsoleOut") as ConsoleOut;
		clearConsoleInst = FindNode("ClearConsole") as Button;
		clearConsoleInst.Connect("pressed", this, nameof(onClearConsolePressed));
		SigPan1Inst = (FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel") as VBoxContainer;
		SigPan1Inst2 = (FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel2") as VBoxContainer;
		SigPan1Inst.Visible = false;
		SigPan1Inst2.Visible = false;

		ComPortInst = FindNode("ComPort") as ComPort;
	}

	public override void _Process(float delta)
	{
		// Display the name of the joypad if we haven't already.
		if (0 != cur_joy)
		{
			cur_joy = 0;
			joyPadNameInst.Text = Input.GetJoyName(0) + "\n" + Input.GetJoyGuid(0);
		}
	}

	//Called whenever a joypad has been connected or disconnected.
	private void inputJoyConnectionChanged(int device_id, bool connected)
	{
		if (device_id == cur_joy)
		{
			if (connected)
				joyPadNameInst.Text = Input.GetJoyName(device_id) + " " + Input.GetJoyGuid(device_id);
			else
				joyPadNameInst.Text = "";
		}
	}
	
	private void onClearConsolePressed()
	{
		ConsoleInst.Clear();
	}

	public void Display()
	{
		if (checkTabsIndices())
		{
			displayChartSignalsPanels();
			ChartInst.Init();

			displayTabs();
			displayVariables();

			display3D();
		}
	}

	private void display3D()
	{
		// Get the top level node of the scene
		var rootNode = visu3DInst.FindNode("Root") as Node;

		// Delete all the rootNode Childs (the meshs we created before)
		var children = rootNode.GetChildren();
		foreach (Node n in children)
		{
			if (n is Solid_t m)
				m.Free();
		}

		// At least one solid must have a parent of type "Root"
		bool rootScene = false;
		foreach(Solid_t solid in ConfigData.Solids.Values)
		{
			if (solid.Parent == "Root")
			{
				rootScene = true;
				break;
			}
		}

		if (rootScene)
		{
			// Add child shapes to their parents
			foreach(Solid_t solid in ConfigData.Solids.Values)
			{
				if (solid.Parent == "Root")
					rootNode.AddChild(solid);
				else
				{
					foreach(Solid_t parentSolid in ConfigData.Solids.Values)
					{
						if(solid.Parent == parentSolid.Name)
						{
							parentSolid.AddChild(solid);
						}
					}
				}
			}
		}
	}

	private bool checkTabsIndices()
	{
		bool checkOk = true;
		foreach(Int32 TabGroupKey in ConfigData.TabGroups.Keys)
		{		
			foreach(Int32 Tab in ConfigData.TabGroups[TabGroupKey].Tabs.Keys)
			{
				foreach(Int32 Column in ConfigData.TabGroups[TabGroupKey].Tabs[Tab].Columns.Keys)
				{
					foreach(Int32 varIdx in ConfigData.TabGroups[TabGroupKey].Tabs[Tab].Columns[Column].Rows.Values)
					{
						if (!ConfigData.Vars.ContainsKey(varIdx))
						{
							ConsoleInst.Print(LogLevel_e.eError, "Index " + varIdx + " of tabgroup " + 
								TabGroupKey.ToString() + " tab " + Tab.ToString() + " column " + Column + " not found\n");
							checkOk = false;
						}
					}
				}
			}
		}
		return checkOk;
	}

	private void displayChartSignalsPanels()
	{
		// Hide Things that are not needed
		VBoxVisu3DInst.Visible = ConfigData.Vue3D;
		VBoxPlotInst.Visible = ConfigData.Plot;	

		// Remove children of signals plot
		VBoxContainer SigPan1Inst = (FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel") as VBoxContainer;
		var children = SigPan1Inst.GetChildren();
		foreach (Node c in children)
		{
			SigPan1Inst.RemoveChild(c);
			c.QueueFree();
		}
		VBoxContainer SigPan1Inst2 = (FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel2") as VBoxContainer;
		children = SigPan1Inst2.GetChildren();
		foreach (Node c in children)
		{
			SigPan1Inst2.RemoveChild(c);
			c.QueueFree();
		}
	}
	
	private void displayTabs()
	{
		var children = HBoxTabGroupsInst.GetChildren();
		foreach (Node c in children)
		{
			HBoxTabGroupsInst.RemoveChild(c);
			c.QueueFree();
		}

		foreach(Int32 TabGroupKey in ConfigData.TabGroups.Keys)
		{
			TabContainer TabContainerInst = new TabContainer();
			TabContainerInst.UseHiddenTabsForMinSize = true;
			TabContainerInst.TabAlign = TabContainer.TabAlignEnum.Left;

			if (!ConfigData.TabGroups[TabGroupKey].NoExpandX)
				TabContainerInst.SizeFlagsHorizontal = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
			if (!ConfigData.TabGroups[TabGroupKey].NoExpandY)
				TabContainerInst.SizeFlagsVertical = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);

			TabContainerInst.Name = "TabGroup"+TabGroupKey.ToString();
			HBoxTabGroupsInst.AddChild(TabContainerInst);

			foreach(Int32 Tab in ConfigData.TabGroups[TabGroupKey].Tabs.Keys)
			{
				HBoxContainer TabInst = new HBoxContainer();
				TabInst.SizeFlagsHorizontal = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
				TabInst.SizeFlagsVertical = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
				TabInst.Name = ConfigData.TabGroups[TabGroupKey].Tabs[Tab].TabName;
				TabContainerInst.AddChild(TabInst);

				foreach(Int32 Column in ConfigData.TabGroups[TabGroupKey].Tabs[Tab].Columns.Keys)
				{
					// Add the Tab
					VBoxContainer columnInst = new VBoxContainer();
					columnInst.SizeFlagsHorizontal = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
					columnInst.SizeFlagsVertical = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
					TabInst.AddChild(columnInst);
					foreach(Int32 varIdx in ConfigData.TabGroups[TabGroupKey].Tabs[Tab].Columns[Column].Rows.Values)
						columnInst.AddChild(new RowVBoxContainer(varIdx));
				}
			}
		}
	}

	private void displayVariables()
	{
		var children = HBoxTabGroupsInst.GetChildren();
		foreach (TabContainer TabContainerInst in children)
		{
			children = TabContainerInst.GetChildren();
			foreach (HBoxContainer TabInst in children)
			{
				children = TabInst.GetChildren();
				foreach(VBoxContainer ColInst in children)
				{
					children = ColInst.GetChildren();
					foreach(RowVBoxContainer RowVBoxInst in children)
					{
						VBoxContainer elemInst = displayRootWidget(RowVBoxInst, RowVBoxInst.IdxVar);
						// Display the widget variable
						displayWidget(elemInst, RowVBoxInst.IdxVar);
					}	
				}
			}
		}
	}

	private VBoxContainer displayRootWidget(VBoxContainer rowInst, Int32 varIdx)
	{
		var widget = ResourceLoader.Load("res://scenes/widget.tscn") as PackedScene;

		var elemInst = new VBoxContainer();
		elemInst.SizeFlagsHorizontal = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
		elemInst.Name = rowInst.Name + "V" + varIdx.ToString();
		rowInst.AddChild(elemInst);

		var widgetInst = widget.Instance() as widget;
		widgetInst.SizeFlagsHorizontal = 0;
		widgetInst.SizeFlagsVertical = 0;

		widgetInst.varIdx = varIdx;
		widgetInst.sizeArray = ConfigData.Vars[varIdx].Data.Keys.Count;
		widgetInst.Name = elemInst.Name + "WDG";

		elemInst.AddChild(widgetInst);
		
		// Set the element name
		(widgetInst.FindNode("VarText") as Label).Text = ConfigData.Vars[varIdx].Text;
		
		// Disable the Monitoring (M) if the Widget is a button
		if (ConfigData.Vars[varIdx].WidgetType == "Button")
			(widgetInst.FindNode("MonitOnOff") as Button).Visible = false;
		else
			(widgetInst.FindNode("MonitOnOff") as Button).Visible = true;
		
		// Save the element path for Monitoring display
		ConfigData.Vars[varIdx].RootWigdet = widgetInst;

		return elemInst;
	}

	private void displayWidget(VBoxContainer elemInst, Int32 varIdx)
	{
		var widgetSimpleValue = ResourceLoader.Load("res://scenes/widgetSimpleValue.tscn") as PackedScene;
		var widgetSliderH = ResourceLoader.Load("res://scenes/widgetSliderH.tscn") as PackedScene;
		var widgetSliderV = ResourceLoader.Load("res://scenes/widgetSliderV.tscn") as PackedScene;
		var widgetBool = ResourceLoader.Load("res://scenes/widgetBool.tscn") as PackedScene;
		var widgetButton = ResourceLoader.Load("res://scenes/widgetButton.tscn") as PackedScene;

		var scrollInst = new ScrollContainer();
		var vBoxInst = new VBoxContainer();

		// Check for adding a scroll
		if (ConfigData.Vars[varIdx].Scroll)
		{
			scrollInst.ScrollHorizontalEnabled = false;
			scrollInst.ScrollVerticalEnabled = true;
			scrollInst.SizeFlagsVertical = (int)(Control.SizeFlags.Fill);
			scrollInst.SizeFlagsHorizontal = (int)(Control.SizeFlags.Fill);
			scrollInst.RectSize = new Vector2(350, 200);
			scrollInst.RectMinSize = new Vector2(350, 200);
			elemInst.AddChild(scrollInst);
			vBoxInst.RectSize = new Vector2(400, 400);
			vBoxInst.RectMinSize = new Vector2(400, 400);
			vBoxInst.SizeFlagsHorizontal = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
			vBoxInst.SizeFlagsVertical = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);
			scrollInst.AddChild(vBoxInst);
		}

		foreach(Int32 dataKey in ConfigData.Vars[varIdx].Data.Keys)
		{
			// Field is boolsOnU8
			if (ConfigData.Vars[varIdx].Data[dataKey].BoolsOnU8)
			{
				for (Byte bit=0; bit<8; bit++)
				{
					widgetBool widgetBoolInst = widgetBool.Instance() as widgetBool;
					widgetBoolInst.Name = "WSV" + dataKey.ToString() + "BIT" + bit.ToString();
					widgetBoolInst.BaseIndex = varIdx;
					widgetBoolInst.ArrIdx = dataKey;
					widgetBoolInst.bitIndex = bit;
					if (!ConfigData.Vars[varIdx].Scroll)
						elemInst.AddChild(widgetBoolInst);
					else
						vBoxInst.AddChild(widgetBoolInst);

					ConfigData.Vars[varIdx].Data[dataKey].Bits[bit].Wigdet = widgetBoolInst;
					
					if (ConfigData.Vars[varIdx].Data[dataKey].Bits[bit].BitText != "NA")
					{
						(widgetBoolInst.FindNode("Label_BitNumber") as Label).Text = bit.ToString();

						(widgetBoolInst.FindNode("Label_BitName") as Label).Text = 
							 ConfigData.Vars[varIdx].Data[dataKey].Bits[bit].BitText;

						// Show/Hide the edit button related to canBitEditX
						(widgetBoolInst.FindNode("Button_SetBit") as Button).Visible = 
							ConfigData.Vars[varIdx].Data[dataKey].Bits[bit].CanEdit ? true : false;

						(widgetBoolInst.FindNode("Button_ClearBit") as Button).Visible = 
							ConfigData.Vars[varIdx].Data[dataKey].Bits[bit].CanEdit ? true : false;
					}
					else
					{
						(widgetBoolInst.FindNode("Label_BitName") as Label).Visible = false;
						(widgetBoolInst.FindNode("Button_SetBit") as Button).Visible = false;
						(widgetBoolInst.FindNode("Button_ClearBit") as Button).Visible = false;
						(widgetBoolInst.FindNode("TextureRect_BitVal") as TextureRect).Visible = false;
					}
				}
			}
			else 
			{
				if (ConfigData.Vars[varIdx].WidgetType == "normal")
				{
					widgetSimpleValue widgetSimpleValueInst = widgetSimpleValue.Instance() as widgetSimpleValue;
					widgetSimpleValueInst.Name = "WSV" + dataKey.ToString();
					widgetSimpleValueInst.BaseIndex = varIdx;
					widgetSimpleValueInst.ArrIdx = dataKey;
					if (!ConfigData.Vars[varIdx].Scroll)
						elemInst.AddChild(widgetSimpleValueInst);
					else
					{
						vBoxInst.AddChild(widgetSimpleValueInst);
					}
					ConfigData.Vars[varIdx].Data[dataKey].Wigdet = widgetSimpleValueInst;

					// Display the index if array
					if (ConfigData.Vars[varIdx].Data.Keys.Count>1)
					{
						if ( ConfigData.Vars[varIdx].Data[dataKey].SingleText == "")
							(widgetSimpleValueInst.FindNode("Label_Index") as Label).Text = dataKey.ToString();
						else
							(widgetSimpleValueInst.FindNode("Label_Index") as Label).Text =
												 ConfigData.Vars[varIdx].Data[dataKey].SingleText;
					}

					// Check for field editing CanEdit
					if (!ConfigData.Vars[varIdx].Data[dataKey].CanEdit)
						(widgetSimpleValueInst.FindNode("LineEdit_Value") as LineEdit).Editable = false;

					// Check for plot enabled
					if (ConfigData.Vars[varIdx].Data[dataKey].CanPlot)
					{
						// Add signal to signals panel
						var signalPkd = ResourceLoader.Load("res://scenes/signal.tscn") as PackedScene;
						signal signalInst = signalPkd.Instance() as signal;
						signalInst.AddToGroup("signalGroup");

						signalInst.idxCurve = varIdx+dataKey;

						ColorRect colorRectInst = signalInst.FindNode("ColorRect") as ColorRect; 

						colorRectInst.Color = ChartInst.Curves[varIdx+dataKey].dataColor;

						Button signalNameInst = signalInst.FindNode("signalName") as Button;
						signalNameInst.Text = ConfigData.Vars[varIdx].Text + " " + 
													ConfigData.Vars[varIdx].Data[dataKey].SingleText;

						if ( SigPan1Inst.GetChildCount() < MAX_SIGNALS_PER_COLUMN )
							SigPan1Inst.AddChild(signalInst);
						else
							SigPan1Inst2.AddChild(signalInst);
					}
						// (widgetSimpleValueInst.FindNode("Button_Plot") as Button).Visible = false;
					
					// Display the default FieldValues
					(widgetSimpleValueInst.FindNode("LineEdit_Value") as LineEdit).Text =
											ConfigData.Vars[varIdx].Data[dataKey].Value.ToString();

					// // Display the unit
					// (widgetSimpleValueInst.FindNode("Unit") as Label).Text =
					// 						ConfigData.Vars[BaseIndex].Data[dataKey].Unit;
				}
				else
				{
					if (ConfigData.Vars[varIdx].WidgetType == "SliderH")
					{
						widgetSliderH widgetSliderHInst = widgetSliderH.Instance() as widgetSliderH;

						widgetSliderHInst.Name = "WSLIDH" + dataKey.ToString();
						widgetSliderHInst.BaseIndex = varIdx;
						widgetSliderHInst.ArrIdx = dataKey;
						elemInst.AddChild(widgetSliderHInst);

						ConfigData.Vars[varIdx].Data[dataKey].Wigdet = widgetSliderHInst;
						
						// Display the default FieldValues
						(widgetSliderHInst.FindNode("HSlider") as HSlider).Value =
									Convert.ToSingle(ConfigData.Vars[varIdx].Data[dataKey].Value);
					}

					if (ConfigData.Vars[varIdx].WidgetType == "SliderV")
					{
						elemInst.SizeFlagsVertical = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);

						widgetSliderV widgetSliderVInst = widgetSliderV.Instance() as widgetSliderV;

						widgetSliderVInst.Name = "WSWSLIDV" + dataKey.ToString();
						widgetSliderVInst.BaseIndex = varIdx;
						widgetSliderVInst.ArrIdx = dataKey;
						elemInst.AddChild(widgetSliderVInst);

						ConfigData.Vars[varIdx].Data[dataKey].Wigdet = widgetSliderVInst;
						
						// Display the default FieldValues
						(widgetSliderVInst.FindNode("VSlider") as VSlider).Value =
									Convert.ToSingle(ConfigData.Vars[varIdx].Data[dataKey].Value);
					}

					if (ConfigData.Vars[varIdx].WidgetType == "Button")
					{
						elemInst.SizeFlagsVertical = (int)(Control.SizeFlags.Fill | Control.SizeFlags.Expand);

						widgetButton widgetButtonInst = widgetButton.Instance() as widgetButton;

						(widgetButtonInst.FindNode("Button") as Button).Text = "Set";

						if (ConfigData.Vars[varIdx].HideData)
							(widgetButtonInst.FindNode("LineEdit") as LineEdit).Visible = false;
						else
							(widgetButtonInst.FindNode("LineEdit") as LineEdit).Visible = true;

						elemInst.AddChild(widgetButtonInst);

						widgetButtonInst.VarIdx = varIdx;

						ConfigData.Vars[varIdx].Data[dataKey].Wigdet = widgetButtonInst;
					}
				}
			}
		}
	}
}




