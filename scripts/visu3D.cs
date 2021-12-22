using Godot;
using System;
using NLua;

public class visu3D : Spatial
{
    public MainTop RInst;
    public ViewportContainer VP3DContainerInst;
    public Camera CameraInst;
    public Spatial PivotInst;

    public override void _Ready()
    {
        RInst = GetNode<MainTop>("/root/Main");
        VP3DContainerInst = RInst.FindNode("VP3DContainer") as ViewportContainer;
        CameraInst = FindNode("Camera") as Camera;
        PivotInst = FindNode("Pivot") as Spatial;
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

    bool onArea = false;
    bool dragging = false;
    Vector2 startDragPosition;
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
                                dragging = true;
                                startDragPosition = pos;
                                break;
                        }
                    else
                        switch ((ButtonList)mouseEvent.ButtonIndex)
                        {
                            case ButtonList.Left:
                                dragging = false;
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
                    dragging = false;
                }
            }


            if (inputEvent is InputEventMouseMotion motionEvent)
            {
                // pos relative to drawing area
                Vector2 pos = motionEvent.Position;
                onArea = (pos.x > 0 && pos.y > 0 && pos.x < areaSize.x && pos.y < areaSize.y);

                if (dragging)
                {
                    if (onArea)
                    {
                        PivotInst.Rotate(CameraInst.GlobalTransform.basis.x, Mathf.Deg2Rad(-motionEvent.Relative.y));
                        PivotInst.Rotate(Vector3.Up, Mathf.Deg2Rad(-motionEvent.Relative.x));
                    }
                }
            }
        }
    }
}
