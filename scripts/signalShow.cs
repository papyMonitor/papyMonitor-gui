using Godot;
using System;

public class signalShow : Button
{
    private MainTop RInst;
    private VBoxContainer sigPan1Inst;
    private VBoxContainer sigPan1Inst2;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RInst = GetNode<MainTop>("/root/Main");
        sigPan1Inst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel") as VBoxContainer;
		sigPan1Inst2 = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel2") as VBoxContainer;
        Connect("toggled", this, nameof(onSignalShowToggled));
    }

    private void onSignalShowToggled(bool Pressed)
    {
        if (Pressed)
        {
            sigPan1Inst.Visible = true;
            sigPan1Inst2.Visible = true;
            Text = "Hide signals";
        }
        else
        {
            sigPan1Inst.Visible = false;
            sigPan1Inst2.Visible = false;
            Text = "Show signals to plot";
        }
    }
}
