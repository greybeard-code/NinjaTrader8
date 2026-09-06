// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.Draw
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public static class Draw
{
  private static readonly Brush defaultRegionBrush;
  private const int defaultRegionOpacity = 25;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork AndrewsPitchforkCore(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    DateTime anchor1Time,
    double anchor1Y,
    int anchor2BarsAgo,
    DateTime anchor2Time,
    double anchor2Y,
    int anchor3BarsAgo,
    DateTime anchor3Time,
    double anchor3Y,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork AndrewsPitchfork(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork AndrewsPitchfork(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork AndrewsPitchfork(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork AndrewsPitchfork(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.AndrewsPitchfork) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Arc ArcCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Arc Arc(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Arc) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static T ChartMarkerCore<T>(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    DateTime time,
    double yVal,
    Brush brush,
    bool isGlobal,
    string templateName)
    where T : ChartMarker
  {
    return default (T);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowDown ArrowDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowDown ArrowDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowDown ArrowDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowDown ArrowDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowDown ArrowDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowDown ArrowDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowUp ArrowUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowUp ArrowUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowUp ArrowUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowUp ArrowUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowUp ArrowUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowUp ArrowUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Diamond Diamond(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Diamond) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Diamond Diamond(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Diamond) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Diamond Diamond(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Diamond) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Diamond Diamond(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Diamond) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Diamond Diamond(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Diamond) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Diamond Diamond(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Diamond) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Dot Dot(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Dot) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Dot Dot(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Dot) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Dot Dot(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Dot) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Dot Dot(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Dot) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Dot Dot(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Dot) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Dot Dot(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Dot) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Square Square(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Square) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Square Square(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Square) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Square Square(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Square) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Square Square(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Square) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Square Square(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Square) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Square Square(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Square) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleDown TriangleDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleDown TriangleDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleDown TriangleDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleDown TriangleDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleDown TriangleDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleDown TriangleDown(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleDown) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleUp TriangleUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleUp TriangleUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleUp TriangleUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleUp TriangleUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleUp TriangleUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TriangleUp TriangleUp(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TriangleUp) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static T FibonacciCore<T>(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
    where T : FibonacciLevels
  {
    return default (T);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions FibonacciExtensionsCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    int extensionBarsAgo,
    DateTime extensionTime,
    double extensionY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle FibonacciCircle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle FibonacciCircle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle FibonacciCircle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle FibonacciCircle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions FibonacciExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    int extensionBarsAgo,
    double extensionY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions FibonacciExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    DateTime extensionTime,
    double extensionY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions FibonacciExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    DateTime extensionTime,
    double extensionY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions FibonacciExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    int extensionBarsAgo,
    double extensionY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements FibonacciRetracements(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements FibonacciRetracements(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements FibonacciRetracements(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements FibonacciRetracements(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions FibonacciTimeExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions FibonacciTimeExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions FibonacciTimeExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions FibonacciTimeExtensions(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.FibonacciTimeExtensions) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.GannFan GannFanCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int barsAgo,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.GannFan) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.GannFan GannFan(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.GannFan) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.GannFan GannFan(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.GannFan) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.GannFan GannFan(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.GannFan) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.GannFan GannFan(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime time,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.GannFan) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static T DrawLineTypeCore<T>(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool isGlobal,
    string templateName)
    where T : NinjaTrader.NinjaScript.DrawingTools.Line
  {
    return default (T);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLineCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLine(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLine(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLine(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLine(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ArrowLine ArrowLine(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ArrowLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLineCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.ExtendedLine ExtendedLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.ExtendedLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLineCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    double y,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.HorizontalLine) null;
  }

  public static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLine(
    NinjaScriptBase owner,
    string tag,
    double y,
    Brush brush)
  {
    return Draw.HorizontalLineCore(owner, false, tag, y, brush, (DashStyleHelper) 0, 1);
  }

  public static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLine(
    NinjaScriptBase owner,
    string tag,
    double y,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return Draw.HorizontalLineCore(owner, false, tag, y, brush, dashStyle, width);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLine(
    NinjaScriptBase owner,
    string tag,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.HorizontalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLine(
    NinjaScriptBase owner,
    string tag,
    double y,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.HorizontalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLine(
    NinjaScriptBase owner,
    string tag,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.HorizontalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    double y,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.HorizontalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.HorizontalLine HorizontalLine(
    NinjaScriptBase owner,
    string tag,
    bool isAutoscale,
    double y,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.HorizontalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Line Line(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Line) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLineCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int barsAgo,
    DateTime time,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    DateTime time,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    DateTime time,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    int barsAgo,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    int barsAgo,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    DateTime time,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    int barsAgo,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    int barsAgo,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.VerticalLine VerticalLine(
    NinjaScriptBase owner,
    string tag,
    DateTime time,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.VerticalLine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Ray RayCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    DashStyleHelper dashStyle,
    int width,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ray Ray(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ray) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.PathTool PathCore(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    List<ChartAnchor> chartAnchors,
    Brush brush,
    DashStyleHelper dashStyle,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.PathTool PathBasic(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    DateTime anchor1Time,
    double anchor1Y,
    int anchor2BarsAgo,
    DateTime anchor2Time,
    double anchor2Y,
    int anchor3BarsAgo,
    DateTime anchor3Time,
    double anchor3Y,
    int anchor4BarsAgo,
    DateTime anchor4Time,
    double anchor4Y,
    int anchor5BarsAgo,
    DateTime anchor5Time,
    double anchor5Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    int anchor4BarsAgo,
    double anchor4Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    DateTime anchor4Time,
    double anchor4Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    int anchor4BarsAgo,
    double anchor4Y,
    int anchor5BarsAgo,
    double anchor5Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    DateTime anchor4Time,
    double anchor4Y,
    DateTime anchor5Time,
    double anchor5Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    List<ChartAnchor> chartAnchors,
    Brush brush,
    DashStyleHelper dashStyle)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.PathTool PathTool(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    List<ChartAnchor> chartAnchors,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.PathTool) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Polygon PolygonCore(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    List<ChartAnchor> chartAnchors,
    Brush brush,
    DashStyleHelper dashStyle,
    Brush areaBrush,
    int areaOpacity,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Polygon PolygonBasic(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    DateTime anchor1Time,
    double anchor1Y,
    int anchor2BarsAgo,
    DateTime anchor2Time,
    double anchor2Y,
    int anchor3BarsAgo,
    DateTime anchor3Time,
    double anchor3Y,
    int anchor4BarsAgo,
    DateTime anchor4Time,
    double anchor4Y,
    int anchor5BarsAgo,
    DateTime anchor5Time,
    double anchor5Y,
    int anchor6BarsAgo,
    DateTime anchor6Time,
    double anchor6Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    int anchor4BarsAgo,
    double anchor4Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    DateTime anchor4Time,
    double anchor4Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    int anchor4BarsAgo,
    double anchor4Y,
    int anchor5BarsAgo,
    double anchor5Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    DateTime anchor4Time,
    double anchor4Y,
    DateTime anchor5Time,
    double anchor5Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    int anchor4BarsAgo,
    double anchor4Y,
    int anchor5BarsAgo,
    double anchor5Y,
    int anchor6BarsAgo,
    double anchor6Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    DateTime anchor4Time,
    double anchor4Y,
    DateTime anchor5Time,
    double anchor5Y,
    DateTime anchor6Time,
    double anchor6Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    List<ChartAnchor> chartAnchors,
    Brush brush,
    DashStyleHelper dashStyle,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Polygon Polygon(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    List<ChartAnchor> chartAnchors,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Polygon) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Region Region(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    DateTime startTime,
    int endBarsAgo,
    DateTime endTime,
    ISeries<double> series1,
    ISeries<double> series2,
    double price,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity,
    int displacement)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Region) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Region Region(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    ISeries<double> series,
    double price,
    Brush areaBrush,
    int areaOpacity,
    int displacement = 0)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Region) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Region Region(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    ISeries<double> series1,
    ISeries<double> series2,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity,
    int displacement = 0)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Region) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Region Region(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    ISeries<double> series,
    double price,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Region) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Region Region(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    ISeries<double> series1,
    ISeries<double> series2,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Region) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static T RegionHighlightCore<T>(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool isGlobal,
    string templateName)
    where T : RegionHighlightBase
  {
    return default (T);
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX RegionHighlightX(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX RegionHighlightX(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX RegionHighlightX(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX RegionHighlightX(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX RegionHighlightX(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX RegionHighlightX(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY RegionHighlightY(
    NinjaScriptBase owner,
    string tag,
    double startY,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY RegionHighlightY(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    double startY,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY) null;
  }

  [CLSCompliant(false)]
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY RegionHighlightY(
    NinjaScriptBase owner,
    string tag,
    double startY,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.RegressionChannel RegressionChannelCore(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    DateTime startTime,
    int endBarsAgo,
    DateTime endTime,
    Brush upperBrush,
    DashStyleHelper upperDashStyle,
    float? upperWidth,
    Brush middleBrush,
    DashStyleHelper middleDashStyle,
    float? middleWidth,
    Brush lowerBrush,
    DashStyleHelper lowerDashStyle,
    float? lowerWidth,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegressionChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegressionChannel RegressionChannel(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegressionChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegressionChannel RegressionChannel(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegressionChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegressionChannel RegressionChannel(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    int endBarsAgo,
    Brush upperBrush,
    DashStyleHelper upperDashStyle,
    int upperWidth,
    Brush middleBrush,
    DashStyleHelper middleDashStyle,
    int middleWidth,
    Brush lowerBrush,
    DashStyleHelper lowerDashStyle,
    int lowerWidth)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegressionChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegressionChannel RegressionChannel(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    DateTime endTime,
    Brush upperBrush,
    DashStyleHelper upperDashStyle,
    int upperWidth,
    Brush middleBrush,
    DashStyleHelper middleDashStyle,
    int middleWidth,
    Brush lowerBrush,
    DashStyleHelper lowerDashStyle,
    int lowerWidth)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegressionChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegressionChannel RegressionChannel(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegressionChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RegressionChannel RegressionChannel(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RegressionChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.RiskReward RiskRewardCore(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int entryBarsAgo,
    DateTime entryTime,
    double entryY,
    int stopBarsAgo,
    DateTime stopTime,
    double stopY,
    int targetBarsAgo,
    DateTime targetTime,
    double targetY,
    double ratio,
    bool isStop,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RiskReward) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RiskReward RiskReward(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime entryTime,
    double entryY,
    DateTime endTime,
    double endY,
    double ratio,
    bool isStop)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RiskReward) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RiskReward RiskReward(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int entryBarsAgo,
    double entryY,
    int endBarsAgo,
    double endY,
    double ratio,
    bool isStop)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RiskReward) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RiskReward RiskReward(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime entryTime,
    double entryY,
    DateTime endTime,
    double endY,
    double ratio,
    bool isStop,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RiskReward) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.RiskReward RiskReward(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int entryBarsAgo,
    double entryY,
    int endBarsAgo,
    double endY,
    double ratio,
    bool isStop,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.RiskReward) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Ruler RulerCore(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    DateTime startTime,
    double startY,
    int endBarsAgo,
    DateTime endTime,
    double endY,
    int textBarsAgo,
    DateTime textTime,
    double textY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ruler) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ruler Ruler(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    int textBarsAgo,
    double textY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ruler) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ruler Ruler(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    DateTime textTime,
    double textY)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ruler) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ruler Ruler(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    int textBarsAgo,
    double textY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ruler) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ruler Ruler(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    DateTime textTime,
    double textY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ruler) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static T ShapeCore<T>(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    DateTime startTime,
    DateTime endTime,
    double startY,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool isGlobal,
    string templateName)
    where T : ShapeBase
  {
    return default (T);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Triangle TriangleCore(
    NinjaScriptBase owner,
    bool isAutoScale,
    string tag,
    int startBarsAgo,
    int midBarsAgo,
    int endBarsAgo,
    DateTime startTime,
    DateTime midTime,
    DateTime endTime,
    double startY,
    double midY,
    double endY,
    Brush color,
    Brush areaColor,
    int areaOpacity,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Ellipse Ellipse(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Ellipse) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Rectangle Rectangle(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Rectangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int middleBarsAgo,
    double middleY,
    int endBarsAgo,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime middleTime,
    double middleY,
    DateTime endTime,
    double endY,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int middleBarsAgo,
    double middleY,
    int endBarsAgo,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime midTime,
    double middleY,
    DateTime endTime,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int middleBarsAgo,
    double middleY,
    int endBarsAgo,
    double endY,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int startBarsAgo,
    double startY,
    int middleBarsAgo,
    double middleY,
    int endBarsAgo,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime startTime,
    double startY,
    DateTime midTime,
    double middleY,
    DateTime endTime,
    double endY,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    double startY,
    int middleBarsAgo,
    double middleY,
    int endBarsAgo,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Triangle Triangle(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    double startY,
    DateTime middleTime,
    double middleY,
    DateTime endTime,
    double endY,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Triangle) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.Text TextCore(
    NinjaScriptBase owner,
    string tag,
    bool autoScale,
    string text,
    int barsAgo,
    DateTime time,
    double y,
    int? yPixelOffset,
    Brush textBrush,
    TextAlignment? textAlignment,
    SimpleFont font,
    Brush outlineBrush,
    Brush areaBrush,
    int? areaOpacity,
    bool isGlobal,
    string templateName,
    DashStyleHelper outlineDashStyle,
    int outlineWidth)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Text Text(
    NinjaScriptBase owner,
    string tag,
    string text,
    int barsAgo,
    double y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Text Text(
    NinjaScriptBase owner,
    string tag,
    string text,
    int barsAgo,
    double y,
    Brush textBrush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Text Text(
    NinjaScriptBase owner,
    string tag,
    string text,
    int barsAgo,
    double y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Text Text(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    string text,
    int barsAgo,
    double y,
    int yPixelOffset,
    Brush textBrush,
    SimpleFont font,
    TextAlignment alignment,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Text Text(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    string text,
    DateTime time,
    double y,
    int yPixelOffset,
    Brush textBrush,
    SimpleFont font,
    TextAlignment alignment,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Text Text(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    string text,
    int barsAgo,
    double y,
    int yPixelOffset,
    Brush textBrush,
    SimpleFont font,
    TextAlignment alignment,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity,
    DashStyleHelper outlineDashStyle,
    int outlineWidth,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.Text Text(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    string text,
    DateTime time,
    double y,
    int yPixelOffset,
    Brush textBrush,
    SimpleFont font,
    TextAlignment alignment,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity,
    DashStyleHelper outlineDashStyle,
    int outlineWidth,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.Text) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.TextFixed TextFixedCore(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPosition textPosition,
    Brush textBrush,
    SimpleFont font,
    Brush outlineBrush,
    Brush areaBrush,
    int? areaOpacity,
    bool isGlobal,
    string templateName,
    DashStyleHelper outlineDashStyle,
    int outlineWidth)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixed) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.TextFixedFine TextFixedFineCore(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPositionFine textPositionFine,
    Brush textBrush,
    SimpleFont font,
    Brush outlineBrush,
    Brush areaBrush,
    int? areaOpacity,
    bool isGlobal,
    string templateName,
    DashStyleHelper outlineDashStyle,
    int outlineWidth)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixedFine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TextFixed TextFixed(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPosition textPosition,
    Brush textBrush,
    SimpleFont font,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixed) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TextFixed TextFixed(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPosition textPosition,
    Brush textBrush,
    SimpleFont font,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity,
    DashStyleHelper outlineDashStyle,
    int outlineWidth,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixed) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TextFixed TextFixed(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPosition textPosition)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixed) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TextFixed TextFixed(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPosition textPosition,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixed) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TextFixedFine TextFixedFine(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPositionFine textPositionFine)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixedFine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TextFixedFine TextFixedFine(
    NinjaScriptBase owner,
    string tag,
    string text,
    TextPositionFine textPositionFine,
    Brush textBrush,
    SimpleFont font,
    Brush outlineBrush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TextFixedFine) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCyclesCore(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    DateTime startTime,
    DateTime endTime,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    Brush brush)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    Brush brush,
    Brush areaBrush,
    int areaOpacity)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    Brush brush,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    Brush brush,
    Brush areaBrush,
    int areaOpacity,
    bool drawOnPricePanel)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    int startBarsAgo,
    int endBarsAgo,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TimeCycles TimeCycles(
    NinjaScriptBase owner,
    string tag,
    DateTime startTime,
    DateTime endTime,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TimeCycles) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static NinjaTrader.NinjaScript.DrawingTools.TrendChannel TrendChannelCore(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    DateTime anchor1Time,
    double anchor1Y,
    int anchor2BarsAgo,
    DateTime anchor2Time,
    double anchor2Y,
    int anchor3BarsAgo,
    DateTime anchor3Time,
    double anchor3Y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TrendChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TrendChannel TrendChannel(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TrendChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TrendChannel TrendChannel(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TrendChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TrendChannel TrendChannel(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    int anchor1BarsAgo,
    double anchor1Y,
    int anchor2BarsAgo,
    double anchor2Y,
    int anchor3BarsAgo,
    double anchor3Y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TrendChannel) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static NinjaTrader.NinjaScript.DrawingTools.TrendChannel TrendChannel(
    NinjaScriptBase owner,
    string tag,
    bool isAutoScale,
    DateTime anchor1Time,
    double anchor1Y,
    DateTime anchor2Time,
    double anchor2Y,
    DateTime anchor3Time,
    double anchor3Y,
    bool isGlobal,
    string templateName)
  {
    return (NinjaTrader.NinjaScript.DrawingTools.TrendChannel) null;
  }

  static Draw()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
    Draw.defaultRegionBrush = (Brush) Brushes.Goldenrod;
  }
}
