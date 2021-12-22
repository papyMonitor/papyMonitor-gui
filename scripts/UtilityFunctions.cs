using System;
using System.Globalization;

public class UtilityFunctions
{
    /// LOOKUP TABLE FOR BYTE TO HEX STRING CONVERSION (A FAST WAY)
    private readonly uint[] lookup32;

    public UtilityFunctions()
    {
        lookup32 = CreateLookup32();
    }

    private uint[] CreateLookup32()
    {
        var result = new uint[256];
        for (int i = 0; i < 256; i++)
        {
            string s = i.ToString("X2");
            result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
        }
        return result;
    }

    public string ByteToHex(Byte U8)
    {
        return U8.ToString("X2");
    }

    public string ByteArrayToHexViaLookup32(byte[] bytes)
    {
        // var lookup32 = _lookup32;
        var result = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var val = lookup32[bytes[i]];
            result[2*i] = (char)val;
            result[2*i + 1] = (char) (val >> 16);
        }
        return new string(result);
    }

    public string ToHexString(float f)
    {
        var bytes = BitConverter.GetBytes(f);
        var i = BitConverter.ToInt32(bytes, 0);
        return i.ToString("X8");
    }

    public string ToMsgHexValue(VarType_e valueType, string displayValue, string endianness)
    {
        string hexValue, msg;

        switch (valueType)
        { 
            case VarType_e.Single:
                float f;
                float.TryParse(displayValue.Replace('.', ','), out f);
                hexValue = ToHexString(f);
                if (endianness=="little")
                {
                    hexValue = hexValue.Substring(6, 2) + hexValue.Substring(4, 2) + hexValue.Substring(2, 2) + hexValue.Substring(0, 2);
                }                
                msg = "f" + hexValue;
                break;

            case VarType_e.UInt32:
                UInt32 U32 = 0;
                UInt32.TryParse(displayValue, out U32);
                hexValue = U32.ToString("X8");
                if (endianness=="little")
                {
                    hexValue = hexValue.Substring(6, 2) + hexValue.Substring(4, 2) + hexValue.Substring(2, 2) + hexValue.Substring(0, 2);
                }
                msg = "W" + hexValue;
                break;

            case VarType_e.Int32:
                Int32 I32 = 0;
                Int32.TryParse(displayValue, out I32);
                hexValue = I32.ToString("X8");
                if (endianness=="little")
                {
                    hexValue = hexValue.Substring(6, 2) + hexValue.Substring(4, 2) + hexValue.Substring(2, 2) + hexValue.Substring(0, 2);
                }
                msg = "w" + hexValue;
                break;

            case VarType_e.UInt16:
                UInt16 U16 = 0;
                UInt16.TryParse(displayValue, out U16);
                hexValue = U16.ToString("X4");
                if (endianness=="little")
                {
                    hexValue = hexValue.Substring(2, 2) + hexValue.Substring(0, 2);
                }
                msg = "I" + hexValue;
                break;

            case VarType_e.Int16:
                Int16 I16 = 0;
                Int16.TryParse(displayValue, out I16); 
                hexValue = I16.ToString("X4");
                if (endianness=="little")
                {
                    hexValue = hexValue.Substring(2, 2) + hexValue.Substring(0, 2);
                }
                msg = "i" + hexValue;
                break;

            case VarType_e.Byte:
                Byte U8 = 0;
                Byte.TryParse(displayValue, out U8);
                hexValue = U8.ToString("X2");
                msg = "B" + hexValue; 
                break;

            case VarType_e.SByte:
                SByte I8 = 0;
                SByte.TryParse(displayValue, out I8);
                hexValue = I8.ToString("X2");
                msg = "b" + hexValue;
                break;

            case 0:
                msg ="";
                break;

            default:
                msg ="";
                break;                   
        }
        return msg;
    }

    public object GetCOMValue(string line, VarType_e varType)
    {
        byte[] bytes;
        string bigEndian;
        object returnValue = null;

        switch (varType)
        { 
            case VarType_e.Single:
                bytes = new byte[4];
                bytes[0] = Convert.ToByte(line.Substring(4, 2), 16);
                bytes[1] = Convert.ToByte(line.Substring(6, 2), 16);
                bytes[2] = Convert.ToByte(line.Substring(8, 2), 16);
                bytes[3] = Convert.ToByte(line.Substring(10, 2), 16);
                returnValue = BitConverter.ToSingle(bytes, 0);
                break;

            case VarType_e.UInt32:
                bigEndian = line.Substring(10, 2) + line.Substring(8, 2) + line.Substring(6, 2) + line.Substring(4, 2);
                returnValue = Convert.ToUInt32(bigEndian , 16);
                break;

            case VarType_e.Int32:
                bigEndian = line.Substring(10, 2) + line.Substring(8, 2) + line.Substring(6, 2) + line.Substring(4, 2);
                returnValue = Convert.ToInt32(bigEndian , 16);
                break;

            case VarType_e.UInt16:
                bigEndian = line.Substring(6, 2) + line.Substring(4, 2);
                returnValue = Convert.ToUInt16(bigEndian , 16);
                break;

            case VarType_e.Int16:
                bigEndian = line.Substring(6, 2) + line.Substring(4, 2);
                returnValue = Convert.ToInt16(bigEndian , 16);                       
                break;

            case VarType_e.Byte:
                bigEndian = line.Substring(4, 2);
                returnValue = Convert.ToByte(bigEndian , 16);
                break;

            case VarType_e.SByte:
                bigEndian = line.Substring(4, 2);
                returnValue = Convert.ToSByte(bigEndian , 16);
                break;                                

            case 0:
                returnValue = 0;
                break;

            default:
                returnValue = 0;
                break;
        }

        return returnValue;
    }

    public object GetValue(string line, VarType_e varType)
{
        NumberFormatInfo nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";
        
        object returnValue = null;

        switch (varType)
        { 
            case VarType_e.Single:
                Single valueSingle;
                Single.TryParse(line, NumberStyles.AllowDecimalPoint | 
                        NumberStyles.AllowLeadingSign, nfi, out valueSingle);
                returnValue = valueSingle;
                break;

            case VarType_e.UInt32:
                UInt32 valueUInt32;
                UInt32.TryParse(line, NumberStyles.None, nfi, out valueUInt32);
                returnValue = valueUInt32;
                break;

            case VarType_e.Int32:
                Int32 valueInt32;
                Int32.TryParse(line, NumberStyles.AllowLeadingSign, nfi, out valueInt32);
                returnValue = valueInt32;
                break;

            case VarType_e.UInt16:
                UInt16 valueUInt16;
                UInt16.TryParse(line, NumberStyles.None, nfi, out valueUInt16);
                returnValue = valueUInt16;
                break;

            case VarType_e.Int16:
                Int16 valueInt16;
                Int16.TryParse(line, NumberStyles.AllowLeadingSign, nfi, out valueInt16); 
                returnValue = valueInt16;                    
                break;

            case VarType_e.Byte:
                Byte valueByte;
                Byte.TryParse(line, NumberStyles.None, nfi, out valueByte);
                returnValue = valueByte;  
                break;

            case VarType_e.SByte:
                SByte valueSByte;
                SByte.TryParse(line, NumberStyles.AllowLeadingSign, nfi, out valueSByte);
                returnValue = valueSByte;
                break;                                

            case 0:
                returnValue = 0;
                break;

            default:
                returnValue = 0;
                break;
        }

        return returnValue;
    }
	
    public UInt32 GetBit(UInt32 val, Byte bitIdx)
	{
		return Convert.ToUInt32((val & (1<<bitIdx)) >> bitIdx);
	}
}
