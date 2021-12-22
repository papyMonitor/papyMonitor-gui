using Godot;
using System;

public class widgetButton : HBoxContainer
{
    private MainTop RInst;
    private Button buttonInst;
    private LineEdit lineEditInst;

 	public Int32 VarIdx;
    private UtilityFunctions utilFuncs;

    public override void _Ready()
    {
        VarIdx = -1;
        RInst = GetNode<MainTop>("/root/Main");
        buttonInst = FindNode("Button") as Button;
        lineEditInst = FindNode("LineEdit") as LineEdit;
        buttonInst.Connect("pressed", this, nameof(onButtonPressed));

        utilFuncs = new UtilityFunctions();
    }

	private void onButtonPressed()
	{
		if (RInst.ComPortInst.IsOpen() && VarIdx >= 0) 
            send();
	}

    private void send()
    {
        
		RInst.ComPortInst.SendCommand(RInst.ConfigData.SetValue, (Byte)(VarIdx), 
					(VarType_e)RInst.ConfigData.Vars[VarIdx].Type[0], lineEditInst.Text);
    }
}
