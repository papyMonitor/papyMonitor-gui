using Godot;
using System;
using NLua;

public class visu3D : Spatial
{
	public MainTop RInst;
	public ViewportContainer VP3DContainerInst;
	public Camera CameraInst;
	public Spatial PivotInst;

	bool onArea;
	bool dragRot, dragPos;
	Vector2 startDragPosition;

	public override void _Ready()
	{
		RInst = GetNode<MainTop>("/root/Main");
		VP3DContainerInst = RInst.FindNode("VP3DContainer") as ViewportContainer;
		CameraInst = FindNode("Camera") as Camera;
		PivotInst = FindNode("Pivot") as Spatial;

		onArea = false;
		dragRot = false;
		dragPos = false;
	}

	public override void _Process(float delta)
	{
		var rootNode = FindNode("Root") as Node;
		updateNode(rootNode);
	}

	private void updateNode(Node node)
	{
		var children = node.GetChildren();
		foreach (Node n in children)
		{
			if (n is Solid_t solid)
			{
				solid.Update();
				updateNode(solid);
			}
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!(RInst.FindNode("LoadFile") as FileDialog).Visible &&
			 !(RInst.FindNode("SaveFile") as FileDialog).Visible)
		{
			Vector2 areaPos = VP3DContainerInst.RectGlobalPosition;
			Vector2 areaSize = VP3DContainerInst.RectSize;

			if (inputEvent is InputEventMouseButton mouseEvent)
			{
				// pos relative to drawing area
				Vector2 pos = mouseEvent.Position;
				onArea = (pos.x > 0 && pos.y > 0 && pos.x < areaSize.x && pos.y < areaSize.y);

				if (onArea)
				{
					if (mouseEvent.Pressed)
						switch ((ButtonList)mouseEvent.ButtonIndex)
						{
							case ButtonList.Left:
								dragRot = true;
								break;

							case ButtonList.Right:
								dragPos = true;
								startDragPosition = pos;
								break;                                
						}
					else
						switch ((ButtonList)mouseEvent.ButtonIndex)
						{
							case ButtonList.Left:
								dragRot = false;
								break;

							case ButtonList.Right:
								dragPos = false;
								break;

							case ButtonList.WheelUp:
								CameraInst.Fov--;
								break;

							case ButtonList.WheelDown:
								CameraInst.Fov++;
								break;
						}
				}
				else
				{
					dragRot = false;
					dragPos = false;
				}
			}


			if (inputEvent is InputEventMouseMotion motionEvent)
			{
				// pos relative to drawing area
				Vector2 pos = motionEvent.Position;
				onArea = (pos.x > 0 && pos.y > 0 && pos.x < areaSize.x && pos.y < areaSize.y);

				if (dragRot)
				{
					if (onArea)
					{
						PivotInst.Rotate(CameraInst.GlobalTransform.basis.x, Mathf.Deg2Rad(-motionEvent.Relative.y));
						PivotInst.Rotate(Vector3.Up, Mathf.Deg2Rad(-motionEvent.Relative.x));
					}
				}

				if (dragPos)
				{
					if (onArea)
					{
						Transform t = Transform.Identity;
						t.basis = Basis.Identity;

					   // t.Translated(new Vector3(0,0,motionEvent.Relative.y));

						CameraInst.Translate(new Vector3(-motionEvent.Relative.x * 0.05f, motionEvent.Relative.y * 0.05f, 0));
						//CameraInst.Transform = t;
					}
				}
			}
		}
	}
}
