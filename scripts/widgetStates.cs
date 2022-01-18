using Godot;
using System;

public class widgetStates : VBoxContainer
{
    private MainTop RInst;
    ItemList ItemListInst;

    public Int32 BaseIndex { get; set; } = -1;

    public override void _Ready()
    {
        RInst = GetNode<MainTop>("/root/Main");
        ItemListInst = FindNode("ItemList") as ItemList;
        ItemListInst.Connect("item_selected", this, nameof(onItemSelected));
    }

    public void onItemSelected(int itemIdx)
    {
        if (  RInst.ConfigData.Vars[BaseIndex].Data[0].CanEdit )
        {
            Byte value = 0;
            Byte maxBits = Convert.ToByte(ItemListInst.GetItemCount());

            if ( RInst.ConfigData.Vars[BaseIndex].Data[0].Exclusive )
            {
                if ( ItemListInst.GetItemCustomBgColor(itemIdx) == new Color(0, 0, 0) )
                {
                    ItemListInst.SetItemCustomBgColor(itemIdx, new Color(0, 1, 0));
                    for( Byte i=0; i<maxBits; i++ )
                        if ( i != itemIdx )
                            ItemListInst.SetItemCustomBgColor(i, new Color(0, 0, 0)); 
                }         
            }
            else
            {
                if ( ItemListInst.GetItemCustomBgColor(itemIdx) == new Color(0, 0, 0))
                    ItemListInst.SetItemCustomBgColor(itemIdx, new Color(0, 1, 0));
                else
                    ItemListInst.SetItemCustomBgColor(itemIdx, new Color(0, 0, 0));
            }

            for( Byte i=0; i<maxBits; i++ )
                if ( ItemListInst.GetItemCustomBgColor(i) == new Color(0, 1, 0) )
                    value += Convert.ToByte(1<<i);

            RInst.ConfigData.Vars[BaseIndex].Data[0].Value = value;

            send(value);
        }
    }


    private void send(Byte currentByte)
	{
		RInst.ComPortInst.SendCommand(RInst.ConfigData.SetValue, (Byte)(BaseIndex), 
					(VarType_e)"B"[0], currentByte.ToString());
	}
}
