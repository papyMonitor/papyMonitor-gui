using Godot;
using System;
using CircularBuffer;
using System.Collections.Generic;

public class Params
{
    public Int32 NbDataPts_X;
    public Int32 NormalModeStartPtX;
    public Int32[] NbRegions_X = new Int32[7]{1, 2, 5, 10, 20, 50, 100};
    public Int32 IdxNbRegions_X;

    // Y
    public float cY;
    public float[] ValPerDiv_Y = new float[16]{0.1f, 0.2f, 0.5f, 1.0f, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000};
    public Int32 IdxValPerDiv_Y;

    public Params()
    {
        // Y
        cY = 0.5f;  // Origin relative to display Y size

        IdxValPerDiv_Y = 6;

        // X Normal mode
        NormalModeStartPtX = -1;  // Starting point in the curve data

        // X Scope mode
        IdxNbRegions_X = -1;
    }
}

public class axles
{
    private Chart Parent;
    private Params Params;

    public float xTimePerDiv;
    public UInt32 nbXDivPrim { get; set; } = 10;
    public UInt32 nbXDivSec { get; set; } = 10;
    public UInt32 nbYDivPrim { get; set; } = 6;
    public UInt32 nbYDivSec { get; set; } = 10;
    public Color originColor { get; set; } = new Color(1,1,1, 0.7F);
    public Color primDivColor { get; set; } = new Color(0.2F,1.0F,0.5F,0.3F);
    public Color triggerDivColor { get; set; } = new Color(0.0F,0.0F,0.0F,0.6F);
    public Color secDivColor { get; set; } = new Color(0.5F,0.2F,0.2F,1);

    public axles(Chart _Parent)
    {
        Parent = _Parent;
        Params = Parent.Params;
    }
    public void setXTimePerDiv(float xTotTime)
    {
        float tmp; float coef = 1.0f;
        // Calculate the division time in 1,2,5,10 units
        if (xTotTime == 0.0f)
            xTotTime = 1.0f;
            
        tmp = xTotTime / nbXDivPrim;
        while (tmp > 10.0f)
        {
            tmp /= 10.0f;
            coef *= 10.0f;
        }
        while (tmp < 1.0f)
        {
            tmp *= 10.0f;
            coef /= 10.0f;
        }
        if (tmp < 1.5f)
        {
            xTimePerDiv = 1.0f * coef;
        }
        else
        {
            if (tmp < 3.5f)
            {
                xTimePerDiv = 2.0f * coef;
            }
            else
            {
                if (tmp < 7.5f)
                    xTimePerDiv = 5.0f * coef;
                else
                    xTimePerDiv = 10.0f * coef;
            }
        }
    }

    public void draw(Params Params, Vector2 areaSize, float xTotTime)
    {
        Vector2 ptStart, ptEnd;
        float centerY = Params.cY * areaSize.y;
        float stepY = areaSize.y / nbYDivPrim;
        // Y=0
        ptStart.x = 1; 
        ptEnd.x = areaSize.x-1;

        ptStart.y = centerY;
        ptEnd.y = centerY;

        Parent.DrawLine(ptStart, ptEnd, originColor, 2);

        // Horizontal
        ptStart.y = centerY + stepY;
        ptEnd.y = centerY + stepY; 
        while(ptStart.y<areaSize.y)
        {
            Parent.DrawLine(ptStart, ptEnd, primDivColor);
            ptStart.y += stepY;
            ptEnd.y += stepY;
        }
        ptStart.y = centerY - stepY;
        ptEnd.y = centerY - stepY; 
        while(ptStart.y>0.0f)
        {
            Parent.DrawLine(ptStart, ptEnd, primDivColor);
            ptStart.y -= stepY;
            ptEnd.y -= stepY;
        }        

        // Vertical
        float r = xTimePerDiv / xTotTime * areaSize.x;
        ptStart.y = 0;
        ptEnd.y = areaSize.y - 1;
        ptStart.x = 0;
        ptEnd.x = 0;
        while (ptStart.x <= areaSize.x)
        {
            Parent.DrawLine(ptStart, ptEnd, primDivColor);
            ptStart.x += r;
            ptEnd.x = ptStart.x;
        } 

        // Horizontal trigger line
        if (Parent.ScopeTriggerMode)
        {
            float ry =  areaSize.y / (float)nbYDivPrim / Params.ValPerDiv_Y[Params.IdxValPerDiv_Y];
            float TrigY = centerY - Parent.TriggerLevel * ry;

            ptStart.x = 1; 
            ptEnd.x = areaSize.x-1;

            ptStart.y = TrigY;
            ptEnd.y = TrigY;
            Parent.DrawLine(ptStart, ptEnd, triggerDivColor);
        }
    }
}

public class Display
{    
    Chart Parent;
    private Params Params;
    public Display(Chart _Parent)
    {
        Parent = _Parent;
        Params = Parent.Params;
    } 
    
    public void Draw(Dictionary<Int32, Curve_t> Curves, Params Params, Vector2 areaSize, UInt32 nbYDivPrim)
    {
        Vector2 plotVal, plotValNext;
        Int32 NbDisplayPts_X = Params.NbDataPts_X / Params.NbRegions_X[Params.IdxNbRegions_X];

        foreach(Int32 key in Curves.Keys)
        if (Curves[key].onPlot)
        {
            float r;
            float ry =  areaSize.y / (float)nbYDivPrim / Params.ValPerDiv_Y[Params.IdxValPerDiv_Y];

            if (!Parent.ScopeMode)
            {
                // Check if Area greater than the number of points to display
                if (areaSize.x > NbDisplayPts_X)
                {
                    // Yes, then display the line regarding the data number
                    r = (float)areaSize.x / (float)NbDisplayPts_X;

                    // Init
                    plotVal.x = 0.0f;

                    if (Params.NormalModeStartPtX < Curves[key].DataNormal.Size)
                        plotVal.y = Params.cY * areaSize.y - Curves[key].DataNormal[Params.NormalModeStartPtX] * ry;
                    else
                        plotVal.y = 0.0f;

                    for (Int32 i=0; i<NbDisplayPts_X-1; i++)
                    {
                        if (Params.NormalModeStartPtX + (i+1) < Curves[key].DataNormal.Size)
                        {
                            plotValNext.x = (float)(i+1) * r;
                            plotValNext.y = Params.cY * areaSize.y - Curves[key].DataNormal[Params.NormalModeStartPtX + (i+1)] * ry;

                            Parent.DrawLine(plotVal, plotValNext, Curves[key].dataColor);

                            plotVal = plotValNext;
                        }
                    }
                }
                else
                {
                    // No, then display for each pixel and seek value in data
                    r = (float)NbDisplayPts_X / (float)areaSize.x;

                    // Init
                    plotVal.x = 0.0f;

                    if (Params.NormalModeStartPtX < Curves[key].DataNormal.Size)
                        plotVal.y = Params.cY * areaSize.y - Curves[key].DataNormal[Params.NormalModeStartPtX] * ry;
                    else
                        plotVal.y = 0.0f;           

                    for (Int32 i=0; i<areaSize.x-1; i++)
                    {
                        if (Params.NormalModeStartPtX + (int)((i+1)*r) < Curves[key].DataNormal.Size)
                        {
                            plotValNext.x = i+1;
                            plotValNext.y = Params.cY * areaSize.y - Curves[key].DataNormal[Params.NormalModeStartPtX + (int)((i+1)*r)] * ry;

                            Parent.DrawLine(plotVal, plotValNext, Curves[key].dataColor);

                            plotVal = plotValNext;
                        }
                    }
                }
            }
            else
            {
                // Check if Area greater than the number of points to display
                if (areaSize.x > NbDisplayPts_X)
                {
                    if (!Parent.ScopeTriggerMode)
                    {
                        Int32 StartIdx = (Curves[key].IdxDataScope / NbDisplayPts_X) * NbDisplayPts_X;
                        // Init
                        plotVal.x = 0.0f;
                        plotVal.y = Params.cY * areaSize.y - Curves[key].DataScope[ StartIdx ] * ry;

                        r = (float)areaSize.x / (float)(NbDisplayPts_X-1);
                            
                        for (Int32 i=0; i<NbDisplayPts_X-1; i++)
                        {
                            plotValNext.x = (float)(i+1) * r;

                            if ( (StartIdx + i+1) < Params.NbDataPts_X) 
                                plotValNext.y = Params.cY * areaSize.y - Curves[key].DataScope[ StartIdx + i+1 ] * ry;
                            else
                                plotValNext.y = Params.cY * areaSize.y - Curves[key].DataScope[ StartIdx - Params.NbDataPts_X + i+1 ] * ry;

                            Parent.DrawLine(plotVal, plotValNext, Curves[key].dataColor);

                            plotVal = plotValNext;
                        }   
                    }
                    else // Scope in trigger mode
                    {
                        NbDisplayPts_X = Curves[key].DataScopeDisplayTrigger.Length;

                        // Init
                        plotVal.x = 0.0f;
                        plotVal.y = Params.cY * areaSize.y - Curves[key].DataScopeDisplayTrigger[ 0 ] * ry;

                        r = (float)areaSize.x / (float)(NbDisplayPts_X-1);
                            
                        for (Int32 i=0; i<NbDisplayPts_X-1; i++)
                        {
                            plotValNext.x = (float)(i+1) * r;
                            plotValNext.y = Params.cY * areaSize.y - Curves[key].DataScopeDisplayTrigger[ i+1 ] * ry;

                            Parent.DrawLine(plotVal, plotValNext, Curves[key].dataColor);

                            plotVal = plotValNext;
                        }  
                      
                    }
                }
                else
                {
                    plotVal.x = 0.0f;
                }
            }
        }
    }
}

public class Curve_t
{
    Chart Parent;
    Params Params;

    private float minYVal, maxYVal;
    // Data pools
    public CircularBuffer<float> DataNormal; // Normal mode
    public float[] DataScope;
    public float[] DataScopeDisplayTrigger;
    public bool OnTriggering;

    public Int32 IdxDataScope;

    public Color dataColor { get; set; }

    public bool onPlot;
    public bool IsTrigger;

    public bool freezed { get; set; }

    public Curve_t(Chart _Parent)
    {
        Parent = _Parent;
        Params = Parent.Params;

        onPlot = false;
    }
    UInt32 Count = 0;
    public void addElem(object oY)
    {
        Int32 StartTriggerWindowIdx, EndTriggerWindowIdx;
        Int32 NbDisplayPts_X = Params.NbDataPts_X / Params.NbRegions_X[Params.IdxNbRegions_X];

        float Y = Convert.ToSingle(oY);

        if (!freezed)
        {
            if (Y > maxYVal) maxYVal = Y;
            if (Y < minYVal) minYVal = Y;

            // Normal mode (circular buffer)
            if (DataNormal.IsFull)
                DataNormal.PopFront();
            DataNormal.PushBack(Y); 
            
            // Scope mode
            DataScope[IdxDataScope] = Y;


            if (Parent.ScopeMode && Parent.ScopeTriggerMode)
            {
                if (IsTrigger)
                {
                    if (!OnTriggering)
                    {
                        if (IdxDataScope == 0)
                        {
                            if ( (DataScope[IdxDataScope] > Parent.TriggerLevel) && (DataScope[Params.NbDataPts_X-1] <= Parent.TriggerLevel) )
                            {
                                OnTriggering = true;
                                Parent.TriggerIdx = IdxDataScope;
                                Parent.SetTrigEventTxt(true);
                            }
                        }
                        else
                        {
                            if ( (DataScope[IdxDataScope] > Parent.TriggerLevel) && (DataScope[IdxDataScope-1] <= Parent.TriggerLevel) )
                            {
                                OnTriggering = true;
                                Parent.TriggerIdx = IdxDataScope;
                                Parent.SetTrigEventTxt(true);
                            }
                        }
                    }
                    else
                    {
                        if ( (Parent.TriggerIdx - NbDisplayPts_X/2) >=0 )
                            StartTriggerWindowIdx = Parent.TriggerIdx - NbDisplayPts_X/2;
                        else
                            StartTriggerWindowIdx = Parent.TriggerIdx - NbDisplayPts_X/2 + Params.NbDataPts_X;

                        if ( (Parent.TriggerIdx + NbDisplayPts_X/2) < Params.NbDataPts_X )
                            EndTriggerWindowIdx = Parent.TriggerIdx + NbDisplayPts_X/2;
                        else
                            EndTriggerWindowIdx = Parent.TriggerIdx + NbDisplayPts_X/2 - Params.NbDataPts_X;

                        Parent.Debug0Inst.Text = "WStart:  " + StartTriggerWindowIdx.ToString();
                        Parent.Debug1Inst.Text = "WEnd:    " + EndTriggerWindowIdx.ToString();


                        if (EndTriggerWindowIdx > StartTriggerWindowIdx)
                        {
                            if (IdxDataScope > EndTriggerWindowIdx)
                            {
                                Array.Copy(DataScope, StartTriggerWindowIdx, DataScopeDisplayTrigger, 0, NbDisplayPts_X);
                                OnTriggering = false;
                            }
                        }
                        else
                        {
                            if (IdxDataScope < StartTriggerWindowIdx && IdxDataScope > EndTriggerWindowIdx)
                            {
                                Array.Copy(DataScope, StartTriggerWindowIdx, DataScopeDisplayTrigger, 0, NbDisplayPts_X-EndTriggerWindowIdx);
                                Array.Copy(DataScope, 0, DataScopeDisplayTrigger, NbDisplayPts_X-EndTriggerWindowIdx, EndTriggerWindowIdx);
                                OnTriggering = false;
                            }
                        }
                    }
                    Parent.Debug2Inst.Text = "TrigIdx: " + Parent.TriggerIdx.ToString();
                    Parent.Debug3Inst.Text = "OnTrig: " + OnTriggering.ToString();
                }
                else
                {
                    if ( (Parent.TriggerIdx - NbDisplayPts_X/2) >=0 )
                        StartTriggerWindowIdx = Parent.TriggerIdx - NbDisplayPts_X/2;
                    else
                        StartTriggerWindowIdx = Parent.TriggerIdx - NbDisplayPts_X/2 + Params.NbDataPts_X;

                    if ( (Parent.TriggerIdx + NbDisplayPts_X/2) < Params.NbDataPts_X )
                        EndTriggerWindowIdx = Parent.TriggerIdx + NbDisplayPts_X/2;
                    else
                        EndTriggerWindowIdx = Parent.TriggerIdx + NbDisplayPts_X/2 - Params.NbDataPts_X;

                    Parent.Debug0Inst.Text = "WStart:  " + StartTriggerWindowIdx.ToString();
                    Parent.Debug1Inst.Text = "WEnd:    " + EndTriggerWindowIdx.ToString();


                    if (EndTriggerWindowIdx > StartTriggerWindowIdx)
                    {
                        if (IdxDataScope > EndTriggerWindowIdx)
                        {
                            Array.Copy(DataScope, StartTriggerWindowIdx, DataScopeDisplayTrigger, 0, NbDisplayPts_X);
                            OnTriggering = false;
                        }
                    }
                    else
                    {
                        if (IdxDataScope < StartTriggerWindowIdx && IdxDataScope > EndTriggerWindowIdx)
                        {
                            Array.Copy(DataScope, StartTriggerWindowIdx, DataScopeDisplayTrigger, 0, NbDisplayPts_X-EndTriggerWindowIdx);
                            Array.Copy(DataScope, 0, DataScopeDisplayTrigger, NbDisplayPts_X-EndTriggerWindowIdx, EndTriggerWindowIdx);
                            OnTriggering = false;
                        }
                    }
                }
            }

            // Stay in the Data buffer please
            if (++IdxDataScope==Params.NbDataPts_X)
                IdxDataScope=0;
        }
    }

    public void resize()
    {
        freezed = false;
        minYVal = Single.MaxValue;
        maxYVal = Single.MinValue;      
        DataNormal = new CircularBuffer<float>((int)Params.NbDataPts_X);
        DataScope = new float[Params.NbDataPts_X];
        OnTriggering = false;
        IdxDataScope = 0;

        ResizeDisplayTrigger();
    }
    public void ResizeDisplayTrigger()
    {
        DataScopeDisplayTrigger = new float[Params.NbDataPts_X / Params.NbRegions_X[Params.IdxNbRegions_X]];
    }
}

public class Chart : Node2D
{    
    /* #region variables */
    // Classes instances references
    private MainTop RInst;
    private ColorRect colorRectInst;
    private VBoxContainer controlAreaInst;
    private Label windowTimeSelectedInst;
    private Label pointsDisplayedInst;
    private Label XDivInst;
    private Label YDivInst;
    public Label TrigNameInst;
    public Label TrigEventInst;
    private VBoxContainer sigPan1Inst;
    private VBoxContainer sigPan2Inst;
    private Label hwSampleTimeInst;
    private Label samplePerElementInst;
    private Label totalTimeDisplayedInst;
    private Button freezeInst;
    private Label nbElementsSampledInst;
    private OptionButton nbPointsPerCurveInst;
    private CheckButton ScopeModeInst;
    private CheckButton ScopeModeTriggerInst;
    public Label Debug0Inst;
    public Label Debug1Inst;
    public Label Debug2Inst;
    public Label Debug3Inst;
    //
    public bool ScopeMode;
    public bool ScopeTriggerMode;
    public float TriggerLevel;
    public Int32 TriggerIdx;
    public Timer TimerTrigDisplayInst;

    private Display Display;
    private Random rnd;
    private bool initDone = false;
    //
    private Resource pointer, closedhand, resize_ver;

    public Dictionary<Int32, Curve_t> Curves;
    private axles axles;
    //
    public Params Params;
    private UInt32 nbElementsSampled = 1;

    private float xTotTime;
    /* #endregion */

    private Vector2 GetDrawArea() {
        return colorRectInst.RectSize;
    }
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        /* #region instanstiation */
        RInst = GetNode<MainTop>("/root/Main");
        colorRectInst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("ColorRect") as ColorRect;
        XDivInst = colorRectInst.FindNode("XDiv") as Label;
        YDivInst = colorRectInst.FindNode("YDiv") as Label;
        TrigNameInst = colorRectInst.FindNode("TrigName") as Label;
        TrigEventInst = colorRectInst.FindNode("TrigEvent") as Label;
        controlAreaInst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("controlArea") as VBoxContainer;
        windowTimeSelectedInst = controlAreaInst.FindNode("windowTimeSelected") as Label;
        pointsDisplayedInst = controlAreaInst.FindNode("pointsDisplayed") as Label;
        hwSampleTimeInst = controlAreaInst.FindNode("hwSampleTime") as Label;
        samplePerElementInst = controlAreaInst.FindNode("samplePerElement") as Label;
        nbElementsSampledInst = controlAreaInst.FindNode("nbElementsSampled") as Label;
        nbPointsPerCurveInst = controlAreaInst.FindNode("nbPointsPerCurve") as OptionButton;
        totalTimeDisplayedInst = controlAreaInst.FindNode("totalTimeDisplayed") as Label;
        nbPointsPerCurveInst.Connect("item_selected", this, nameof(NbPointsPerCurve_Pressed));
        freezeInst = controlAreaInst.FindNode("freeze") as Button;
        freezeInst.Connect("pressed", this, nameof(Freeze_Pressed));
        ScopeModeInst = controlAreaInst.FindNode("ScopeMode") as CheckButton;
        ScopeModeInst.Connect("toggled", this, nameof(ScopeMode_Toggled));
        ScopeModeTriggerInst = controlAreaInst.FindNode("TriggerMode") as CheckButton;
        ScopeModeTriggerInst.Connect("toggled", this, nameof(TriggerMode_Toggled));
        Debug0Inst = RInst.FindNode("Debug0") as Label;
        Debug1Inst = RInst.FindNode("Debug1") as Label;
        Debug2Inst = RInst.FindNode("Debug2") as Label;
        Debug3Inst = RInst.FindNode("Debug3") as Label;
        sigPan1Inst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel") as VBoxContainer;
        sigPan2Inst = (RInst.FindNode("drawEngine") as VBoxContainer).FindNode("signalsPanel2") as VBoxContainer;
        TimerTrigDisplayInst = FindNode("TimerTrigDisplay") as Timer;

        TimerTrigDisplayInst.WaitTime = 0.2f;
        TimerTrigDisplayInst.OneShot = true;
        TimerTrigDisplayInst.Connect("timeout", this , nameof(OnTimerTrigDisplayInst_timeout));
        /* #endregion */

        Curves = new Dictionary<Int32, Curve_t>();

        // Init drawing parameters
        Params = new Params();

        axles = new axles(this);
        rnd = new Random();
        Display = new Display(this);

        pointer = ResourceLoader.Load("res://icons/pointer@1x.png");
    	closedhand = ResourceLoader.Load("res://icons/closedhand.png");
        resize_ver = ResourceLoader.Load("res://icons/resize_ver2.png");

        colorRectInst.RectClipContent = true;

        ScopeMode = false;
        ScopeTriggerMode = false;
        ScopeModeTriggerInst.Disabled = true;

        TriggerLevel = 0.5f;
        TriggerIdx = 0;
    }
    public override void _Process(float delta)
    {
        Update();
    }
    public void Init()
    {
        foreach(Var_t var in RInst.ConfigData.Vars.Values)
        {
            foreach(Int32 dataKey in var.Data.Keys)
            {
                if (var.Data[dataKey].CanPlot)
                {
                    Curve_t Curve = new Curve_t(this);
                    Curves[var.Index+dataKey] = Curve;
                    Curves[var.Index+dataKey].dataColor = 
                        new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble());
                    Curves[var.Index+dataKey].onPlot = false;
                }
            }
        }

        UpdateDisplayParameters();

        // Init done, enable drawing
        initDone = true;
    }
    public void SetTrigEventTxt(bool trig)
    {
        if (trig)
        {
            TrigEventInst.AddColorOverride("font_color", new Color(1, 0, 0, 1));
            TrigEventInst.Text = "TRIG";
            TimerTrigDisplayInst.Start();
        }   
        else
        {
            TrigEventInst.AddColorOverride("font_color", new Color(1, 1, 1, 1));
            TrigEventInst.Text = "WAIT"; 
        }
    }
    public void OnTimerTrigDisplayInst_timeout()
    {
        TrigEventInst.AddColorOverride("font_color", new Color(1, 1, 1, 1));
        TrigEventInst.Text = "WAIT";
    }
    public override void _Draw()
    {
        float yTot;

        if (initDone)
        {
            yTot = GetDrawArea().y;

            // Draw the h and v axles + origin axle
            axles.draw(Params, GetDrawArea(), xTotTime);
            
            // Draw the Curves
            Display.Draw(Curves, Params, GetDrawArea(), axles.nbYDivPrim);

            // Display the scale
            // X
            if (xTotTime>1.0f)
                totalTimeDisplayedInst.Text = xTotTime.ToString() + "s";
            else
                totalTimeDisplayedInst.Text = (xTotTime*1000.0f).ToString() + "ms";

            if (axles.xTimePerDiv>1.0f)
                XDivInst.Text = "X: " + axles.xTimePerDiv.ToString() + "s/div";
            else
                XDivInst.Text = "X: " + (axles.xTimePerDiv*1000.0f).ToString() + "ms/div";

            // Y
            YDivInst.Text = "Y: " + Params.ValPerDiv_Y[Params.IdxValPerDiv_Y].ToString() + "/div";
        }
    }
    public void ScopeMode_Toggled(bool on)
    {
        UpdateDisplayParameters();
    }
    public void TriggerMode_Toggled(bool on)
    {
        UpdateDisplayParameters();
    }
    public void NbPointsPerCurve_Pressed(int index)
    {
        UpdateDisplayParameters();
    }
    public void SeekTriggerName()
    {
        TrigNameInst.Text = "NO TRIG SOURCE";

        var children = sigPan1Inst.GetChildren();
        foreach (signal c in children)
            if ((c.FindNode("Trig") as CheckBox).Pressed )
                    TrigNameInst.Text = (c.FindNode("signalName") as Button).Text;

        children = sigPan2Inst.GetChildren();
        foreach (signal c in children)
            if ((c.FindNode("Trig") as CheckBox).Pressed )
                    TrigNameInst.Text = (c.FindNode("signalName") as Button).Text;
    }
    public void UpdateDisplayParameters()
    {
        // Reinit drawing parameters
        Params.NormalModeStartPtX = 0;
        Params.NbDataPts_X = Convert.ToInt32(nbPointsPerCurveInst.Text);

        ScopeMode = ScopeModeInst.Pressed;

        if (ScopeMode)
        {
            ScopeModeTriggerInst.Disabled = false;

            ScopeTriggerMode = ScopeModeTriggerInst.Pressed;

            if (ScopeTriggerMode)
            {
                SeekTriggerName();
                SetTrigEventTxt(false);
                Params.IdxNbRegions_X = 1;
            }
            else
            {
                TrigNameInst.Text = "";
                TrigEventInst.Text = "";
                Params.IdxNbRegions_X = 0;
            }
        }
        else
        {
            TrigNameInst.Text = "";
            TrigEventInst.Text = "";

            ScopeModeTriggerInst.Disabled = true;
            ScopeTriggerMode = false;

            Params.IdxNbRegions_X = 0;
        }

        foreach(Int32 key in Curves.Keys)
            Curves[key].resize();

        RegionsXChanged();

        hwSampleTimeInst.Text = (RInst.ConfigData.SampleTimeHW*1000.0f).ToString()+"ms ";
        samplePerElementInst.Text = (RInst.ConfigData.SampleTimeHW*1000.0f*nbElementsSampled).ToString()+"ms ";
    }
    private void RegionsXChanged()
    {
        Int32 NbDisplayPts_X = Params.NbDataPts_X / Params.NbRegions_X[Params.IdxNbRegions_X];

        xTotTime = RInst.ConfigData.SampleTimeHW * (float)NbDisplayPts_X * (float)nbElementsSampled;
        axles.setXTimePerDiv(xTotTime); 
        pointsDisplayedInst.Text = NbDisplayPts_X.ToString(); 

        foreach(Int32 key in Curves.Keys)
            Curves[key].ResizeDisplayTrigger(); 
    }

    public void SetNbElementsSampled(UInt32 elements)
    {
        nbElementsSampledInst.Text = elements.ToString();
        if (elements==0)
            elements = 1;
        nbElementsSampled = elements;
        UpdateDisplayParameters();
    }
    public void Freeze_Pressed()
    {
        if (initDone)
        {
            foreach(Int32 key in Curves.Keys)
            {
                if (Curves[key].onPlot)
                    Curves[key].freezed = !Curves[key].freezed;
            }
        }
    }
    public void AddElement(Int32 IdxCurve, object value)
    {
        Curves[IdxCurve].addElem(value);
    }

    private bool dragging = false;
    private bool VertTrigDrag = false;
    private bool ctrlPressed = false;
    private Vector2 mousePosWhenLeftPressed;
    private Int32 startDataPtWhenLeftPressed;
    private float cYWhenLeftPressed;
    Vector2 pos = new Vector2();
    
    public override void _Input(InputEvent inputEvent)
    {
        if (initDone)
        {
            Int32 NbDisplayPts_X = Params.NbDataPts_X / Params.NbRegions_X[Params.IdxNbRegions_X];

            if ( !(RInst.FindNode("LoadFile") as FileDialog).Visible &&
                 !(RInst.FindNode("SaveFile") as FileDialog).Visible )
            {
                Vector2 drawRectPos = new Vector2(colorRectInst.GetGlobalRect().Position);
                
                Vector2 drawArea = GetDrawArea();
                Int32 idxPtFixeData;
                float rx;

                String txtEvent = inputEvent.AsText();
            
                bool onArea = (pos.x > 0 && pos.y > 0 && pos.x < drawArea.x && pos.y < drawArea.y);

                if (inputEvent is InputEventKey keyEvent)
                {
                    if (onArea)
                    {
                        if (OS.GetScancodeString(keyEvent.Scancode) == "Control")
                        {
                            ctrlPressed = keyEvent.Pressed;
                        } 
                    }    
                }

                if (inputEvent is InputEventMouseButton mouseEvent)
                {
                    // pos relative to drawing area
                    pos = mouseEvent.Position - drawRectPos;
                    onArea = (pos.x > 0 && pos.y > 0 && pos.x < drawArea.x && pos.y < drawArea.y);

                    if (onArea)
                    {
                        if (mouseEvent.Pressed)
                            switch ((ButtonList)mouseEvent.ButtonIndex)
                            {
                                case ButtonList.Left:
                                    mousePosWhenLeftPressed = pos;
                                    startDataPtWhenLeftPressed = Params.NormalModeStartPtX;
                                    cYWhenLeftPressed = Params.cY;
                                    dragging = true;
                                    Input.SetCustomMouseCursor(closedhand, Input.CursorShape.Arrow,  new Vector2(15, 15));
                                    break;
                            }
                        else
                            switch ((ButtonList)mouseEvent.ButtonIndex)
                            {
                                case ButtonList.Left:
                                    dragging = false;
                                    Input.SetCustomMouseCursor(pointer, Input.CursorShape.Arrow,  new Vector2(15, 15));
                                    break;

                                case ButtonList.WheelUp:
                                    // X axis Params
                                    if (!ctrlPressed)
                                    {
                                        rx = pos.x/drawArea.x;
                                        idxPtFixeData = Params.NormalModeStartPtX + (Int32)(NbDisplayPts_X * rx);

                                        Params.IdxNbRegions_X += 1;
                                        if (Params.IdxNbRegions_X > 6)
                                            Params.IdxNbRegions_X = 6;
                                        RegionsXChanged();

                                        Params.NormalModeStartPtX = idxPtFixeData - (Int32)(NbDisplayPts_X * rx);
                                        if (Params.NormalModeStartPtX < 0)
                                            Params.NormalModeStartPtX = 0;
                                    }
                                    else
                                    {
                                        if (Params.IdxValPerDiv_Y < 15)
                                            Params.IdxValPerDiv_Y++;
                                    }
                                    break;

                                case ButtonList.WheelDown:
                                    // X axis Params
                                    if (!ctrlPressed)
                                    {
                                        rx = pos.x/drawArea.x;
                                        idxPtFixeData = Params.NormalModeStartPtX + (Int32)(NbDisplayPts_X * rx);

                                        Params.IdxNbRegions_X -= 1;
                                        if (ScopeTriggerMode)
                                        {
                                        if (Params.IdxNbRegions_X < 1)
                                            Params.IdxNbRegions_X = 1;
                                        }
                                        else
                                        {
                                        if (Params.IdxNbRegions_X < 0)
                                            Params.IdxNbRegions_X = 0;
                                        }

                                        RegionsXChanged();

                                        if (Params.NormalModeStartPtX + NbDisplayPts_X > Params.NbDataPts_X-1)
                                        {
                                            Params.NormalModeStartPtX = Params.NbDataPts_X-1 - NbDisplayPts_X;
                                            if (Params.NormalModeStartPtX < 0)
                                                Params.NormalModeStartPtX = 0;
                                        }
                                    }
                                    else
                                    {
                                        if (Params.IdxValPerDiv_Y > 0)
                                            Params.IdxValPerDiv_Y--;
                                    }                                                     
                                    break;                        
                            }   
                    } 
                }

                if (inputEvent is InputEventMouseMotion motionEvent)
                {
                    // pos relative to drawing area
                    pos = motionEvent.Position - drawRectPos;
                    onArea = (pos.x > 0 && pos.y > 0 && pos.x < drawArea.x && pos.y < drawArea.y);

                    float ry =  drawArea.y / (float)axles.nbYDivPrim / Params.ValPerDiv_Y[Params.IdxValPerDiv_Y];
                    float centerY = Params.cY * drawArea.y;
                    float TrigY = centerY - TriggerLevel * ry;

                    if (dragging)
                    {
                        if (onArea)
                        {
                            if ( VertTrigDrag )
                            {
                                TriggerLevel = ( centerY - pos.y ) / ry;
                            }
                            else
                            {
                                // X Displacement
                                Params.NormalModeStartPtX = startDataPtWhenLeftPressed - (int)((pos.x - mousePosWhenLeftPressed.x) * NbDisplayPts_X / drawArea.x);

                                if (Params.NormalModeStartPtX < 0)
                                    Params.NormalModeStartPtX = 0;
                                
                                if (Params.NormalModeStartPtX + NbDisplayPts_X > Params.NbDataPts_X-1)
                                {
                                    Params.NormalModeStartPtX = Params.NbDataPts_X-1 - NbDisplayPts_X;
                                    if (Params.NormalModeStartPtX < 0)
                                        Params.NormalModeStartPtX = 0;
                                }

                                // Y Displacement
                                Params.cY = cYWhenLeftPressed + (pos.y - mousePosWhenLeftPressed.y) / drawArea.y;                            
                            }
                        }
                    }
                    else
                    {
                        if ( (pos.y >= TrigY-5) && (pos.y <= TrigY+5) )
                        {
                            if (ScopeTriggerMode)
                            {
                                Input.SetCustomMouseCursor(resize_ver, Input.CursorShape.Arrow,  new Vector2(4, 15));
                                VertTrigDrag = true;
                            }
                        }
                        else
                        {
                            Input.SetCustomMouseCursor(pointer, Input.CursorShape.Arrow,  new Vector2(15, 15));
                            VertTrigDrag = false;
                        }
                    }
                }

                if (!onArea)
                {
                    dragging = false;
                    Input.SetCustomMouseCursor(pointer, Input.CursorShape.Arrow,  new Vector2(15, 15)); 
                } 
            }
        }
    }
}
