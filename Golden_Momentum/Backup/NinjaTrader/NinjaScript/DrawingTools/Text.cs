// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.Text
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class Text : DrawingTool
{
  private Brush areaBrush;
  private DeviceBrush areaBrushDevice;
  private int areaOpacity;
  private TextAlignment alignment;
  [CLSCompliant(false)]
  protected TextLayout cachedTextLayout;
  private SimpleFont font;
  private Rect layoutRect;
  private bool needsLayoutUpdate;
  private readonly float outlinePadding;
  private Brush textBrush;
  private DeviceBrush textBrushDevice;
  private string text;

  public virtual object Icon => (object) Icons.DrawText;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextAlignment", GroupName = "NinjaScriptGeneral", Order = 7)]
  public TextAlignment Alignment
  {
    get => this.alignment;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Browsable(false)]
  [XmlIgnore]
  public bool UseChartTextBrush { get; set; }

  [Browsable(false)]
  public bool UseChartTextBrushSerialize
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => false;
    set => this.UseChartTextBrush = value;
  }

  [Browsable(false)]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public bool ManuallyDrawn { get; set; }

  [Browsable(false)]
  [XmlIgnore]
  public Brush LastBrush { get; set; }

  public ChartAnchor Anchor { get; set; }

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [XmlIgnore]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
  public Brush AreaBrush
  {
    get => this.areaBrush;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Browsable(false)]
  public string AreaBrushSerialize
  {
    get => Serialize.BrushToString(this.AreaBrush);
    set => this.AreaBrush = Serialize.StringToBrush(value);
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
  [Range(0, 100)]
  public int AreaOpacity
  {
    get => this.areaOpacity;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextFont", GroupName = "NinjaScriptGeneral", Order = 4)]
  public SimpleFont Font
  {
    get => this.font;
    set
    {
      this.font = value;
      this.needsLayoutUpdate = true;
    }
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 3)]
  public Stroke OutlineStroke { get; set; }

  [ExcludeFromTemplate]
  [PropertyEditor("NinjaTrader.Gui.Tools.MultilineEditor")]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolText", GroupName = "NinjaScriptGeneral", Order = 5)]
  public string DisplayText
  {
    get => this.text;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [XmlIgnore]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
  public Brush TextBrush
  {
    get => this.textBrush;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Browsable(false)]
  public string TextBrushSerialize
  {
    get => Serialize.BrushToString(this.TextBrush);
    set => this.TextBrush = Serialize.StringToBrush(value);
  }

  [Browsable(false)]
  public int YPixelOffset { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void Dispose(bool disposing)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void DrawText(ChartControl chartControl)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual Cursor GetCursor(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    Point point)
  {
    return (Cursor) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual Rect GetCurrentRect(Rect pLayoutRect, double pOutlinePadding) => new Rect();

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static float GetPadding() => 0.0f;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual Point GetTextDrawingPosition(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale)
  {
    return new Point();
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual bool IsVisibleOnChart(
    ChartControl chartControl,
    ChartScale chartScale,
    DateTime firstTimeOnChart,
    DateTime lastTimeOnChart)
  {
    return false;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnCalculateMinMax()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnMouseDown(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnMouseMove(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  public virtual void OnMouseUp(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
    this.DrawingState = (DrawingState) 2;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnRender(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void UpdateTextLayout(float maxWidth)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public Text()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static Text()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
