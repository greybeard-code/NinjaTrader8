// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.PriceLevel
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Xml.Serialization;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

[XmlInclude(typeof (GannAngle))]
[CategoryDefaultExpanded(true)]
[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.PriceLevelTypeConverter")]
[XmlInclude(typeof (TrendLevel))]
public class PriceLevel : NotifyPropertyChangedBase, ICloneable, IStrokeProvider
{
  private double val;
  private string name;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolsPriceLevelIsVisible", GroupName = "NinjaScriptGeneral")]
  public bool IsVisible { get; set; }

  [Browsable(false)]
  [XmlIgnore]
  public bool IsValueVisible { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolsPriceLevelLineStroke", GroupName = "NinjaScriptGeneral")]
  public Stroke Stroke { get; set; }

  [XmlIgnore]
  [Browsable(false)]
  public object Tag { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolsPriceLevelValue", GroupName = "NinjaScriptGeneral")]
  public double Value
  {
    get => this.val;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [XmlIgnore]
  [Browsable(false)]
  public Func<double, string> ValueFormatFunc { get; set; }

  [Browsable(false)]
  public string Name
  {
    get => this.name;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual object Clone() => (object) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public object AssemblyClone(Type t) => (object) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void CopyTo(PriceLevel other)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public double GetPrice(double startPrice, double totalPriceRange, bool isInverted) => 0.0;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public float GetY(
    ChartScale chartScale,
    double startPrice,
    double totalPriceRange,
    bool isInverted)
  {
    return 0.0f;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public PriceLevel()
  {
  }

  public PriceLevel(double value, Brush brush)
    : this(value, brush, 2f)
  {
  }

  public PriceLevel(double value, Brush brush, float strokeWidth)
    : this(value, brush, strokeWidth, (DashStyleHelper) 0, 100)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public PriceLevel(
    double value,
    Brush brush,
    float strokeWidth,
    DashStyleHelper dashStyle,
    int opacity)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static PriceLevel()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
