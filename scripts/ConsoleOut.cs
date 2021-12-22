using Godot;
using System;

public enum consoleColors
{
    RED = 0,
    GREEN = 1,
    BLUE = 2
}

public enum LogLevel_e : byte
{
	eInfo,
	eWarning,
	eError
}

public class ConsoleOut : RichTextLabel
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void Print(LogLevel_e level, string text)
    {
        string logLev = "";
        switch (level)
        {
            case LogLevel_e.eInfo: PushColor(new Color(1, 1, 1)); logLev = "Info: "; break;
            case LogLevel_e.eWarning: PushColor(new Color(1.0f, 0.608f, 0)); logLev = "Warning: "; break;
            case LogLevel_e.eError: PushColor(new Color(1, 0, 0)); logLev = "Error: "; break;
        }
        AppendBbcode(logLev + text);
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
