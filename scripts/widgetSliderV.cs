using Godot;
using System;

public class widgetSliderV : VBoxContainer
{
    private const int IDX_CMD_JOY_Y = 7;
    private const int IDX_GNR_JOY_Y = 9;

    // Classes instances
	private MainTop RInst;
    private CheckBox CanEditInst;
    private CheckBox UseJoyPadInst;
    private VSlider VSliderInst;

    public Int32 BaseIndex { get; set; } = -1;
    public Int32 ArrIdx { get; set; } = -1;

    private UtilityFunctions utilFuncs;

    private bool joyEnabled = false;

    public override void _Ready()
    {
        utilFuncs = new UtilityFunctions();

        // Seek Chart class instance
        RInst = GetNode<MainTop>("/root/Main");
        CanEditInst = FindNode("CanEdit") as CheckBox;
        UseJoyPadInst = FindNode("UseJoyPad") as CheckBox;
        VSliderInst = FindNode("VSlider") as VSlider;

        CanEditInst.Connect("toggled", this, nameof(_on_CanEdit_toggled));
        UseJoyPadInst.Connect("toggled", this, nameof(_on_UseJoyPad_toggled));
        VSliderInst.Connect("value_changed", this, nameof(_on_VSlider_changed));
    }

	public override void _Process(float delta)
    {
        if (joyEnabled)
        {
            if (BaseIndex == IDX_CMD_JOY_Y)
                VSliderInst.Value = (-Input.GetJoyAxis(0, 3) + 1.0f) * 127.5f;

            if (BaseIndex == IDX_GNR_JOY_Y)
                VSliderInst.Value = (-Input.GetJoyAxis(0, 1) + 1.0f) * 127.5f;
        }
    }

    private void _on_UseJoyPad_toggled(bool pressed)
    {
        if (pressed && CanEditInst.Pressed && RInst.joyPadNameInst.Text != "")
            joyEnabled = true;
        else
            joyEnabled = false;
    }

    private void _on_CanEdit_toggled(bool pressed)
    {
        if (pressed)
        {
            RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].OnEdit = true;
        }
        else
        {
            RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].OnEdit = false;
        }
    }

    private void _on_VSlider_changed(float value)
    {
        if (RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].OnEdit)
            send(Convert.ToByte(value));
    }

    private void send(Byte value)
    {
        string hexIdx = utilFuncs.ByteToHex(value);

        RInst.ComPortInst.SendCommand(RInst.ConfigData.SetValue, (Byte)(BaseIndex+ArrIdx), 
            (VarType_e)RInst.ConfigData.Vars[BaseIndex].Type[0], hexIdx); 
    }
}
