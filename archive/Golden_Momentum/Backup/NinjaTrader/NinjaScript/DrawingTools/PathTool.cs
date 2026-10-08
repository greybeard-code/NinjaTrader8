// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.PathTool
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class PathTool : PathToolSegmentContainer
{
  private PathGeometry arrowPathGeometry;
  private const double cursorSensitivity = 15.0;
  private DispatcherTimer doubleClickTimer;
  private ChartAnchor editingAnchor;

  [ExcludeFromTemplate]
  [Browsable(false)]
  [SkipOnCopyTo(true)]
  public List<ChartAnchor> ChartAnchors { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 0)]
  public Stroke OutlineStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolPathBegin", GroupName = "NinjaScriptGeneral", Order = 1)]
  public PathTool.PathToolCapMode PathBegin { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolPathEnd", GroupName = "NinjaScriptGeneral", Order = 2)]
  public PathTool.PathToolCapMode PathEnd { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolPathShowCount", GroupName = "NinjaScriptGeneral", Order = 3)]
  public bool ShowCount { get; set; }

  [ExcludeFromTemplate]
  [SkipOnCopyTo(true)]
  [Display(Order = 0)]
  public ChartAnchor StartAnchor
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (ChartAnchor) null;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void CopyTo(NinjaTrader.NinjaScript.NinjaScript ninjaScript)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private PathGeometry CreatePathGeometry(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    double pixelAdjust)
  {
    return (PathGeometry) null;
  }

  private void DoubleClickTimerTick(object sender, EventArgs e) => this.doubleClickTimer.Stop();

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new PathTool.\u003CGetAlertConditionItems\u003Ed__33(-2)
    {
      \u003C\u003E4__this = this
    };
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

  [DllImport("user32.dll")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  private static extern uint GetDoubleClickTime();

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Point[] GetPathAnchorPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

  public virtual Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return this.GetPathAnchorPoints(chartControl, chartScale);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual IEnumerable<Condition> GetValidAlertConditions() => (IEnumerable<Condition>) null;

  public virtual object Icon => (object) Icons.DrawPath;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual bool IsAlertConditionTrue(
    AlertConditionItem conditionItem,
    Condition condition,
    ChartAlertValue[] values,
    ChartControl chartControl,
    ChartScale chartScale)
  {
    return false;
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
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnRender(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  public virtual bool SupportsAlerts => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  static PathTool()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }

  [TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
  public enum PathToolCapMode
  {
    Arrow,
    Line,
  }
}
