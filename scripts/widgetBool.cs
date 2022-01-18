using Godot;
using System;

public class widgetBool : HBoxContainer
{
	private const int IDX_GRIPS_ANA = 4;

    private static Texture textOff = ResourceLoader.Load("res://icons/blackButton.png") as Texture;
    private static Texture textGreen = ResourceLoader.Load("res://icons/greenButton.png") as Texture;
    private static Texture textRed = ResourceLoader.Load("res://icons/redButton.png") as Texture;

	// Classes instances
	private MainTop RInst;
	// 
    public Int32 BaseIndex { get; set; } = -1;
    public Int32 ArrIdx { get; set; } = -1;
	public Byte bitIndex = 0;
	private UtilityFunctions helper;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RInst = GetNode<MainTop>("/root/Main");
		helper = new UtilityFunctions();
	}

	private float lastPalm = 0.0f;
	private bool lastButton = false;
	public override void _Process(float delta)
	{
		float palm;
		bool button;

		if (BaseIndex == IDX_GRIPS_ANA && RInst.joyPadNameInst.Text != "")
		{
			if (bitIndex==4)
			{
				palm = Input.GetJoyAxis(0, 6);

				if (palm > 0.9f && lastPalm <= 0.9f)
					_on_Button_SetBit_pressed();

				if (palm < 0.1f && lastPalm >= 0.1f)
					_on_Button_ClearBit_pressed();

				lastPalm = palm;
			}

			if (bitIndex==0)
			{
				palm = Input.GetJoyAxis(0, 7);

				if (palm > 0.9f && lastPalm <= 0.9f)
					_on_Button_SetBit_pressed();

				if (palm < 0.1f && lastPalm >= 0.1f)
					_on_Button_ClearBit_pressed();

				lastPalm = palm;
			}

			if (bitIndex==1)
			{
				button = Input.IsJoyButtonPressed(0, 2);

				if ( button && !lastButton)
				{
					_on_Button_SetBit_pressed();
				}
				if ( !button && lastButton)
				{
					_on_Button_ClearBit_pressed();
				}

				lastButton = button;
			}

			if (bitIndex==2)
			{
				button = Input.IsJoyButtonPressed(0, 1);

				if ( button && !lastButton)
				{
					_on_Button_SetBit_pressed();
				}
				if ( !button && lastButton)
				{
					_on_Button_ClearBit_pressed();
				}

				lastButton = button;
			}

			if (bitIndex==5)
			{
				button = Input.IsJoyButtonPressed(0, 14);

				if ( button && !lastButton)
				{
					_on_Button_SetBit_pressed();
				}
				if ( !button && lastButton)
				{
					_on_Button_ClearBit_pressed();
				}

				lastButton = button;
			}

			if (bitIndex==6)
			{
				button = Input.IsJoyButtonPressed(0, 15);

				if ( button && !lastButton)
				{
					_on_Button_SetBit_pressed();
				}
				if ( !button && lastButton)
				{
					_on_Button_ClearBit_pressed();
				}

				lastButton = button;
			}


			// if Input.is_joy_button_pressed(joy_num, btn):
			// 	button_grid.get_child(btn).add_color_override("font_color", Color.white)
			// 	if btn < 17:
			// 		joypad_buttons.get_child(btn).show()
			// else:
			// 	button_grid.get_child(btn).add_color_override("font_color", Color(0.2, 0.1, 0.3, 1))
			// 	if btn < 17:
			// 		joypad_buttons.get_child(btn).hide()
		}	
	}

	private void _on_Button_SetBit_pressed()
	{
		Byte currentByte = Convert.ToByte(RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].Value);

		currentByte |= (Byte)(1 << bitIndex);

		// Update local variable for Visu3D
		RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].Value = currentByte;

		UpdateTexture(true);

		send(currentByte);
	}

	private void _on_Button_ClearBit_pressed()
	{
		Byte currentByte = Convert.ToByte(RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].Value);

		currentByte &= (Byte)~(1 << bitIndex);

		// Update local variable for Visu3D
		RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].Value = currentByte;

		UpdateTexture(false);

		send(currentByte);
	}

    public void UpdateTexture(bool set)
    {
		TextureRect texture = FindNode("TextureRect_BitVal") as TextureRect;

		string color = RInst.ConfigData.Vars[BaseIndex].Data[ArrIdx].Bits[bitIndex].Color;

		if (set)
		{
			if (color=="red")
				texture.Texture = textRed;
			else
				texture.Texture = textGreen;
		}
		else
			texture.Texture = textOff;
    }

	private void send(Byte currentByte)
	{
		RInst.ComPortInst.SendCommand(RInst.ConfigData.SetValue, (Byte)(BaseIndex+ArrIdx), 
					(VarType_e)"B"[0], currentByte.ToString());
	}
}




