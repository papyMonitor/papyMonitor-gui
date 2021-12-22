using Godot;
using System;

public class widgetSimpleValue : HBoxContainer
{
	// Classes instances
	private MainTop RInst;
	private Chart chartInst;
    private VBoxContainer signalsPanelInst;

    private Label Label_IndexInst;
    private LineEdit LineEdit_ValueInst;

    public Int32 BaseIndex { get; set; } = -1;
    public Int32 ArrIdx { get; set; } = -1;

    private UtilityFunctions utilFuncs;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Seek Chart class instance
        RInst = GetNode<MainTop>("/root/Main");

        // Find the chart
		chartInst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("Chart") as Chart;
        // Find the signalsPanel
        signalsPanelInst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel") as VBoxContainer;
        
        Label_IndexInst = FindNode("Label_Index") as Label;
        LineEdit_ValueInst = FindNode("LineEdit_Value") as LineEdit;
        
        LineEdit_ValueInst.Connect("text_entered", this, nameof(_on_LineEdit_Value_Key_enter_pressed));
        LineEdit_ValueInst.Connect("focus_entered", this, nameof(_on_LineEdit_Value_focus_entered));
        LineEdit_ValueInst.Connect("focus_exited", this, nameof(_on_LineEdit_Value_focus_exited));

        utilFuncs = new UtilityFunctions();
    }

    private void _on_LineEdit_Value_Key_enter_pressed(string txt)
    {
        if (RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].OnEdit)
        {
            // Update local variable for Visu3D
            RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].Value = 
                utilFuncs.GetValue(txt, (VarType_e)RInst.ConfigData.Vars[BaseIndex].Type[0]);

            send(txt);
        }
    }

    private void _on_LineEdit_Value_focus_entered()
    {
        RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].OnEdit = true;
    }
    private void _on_LineEdit_Value_focus_exited()
    {
        RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].OnEdit = false;
    }

    private void send(string txt)
    {
        RInst.ComPortInst.SendCommand(RInst.ConfigData.SetValue, (Byte)(BaseIndex+ArrIdx), 
            (VarType_e)RInst.ConfigData.Vars[BaseIndex].Type[0], txt);
    }
}
