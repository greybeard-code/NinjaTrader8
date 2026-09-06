// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.PathToolSegmentContainer
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class PathToolSegmentContainer : DrawingTool
{
  [SkipOnCopyTo(true)]
  [Browsable(false)]
  public List<PathToolSegment> PathToolSegments { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void CopyTo(NinjaTrader.NinjaScript.NinjaScript ninjaScript)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected PathToolSegmentContainer()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static PathToolSegmentContainer()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
