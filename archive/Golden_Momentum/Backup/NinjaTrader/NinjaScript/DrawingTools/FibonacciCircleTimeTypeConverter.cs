// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.FibonacciCircleTimeTypeConverter
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Gui.DrawingTools;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class FibonacciCircleTimeTypeConverter : DrawingToolPropertiesConverter
{
  public virtual bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual PropertyDescriptorCollection GetProperties(
    ITypeDescriptorContext context,
    object value,
    Attribute[] attributes)
  {
    return (PropertyDescriptorCollection) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static FibonacciCircleTimeTypeConverter()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
