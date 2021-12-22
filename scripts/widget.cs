using Godot;
using System;

public class widget : HBoxContainer
{
	// Classes instances
	private MainTop RInst;
	private Button MonitOnOffInst;
	private Timer TimerInst;
	private static Texture textureMonitOff = ResourceLoader.Load("res://icons/monitOff.png") as Texture;
	private static Texture textureMonitOn = ResourceLoader.Load("res://icons/monitOn.png") as Texture;

 	public Int32 varIdx { get; set; } = -1;
	public Int32 sizeArray { get; set; } = 1;

	private bool onMonitoring = false;

	private UtilityFunctions utilFuncs;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RInst = GetNode<MainTop>("/root/Main");

		MonitOnOffInst = FindNode("MonitOnOff") as Button;
		TimerInst = FindNode("Timer") as Timer;
		utilFuncs = new UtilityFunctions();
	}

	private void _on_MonitOnOff_pressed()
	{
		if (RInst.ComPortInst.IsOpen() && varIdx >= 0) 
		{
			char Cmd;

			if (!onMonitoring)
				Cmd = RInst.ConfigData.ReportValueOn;
			else
				Cmd = RInst.ConfigData.ReportValueOff;

			send(Cmd);
		}
	}

	public void setOnMonitoring()
	{
		onMonitoring = true;
		MonitOnOffInst.AddColorOverride("font_color", new Color(1,0,0,1));
		MonitOnOffInst.AddColorOverride("font_color_hover", new Color(1,0,0,1));
		MonitOnOffInst.AddColorOverride("font_color_focus", new Color(1,0,0,1));
		TimerInst.Start();
	}

	public void _on_Timer_timeout()
	{
		onMonitoring = false;
		MonitOnOffInst.AddColorOverride("font_color", new Color(1,1,1,1));
		MonitOnOffInst.AddColorOverride("font_color_hover", new Color(1,1,1,1));
		MonitOnOffInst.AddColorOverride("font_color_focus", new Color(1,1,1,1));
	}

	private void send(char Cmd)
    {
		RInst.ComPortInst.SendCommand(Cmd, (Byte)varIdx, 
				(VarType_e)"B"[0], sizeArray.ToString());
    }
}

