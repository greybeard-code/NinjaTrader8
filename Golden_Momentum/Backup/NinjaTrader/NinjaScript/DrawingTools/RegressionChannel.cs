// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.RegressionChannel
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Data;
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

[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.RegressionChannelTypeConverter")]
public class RegressionChannel : DrawingTool
{
  private ChartControl cControl;
  private ChartScale cScale;
  private ChartAnchor editingAnchor;

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [RefreshProperties(RefreshProperties.All)]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelType", GroupName = "NinjaScriptGeneral", Order = 2)]
  public RegressionChannel.RegressionChannelType ChannelType { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendLeft", GroupName = "NinjaScriptLines")]
  public bool ExtendLeft { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendRight", GroupName = "NinjaScriptLines")]
  public bool ExtendRight { get; set; }

  [Display(Order = 2)]
  public ChartAnchor EndAnchor { get; set; }

  public virtual object Icon => (object) Icons.DrawRegressionChannel;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelLowerChannel", GroupName = "NinjaScriptLines", Order = 3)]
  public Stroke LowerChannelStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelRegressionChannel", GroupName = "NinjaScriptLines", Order = 2)]
  public Stroke RegressionStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelPriceType", GroupName = "NinjaScriptGeneral", Order = 1)]
  public PriceType PriceType { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationUpperDistance", GroupName = "NinjaScriptGeneral", Order = 3)]
  public double StandardDeviationUpperDistance { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationLowerDistance", GroupName = "NinjaScriptGeneral", Order = 4)]
  public double StandardDeviationLowerDistance { get; set; }

  [Display(Order = 1)]
  public ChartAnchor StartAnchor { get; set; }

  public virtual bool SupportsAlerts => true;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRegressionChannelUpperChannel", GroupName = "NinjaScriptLines", Order = 1)]
  public Stroke UpperChannelStroke { get; set; }

  public virtual void AddPastedOffset(ChartPanel panel, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public double[] CalculateRegressionPriceValues(Bars baseBars, int startIndex, int endIndex)
  {
    return (double[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Point[] CreateRegressionPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public double GetBarPrice(Bars barObject, int barIndex) => 0.0;

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new RegressionChannel.\u003CGetAlertConditionItems\u003Ed__58(-2);
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
  public virtual void OnBarsChanged()
  {
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
  private void SetAnchorsToRegression(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static RegressionChannel()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }

  [TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
  public enum RegressionChannelType
  {
    Segment,
    StandardDeviation,
  }
}
