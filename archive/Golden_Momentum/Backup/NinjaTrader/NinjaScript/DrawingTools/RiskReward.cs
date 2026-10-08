// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.RiskReward
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

public class RiskReward : DrawingTool
{
  private const int cursorSensitivity = 15;
  private ChartAnchor editingAnchor;
  private double entryPrice;
  private bool needsRatioUpdate;
  private double ratio;
  private double risk;
  private double reward;
  private double stopPrice;
  private double targetPrice;
  private double textleftPoint;
  private double textRightPoint;

  [Browsable(false)]
  private bool DrawTarget
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => false;
  }

  [Display(Order = 1)]
  public ChartAnchor EntryAnchor { get; set; }

  [Display(Order = 2)]
  public ChartAnchor RiskAnchor { get; set; }

  [Browsable(false)]
  public ChartAnchor RewardAnchor { get; set; }

  public virtual object Icon => (object) Icons.DrawRiskReward;

  [Range(0.0, 1.7976931348623157E+308)]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRiskRewardRatio", GroupName = "NinjaScriptGeneral", Order = 1)]
  [NinjaScriptProperty]
  public double Ratio
  {
    get => this.ratio;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptLines", Order = 3)]
  public Stroke AnchorLineStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeEntry", GroupName = "NinjaScriptLines", Order = 6)]
  public Stroke EntryLineStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeRisk", GroupName = "NinjaScriptLines", Order = 4)]
  public Stroke StopLineStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeReward", GroupName = "NinjaScriptLines", Order = 5)]
  public Stroke TargetLineStroke { get; set; }

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines", Order = 2)]
  public bool IsExtendedLinesRight { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines", Order = 1)]
  public bool IsExtendedLinesLeft { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextAlignment", GroupName = "NinjaScriptGeneral", Order = 2)]
  public TextLocation TextAlignment { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRulerYValueDisplayUnit", GroupName = "NinjaScriptGeneral", Order = 3)]
  public ValueUnit DisplayUnit { get; set; }

  public virtual bool SupportsAlerts => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void DrawPriceText(
    ChartAnchor anchor,
    Point point,
    double price,
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    return (IEnumerable<AlertConditionItem>) null;
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
  private string GetPriceString(double price, ChartBars chartBars) => (string) null;

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

  [EditorBrowsable(EditorBrowsableState.Never)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public void SetReward()
  {
  }

  [EditorBrowsable(EditorBrowsableState.Never)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public void SetRisk()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public RiskReward()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static RiskReward()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
