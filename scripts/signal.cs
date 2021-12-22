using Godot;
using System;

public class signal : VBoxContainer
{
    private MainTop RInst;
    private Chart chartInst;
    VBoxContainer sigPan1Inst;
    VBoxContainer sigPan2Inst;
    private PopupPanel popupPanelInst;
    public Button signalNameInst;
    public Button TrigInst;

    public Button colorButtonInst;
    private ColorRect colorRectInst;
    private ColorPicker colorPickerInst;
    public Int32 idxCurve;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RInst = GetNode<MainTop>("/root/Main");

        // Find the chart
		chartInst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("Chart") as Chart;

        popupPanelInst = FindNode("PopupPanel") as PopupPanel;
        popupPanelInst.Connect("modal_closed", this, nameof(onModalClosed));

        colorPickerInst = FindNode("ColorPicker") as ColorPicker;

        signalNameInst = FindNode("signalName") as Button;
        signalNameInst.Connect("pressed", this, nameof(onSignalNamePressed));

        TrigInst = FindNode("Trig") as CheckBox;
        TrigInst.Connect("toggled", this, nameof(onTrigToggled));

        colorRectInst = FindNode("ColorRect") as ColorRect;
        colorRectInst.Connect("gui_input", this, nameof(onColorRectGuiInput));

        sigPan1Inst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel") as VBoxContainer;
        sigPan2Inst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel2") as VBoxContainer;
    }

    public void onSignalNamePressed()
    {
        chartInst.Curves[idxCurve].onPlot = signalNameInst.Pressed;
    }

    public void onTrigToggled(bool Pressed)
    {
        if (Pressed)
        {
            // Uncheck all boxes
            var children = sigPan1Inst.GetChildren();
            foreach (signal c in children)
                if (c != this)
                    (c.FindNode("Trig") as CheckBox).Pressed = false;    
 
            children = sigPan2Inst.GetChildren();
            foreach (signal c in children)
                if (c != this)
                    (c.FindNode("Trig") as CheckBox).Pressed = false;

            // And set this one
            chartInst.Curves[idxCurve].IsTrigger = true;
            if(chartInst.ScopeTriggerMode)
                 chartInst.SeekTriggerName();
        }
        else
        {
            // This is called for each signal
            chartInst.Curves[idxCurve].IsTrigger = false;
            if(chartInst.ScopeTriggerMode)
                 chartInst.SeekTriggerName();
        }
    }

    public void onColorRectGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseEvent)
        {
            // Show popup with Color picker
            colorPickerInst.Color = chartInst.Curves[idxCurve].dataColor;
            popupPanelInst.ShowModal();
        }
    }
    public void onModalClosed()
    {
        // Update Color
        colorRectInst.Color = colorPickerInst.Color;
        chartInst.Curves[idxCurve].dataColor = colorPickerInst.Color;
    }
}
