using Godot;
using System;

public class widgetStates : VBoxContainer
{
    private MainTop RInst;

    private PopupPanel popupPanelInst;

    private ColorRect colorRectInst;
    private ColorPicker colorPickerInst;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RInst = GetNode<MainTop>("/root/Main");

        popupPanelInst = FindNode("PopupPanel") as PopupPanel;
        popupPanelInst.Connect("modal_closed", this, nameof(onModalClosed));

        colorPickerInst = FindNode("ColorPicker") as ColorPicker;

        colorRectInst = FindNode("ColorRect") as ColorRect;
        colorRectInst.Connect("gui_input", this, nameof(onColorRectGuiInput));
    }

    public void onColorRectGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseEvent)
        {
            // Show popup with Color picker
            colorPickerInst.Color = colorRectInst.Color;
            popupPanelInst.ShowModal();
        }
    }
    public void onModalClosed()
    {
        // Update Color
        colorRectInst.Color = colorPickerInst.Color;
    }
}
