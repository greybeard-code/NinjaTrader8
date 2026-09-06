// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class AndrewsPitchfork : PriceLevelContainer
{
  private const int cursorSensitivity = 15;
  private ChartAnchor editingAnchor;

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptLines", Order = 1)]
  public Stroke AnchorLineStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkCalculationMethod", GroupName = "NinjaScriptGeneral", Order = 4)]
  public AndrewsPitchfork.AndrewsPitchforkCalculationMethod CalculationMethod { get; set; }

  [Display(Order = 3)]
  public ChartAnchor ExtensionAnchor { get; set; }

  [Display(Order = 2)]
  public ChartAnchor EndAnchor { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkRetracement", GroupName = "NinjaScriptLines", Order = 2)]
  public Stroke RetracementLineStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkExtendLinesBack", GroupName = "NinjaScriptLines")]
  public bool IsExtendedLinesBack { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciTimeExtensionsShowText", GroupName = "NinjaScriptGeneral")]
  public bool IsTextDisplayed { get; set; }

  public virtual object Icon => (object) Icons.DrawAndrewsPitchfork;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
  public int PriceLevelOpacity { get; set; }

  [Display(Order = 1)]
  public ChartAnchor StartAnchor { get; set; }

  public virtual bool SupportsAlerts => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected void DrawPriceLevelText(
    double minX,
    double maxX,
    Point endPoint,
    PriceLevel priceLevel,
    ChartPanel panel)
  {
  }

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new AndrewsPitchfork.\u003CGetAlertConditionItems\u003Ed__46(-2)
    {
      \u003C\u003E4__this = this
    };
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private IEnumerable<Tuple<Point, Point>> GetAndrewsEndPoints(
    ChartControl chartControl,
    ChartScale chartScale)
  {
    return (IEnumerable<Tuple<Point, Point>>) null;
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

  [MethodImpl(MethodImplOptions.NoInlining)]
  static AndrewsPitchfork()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }

  [TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
  public enum AndrewsPitchforkCalculationMethod
  {
    StandardPitchfork,
    Schiff,
    ModifiedSchiff,
  }
}
