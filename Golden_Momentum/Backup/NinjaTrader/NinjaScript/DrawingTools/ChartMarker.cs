// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.ChartMarker
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
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

public abstract class ChartMarker : DrawingTool
{
  private Brush areaBrush;
  [CLSCompliant(false)]
  protected readonly DeviceBrush AreaDeviceBrush;
  private Brush outlineBrush;
  [CLSCompliant(false)]
  protected readonly DeviceBrush OutlineDeviceBrush;

  public ChartAnchor Anchor { get; set; }

  [XmlIgnore]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
  public Brush AreaBrush
  {
    get => this.areaBrush;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesSize", GroupName = "GuiGeneral", Order = 2)]
  public ChartMarkerSize Size { get; set; }

  [Browsable(false)]
  public string AreaBrushSerialize
  {
    get => Serialize.BrushToString(this.AreaBrush);
    set => this.AreaBrush = Serialize.StringToBrush(value);
  }

  protected double BarWidth
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => 0.0;
  }

  [XmlIgnore]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesOutlineBrush", GroupName = "NinjaScriptGeneral", Order = 3)]
  public Brush OutlineBrush
  {
    get => this.outlineBrush;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Browsable(false)]
  public string OutlineBrushSerialize
  {
    get => Serialize.BrushToString(this.OutlineBrush);
    set => this.OutlineBrush = Serialize.StringToBrush(value);
  }

  protected static float MinimumSize => 5f;

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnCalculateMinMax()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected float GetSizeMultiplier() => 0.0f;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void Dispose(bool disposing)
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
  public virtual Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private double GetSelectionSensitivity() => 0.0;

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

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnMouseUp(
    ChartControl control,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected ChartMarker()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static ChartMarker()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
