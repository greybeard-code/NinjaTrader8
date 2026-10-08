// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.PathToolSegment
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class PathToolSegment : ICloneable
{
  [Browsable(false)]
  public ChartAnchor EndAnchor { get; set; }

  [Browsable(false)]
  public string Name { get; set; }

  [Browsable(false)]
  public ChartAnchor StartAnchor { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public object AssemblyClone(Type t) => (object) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual object Clone() => (object) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void CopyTo(PathToolSegment other)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public PathToolSegment()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public PathToolSegment(ChartAnchor startAnchor, ChartAnchor endAnchor, string name)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static PathToolSegment()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
