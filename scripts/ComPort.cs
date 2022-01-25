using Godot;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;

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

    private bool portReady;
    private MainTop RInst;
    public static SerialPort port;
    private bool isSocket;
    private TcpClient clientSocket;
    private NetworkStream stream;
    private Byte[] dataRX;

    private UtilityFunctions helper;

    public override void _Ready()
    {
        // Seek class instances
        RInst = GetNode<MainTop>("/root/Main");
        CmdPool = new Queue<CmdPool_t>();
        helper = new UtilityFunctions();
        isSocket = false;
        portReady = false;
    }

    public override void _Process(float delta)
    {
        if (CmdPool.Count>0)
        {
            CmdPool_t cmd = CmdPool.Dequeue();
            SendCommand(cmd.Cmd, cmd.Index, cmd.Type, cmd.Value);
        }
        
    }

    private string[] GetPorts()
    {
        return SerialPort.GetPortNames();
    }

    public void updatePortListToMenu()
    {
        List<string> ports;

        if (!isSocket)
        {
            // Scan all available serial ports and put on the first menu
            ports = new List<string>(RInst.ComPortInst.GetPorts());
        }
        else
        {
            ports = new List<string>();
            ports.Add("Socket");
        }

        // Fill the port menu with all the ports found
        var idx = 0;
        foreach (string port in ports)
            RInst.MenuInst.openPortMenuElementsInst.AddItem(port, idx++);
    }

    public void SetConnectionType(bool isSocketConnection)
    {
        isSocket = isSocketConnection;
        portReady = false;
        // Update the ports accessibles
        updatePortListToMenu();
    }

    public void Open(string portname)
    {
        if (!isSocket)
        {
            try
            {
                port = new SerialPort();
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.BaudRate = RInst.ConfigData.Baudrate;
                port.PortName = portname;
                port.Open();
                port.DiscardInBuffer();
                portReady = true;
            }
            catch(Exception ex)
            {
                GetNode<MainTop>("/root/Main").ConsoleInst.Print(LogLevel_e.eError, ex.Message + "\n");
                portReady = false;
            }
        }
        else
        {   
            try 
            {
                clientSocket = new TcpClient(RInst.ConfigData.SocketIP, Convert.ToInt32(RInst.ConfigData.SocketPort));
                stream = clientSocket.GetStream();
                dataRX = new Byte[256];
                portReady = true;
            }
            catch(Exception ex)
            {
                GetNode<MainTop>("/root/Main").ConsoleInst.Print(LogLevel_e.eError, ex.Message + "\n");
                portReady = false;
            }
        }
    }

    public bool IsOpen()
    {
        if (portReady)
        {
            if (!isSocket)
            {
                return port.IsOpen;
            }
            else
            {
                return clientSocket.Connected;
            }
        }
        else
            return false;
    }

    public void Close()
    {
        if (portReady)
        {
            if (!isSocket)
            {
                port.Close();
            }
            else
            {
                clientSocket.Close();
            }    
        }    
    }

    public bool DataAvailable()
    {      
        if (!isSocket)
        {
            return port.BytesToRead > 0;
        }
        else
        {
            return stream.DataAvailable;
        }
    }

    public string ReadPendingBytes()
    {
        if (!isSocket)
        {
            return port.ReadExisting();
        }
        else
        {
            Int32 bytes = stream.Read(dataRX, 0, dataRX.Length);
            return System.Text.Encoding.ASCII.GetString(dataRX, 0, bytes);
        }
    }

    private void WriteString(string msg)
    {
        if (!isSocket)
        {
            port.Write(msg);
        }
        else
        {
            Byte[] dataTX = System.Text.Encoding.ASCII.GetBytes(msg);
            // Test if server not disconnected
            if (clientSocket.Connected)
                stream.Write(dataTX, 0, dataTX.Length);
        }
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
