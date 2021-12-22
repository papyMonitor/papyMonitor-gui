using Godot;
using System;
using System.Collections.Generic;
using NLua;

public enum VarType_e : byte
{
	Single=(byte)'f', // 8 chars
	UInt32=(byte)'W', // 8 chars
	Int32=(byte)'w', // 8 chars
	UInt16=(byte)'I', // 4 chars
	Int16=(byte)'i', // 4 chars
	Byte=(byte)'B', // 2 chars
	SByte=(byte)'b', // 2 chars
}

public class Helper_t
{
	ConsoleOut c;

	public Helper_t(ConsoleOut _c)
	{
		c = _c;
	}
	public void errortype(string parameter)
	{
		c.Print(LogLevel_e.eError, "Bad type for field:" + parameter + "\n");
	}

	private void errorfound(string parameter)
	{
		c.Print(LogLevel_e.eError, "Field: " + parameter + " not found\n");
	}

	// Recursive function
	public Dictionary<string, object> LuaTable2Dic(LuaTable table)
	{
		Dictionary<string, object> Dic = new Dictionary<string, object>();
		var e = table.GetEnumerator();
		while(e.MoveNext())
		{
			// If key is numeral then substract 1 (Lua array begins at 1)
			int key;
			bool isInt = int.TryParse(Convert.ToString(e.Key), out key);

			if (isInt)
			{
				if (e.Value is LuaTable)
				{
					Helper_t helper = new Helper_t(c);
					Dic.Add(Convert.ToString(key-1), helper.LuaTable2Dic((LuaTable)e.Value));
				}
				else
					Dic.Add(Convert.ToString(key-1), e.Value);
			}
			else
			{
				if (e.Value is LuaTable)
				{
					Helper_t helper = new Helper_t(c);
					Dic.Add(Convert.ToString(e.Key), helper.LuaTable2Dic((LuaTable)e.Value));
				}
				else
					Dic.Add(Convert.ToString(e.Key), e.Value);
			}
		}
		return Dic;
	}

	public bool CheckDicKeys(Dictionary<string, object> dic, Dictionary<string, Type> vars)
	{
		bool Ok = true;

		foreach(string key in vars.Keys)
		{
			if (!dic.ContainsKey(key))
			{
				errorfound(key);
				Ok = false;
				break;
			}
		}

		return Ok;
	}
}

public class Bit_t
{
	public string BitText;
	public bool CanEdit;
	public string Color;

	// Field widget reference
	public object Wigdet;

	public Bit_t(Byte bit)
	{
		Color = "green";
		BitText = "Bit" + bit.ToString();
		CanEdit = false;
	}
}

public class Data_t
{
	public bool DataOk;
	private ConsoleOut c;
	private Helper_t helper;

	public string SingleText;
	public bool OnEdit;
	public bool CanEdit;
	public bool CanPlot;
	public bool SpinBox;

	private bool _boolsOnU8;
	public bool BoolsOnU8
	{
		get { return _boolsOnU8; }
		set {
				_boolsOnU8 = value;

				if (_boolsOnU8)
				{
					Bits = new Bit_t[8];
					for (Byte bit=0; bit<8; bit++)
						Bits[bit] = new Bit_t(bit);
				}
			}
	}
	public Bit_t[] Bits;
	public object Value;
	public UInt32 Precision;

	// Field widget reference
	public object Wigdet;

	public Data_t(ConsoleOut _c, VarType_e _type, LuaTable data)
	{
		c = _c;
		DataOk = true;

		OnEdit = false;
		CanEdit = false;
		CanPlot = false;
		SpinBox = false;
		Precision = 3;	
		SingleText = "";

		helper = new Helper_t(c);

		// Value cannot be null, hence an init regarding the type is necessary
		switch (_type)
		{
			case VarType_e.Single:  Value = (float)0.0f; break;
			case VarType_e.UInt32:  Value = (UInt32)0; break;
			case VarType_e.Int32:  Value = (Int32)0; break;
			case VarType_e.UInt16:  Value = (UInt16)0; break;
			case VarType_e.Int16:  Value = (Int16)0; break;
			case VarType_e.Byte:  Value = (Byte)0; break;
			case VarType_e.SByte:  Value = (SByte)0; break;
		}

		if (data != null)
		{
			if (data["Value"] != null)
			{
				try { Value = data["Value"]; } 
				catch { helper.errortype("Value"); DataOk = false;}
			}

			if (data["BoolsOnU8"] != null)
			{
				try { BoolsOnU8 = Convert.ToBoolean(data["BoolsOnU8"]); } 
				catch { helper.errortype("BoolsOnU8"); DataOk = false;}
			}

			if (data["SingleText"] != null)
			{
				try { SingleText = (string)data["SingleText"]; } 
				catch { helper.errortype("SingleText"); DataOk = false;}
			}

			if (data["Precision"] != null)
			{
				try { Precision = Convert.ToUInt32(data["Precision"]); } 
				catch { helper.errortype("Precision"); DataOk = false;}
			}

			if (data["CanEdit"] != null)
			{
				try { CanEdit = Convert.ToBoolean(data["CanEdit"]); } 
				catch { helper.errortype("CanEdit"); DataOk = false;}
			}

			if(data["CanPlot"]!=null)
			{
				try { CanPlot = Convert.ToBoolean(data["CanPlot"]); } 
				catch { helper.errortype("CanPlot"); DataOk = false; }
			}

			if (BoolsOnU8)
			{
				if (data["BitsTexts"] != null)
				{
					try
					{ 
						var BitsTexts = (LuaTable)data["BitsTexts"]; 
						var e = BitsTexts.GetEnumerator();
						while(e.MoveNext())
						{
							Int32 keyBitsTexts = Convert.ToInt32(e.Key) - 1;
							Bits[keyBitsTexts].BitText = (string)e.Value;
						}
					} 
					catch { helper.errortype("BitsTexts"); DataOk = false;}
				}

				if (data["BitsColors"] != null)
				{
					try
					{ 
						var BitsColors = (LuaTable)data["BitsColors"]; 
						var e = BitsColors.GetEnumerator();
						while(e.MoveNext())
						{
							Int32 keyBitsColors = Convert.ToInt32(e.Key) - 1;
							Bits[keyBitsColors].Color = (string)e.Value;
						}
					} 
					catch { helper.errortype("BitsColors"); DataOk = false;}
				}

				if (data["CanBitsEdits"] != null)
				{
					try
					{ 
						var BitsEdits = (LuaTable)data["CanBitsEdits"]; 
						var e = BitsEdits.GetEnumerator();
						while(e.MoveNext())
						{
							Int32 keyBitsEdits = Convert.ToInt32(e.Key) - 1;
							Bits[keyBitsEdits].CanEdit = Convert.ToBoolean(e.Value);
						}
					} 
					catch { helper.errortype("BitsColors"); DataOk = false;}
				}
			}	
		}
	}
}

public class Var_t
{
	public bool VarOk;
	private ConsoleOut c;
	private Helper_t helper;

	public string Text;
	private VarType_e _type;
	public string Type
	{
		get { return ((char)_type).ToString(); }
		set {
				_type = (VarType_e)value[0];
			}
	}
	public string Parameter;
	public Int32 Index;

	public Dictionary<Int32, Data_t> Data;
	public bool HideData;

	public string WidgetType;

	public bool Monitor, Scroll;
	// root Widget reference
	public object RootWigdet;

	public Single SliderMin, SliderMax;


	public Var_t(ConsoleOut _c, LuaTable vars)
	{
		c = _c;
		VarOk = true;

		helper = new Helper_t(c);
		Data = new Dictionary<Int32, Data_t>();

		SliderMin = 0.0f;
		SliderMax = 1.0f;
		Scroll = false;
		HideData = false;
		Parameter = "";

		// Check for mandatory fields
		if (vars["Name"] != null && vars["Type"] != null && vars["Index"] != null)
		{
			try { Text = (string)vars["Name"]; } 
			catch { helper.errortype("Name"); VarOk = false; }
			try { Type = (string)vars["Type"]; } 
			catch { helper.errortype("Type"); VarOk = false; }
			try { Index = Convert.ToInt32(vars["Index"]); } 
			catch { helper.errortype("Index"); VarOk = false; }

			if(vars["Scroll"]!=null)
			{
				try { Scroll = Convert.ToBoolean(vars["Scroll"]); } 
				catch { helper.errortype("Scroll"); VarOk = false; }
			}

			if(vars["HideData"]!=null)
			{
				try { HideData = Convert.ToBoolean(vars["HideData"]); } 
				catch { helper.errortype("HideData"); VarOk = false; }
			}

			if(vars["WidgetType"]!=null)
			{
				try { WidgetType = (string)vars["WidgetType"]; } 
				catch { helper.errortype("WidgetType"); VarOk = false; }

				if (WidgetType=="SliderH" || WidgetType=="SliderV")
				{
					if(vars["SliderMin"]!=null)
					{
						try { SliderMin = Convert.ToSingle(vars["SliderMin"]); } 
						catch { helper.errortype("SliderMin"); VarOk = false; }
					}

					if(vars["SliderMax"]!=null)
					{
						try { SliderMax = Convert.ToSingle(vars["SliderMax"]); } 
						catch { helper.errortype("SliderMax"); VarOk = false; }
					}
				}
			}
			else
				WidgetType = "normal";

			if(vars["Parameter"]!=null)
			{
				try { Parameter = (string)vars["Parameter"]; } 
				catch { helper.errortype("Parameter"); VarOk = false; }
			}

			// Retrieve Data related
			try { LuaTable test = (LuaTable)vars["Data"]; } 
			catch { helper.errortype("Data"); VarOk = false; }	

			if (VarOk)
			{
				var DataTable = (LuaTable) vars["Data"];

				if (DataTable == null)
				{
					Data.Add(0, new Data_t(c, _type, null));
				}
				else
				{
					var e = DataTable.GetEnumerator();
					while(e.MoveNext())
					{
						Int32 keyDataTable = Convert.ToInt32(e.Key) - 1;
						Data.Add(keyDataTable, new Data_t(c, _type, (LuaTable)e.Value));
						VarOk = VarOk && Data[keyDataTable].DataOk;
					}
				}
			}
		}
		else
		{
			try {
				string Name;
				Name = (string)vars["Name"];
				c.Print(LogLevel_e.eError, "Error in Var " + Name + 
					": Mandatory fields not present\n");
			} 
			catch { 
				c.Print(LogLevel_e.eError, "Error in Var NO NAME" + 
					": Mandatory fields not present\n");
			}
		}
	}
}

public class SmoothFloat
{
	public float vel;
	public bool smooth;

	private float _f, newvalue;
	public float f
	{
		get 
		{
			if (smooth)
				_f = vel * newvalue + (1-vel) * _f;
			else
				_f = newvalue;
			return _f;
		}
		set { newvalue = value; }
	}

	public SmoothFloat(bool _smooth = false, float _vel = 0.4f)
	{
		smooth = _smooth;
		vel = _vel;
	}
	public static SmoothFloat operator+ (SmoothFloat a, SmoothFloat b)
	{
		SmoothFloat s = new SmoothFloat();
		s.smooth = a.smooth && b.smooth;
		s.f = a.f + b.f;
		return s;
	}
	public static SmoothFloat operator- (SmoothFloat a, SmoothFloat b)
	{
		SmoothFloat s = new SmoothFloat();
		s.smooth = a.smooth && b.smooth;
		s.f = a.f + b.f;
		return s;
	}
	public static SmoothFloat operator* (SmoothFloat a, float b)
	{
		SmoothFloat s = new SmoothFloat();
		s.smooth = a.smooth;
		s.f = a.f * b;
		return s;
	}
	public static implicit operator float(SmoothFloat sf)
    {
		float f = new float();
		f = sf.f;
		return f;
    }
}

public class ClassVector3
{
	private SmoothFloat _x, _y, _z;
	public float x
	{
		get { return _x.f; }
		set { _x.f = value; }
	}
	public float y
	{
		get { return _y.f; }
		set { _y.f = value; }
	}
	public float z
	{
		get { return _z.f; }
		set { _z.f = value; }
	}

	public ClassVector3(bool _smooth = false, float _vel = 0.4f)
	{
		_x = new SmoothFloat(_smooth, _vel);
		_y = new SmoothFloat(_smooth, _vel);
		_z = new SmoothFloat(_smooth, _vel);
	}

	public static ClassVector3 operator+ (ClassVector3 a, ClassVector3 b)
	{
		ClassVector3 cv3 = new ClassVector3();
		cv3.x = a.x + b.x;
		cv3.y = a.y + b.y;
		cv3.z = a.z + b.z;
		return cv3;
	}
	public static ClassVector3 operator- (ClassVector3 a, ClassVector3 b)
	{
		ClassVector3 cv3 = new ClassVector3();
		cv3.x = a.x - b.x;
		cv3.y = a.y - b.y;
		cv3.z = a.z - b.z;
		return cv3;
	}
	// public static ClassVector3 operator* (ClassVector3 a, float b)
	// {
	// 	ClassVector3 cv3 = new ClassVector3();
	// 	cv3.x = a.x * b;
	// 	cv3.y = a.y * b;
	// 	cv3.z = a.z * b;
	// 	return cv3;
	// }
	public static implicit operator Vector3(ClassVector3 cv3)
    {
        Vector3 v3 = new Vector3();
		v3.x = cv3.x;
		v3.y = cv3.y;
		v3.z = cv3.z;
		return v3;
    }	
	
	public void SetSmooth(bool smt, float _vel = 0.4f)
	{
		_x.smooth = smt; _x.vel = _vel;
		_y.smooth = smt; _y.vel = _vel;
		_z.smooth = smt; _z.vel = _vel;
	}
}

public class ClassColor
{
	public float r, g, b, a;

	public ClassColor()
	{
		r = 1.0f;
		g = 1.0f;
		b = 1.0f;
		a = 1.0f;
	}
}

public class Solid_t : Spatial
{
	public bool SolidOk;
	private ConsoleOut c;
	private Helper_t helper;
	private UtilityFunctions utilFunc;
	private ConfigData_t configData;

	public ClassVector3 P, R, oldP, oldR, DP;

	SpatialMaterial sp;
	bool errorInFormula;

	// Mandatory Data from Lua file

	// 			Field 
	// public string Name
	// 			is defined in MeshInstance
	public string Parent;

	public string Body
	{
		get {
			return Name;
		}
		set {
			if (value == "Cube")
			{
				MeshInstance mesh = new MeshInstance();
				mesh.Mesh = new CubeMesh();

				((CubeMesh)mesh.Mesh).Size = new Vector3(
					CubeSize.x, CubeSize.y, CubeSize.z
				);

				sp.AlbedoColor = new Color(Color.r, Color.g, Color.b);
				mesh.MaterialOverride = sp;
				
				AddChild(mesh);
			}
			else
			{
				if (value == "Cylinder")
				{
					MeshInstance mesh = new MeshInstance();
					mesh.Mesh = new CylinderMesh();

					((CylinderMesh)mesh.Mesh).TopRadius = CylinderTopRadius;
					((CylinderMesh)mesh.Mesh).BottomRadius = CylinderBottomRadius;
					((CylinderMesh)mesh.Mesh).Height = CylinderHeight;

					sp.AlbedoColor = new Color(Color.r, Color.g, Color.b);
					mesh.MaterialOverride = sp;

					AddChild(mesh);
				}
				else
				{
					// Custom loaded mesh
					PackedSceneGLTF model = new PackedSceneGLTF();
					if (System.IO.File.Exists(value))
					{
						Node node = model.ImportGltfScene(value);
						sp.AlbedoColor = new Color(Color.r, Color.g, Color.b);

						var children = node.GetChildren();
						foreach (Node n in children)
						{
							if (n is MeshInstance s)
							{
								s.MaterialOverride = sp;
								// c.Print(LogLevel_e.eInfo, "Solid " + Name + " child count: " 
								// 			+ this.GetChildCount() + "\n");
							}
						}

						AddChild(node);
					}					
				}
			}	
			Transform t = Transform.Identity;
			t.basis = Basis.Identity;

			t.Translated(StartPosition + P);

			Transform = t;

			RotateObjectLocal(Vector3.Right, 	StartRotation.x + R.x/180.0f*(float)Math.PI);	
			RotateObjectLocal(Vector3.Up, 		StartRotation.y + R.y/180.0f*(float)Math.PI);
			RotateObjectLocal(Vector3.Forward, 	StartRotation.z + R.z/180.0f*(float)Math.PI);									
		}
	}
	// End of mandatory Data from Lua file


	// Optional Data here
	public ClassColor Color;
	public ClassVector3 CubeSize, StartPosition, StartRotation;
	public Single CylinderTopRadius, CylinderBottomRadius, CylinderHeight;
	private LuaFunction Formula;

	// User data here
	public Dictionary<string, object> opt;

	// Default values for check
	private static readonly Dictionary<string, Type> solidVars
    	=  new Dictionary<string, Type>
		{
			{ nameof(Name), typeof(string) },
			{ nameof(Parent), typeof(string) },
			{ nameof(Body), typeof(string) },
		};

	public Solid_t(ConfigData_t _configData, ConsoleOut _c, Dictionary<string, object> solidObject)
	{
		c = _c;
		configData = _configData;

		helper = new Helper_t(c);
		utilFunc = new UtilityFunctions();
		opt = new Dictionary<string, object>();
		P = new ClassVector3();
		R = new ClassVector3();
		sp = new SpatialMaterial();
		oldP = new ClassVector3();
		oldR = new ClassVector3();
		DP = new ClassVector3();
		errorInFormula = false;

		SolidOk = true;

		if (helper.CheckDicKeys(solidObject, solidVars))
		{
			// User fields in a Dictionary
			foreach (string key in solidObject.Keys)
			{
				if( key != nameof(Name) && key != nameof(Parent) && key != nameof(Body) && 
					key != nameof(Color) && key != nameof(CubeSize) && 
					key != nameof(StartPosition) && key != nameof(StartRotation) && 
					key != nameof(CylinderTopRadius) && key != nameof(CylinderBottomRadius) &&
					key != nameof(CylinderHeight) && key != nameof(Formula))			
						opt[key] = solidObject[key];
			}

			// Set optional fields to default if not present
			// or convert to good type if present
			Color = new ClassColor();
			if (solidObject.ContainsKey("Color"))
			{
				if (solidObject["Color"].GetType() == typeof(Dictionary<string, object>))
				{
					foreach(string key in ((Dictionary<string, object>)solidObject["Color"]).Keys)
					{
						if (key == "0" || key == "r" || key == "R" || key == "red" || key == "Red" )
							Color.r = Convert.ToSingle(((Dictionary<string, object>)solidObject["Color"])[key]);
						if (key == "1" || key == "g" || key == "G" || key == "green" || key == "Green" )
							Color.g = Convert.ToSingle(((Dictionary<string, object>)solidObject["Color"])[key]);
						if (key == "2" || key == "b" || key == "B" || key == "blue" || key == "Blue" )
							Color.b = Convert.ToSingle(((Dictionary<string, object>)solidObject["Color"])[key]);														
					}
				}
				else
				{
					helper.errortype("Color");
					SolidOk = false;
				}
			}

			StartPosition = new ClassVector3();
			if (solidObject.ContainsKey("StartPosition"))
			{
				if (solidObject["StartPosition"].GetType() == typeof(Dictionary<string, object>))
				{
					foreach(string key in ((Dictionary<string, object>)solidObject["StartPosition"]).Keys)
					{
						if (key == "0" || key == "x" )
							StartPosition.x = Convert.ToSingle(((Dictionary<string, object>)solidObject["StartPosition"])[key]);
						if (key == "1" || key == "y" )
							StartPosition.y = Convert.ToSingle(((Dictionary<string, object>)solidObject["StartPosition"])[key]);
						if (key == "2" || key == "z" )
							StartPosition.z = Convert.ToSingle(((Dictionary<string, object>)solidObject["StartPosition"])[key]);														
					}
				}
				else
				{
					helper.errortype("StartPosition");
					SolidOk = false;
				}
			}

			StartRotation = new ClassVector3();
			if (solidObject.ContainsKey("StartRotation"))
			{
				if (solidObject["StartRotation"].GetType() == typeof(Dictionary<string, object>))
				{
					foreach(string key in ((Dictionary<string, object>)solidObject["StartRotation"]).Keys)
					{
						if (key == "0" || key == "x" )
							StartRotation.x = Convert.ToSingle(((Dictionary<string, object>)solidObject["StartRotation"])[key]);
						if (key == "1" || key == "y" )
							StartRotation.y = Convert.ToSingle(((Dictionary<string, object>)solidObject["StartRotation"])[key]);
						if (key == "2" || key == "z" )
							StartRotation.z = Convert.ToSingle(((Dictionary<string, object>)solidObject["StartRotation"])[key]);														
					}
				}
				else
				{
					helper.errortype("StartRotation");
					SolidOk = false;
				}
			}	

			CubeSize = new ClassVector3();
			if (solidObject.ContainsKey("CubeSize"))
			{
				if (solidObject["CubeSize"].GetType() == typeof(Dictionary<string, object>))
				{
					foreach(string key in ((Dictionary<string, object>)solidObject["CubeSize"]).Keys)
					{
						if (key == "0" || key == "x" )
							CubeSize.x = Convert.ToSingle(((Dictionary<string, object>)solidObject["CubeSize"])[key]);
						if (key == "1" || key == "y" )
							CubeSize.y = Convert.ToSingle(((Dictionary<string, object>)solidObject["CubeSize"])[key]);
						if (key == "2" || key == "z" )
							CubeSize.z = Convert.ToSingle(((Dictionary<string, object>)solidObject["CubeSize"])[key]);														
					}
				}
				else
				{
					helper.errortype("CubeSize");
					SolidOk = false;
				}
			}

			CylinderTopRadius = 1.0f;
			if (solidObject.ContainsKey("CylinderTopRadius"))
			{
				if (solidObject["CylinderTopRadius"].GetType() != typeof(Dictionary<string, object>))
					CylinderTopRadius = Convert.ToSingle(solidObject["CylinderTopRadius"]);
				else
				{
					helper.errortype("CylinderTopRadius");
					SolidOk = false;
				}
			}	

			CylinderBottomRadius = 1.0f;
			if (solidObject.ContainsKey("CylinderBottomRadius"))
			{
				if (solidObject["CylinderBottomRadius"].GetType() != typeof(Dictionary<string, object>))
					CylinderBottomRadius = Convert.ToSingle(solidObject["CylinderBottomRadius"]);
				else
				{
					helper.errortype("CylinderBottomRadius");
					SolidOk = false;
				}
			}	

			CylinderHeight = 1.0f;
			if (solidObject.ContainsKey("CylinderHeight"))
			{
				if (solidObject["CylinderHeight"].GetType() != typeof(Dictionary<string, object>))
					CylinderHeight = Convert.ToSingle(solidObject["CylinderHeight"]);
				else
				{
					helper.errortype("CylinderHeight");
					SolidOk = false;
				}
			}							
			
			if (solidObject.ContainsKey("MovePositionSmooth"))
			{
				if (solidObject["MovePositionSmooth"].GetType() != typeof(Dictionary<string, object>))
				{
					if(Convert.ToBoolean(solidObject["MovePositionSmooth"]))
						P.SetSmooth(true);
				}
				else
				{
					helper.errortype("MovePositionSmooth");
					SolidOk = false;
				}
			}

			if (solidObject.ContainsKey("MoveRotationSmooth"))
			{
				if (solidObject["MoveRotationSmooth"].GetType() != typeof(Dictionary<string, object>))
				{
					if(Convert.ToBoolean(solidObject["MoveRotationSmooth"]))
						R.SetSmooth(true);
				}
				else
				{
					helper.errortype("MoveRotationSmooth");
					SolidOk = false;
				}
			}	



			if (solidObject.ContainsKey("Formula"))
			{
				if (solidObject["Formula"].GetType() != typeof(LuaFunction))
				{
					helper.errortype("Formula");
					SolidOk = false;
				}
				else
					Formula = (LuaFunction)solidObject["Formula"];
			}

			// Mandarory fields
			Name = (string)solidObject[nameof(Name)];
			Parent = (string)solidObject[nameof(Parent)];
			// This affectation must Add a child to the class
			Body = (string)solidObject[nameof(Body)]; 
			if(GetChildCount() == 0)
			{
				c.Print(LogLevel_e.eError, "Solid " + Name + ": Bad Body " +(
							string)solidObject[nameof(Body)] + " or body not found\n");
				SolidOk = false;
			}
		}
		else
			SolidOk = false;
	}

	public object GetVariable(long index)
	{
		object objVal = configData.GetVarIdxValue(Convert.ToInt32(index));
		if (objVal != null)
			return objVal;
		else
			return 0;
	}

	public object GetVariable(long index, Byte bitIdx)
	{
		object objVal = configData.GetVarIdxValue(Convert.ToInt32(index));
		if (objVal != null)
		{
			return utilFunc.GetBit(Convert.ToUInt32(objVal), bitIdx);
		}
		else
			return 0;
	}

	public void Update()
	{
		if (Formula != null && !errorInFormula)
		{
			try
			{
				Formula.Call(this);
			}
			catch
			{
				c.Print(LogLevel_e.eError, "Bad Formula for Solid " + Name + "\n");
				errorInFormula = true;
			}

			Transform t = Transform.Identity;
			t.basis = Basis.Identity;

			t.Translated(StartPosition + P);

			Transform = t;

			RotateObjectLocal(Vector3.Right, 	StartRotation.x + R.x/180.0f*(float)Math.PI);	
			RotateObjectLocal(Vector3.Up, 		StartRotation.y + R.y/180.0f*(float)Math.PI);
			RotateObjectLocal(Vector3.Forward, 	StartRotation.z + R.z/180.0f*(float)Math.PI);

			// Update colors, and other optional values
			sp.AlbedoColor = new Color(Color.r, Color.g, Color.b, Color.a);
			var children = GetChildren();
			foreach (Node n in children)
			{
				if (n.GetType() == typeof(MeshInstance))
				{
					var s = (MeshInstance)n;
					// This is a simple mesh (cube, cylinder,...)
					s.MaterialOverride = sp;
					if (s.Mesh.GetType() == typeof(CubeMesh))
					{
						((CubeMesh)s.Mesh).Size = new Vector3(
							CubeSize.x, CubeSize.y, CubeSize.z
						);
					}
					if (s.Mesh.GetType() == typeof(CylinderMesh))
					{
						((CylinderMesh)s.Mesh).TopRadius = CylinderTopRadius;
						((CylinderMesh)s.Mesh).BottomRadius = CylinderBottomRadius;
						((CylinderMesh)s.Mesh).Height = CylinderHeight;
					}					
				}
				else
				{
					// This is a loaded node from file
					// Update only the color
					if (n is Node)
					{
						var children2 = n.GetChildren();
						foreach (Node n2 in children2)
						{
							if (n2 is MeshInstance s2)
								s2.MaterialOverride = sp;							
						}
					}
				}			
			}
		}
	}
}

public class TabColumn_t
{
	public bool ColumnOk;
	public Dictionary<Int32, Int32> Rows;
	ConsoleOut c;
	private void errortype(string parameter)
	{
		c.Print(LogLevel_e.eError, "Bad type for field :" + parameter + "\n");
	}

	public TabColumn_t(ConsoleOut _c, LuaTable column)
	{
		c = _c;
		Rows = new Dictionary<Int32, Int32>();

		ColumnOk = true;

		var e = column.GetEnumerator();
		while(e.MoveNext())
		{
			Int32 keycolumnTable = Convert.ToInt32(e.Key) - 1;
			Rows.Add(keycolumnTable, Convert.ToInt32(e.Value));
		}
	}
}

public class Tab_t
{
	public bool TabOk;
	public string TabName;
	public Dictionary<Int32, TabColumn_t> Columns;

	ConsoleOut c;

	private void errortype(string parameter)
	{
		c.Print(LogLevel_e.eError, "Bad type for field :" + parameter + "\n");
	}

	public Tab_t(ConsoleOut _c, LuaTable tab)
	{
		c = _c;
		Columns = new Dictionary<Int32, TabColumn_t>();

		TabOk = true;
		try { TabName = (string)tab["TabName"]; } 
		catch { errortype("TabName"); TabOk = false;}

		// Retrieve Data related
		try { LuaTable test = (LuaTable)tab["Columns"]; } 
		catch { errortype("Columns"); TabOk = false; }	

		if (TabOk)
		{
			var columnTable = (LuaTable) tab["Columns"];
			if (columnTable != null)
			{
				var e = columnTable.GetEnumerator();
				while(e.MoveNext())
				{
					Int32 keycolumnTable = Convert.ToInt32(e.Key) - 1;
					Columns.Add(keycolumnTable, new TabColumn_t(c, (LuaTable)e.Value));
					TabOk = TabOk && Columns[keycolumnTable].ColumnOk;
				}
			}
		}
	}
}

public class TabGroup_t
{
	public bool TabGroupOk;
	public bool NoExpandX, NoExpandY;

	public Dictionary<Int32, Tab_t> Tabs;

	ConsoleOut c;

	private void errortype(string parameter)
	{
		c.Print(LogLevel_e.eError, "Bad type for field :" + parameter + "\n");
	}

	public TabGroup_t(ConsoleOut _c, LuaTable group)
	{
		c = _c;
		Tabs = new Dictionary<Int32, Tab_t>();

		NoExpandX = false;
		NoExpandY = false;

		TabGroupOk = true;

		if (group["NoExpandX"] != null)
		{
			try { NoExpandX = Convert.ToBoolean(group["NoExpandX"]); } 
			catch { errortype("Vue3D"); TabGroupOk = false; }
		}

		if (group["NoExpandY"] != null)
		{
			try { NoExpandY = Convert.ToBoolean(group["NoExpandY"]); } 
			catch { errortype("NoExpandY"); TabGroupOk = false; }
		}

		// var e = group.GetEnumerator();
		// while(e.MoveNext())
		// {
		// 	Int32 keytabs = Convert.ToInt32(e.Key) - 1;
		// 	Tabs.Add(keytabs, new Tab_t(c, (LuaTable)e.Value));
		// 	TabGroupOk = TabGroupOk && Tabs[keytabs].TabOk;
		// }

		try { LuaTable test = (LuaTable)group["Tabs"]; } 
		catch { errortype("Tabs"); TabGroupOk = false;}	

		if (TabGroupOk)
		{
			var tabsTable = (LuaTable) group["Tabs"];
			var e = tabsTable.GetEnumerator();
			while(e.MoveNext())
			{
				Int32 keytabs = Convert.ToInt32(e.Key) - 1;
				Tabs.Add(keytabs, new Tab_t(c, (LuaTable)e.Value));
				TabGroupOk = TabGroupOk && Tabs[keytabs].TabOk;
			}
		}
	}
}

public class ConfigData_t
{
	public bool ConfigOk;
	public Int32 Baudrate;
	public string Endian;
	//Commands from Monitor to target
	public char SetValue;
	public char ReportValueOn, ReportValueOff;
	public char IReportValue, IReportTextConsole;
	//In ms, every ms a report is sent
	public float SampleTimeHW;

	//Fields layouting
	public Dictionary<Int32, TabGroup_t> TabGroups;
	public Dictionary<Int32, Var_t> Vars;
	public Dictionary<string, Solid_t> Solids;

	public bool Vue3D, Plot;

	private ConsoleOut c;

	public ConfigData_t(ConsoleOut _c)
	{
		c = _c;
		Vars = new Dictionary<Int32, Var_t>();
		Solids = new Dictionary<string, Solid_t>();
		TabGroups = new Dictionary<Int32, TabGroup_t>();
		// Default value if not found in cfg file
		Endian = "little";
		Vue3D = false;
		Plot = false;
		ConfigOk = false;
	}

	public object GetVarIdxValue(Int32 idx)
	{
		Int32 keyVar = -1;
        Int32 keyData = -1;

        // Search the key that contains this index in the dictionnary
        foreach(Int32 _keyVar in Vars.Keys)
        {
            Var_t var = Vars[_keyVar];

            if (Math.Abs(_keyVar-idx) < var.Data.Keys.Count)
            {
                    keyVar = _keyVar;
                    keyData = idx - _keyVar;
                    break;
            }
        }

		if (keyVar != -1 && keyData != -1)
        	return Vars[keyVar].Data[keyData].Value;
		else
			return null;
	}

	private void errortype(string parameter)
	{
		c.Print(LogLevel_e.eError, "Bad type for field :" + parameter + "\n");
	}

	public void Default(LuaTable pars)
	{
		ConfigOk = true;

		if (pars["Baudrate"] != null && pars["SetValue"] != null && 
			pars["ReportValueOn"] != null && pars["ReportValueOff"] != null &&
			pars["IReportValue"] != null &&
			pars["IReportTextConsole"] != null && pars["SampleTimeHW"] != null && 
			pars["GroupTabs"] != null)
		{
			try { Baudrate = Convert.ToInt32(pars["Baudrate"]); } 
			catch { errortype("Baudrate"); ConfigOk = false; }

			try { SetValue = ((string)pars["SetValue"])[0]; } 
			catch { errortype("SetValue"); ConfigOk = false; }

			try { ReportValueOn = ((string)pars["ReportValueOn"])[0]; } 
			catch { errortype("ReportValueOn"); ConfigOk = false; }

			try { ReportValueOff = ((string)pars["ReportValueOff"])[0]; } 
			catch { errortype("ReportValueOff"); ConfigOk = false; }

			try { IReportValue = ((string)pars["IReportValue"])[0]; } 
			catch { errortype("IReportValue"); ConfigOk = false; }

			try { IReportTextConsole = ((string)pars["IReportTextConsole"])[0]; } 
			catch { errortype("IReportTextConsole"); ConfigOk = false; }

			if (pars["Vue3D"] != null)
			{
				try { Vue3D = Convert.ToBoolean(pars["Vue3D"]); } 
				catch { errortype("Vue3D"); ConfigOk = false; }
			}

			if (pars["Plot"] != null)
			{
				try { Plot = Convert.ToBoolean(pars["Plot"]); } 
				catch { errortype("Plot"); ConfigOk = false; }
			}

			try { LuaTable test = (LuaTable)pars["GroupTabs"]; } 
			catch { errortype("GroupTabs"); ConfigOk = false;}	

			if (ConfigOk)
			{
				var GroupTabs = (LuaTable) pars["GroupTabs"];
				var e = GroupTabs.GetEnumerator();
				while(e.MoveNext())
				{
					Int32 keyGroupTabs = Convert.ToInt32(e.Key) - 1;
					TabGroups.Add(keyGroupTabs, new TabGroup_t(c, (LuaTable) e.Value));
					ConfigOk = ConfigOk && TabGroups[keyGroupTabs].TabGroupOk;
				}
			}
		}
		else
		{
			ConfigOk = false;
			c.Print(LogLevel_e.eError, "Error in Params: " +  
				" Mandatory fields not present\n");
		}
	}

	public void Variable(LuaTable variableTable)
	{
		Var_t Var = new Var_t(c, variableTable);
		if (Vars.ContainsKey(Var.Index))
		{
			ConfigOk = false;
			c.Print(LogLevel_e.eError, "Duplicate index " + Var.Index.ToString() + "\n");
		}
		else
		{
			Vars.Add(Var.Index, Var);
			ConfigOk = ConfigOk && Vars[Var.Index].VarOk;
		}
	}

	public void Solid(LuaTable solidTable)
	{
		Helper_t helper = new Helper_t(c);

		Solid_t Solid = new Solid_t(this, c, helper.LuaTable2Dic(solidTable));

		if (Solids.ContainsKey(Solid.Name))
		{
			ConfigOk = false;
			c.Print(LogLevel_e.eError, "Duplicate Solid name " + Solid.Name + "\n");
		}
		else
		{
			Solids.Add(Solid.Name, Solid);
			ConfigOk = ConfigOk && Solids[Solid.Name].SolidOk;
		}
	}
}




public class RowVBoxContainer : VBoxContainer
{
	public Int32 IdxVar = -1;
	public RowVBoxContainer(Int32 idxvar)
	{
		IdxVar = idxvar;
	}
}

public class ParametersGroups_t
{
	//Fields layouting
	public Dictionary<Int32, Var_t> Vars;
	private ConsoleOut c;
	private string parametersName;
	public bool ParametersOk;

	public ParametersGroups_t(ConsoleOut _c, string _parametersName)
	{
		c = _c;
		parametersName = _parametersName;
		Vars = new Dictionary<Int32, Var_t>();
	}

	//////////////////////////////////////////////
	// Default and Variable for parsing ini string
	//////////////////////////////////////////////	
	public void Default(LuaTable pars){	}
	public void Variable(LuaTable var)
	{
		Var_t Var = new Var_t(c, var);
		if (Var.Parameter == parametersName)
			Vars.Add(Var.Index, Var);
	}

	//////////////////////////////////////////////
	// Parameter is for parsing param file
	//////////////////////////////////////////////
	public void Parameter(LuaTable var)
	{
		Var_t Var = new Var_t(c, var);
		if (Var.Parameter == parametersName)
			Vars.Add(Var.Index, Var);
		else
			c.Print(LogLevel_e.eWarning, 
				" Variable " + Var.Index.ToString() + " is not tagged with same parameter," +
				" value(s) not changed.\n");
	}
}