using Godot;
using System;
using System.Collections.Generic;
using System.IO.Ports;

public class ComPort : Timer
{
    private class CmdPool_t
    {
        public char Cmd;
        public Byte Index;
        public VarType_e Type;
        public string Value;

        public CmdPool_t(char cmd, Byte index, VarType_e type, string value)
        {
            Cmd = cmd;
            Index = index;
            Type = type;
            Value = value;
        }
    }

    private Queue<CmdPool_t> CmdPool;

    private MainTop RInst;
    public static SerialPort port;

    private UtilityFunctions helper;

    public override void _Ready()
    {
        // Seek class instances
        RInst = GetNode<MainTop>("/root/Main");
        CmdPool = new Queue<CmdPool_t>();
        helper = new UtilityFunctions();

        port = new SerialPort();
        port.Parity = Parity.None;
        port.StopBits = StopBits.One;
    }

    public override void _Process(float delta)
    {
        if (CmdPool.Count>0)
        {
            CmdPool_t cmd = CmdPool.Dequeue();
            SendCommand(cmd.Cmd, cmd.Index, cmd.Type, cmd.Value);
        }
        
    }

    public string[] GetPorts()
    {
        return SerialPort.GetPortNames();
    }

    public void Open(string portname, Int32 baudrate)
    {
        port.PortName = portname;
        port.BaudRate  = baudrate;
        try
        {
            port.Open();
            port.DiscardInBuffer();
        }
        catch(Exception ex)
        {
            GetNode<MainTop>("/root/Main").ConsoleInst.Print(LogLevel_e.eError, ex.Message + "\n");
        }
    }

    public bool IsOpen()
    {
        return port.IsOpen;
    }

    public void Close()
    {
        port.Close();
    }

    public Int32 GetPendingBytes()
    {
        return port.BytesToRead;
    }

    public string ReadPendingBytes()
    {
        return port.ReadExisting();
    }

    private void WriteString(string msg)
    {
        port.Write(msg);
    }  

    public void SendCommand(char Cmd, Byte Index, VarType_e Type, string Value)
    {
        string msg;

        string hexIdx = helper.ByteToHex((Byte)(Index));

        msg = Cmd.ToString() + hexIdx;
        msg += helper.ToMsgHexValue(Type, Value, RInst.ConfigData.Endian);
        msg += "\n";

		if (IsOpen())
			WriteString(msg);  
    } 

    public void SendParameters(string ParameterName)
    {
        // Create a pool of command that will be send
        // every tick time to the target
        CmdPool.Clear();

        foreach(Byte keyVar in RInst.ConfigData.Vars.Keys)
        {
            foreach(Byte keyData in RInst.ConfigData.Vars[keyVar].Data.Keys)
            {           
                if (RInst.ConfigData.Vars[keyVar].Parameter == ParameterName)
                {
                    CmdPool.Enqueue( new CmdPool_t(
                        RInst.ConfigData.SetValue, 
                        (Byte)(keyVar+keyData), 
                        (VarType_e)RInst.ConfigData.Vars[keyVar].Type[0], 
                        RInst.ConfigData.Vars[keyVar].Data[keyData].Value.ToString())
                    );
                }
            }
        }

        Start();
    } 
}
