// ============================================================
// SELF-AWARE TREND SYSTEM (SATS) v1.9.0 — NinjaScript 8 Port
// Original PineScript by WillyAlgoTrader
// NinjaScript port by gbSATS
// ============================================================
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Core.FloatingPoint;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class gbSATS : Indicator
    {
        // ── Constants ────────────────────────────────────────
        private const int    WARMUP_FLOOR       = 50;
        private const int    MAX_HISTORY_SIGS   = 100;
        private const double BYPASS_SCORE       = 12.0;
        private const double ER_LOW_THRESH      = 0.25;
        private const double ER_HIGH_THRESH     = 0.50;
        private const double VOL_LOW_THRESH     = 0.7;
        private const double VOL_HIGH_THRESH    = 1.3;
        private const double EWMA_ALPHA         = 0.2;
        private const double MULT_SMOOTH_ALPHA  = 0.15;
        private const int    MAX_SCORE_REF      = 102;   // 6 × 17

        // ── Resolved preset fields ────────────────────────────
        private int    effectiveAtrLen;
        private double effectiveBaseMult;
        private int    effectiveErLen;
        private int    effectiveRsiLen;
        private double effectiveSlMult;

        // ── SuperTrend state ──────────────────────────────────
        private double lowerBand     = double.NaN;
        private double upperBand     = double.NaN;
        private int    stTrend       = 1;
        private int    prevStTrend   = 1;
        private int    trendStartBar = 0;

        // ── Adaptive multiplier smoothing ─────────────────────
        private double activeMultSm  = double.NaN;
        private double passiveMultSm = double.NaN;

        // ── TQI tracking ─────────────────────────────────────
        private double prevBarTqi    = 0.5;

        // ── Self-learning state ───────────────────────────────
        private List<double> signalRBuffer   = new List<double>();
        private int[]        gridCount       = new int[9];
        private double[]     gridEwmR        = new double[9];
        private double       effectiveQualityStrength;
        private int          signalsSinceCalib = 0;

        // ── All-time stats ────────────────────────────────────
        private double allTimeRSum    = 0.0;
        private int    allTimeCount   = 0;
        private double allTimePeak    = 0.0;
        private double allTimeTrough  = 0.0;
        private double allTimeCumR    = 0.0;
        private int    curWinStreak   = 0;
        private int    curLossStreak  = 0;
        private int    maxWinStreak   = 0;
        private int    maxLossStreak  = 0;

        // ── Active trade state ────────────────────────────────
        private int    tradeDir       = 0;
        private int    tradeEntryBar  = 0;
        private double tradeEntry     = double.NaN;
        private double tradeSl        = double.NaN;
        private double tradeTp1       = double.NaN;
        private double tradeTp2       = double.NaN;
        private double tradeTp3       = double.NaN;
        private double tradeTp1R      = double.NaN;
        private double tradeTp2R      = double.NaN;
        private double tradeTp3R      = double.NaN;
        private bool   hitTp1         = false;
        private bool   hitTp2         = false;
        private bool   hitTp3         = false;
        private int    tradeCellIdx   = -1;

        // ── Pivot tracking ────────────────────────────────────
        private double lastPivotHigh  = double.NaN;
        private double lastPivotLow   = double.NaN;

        // ── Score breakdown (last signal) ─────────────────────
        private double lastScoreMom    = double.NaN;
        private double lastScoreEr     = double.NaN;
        private double lastScoreVol    = double.NaN;
        private double lastScoreRsi    = double.NaN;
        private double lastScoreStruct = double.NaN;
        private double lastScoreBreak  = double.NaN;
        private int    lastDisplaySide = 0;
        private int    lastSignalBar   = 0;

        // ── Warmup ────────────────────────────────────────────
        private int    warmupBars;
        private bool   isWarmedUp     => CurrentBar >= warmupBars;

        // ── Fixed TP order after sort ─────────────────────────
        private double fixedTp1R;
        private double fixedTp2R;
        private double fixedTp3R;

        // ── Draw object tag bases ─────────────────────────────
        private const string TAG_ENTRY  = "SATS_Entry";
        private const string TAG_SL     = "SATS_SL";
        private const string TAG_TP1    = "SATS_TP1";
        private const string TAG_TP2    = "SATS_TP2";
        private const string TAG_TP3    = "SATS_TP3";

        // ── Per-bar cached values (for output window) ─────────
        private double curTqi     = 0.5;
        private double curEr      = 0.0;
        private double curRsi     = 50.0;
        private double curVolZ    = 0.0;
        private double curVolRatio = 1.0;

        // =====================================================
        // STATE MACHINE
        // =====================================================
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Self-Aware Trend System (SATS) v1.9.0 - NinjaScript port";
                Name        = "gbSATS";
                Calculate   = Calculate.OnBarClose;
                IsOverlay   = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                IsSuspendedWhileInactive = false;
                ScaleJustification = ScaleJustification.Right;

                // ── Main Settings ─────────────────────────────
                Preset          = "Auto";
                AtrLen          = 13;
                BaseMultiplier  = 2.0;

                // ── Adaptive Engine ───────────────────────────
                UseAdaptive      = true;
                ErLength         = 20;
                AdaptStrength    = 0.5;
                AtrBaselineLen   = 100;

                // ── Trend Quality Engine ──────────────────────
                UseTqi           = true;
                QualityStrength  = 0.4;
                QualityCurve     = 1.5;
                MultSmooth       = true;
                UseAsymBands     = true;
                AsymStrength     = 0.5;
                UseEffAtr        = true;
                UseCharFlip      = true;
                CharFlipMinAge   = 5;
                CharFlipHigh     = 0.55;
                CharFlipLow      = 0.25;
                TqiWeightEr      = 0.35;
                TqiWeightVol     = 0.20;
                TqiWeightStruct  = 0.25;
                TqiWeightMom     = 0.20;
                TqiStructLen     = 20;
                TqiMomLen        = 10;

                // ── Signal Filters ────────────────────────────
                UseStructure     = true;
                PivotLen         = 3;
                UseRsi           = true;
                RsiLen           = 14;
                RsiOB            = 70;
                RsiOS            = 30;
                RsiLookback      = 20;
                UseVol           = true;
                VolLen           = 20;
                MinScore         = 60;

                // ── Risk Management ───────────────────────────
                ShowRisk         = true;
                SlAtrMult        = 1.5;
                TpMode           = "Fixed";
                Tp1R             = 1.0;
                Tp2R             = 2.0;
                Tp3R             = 3.0;
                LabelOffset      = 10;
                ShowHits         = true;
                TradeMaxAge      = 100;

                // ── Dynamic TP ────────────────────────────────
                DynTpTqiWeight   = 0.6;
                DynTpVolWeight   = 0.4;
                DynTpMinScale    = 0.5;
                DynTpMaxScale    = 2.0;
                DynTpFloorR1     = 0.5;
                DynTpCeilR3      = 8.0;

                // ── Self-Learning ─────────────────────────────
                UseAutoCalib     = false;
                CalibWindow      = 20;
                CalibBadR        = 0.0;
                CalibGoodR       = 0.7;
                CalibStepQ       = 0.05;
                CalibCooldown    = 5;
                CalibMinQ        = 0.1;
                CalibMaxQ        = 0.9;

                // ── Visual ────────────────────────────────────
                ShowBands        = true;
                ShowSignals      = true;
                ShowBackground   = false;

                // ── Alerts ────────────────────────────────────
                EnableAlerts     = true;

                // Add the single SuperTrend plot
                AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Line, "SuperTrend");
            }
            else if (State == State.Configure)
            {
                // Nothing extra needed
            }
            else if (State == State.DataLoaded)
            {
                ResolvePreset();

                // Sort TP inputs so tp1 <= tp2 <= tp3
                double a = Tp1R, b2 = Tp2R, c = Tp3R;
                fixedTp1R = Math.Min(a, Math.Min(b2, c));
                fixedTp3R = Math.Max(a, Math.Max(b2, c));
                fixedTp2R = a + b2 + c - fixedTp1R - fixedTp3R;

                effectiveQualityStrength = QualityStrength;

                // Warmup
                int maxLen = Math.Max(effectiveAtrLen,
                             Math.Max(AtrBaselineLen,
                             Math.Max(effectiveErLen,
                             Math.Max(effectiveRsiLen,
                             Math.Max(VolLen,
                             Math.Max(PivotLen * 2 + 1,
                             Math.Max(TqiMomLen, TqiStructLen)))))));
                warmupBars = Math.Max(WARMUP_FLOOR, maxLen) + 10;

                // Reset state
                lowerBand     = double.NaN;
                upperBand     = double.NaN;
                stTrend       = 1;
                prevStTrend   = 1;
                trendStartBar = 0;
                activeMultSm  = double.NaN;
                passiveMultSm = double.NaN;
                prevBarTqi    = 0.5;

                signalRBuffer = new List<double>();
                gridCount     = new int[9];
                gridEwmR      = new double[9];

                allTimeRSum    = 0; allTimeCount   = 0;
                allTimePeak    = 0; allTimeTrough  = 0;
                allTimeCumR    = 0;
                curWinStreak   = 0; curLossStreak  = 0;
                maxWinStreak   = 0; maxLossStreak  = 0;
                signalsSinceCalib = 0;

                tradeDir     = 0;
                lastPivotHigh = double.NaN;
                lastPivotLow  = double.NaN;
            }
        }

        // =====================================================
        // PRESET RESOLUTION
        // =====================================================
        private void ResolvePreset()
        {
            string preset = Preset;

            // Auto: pick based on bar period in minutes
            if (preset == "Auto")
            {
                int mins = BarsPeriodToMinutes();
                preset = mins <= 5 ? "Scalping" : (mins <= 240 ? "Default" : "Swing");
            }

            switch (preset)
            {
                case "Scalping":
                    effectiveAtrLen   = 10;  effectiveBaseMult = 1.5;
                    effectiveErLen    = 14;  effectiveRsiLen   = 9;
                    effectiveSlMult   = 1.0; break;
                case "Default":
                    effectiveAtrLen   = 14;  effectiveBaseMult = 2.0;
                    effectiveErLen    = 20;  effectiveRsiLen   = 14;
                    effectiveSlMult   = 1.5; break;
                case "Swing":
                    effectiveAtrLen   = 21;  effectiveBaseMult = 2.5;
                    effectiveErLen    = 30;  effectiveRsiLen   = 21;
                    effectiveSlMult   = 2.0; break;
                case "Crypto 24/7":
                    effectiveAtrLen   = 14;  effectiveBaseMult = 2.8;
                    effectiveErLen    = 20;  effectiveRsiLen   = 14;
                    effectiveSlMult   = 2.5; break;
                default: // "Custom" or anything else
                    effectiveAtrLen   = AtrLen;
                    effectiveBaseMult = BaseMultiplier;
                    effectiveErLen    = ErLength;
                    effectiveRsiLen   = RsiLen;
                    effectiveSlMult   = SlAtrMult;
                    break;
            }
        }

        private int BarsPeriodToMinutes()
        {
            switch (BarsPeriod.BarsPeriodType)
            {
                case BarsPeriodType.Minute:  return BarsPeriod.Value;
                case BarsPeriodType.Hour:    return BarsPeriod.Value * 60;
                case BarsPeriodType.Day:     return BarsPeriod.Value * 1440;
                case BarsPeriodType.Week:    return BarsPeriod.Value * 10080;
                default:                     return 60; // treat unknown as 60m → Default
            }
        }

        // =====================================================
        // MAIN CALCULATION
        // =====================================================
        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(effectiveAtrLen, Math.Max(effectiveErLen, Math.Max(AtrBaselineLen, Math.Max(TqiStructLen, TqiMomLen)))))
                return;

            // ── 1. ATR & baseline ─────────────────────────────
            double rawAtr     = ATR(effectiveAtrLen)[0];
            double atrBaseline = SMA(ATR(effectiveAtrLen), AtrBaselineLen)[0];
            double volRatio    = SafeDiv(rawAtr, atrBaseline, 1.0);
            curVolRatio        = volRatio;

            // ── 2. Efficiency Ratio ───────────────────────────
            double erValue = CalcEfficiencyRatio(effectiveErLen);
            curEr = erValue;

            // ── 3. Effective ATR ──────────────────────────────
            double atrValue = UseEffAtr ? rawAtr * (0.5 + 0.5 * erValue) : rawAtr;

            // ── 4. TQI ────────────────────────────────────────
            double tqi = CalcTqi(erValue, volRatio);
            curTqi = tqi;

            // ── 5. RSI ────────────────────────────────────────
            double rsiVal = (CurrentBar >= effectiveRsiLen + 3) ? RSI(effectiveRsiLen, 3)[0] : 50.0;
            curRsi = rsiVal;

            // ── 6. Volume Z ───────────────────────────────────
            bool hasVolume = Volume[0] > 0;
            double volZ = 0.0;
            if (hasVolume && CurrentBar >= VolLen)
            {
                double vMean = SMA(Volume, VolLen)[0];
                double vStd  = StdDev(Volume, VolLen)[0];
                volZ = SafeDiv(Volume[0] - vMean, vStd, 0.0);
            }
            curVolZ = volZ;

            // ── 7. Adaptive multipliers ───────────────────────
            double legacyAdapt = UseAdaptive ? (1.0 + AdaptStrength * (0.5 - erValue)) : 1.0;

            if (!UseAutoCalib)
                effectiveQualityStrength = QualityStrength;

            double qualityDeviation = UseTqi ? Math.Pow(1.0 - tqi, QualityCurve) : 0.5;
            double tqiMult = 1.0 - effectiveQualityStrength + effectiveQualityStrength * (0.6 + 0.8 * qualityDeviation);
            double symMult = effectiveBaseMult * legacyAdapt * tqiMult;

            double activeMultRaw  = symMult;
            double passiveMultRaw = symMult;
            if (UseTqi && UseAsymBands)
            {
                activeMultRaw  = symMult * (1.0 - AsymStrength * tqi * 0.3);
                passiveMultRaw = symMult * (1.0 + AsymStrength * tqi * 0.4);
            }

            // EWMA smooth multipliers
            if (double.IsNaN(activeMultSm))
                activeMultSm = activeMultRaw;
            else
                activeMultSm = MultSmooth
                    ? activeMultSm  * (1.0 - MULT_SMOOTH_ALPHA) + activeMultRaw  * MULT_SMOOTH_ALPHA
                    : activeMultRaw;

            if (double.IsNaN(passiveMultSm))
                passiveMultSm = passiveMultRaw;
            else
                passiveMultSm = MultSmooth
                    ? passiveMultSm * (1.0 - MULT_SMOOTH_ALPHA) + passiveMultRaw * MULT_SMOOTH_ALPHA
                    : passiveMultRaw;

            double activeMult  = activeMultSm;
            double passiveMult = passiveMultSm;

            // ── 8. Asymmetric SuperTrend ──────────────────────
            int prevTrend = prevStTrend;

            double lowerMult = (prevTrend == 1) ? activeMult  : passiveMult;
            double upperMult = (prevTrend == 1) ? passiveMult : activeMult;

            double lowerBandRaw = Close[0] - lowerMult * atrValue;
            double upperBandRaw = Close[0] + upperMult * atrValue;

            double prevLower = lowerBand;
            double prevUpper = upperBand;

            if (double.IsNaN(lowerBand))
                lowerBand = lowerBandRaw;
            else
                lowerBand = (CurrentBar > 0 && Close[1] > prevLower)
                    ? Math.Max(lowerBandRaw, prevLower)
                    : lowerBandRaw;

            if (double.IsNaN(upperBand))
                upperBand = upperBandRaw;
            else
                upperBand = (CurrentBar > 0 && Close[1] < prevUpper)
                    ? Math.Min(upperBandRaw, prevUpper)
                    : upperBandRaw;

            // Flip detection
            bool priceFlipUp   = (prevTrend == -1) && (Close[0] > (double.IsNaN(prevUpper) ? upperBand : prevUpper));
            bool priceFlipDown = (prevTrend ==  1) && (Close[0] < (double.IsNaN(prevLower) ? lowerBand : prevLower));

            int trendAge = CurrentBar - trendStartBar;

            // Character-flip
            bool charFlipCondBase = UseCharFlip && UseTqi
                && prevBarTqi > CharFlipHigh
                && tqi < CharFlipLow
                && trendAge >= CharFlipMinAge;
            // In Pine, sourceInput defaults to close so charFlipDown/Up rarely fire unless source != close
            bool charFlipDown = charFlipCondBase && (prevTrend ==  1) && (Close[0] < Close[0]);  // always false with src=close
            bool charFlipUp   = charFlipCondBase && (prevTrend == -1) && (Close[0] > Close[0]);  // always false with src=close

            bool finalFlipUp   = priceFlipUp   || charFlipUp;
            bool finalFlipDown = priceFlipDown  || charFlipDown;

            int newTrend = finalFlipUp ? 1 : (finalFlipDown ? -1 : prevTrend);
            if (newTrend != prevTrend)
                trendStartBar = CurrentBar;

            stTrend     = newTrend;
            prevStTrend = stTrend;
            prevBarTqi  = tqi;

            double stLine = (stTrend == 1) ? lowerBand : upperBand;

            bool flipUp   = (stTrend ==  1) && (prevTrend == -1);
            bool flipDown = (stTrend == -1) && (prevTrend ==  1);

            // ── 9. Dynamic TP scale ───────────────────────────
            bool useDynTp = TpMode == "Dynamic";
            double dynScale = useDynTp
                ? CalcDynTpScale(tqi, volRatio, DynTpTqiWeight, DynTpVolWeight, DynTpMinScale, DynTpMaxScale)
                : 1.0;

            double tp1Floor = DynTpFloorR1;
            double tp2Floor = DynTpFloorR1 * (fixedTp2R / Math.Max(fixedTp1R, 0.01));
            double tp3Floor = DynTpFloorR1 * (fixedTp3R / Math.Max(fixedTp1R, 0.01));

            double effTp1R = useDynTp ? Clamp(fixedTp1R * dynScale, tp1Floor, DynTpCeilR3) : fixedTp1R;
            double effTp2R = useDynTp ? Clamp(fixedTp2R * dynScale, tp2Floor, DynTpCeilR3) : fixedTp2R;
            double effTp3R = useDynTp ? Clamp(fixedTp3R * dynScale, tp3Floor, DynTpCeilR3) : fixedTp3R;

            double liveTp1R = Math.Min(effTp1R, Math.Min(effTp2R, effTp3R));
            double liveTp3R = Math.Max(effTp1R, Math.Max(effTp2R, effTp3R));
            double liveTp2R = effTp1R + effTp2R + effTp3R - liveTp1R - liveTp3R;

            // ── 10. Pivot tracking ────────────────────────────
            UpdatePivots();

            // ── 11. Regime grid index ─────────────────────────
            int curErBin  = ErBin(erValue);
            int curVolBin = VolBin(volRatio);
            int curCell   = curErBin * 3 + curVolBin;

            // ── 12. Signal confirmation ───────────────────────
            bool confirmedBuy  = flipUp   && isWarmedUp;
            bool confirmedSell = flipDown && isWarmedUp;

            // ── 13. Trade entry ───────────────────────────────
            if (confirmedBuy)
            {
                double tEntry  = Close[0];
                double slBase  = !double.IsNaN(lastPivotLow) ? lastPivotLow : Low[0];
                double rawSl   = slBase - effectiveSlMult * atrValue;
                double minSl   = tEntry - effectiveSlMult * atrValue;
                double tSl     = Math.Min(rawSl, minSl);
                double risk    = tEntry - tSl;

                tradeDir      = 1;
                tradeEntryBar = CurrentBar;
                tradeEntry    = tEntry;
                tradeSl       = tSl;
                tradeTp1      = tEntry + risk * liveTp1R;
                tradeTp2      = tEntry + risk * liveTp2R;
                tradeTp3      = tEntry + risk * liveTp3R;
                tradeTp1R     = liveTp1R;
                tradeTp2R     = liveTp2R;
                tradeTp3R     = liveTp3R;
                hitTp1 = hitTp2 = hitTp3 = false;
                tradeCellIdx  = curCell;

                // Score breakdown
                CalcScoreBreakdown(true, erValue, atrValue, volZ, rsiVal, out lastScoreMom, out lastScoreEr, out lastScoreVol, out lastScoreRsi, out lastScoreStruct, out lastScoreBreak);
                lastDisplaySide = 1;
                lastSignalBar   = CurrentBar;

                if (ShowSignals)
                    DrawSignalArrow(true, CalcSignalScore(true, erValue, atrValue, volZ, rsiVal));

                if (ShowRisk)
                    DrawRiskLines(useDynTp, liveTp1R, liveTp2R, liveTp3R);

                if (EnableAlerts)
                    Alert("SATS_BUY", Priority.Medium,
                        string.Format("SATS BUY | TQI:{0:F2} ER:{1:F2} Score:{2:F0} SL:{3} TP1:{4} TP2:{5} TP3:{6}",
                            tqi, erValue, CalcSignalScore(true, erValue, atrValue, volZ, rsiVal),
                            tradeSl.ToString("F4"), tradeTp1.ToString("F4"), tradeTp2.ToString("F4"), tradeTp3.ToString("F4")),
                        NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 10, Brushes.LimeGreen, Brushes.Black);
            }

            if (confirmedSell)
            {
                double tEntry  = Close[0];
                double slBase  = !double.IsNaN(lastPivotHigh) ? lastPivotHigh : High[0];
                double rawSl   = slBase + effectiveSlMult * atrValue;
                double minSl   = tEntry + effectiveSlMult * atrValue;
                double tSl     = Math.Max(rawSl, minSl);
                double risk    = tSl - tEntry;

                tradeDir      = -1;
                tradeEntryBar = CurrentBar;
                tradeEntry    = tEntry;
                tradeSl       = tSl;
                tradeTp1      = tEntry - risk * liveTp1R;
                tradeTp2      = tEntry - risk * liveTp2R;
                tradeTp3      = tEntry - risk * liveTp3R;
                tradeTp1R     = liveTp1R;
                tradeTp2R     = liveTp2R;
                tradeTp3R     = liveTp3R;
                hitTp1 = hitTp2 = hitTp3 = false;
                tradeCellIdx  = curCell;

                CalcScoreBreakdown(false, erValue, atrValue, volZ, rsiVal, out lastScoreMom, out lastScoreEr, out lastScoreVol, out lastScoreRsi, out lastScoreStruct, out lastScoreBreak);
                lastDisplaySide = -1;
                lastSignalBar   = CurrentBar;

                if (ShowSignals)
                    DrawSignalArrow(false, CalcSignalScore(false, erValue, atrValue, volZ, rsiVal));

                if (ShowRisk)
                    DrawRiskLines(useDynTp, liveTp1R, liveTp2R, liveTp3R);

                if (EnableAlerts)
                    Alert("SATS_SELL", Priority.Medium,
                        string.Format("SATS SELL | TQI:{0:F2} ER:{1:F2} Score:{2:F0} SL:{3} TP1:{4} TP2:{5} TP3:{6}",
                            tqi, erValue, CalcSignalScore(false, erValue, atrValue, volZ, rsiVal),
                            tradeSl.ToString("F4"), tradeTp1.ToString("F4"), tradeTp2.ToString("F4"), tradeTp3.ToString("F4")),
                        NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert2.wav", 10, Brushes.Red, Brushes.Black);
            }

            // ── 14. Hit detection (not on entry bar) ──────────
            if (tradeDir != 0 && !double.IsNaN(tradeEntry) && !double.IsNaN(tradeSl) && CurrentBar > tradeEntryBar)
            {
                bool tp1Reached = (tradeDir ==  1) ? High[0] >= tradeTp1 : Low[0] <= tradeTp1;
                bool tp2Reached = (tradeDir ==  1) ? High[0] >= tradeTp2 : Low[0] <= tradeTp2;
                bool tp3Reached = (tradeDir ==  1) ? High[0] >= tradeTp3 : Low[0] <= tradeTp3;
                bool slHit      = (tradeDir ==  1) ? Low[0]  <= tradeSl  : High[0] >= tradeSl;
                int  tradeAge   = CurrentBar - tradeEntryBar;
                bool timeoutHit = tradeAge >= TradeMaxAge;

                if (tp1Reached && !hitTp1) { hitTp1 = true; if (ShowHits && ShowRisk) MarkTpHit(1); }
                if (tp2Reached && !hitTp2) { hitTp2 = true; if (ShowHits && ShowRisk) MarkTpHit(2); }
                if (tp3Reached && !hitTp3) { hitTp3 = true; if (ShowHits && ShowRisk) MarkTpHit(3); }

                if (hitTp3 || slHit || timeoutHit)
                {
                    double useTp1R = double.IsNaN(tradeTp1R) ? fixedTp1R : tradeTp1R;
                    double useTp2R = double.IsNaN(tradeTp2R) ? fixedTp2R : tradeTp2R;
                    double useTp3R = double.IsNaN(tradeTp3R) ? fixedTp3R : tradeTp3R;

                    double realizedR;
                    if (hitTp3 || slHit)
                        realizedR = CalcRealizedR(slHit, hitTp1, hitTp2, hitTp3, useTp1R, useTp2R, useTp3R);
                    else
                        realizedR = CalcTimeoutR(hitTp1, hitTp2, hitTp3, useTp1R, useTp2R, useTp3R);

                    realizedR = Clamp(realizedR, -1.0, useTp3R);

                    // Push to buffer
                    signalRBuffer.Add(realizedR);
                    while (signalRBuffer.Count > MAX_HISTORY_SIGS)
                        signalRBuffer.RemoveAt(0);

                    // Update all-time stats
                    allTimeRSum   += realizedR;
                    allTimeCount  += 1;
                    allTimeCumR   += realizedR;
                    if (allTimeCumR > allTimePeak)   allTimePeak  = allTimeCumR;
                    double curDD  = allTimeCumR - allTimePeak;
                    if (curDD < allTimeTrough)        allTimeTrough = curDD;

                    if (realizedR > 0)
                    {
                        curWinStreak++;
                        curLossStreak = 0;
                        if (curWinStreak > maxWinStreak) maxWinStreak = curWinStreak;
                    }
                    else
                    {
                        curLossStreak++;
                        curWinStreak = 0;
                        if (curLossStreak > maxLossStreak) maxLossStreak = curLossStreak;
                    }

                    UpdateGridCell(tradeCellIdx, realizedR);
                    signalsSinceCalib++;

                    // Auto-calibration
                    if (UseAutoCalib && signalRBuffer.Count >= CalibWindow && signalsSinceCalib >= CalibCooldown)
                    {
                        int    n       = signalRBuffer.Count;
                        int    startI  = Math.Max(0, n - CalibWindow);
                        double sumR    = 0.0;
                        for (int i = startI; i < n; i++) sumR += signalRBuffer[i];
                        double postAvgR = sumR / (n - startI);

                        if (postAvgR < CalibBadR)
                        {
                            double drift = effectiveQualityStrength > QualityStrength ? -CalibStepQ : CalibStepQ;
                            effectiveQualityStrength = Clamp(effectiveQualityStrength + drift, CalibMinQ, CalibMaxQ);
                            signalsSinceCalib = 0;
                        }
                        else if (postAvgR > CalibGoodR)
                        {
                            signalsSinceCalib = 0;
                        }
                    }

                    // Close trade
                    if (ShowRisk) RemoveRiskLines();
                    tradeDir     = 0;
                    tradeCellIdx = -1;
                }
                else if (ShowRisk)
                {
                    // Update line endpoints each bar
                    UpdateRiskLineEndpoints();
                }
            }
            else if (tradeDir != 0 && ShowRisk)
            {
                UpdateRiskLineEndpoints();
            }

            // ── 15. Plot SuperTrend ───────────────────────────
            if (isWarmedUp && ShowBands)
            {
                Values[0][0] = stLine;
                PlotBrushes[0][0] = (stTrend == 1) ? Brushes.LimeGreen : Brushes.Red;
            }
            else
            {
                Values[0][0] = double.NaN;
            }

            // ── 16. Background ────────────────────────────────
            if (ShowBackground && isWarmedUp)
            {
                BackBrushes[0] = (stTrend == 1)
                    ? new SolidColorBrush(Color.FromArgb(15, 0, 230, 118))
                    : new SolidColorBrush(Color.FromArgb(15, 255, 82, 82));
            }

            // ── 17. Print stats on last bar ───────────────────
            if (IsLastBarOnChart())
            {
                PrintStats(erValue, rsiVal, volZ, dynScale, useDynTp, liveTp1R, liveTp2R, liveTp3R);
            }
        }

        // =====================================================
        // HELPER — Calc Efficiency Ratio
        // =====================================================
        private double CalcEfficiencyRatio(int len)
        {
            if (CurrentBar < len) return 0.0;
            double change = Math.Abs(Close[0] - Close[len]);
            double volatility = 0.0;
            for (int i = 0; i < len; i++)
                volatility += Math.Abs(Close[i] - Close[i + 1]);
            return SafeDiv(change, volatility, 0.0);
        }

        // =====================================================
        // HELPER — Calc TQI
        // =====================================================
        private double CalcTqi(double erValue, double volRatio)
        {
            if (!UseTqi) return 0.5;

            double tqiEr = Clamp(erValue, 0.0, 1.0);

            // Volume component
            double tqiVol;
            bool hasVol = Volume[0] > 0 && CurrentBar >= VolLen;
            if (hasVol)
            {
                double vMean = SMA(Volume, VolLen)[0];
                double vStd  = StdDev(Volume, VolLen)[0];
                double volZ  = SafeDiv(Volume[0] - vMean, vStd, 0.0);
                tqiVol = MapClamp(volZ, -1.0, 2.0, 0.0, 1.0);
            }
            else
            {
                tqiVol = MapClamp(volRatio, 0.6, 1.8, 0.0, 1.0);
            }

            // Structure component
            double tqiStruct = 0.5;
            if (CurrentBar >= TqiStructLen)
            {
                double structHi   = MAX(High, TqiStructLen)[0];
                double structLo   = MIN(Low,  TqiStructLen)[0];
                double structRange = structHi - structLo;
                double pricePos   = SafeDiv(Close[0] - structLo, structRange, 0.5);
                tqiStruct = Clamp(Math.Abs(pricePos - 0.5) * 2.0, 0.0, 1.0);
            }

            // Momentum persistence component
            double tqiMom = 0.5;
            if (CurrentBar >= TqiMomLen)
            {
                double windowChange = Close[0] - Close[TqiMomLen];
                int alignedBars = 0;
                for (int i = 0; i < TqiMomLen; i++)
                {
                    double barChange = Close[i] - Close[i + 1];
                    if ((windowChange > 0 && barChange > 0) || (windowChange < 0 && barChange < 0))
                        alignedBars++;
                }
                tqiMom = (double)alignedBars / TqiMomLen;
            }

            double wSum = TqiWeightEr + TqiWeightVol + TqiWeightStruct + TqiWeightMom;
            double wDen = wSum > 0 ? wSum : 1.0;
            double tqiRaw = (tqiEr * TqiWeightEr + tqiVol * TqiWeightVol + tqiStruct * TqiWeightStruct + tqiMom * TqiWeightMom) / wDen;
            return Clamp(tqiRaw, 0.0, 1.0);
        }

        // =====================================================
        // HELPER — Pivot tracking
        // =====================================================
        private void UpdatePivots()
        {
            int pl = PivotLen;
            if (CurrentBar < pl * 2 + 1) return;

            // Pivot High: high[pl] must be strictly > all surrounding bars in [0..pl-1] and [pl+1..pl*2]
            double ph = High[pl];
            bool isPivH = true;
            for (int i = 0; i < pl; i++)
                if (High[i] >= ph) { isPivH = false; break; }
            if (isPivH)
                for (int i = pl + 1; i <= pl * 2; i++)
                    if (High[i] >= ph) { isPivH = false; break; }
            if (isPivH) lastPivotHigh = ph;

            // Pivot Low
            double plo = Low[pl];
            bool isPivL = true;
            for (int i = 0; i < pl; i++)
                if (Low[i] <= plo) { isPivL = false; break; }
            if (isPivL)
                for (int i = pl + 1; i <= pl * 2; i++)
                    if (Low[i] <= plo) { isPivL = false; break; }
            if (isPivL) lastPivotLow = plo;
        }

        // =====================================================
        // HELPER — Signal score
        // =====================================================
        private void CalcScoreBreakdown(bool isBuy, double erValue, double atrValue, double volZ, double rsiVal,
            out double momScore, out double erScore, out double vScore, out double rsiScore, out double structScore, out double breakScore)
        {
            // Momentum score: uses close[3] vs close[0]; need at least 3 bars back
            double dirMove = 0.0;
            if (CurrentBar >= 3)
                dirMove = isBuy ? (Close[3] - Close[0]) : (Close[0] - Close[3]);
            momScore = MapClamp(SafeDiv(dirMove, atrValue, 0.0), 0.3, 2.0, 0.0, 17.0);

            erScore = MapClamp(erValue, 0.15, 0.7, 0.0, 17.0);

            bool hasVol = Volume[0] > 0 && CurrentBar >= VolLen;
            vScore = (UseVol && hasVol) ? MapClamp(volZ, 0.0, 3.0, 0.0, 17.0) : BYPASS_SCORE;

            double rsiDepth;
            if (UseRsi && CurrentBar >= RsiLookback)
            {
                double rsiLow  = double.MaxValue;
                double rsiHigh = double.MinValue;
                for (int i = 0; i < RsiLookback; i++)
                {
                    double r = (CurrentBar >= effectiveRsiLen + 3 + i) ? RSI(effectiveRsiLen, 3)[i] : 50.0;
                    if (r < rsiLow)  rsiLow  = r;
                    if (r > rsiHigh) rsiHigh = r;
                }
                rsiDepth = isBuy ? Math.Max(0.0, RsiOS - rsiLow) : Math.Max(0.0, rsiHigh - RsiOB);
                rsiScore = MapClamp(rsiDepth, 0.0, 15.0, 0.0, 17.0);
            }
            else
            {
                rsiScore = BYPASS_SCORE;
            }

            if (UseStructure)
            {
                double pivDist = 0.0;
                if (isBuy && !double.IsNaN(lastPivotLow))
                    pivDist = Math.Abs(Close[0] - lastPivotLow);
                else if (!isBuy && !double.IsNaN(lastPivotHigh))
                    pivDist = Math.Abs(lastPivotHigh - Close[0]);
                structScore = MapClampInv(SafeDiv(pivDist, atrValue, 0.0), 0.0, 1.5, 16.0, 6.0);
            }
            else
            {
                structScore = BYPASS_SCORE;
            }

            double breakDepth;
            if (CurrentBar > 0)
            {
                breakDepth = isBuy
                    ? Math.Max(0.0, (!double.IsNaN(upperBand) ? upperBand : 0.0) - (CurrentBar > 0 ? Close[1] : Close[0]))
                    : Math.Max(0.0, (CurrentBar > 0 ? Close[1] : Close[0]) - (!double.IsNaN(lowerBand) ? lowerBand : 0.0));
            }
            else
            {
                breakDepth = 0.0;
            }
            breakScore = MapClamp(SafeDiv(breakDepth, atrValue, 0.0), 0.0, 1.0, 0.0, 16.0);
        }

        private double CalcSignalScore(bool isBuy, double erValue, double atrValue, double volZ, double rsiVal)
        {
            double m, e, v, r, s, b;
            CalcScoreBreakdown(isBuy, erValue, atrValue, volZ, rsiVal, out m, out e, out v, out r, out s, out b);
            return m + e + v + r + s + b;
        }

        // =====================================================
        // HELPER — Dynamic TP scale
        // =====================================================
        private double CalcDynTpScale(double tqiVal, double volRatioVal, double tqiWeight, double volWeight, double minScale, double maxScale)
        {
            double tqiComp  = Clamp(tqiVal, 0.0, 1.0);
            double volComp  = Clamp(MapClamp(volRatioVal, 0.5, 2.0, 0.0, 1.0), 0.0, 1.0);
            double wSum     = tqiWeight + volWeight;
            double wDen     = wSum > 0 ? wSum : 1.0;
            double rawScale = (tqiComp * tqiWeight + volComp * volWeight) / wDen;
            return minScale + rawScale * (maxScale - minScale);
        }

        // =====================================================
        // HELPER — R calculations
        // =====================================================
        private double CalcRealizedR(bool slHit, bool tp1H, bool tp2H, bool tp3H, double tp1R, double tp2R, double tp3R)
        {
            if (tp3H)
                return (tp1R + tp2R + tp3R) / 3.0;

            if (slHit)
            {
                double taken     = 0.0;
                double remaining = 1.0;
                if (tp1H) { taken += (1.0 / 3.0) * tp1R; remaining -= 1.0 / 3.0; }
                if (tp2H) { taken += (1.0 / 3.0) * tp2R; remaining -= 1.0 / 3.0; }
                return taken + remaining * (-1.0);
            }

            return 0.0;
        }

        private double CalcTimeoutR(bool tp1H, bool tp2H, bool tp3H, double tp1R, double tp2R, double tp3R)
        {
            double taken = 0.0;
            if (tp1H) taken += (1.0 / 3.0) * tp1R;
            if (tp2H) taken += (1.0 / 3.0) * tp2R;
            if (tp3H) taken += (1.0 / 3.0) * tp3R;
            return taken;
        }

        // =====================================================
        // HELPER — Regime grid
        // =====================================================
        private int ErBin(double er)
            => er < ER_LOW_THRESH ? 0 : (er < ER_HIGH_THRESH ? 1 : 2);

        private int VolBin(double vr)
            => vr < VOL_LOW_THRESH ? 0 : (vr < VOL_HIGH_THRESH ? 1 : 2);

        private void UpdateGridCell(int idx, double r)
        {
            if (idx < 0 || idx >= 9) return;
            int   c       = gridCount[idx] + 1;
            double prevEwm = gridEwmR[idx];
            double newEwm  = (c == 1) ? r : prevEwm * (1.0 - EWMA_ALPHA) + r * EWMA_ALPHA;
            gridCount[idx] = c;
            gridEwmR[idx]  = newEwm;
        }

        // =====================================================
        // HELPER — Draw objects
        // =====================================================
        private void DrawSignalArrow(bool isBuy, double score)
        {
            string tag = isBuy ? "SATS_BUY_" + CurrentBar : "SATS_SELL_" + CurrentBar;
            if (isBuy)
                Draw.ArrowUp(this, tag, false, 0, Low[0] - TickSize * 4, Brushes.LimeGreen);
            else
                Draw.ArrowDown(this, tag, false, 0, High[0] + TickSize * 4, Brushes.Red);
        }

        private void DrawRiskLines(bool useDynTp, double tp1R, double tp2R, double tp3R)
        {
            if (double.IsNaN(tradeEntry) || double.IsNaN(tradeSl)) return;

            RemoveRiskLines();

            int startBar = 0;
            int endBar   = -LabelOffset;  // negative barsAgo = future bars is not supported; use 0

            // In NT8 Draw.Line barsAgo: positive = past bars ago. We draw from entry to current bar.
            // Since we're on the entry bar, startBarsAgo=0, endBarsAgo=0 (will be updated each bar)
            Draw.Line(this, TAG_ENTRY, false, 0, tradeEntry, 0, tradeEntry, Brushes.Gray,    DashStyleHelper.Solid,  2);
            Draw.Line(this, TAG_SL,    false, 0, tradeSl,    0, tradeSl,    Brushes.Red,     DashStyleHelper.Solid,  2);
            Draw.Line(this, TAG_TP1,   false, 0, tradeTp1,   0, tradeTp1,   Brushes.LimeGreen, DashStyleHelper.Dash, 1);
            Draw.Line(this, TAG_TP2,   false, 0, tradeTp2,   0, tradeTp2,   Brushes.LimeGreen, DashStyleHelper.Dash, 1);
            Draw.Line(this, TAG_TP3,   false, 0, tradeTp3,   0, tradeTp3,   Brushes.LimeGreen, DashStyleHelper.Dash, 2);
        }

        private void UpdateRiskLineEndpoints()
        {
            // NT8 Draw.Line with tag reuse updates in-place at the new bar
            // Re-draw from entryBar to current bar
            if (double.IsNaN(tradeEntry)) return;

            int barsAgoEntry = CurrentBar - tradeEntryBar;

            try
            {
                Draw.Line(this, TAG_ENTRY, false, barsAgoEntry, tradeEntry, 0, tradeEntry, Brushes.Gray,     DashStyleHelper.Solid,  2);
                Draw.Line(this, TAG_SL,    false, barsAgoEntry, tradeSl,    0, tradeSl,    Brushes.Red,      DashStyleHelper.Solid,  2);
                Draw.Line(this, TAG_TP1,   false, barsAgoEntry, tradeTp1,   0, tradeTp1,   Brushes.LimeGreen, DashStyleHelper.Dash,  1);
                Draw.Line(this, TAG_TP2,   false, barsAgoEntry, tradeTp2,   0, tradeTp2,   Brushes.LimeGreen, DashStyleHelper.Dash,  1);
                Draw.Line(this, TAG_TP3,   false, barsAgoEntry, tradeTp3,   0, tradeTp3,   Brushes.LimeGreen, DashStyleHelper.Dash,  2);
            }
            catch { /* ignore if lines not yet created */ }
        }

        private void MarkTpHit(int tpNum)
        {
            string tag   = tpNum == 1 ? TAG_TP1 : (tpNum == 2 ? TAG_TP2 : TAG_TP3);
            double price = tpNum == 1 ? tradeTp1 : (tpNum == 2 ? tradeTp2 : tradeTp3);
            int    barsAgoEntry = CurrentBar - tradeEntryBar;
            Brush  hitBrush = new SolidColorBrush(Color.FromRgb(64, 224, 208)); // turquoise
            hitBrush.Freeze();
            Draw.Line(this, tag, false, barsAgoEntry, price, 0, price, hitBrush, DashStyleHelper.Solid, 2);
        }

        private void RemoveRiskLines()
        {
            RemoveDrawObject(TAG_ENTRY);
            RemoveDrawObject(TAG_SL);
            RemoveDrawObject(TAG_TP1);
            RemoveDrawObject(TAG_TP2);
            RemoveDrawObject(TAG_TP3);
        }

        // =====================================================
        // HELPER — Performance stats print
        // =====================================================
        private void PrintStats(double erValue, double rsiVal, double volZ, double dynScale, bool useDynTp, double tp1R, double tp2R, double tp3R)
        {
            string trendStr  = stTrend == 1 ? "Bullish" : "Bearish";
            string regimeStr = erValue >= ER_HIGH_THRESH ? "Trending" : (erValue >= ER_LOW_THRESH ? "Mixed" : "Choppy");
            string volStr    = curVolRatio < VOL_LOW_THRESH ? "Low Vol" : (curVolRatio < VOL_HIGH_THRESH ? "Normal" : "High Vol");

            Print("=== SATS v1.9.0 | Last Bar Stats ===");
            Print(string.Format("  Trend:    {0}  |  Regime: {1} / {2}", trendStr, regimeStr, volStr));
            Print(string.Format("  TQI:      {0:F3}  |  ER: {1:F3}  |  RSI: {2:F1}  |  Vol Z: {3:F2}", curTqi, erValue, rsiVal, volZ));
            Print(string.Format("  TP Mode:  {0}  |  Scale: x{1:F2}  |  Live R: {2:F1}/{3:F1}/{4:F1}", useDynTp ? "Dynamic" : "Fixed", dynScale, tp1R, tp2R, tp3R));
            Print(string.Format("  Q.Strength (effective): {0:F3}", effectiveQualityStrength));

            if (allTimeCount > 0)
            {
                double winRate = 0.0;
                int    wins    = 0;
                foreach (double r in signalRBuffer) if (r > 0) wins++;
                winRate = signalRBuffer.Count > 0 ? (double)wins / signalRBuffer.Count : 0.0;

                double avgR = allTimeCount > 0 ? allTimeRSum / allTimeCount : 0.0;

                Print("=== Performance Stats ===");
                Print(string.Format("  Signals: {0}  |  Win Rate: {1:P1}  |  Avg R: {2:+0.00;-0.00}", allTimeCount, winRate, avgR));
                Print(string.Format("  All-Time DD: {0:F2}R  |  Streak W:{1}/{2}  L:{3}/{4}", allTimeTrough, curWinStreak, maxWinStreak, curLossStreak, maxLossStreak));

                if (tradeCellIdx >= 0)
                    Print(string.Format("  Regime Grid Edge [{0}]: {1:+0.00;-0.00}R ({2} trades)", tradeCellIdx, gridEwmR[tradeCellIdx], gridCount[tradeCellIdx]));
            }
            else
            {
                Print("  No closed trades yet.");
            }
            Print("=====================================");
        }

        // =====================================================
        // MATH HELPERS
        // =====================================================
        private static double SafeDiv(double num, double den, double fallback = 0.0)
            => (den != 0 && !double.IsNaN(num) && !double.IsNaN(den)) ? num / den : fallback;

        private static double Clamp(double v, double lo, double hi)
            => Math.Max(lo, Math.Min(hi, v));

        private static double MapClamp(double v, double inLo, double inHi, double outLo, double outHi)
        {
            double t = Clamp(SafeDiv(v - inLo, inHi - inLo, 0.0), 0.0, 1.0);
            return outLo + t * (outHi - outLo);
        }

        private static double MapClampInv(double v, double inLo, double inHi, double outHigh, double outLow)
        {
            double t = Clamp(SafeDiv(v - inLo, inHi - inLo, 0.0), 0.0, 1.0);
            return outHigh - t * (outHigh - outLow);
        }

        // =====================================================
        // PROPERTIES
        // =====================================================

        #region Main Settings
        [NinjaScriptProperty]
        [Display(Name = "Preset", GroupName = "Main Settings", Order = 1,
            Description = "Auto picks Scalping/Default/Swing by timeframe. Custom uses raw inputs.")]
        public string Preset { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "ATR Length", GroupName = "Main Settings", Order = 2)]
        public int AtrLen { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 5.0)]
        [Display(Name = "Base Band Width (xATR)", GroupName = "Main Settings", Order = 3)]
        public double BaseMultiplier { get; set; }
        #endregion

        #region Adaptive Engine
        [NinjaScriptProperty]
        [Display(Name = "Enable Vol-Adaptive Bands", GroupName = "Adaptive Engine", Order = 1)]
        public bool UseAdaptive { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Efficiency Window", GroupName = "Adaptive Engine", Order = 2)]
        public int ErLength { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Adaptation Strength", GroupName = "Adaptive Engine", Order = 3)]
        public double AdaptStrength { get; set; }

        [NinjaScriptProperty]
        [Range(20, 500)]
        [Display(Name = "ATR Baseline Length", GroupName = "Adaptive Engine", Order = 4)]
        public int AtrBaselineLen { get; set; }
        #endregion

        #region Trend Quality Engine
        [NinjaScriptProperty]
        [Display(Name = "Enable Trend Quality Engine", GroupName = "Trend Quality Engine", Order = 1)]
        public bool UseTqi { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Quality Influence", GroupName = "Trend Quality Engine", Order = 2)]
        public double QualityStrength { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 3.0)]
        [Display(Name = "Quality Curve Power", GroupName = "Trend Quality Engine", Order = 3)]
        public double QualityCurve { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Smooth Adaptive Multipliers", GroupName = "Trend Quality Engine", Order = 4)]
        public bool MultSmooth { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Asymmetric Bands", GroupName = "Trend Quality Engine", Order = 5)]
        public bool UseAsymBands { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Asymmetry Strength", GroupName = "Trend Quality Engine", Order = 6)]
        public double AsymStrength { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Efficiency-Weighted ATR", GroupName = "Trend Quality Engine", Order = 7)]
        public bool UseEffAtr { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Character-Flip Detection", GroupName = "Trend Quality Engine", Order = 8)]
        public bool UseCharFlip { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "Char-Flip: Min Trend Age", GroupName = "Trend Quality Engine", Order = 9)]
        public int CharFlipMinAge { get; set; }

        [NinjaScriptProperty]
        [Range(0.3, 0.9)]
        [Display(Name = "Char-Flip: High TQI", GroupName = "Trend Quality Engine", Order = 10)]
        public double CharFlipHigh { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 0.5)]
        [Display(Name = "Char-Flip: Low TQI", GroupName = "Trend Quality Engine", Order = 11)]
        public double CharFlipLow { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Weight: Efficiency", GroupName = "Trend Quality Engine", Order = 12)]
        public double TqiWeightEr { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Weight: Volatility Regime", GroupName = "Trend Quality Engine", Order = 13)]
        public double TqiWeightVol { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Weight: Structure", GroupName = "Trend Quality Engine", Order = 14)]
        public double TqiWeightStruct { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Weight: Momentum Persist", GroupName = "Trend Quality Engine", Order = 15)]
        public double TqiWeightMom { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Structure Window", GroupName = "Trend Quality Engine", Order = 16)]
        public int TqiStructLen { get; set; }

        [NinjaScriptProperty]
        [Range(3, 50)]
        [Display(Name = "Momentum Persist Window", GroupName = "Trend Quality Engine", Order = 17)]
        public int TqiMomLen { get; set; }
        #endregion

        #region Signal Filters
        [NinjaScriptProperty]
        [Display(Name = "Use Structure in Score", GroupName = "Signal Filters", Order = 1)]
        public bool UseStructure { get; set; }

        [NinjaScriptProperty]
        [Range(2, 10)]
        [Display(Name = "Pivot Strength", GroupName = "Signal Filters", Order = 2)]
        public int PivotLen { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use RSI in Score", GroupName = "Signal Filters", Order = 3)]
        public bool UseRsi { get; set; }

        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name = "RSI Length", GroupName = "Signal Filters", Order = 4)]
        public int RsiLen { get; set; }

        [NinjaScriptProperty]
        [Range(55, 90)]
        [Display(Name = "RSI Overbought", GroupName = "Signal Filters", Order = 5)]
        public int RsiOB { get; set; }

        [NinjaScriptProperty]
        [Range(10, 45)]
        [Display(Name = "RSI Oversold", GroupName = "Signal Filters", Order = 6)]
        public int RsiOS { get; set; }

        [NinjaScriptProperty]
        [Range(3, 100)]
        [Display(Name = "RSI Memory (bars)", GroupName = "Signal Filters", Order = 7)]
        public int RsiLookback { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Volume in Score", GroupName = "Signal Filters", Order = 8)]
        public bool UseVol { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Volume Z Window", GroupName = "Signal Filters", Order = 9)]
        public int VolLen { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Min Signal Score (display)", GroupName = "Signal Filters", Order = 10)]
        public int MinScore { get; set; }
        #endregion

        #region Risk Management
        [NinjaScriptProperty]
        [Display(Name = "Show TP/SL Levels", GroupName = "Risk Management", Order = 1)]
        public bool ShowRisk { get; set; }

        [NinjaScriptProperty]
        [Range(0.3, 5.0)]
        [Display(Name = "SL Buffer (xATR)", GroupName = "Risk Management", Order = 2)]
        public double SlAtrMult { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TP Mode (Fixed or Dynamic)", GroupName = "Risk Management", Order = 3)]
        public string TpMode { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 10.0)]
        [Display(Name = "TP1 (R-multiple)", GroupName = "Risk Management", Order = 4)]
        public double Tp1R { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 10.0)]
        [Display(Name = "TP2 (R-multiple)", GroupName = "Risk Management", Order = 5)]
        public double Tp2R { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 10.0)]
        [Display(Name = "TP3 (R-multiple)", GroupName = "Risk Management", Order = 6)]
        public double Tp3R { get; set; }

        [NinjaScriptProperty]
        [Range(10, 100)]
        [Display(Name = "Label Offset (bars)", GroupName = "Risk Management", Order = 7)]
        public int LabelOffset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Mark TP Hits (visual)", GroupName = "Risk Management", Order = 8)]
        public bool ShowHits { get; set; }

        [NinjaScriptProperty]
        [Range(10, 500)]
        [Display(Name = "Trade Timeout (bars)", GroupName = "Risk Management", Order = 9)]
        public int TradeMaxAge { get; set; }
        #endregion

        #region Dynamic TP Settings
        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "TQI Influence on TP", GroupName = "Dynamic TP Settings", Order = 1)]
        public double DynTpTqiWeight { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Volatility Influence on TP", GroupName = "Dynamic TP Settings", Order = 2)]
        public double DynTpVolWeight { get; set; }

        [NinjaScriptProperty]
        [Range(0.2, 1.0)]
        [Display(Name = "Min TP Scale", GroupName = "Dynamic TP Settings", Order = 3)]
        public double DynTpMinScale { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 4.0)]
        [Display(Name = "Max TP Scale", GroupName = "Dynamic TP Settings", Order = 4)]
        public double DynTpMaxScale { get; set; }

        [NinjaScriptProperty]
        [Range(0.2, 2.0)]
        [Display(Name = "TP1 Absolute Floor (R)", GroupName = "Dynamic TP Settings", Order = 5)]
        public double DynTpFloorR1 { get; set; }

        [NinjaScriptProperty]
        [Range(2.0, 20.0)]
        [Display(Name = "TP3 Absolute Ceiling (R)", GroupName = "Dynamic TP Settings", Order = 6)]
        public double DynTpCeilR3 { get; set; }
        #endregion

        #region Self-Learning
        [NinjaScriptProperty]
        [Display(Name = "Enable Auto-Calibration (experimental)", GroupName = "Self-Learning", Order = 1)]
        public bool UseAutoCalib { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Calibration Window", GroupName = "Self-Learning", Order = 2)]
        public int CalibWindow { get; set; }

        [NinjaScriptProperty]
        [Range(-2.0, 1.0)]
        [Display(Name = "Bad-Edge Threshold (R)", GroupName = "Self-Learning", Order = 3)]
        public double CalibBadR { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 5.0)]
        [Display(Name = "Good-Edge Threshold (R)", GroupName = "Self-Learning", Order = 4)]
        public double CalibGoodR { get; set; }

        [NinjaScriptProperty]
        [Range(0.01, 0.3)]
        [Display(Name = "Quality Step", GroupName = "Self-Learning", Order = 5)]
        public double CalibStepQ { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "Calibration Cooldown (signals)", GroupName = "Self-Learning", Order = 6)]
        public int CalibCooldown { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Quality Floor", GroupName = "Self-Learning", Order = 7)]
        public double CalibMinQ { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Quality Ceiling", GroupName = "Self-Learning", Order = 8)]
        public double CalibMaxQ { get; set; }
        #endregion

        #region Visual
        [NinjaScriptProperty]
        [Display(Name = "Show SuperTrend Bands", GroupName = "Visual", Order = 1)]
        public bool ShowBands { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Signal Arrows", GroupName = "Visual", Order = 2)]
        public bool ShowSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Trend Background", GroupName = "Visual", Order = 3)]
        public bool ShowBackground { get; set; }
        #endregion

        #region Alerts
        [NinjaScriptProperty]
        [Display(Name = "Enable Alert Signals", GroupName = "Alerts", Order = 1)]
        public bool EnableAlerts { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private gbSATS[] cachegbSATS;
        public gbSATS gbSATS(string preset, int atrLen, double baseMultiplier, bool useAdaptive, int erLength, double adaptStrength, int atrBaselineLen, bool useTqi, double qualityStrength, double qualityCurve, bool multSmooth, bool useAsymBands, double asymStrength, bool useEffAtr, bool useCharFlip, int charFlipMinAge, double charFlipHigh, double charFlipLow, double tqiWeightEr, double tqiWeightVol, double tqiWeightStruct, double tqiWeightMom, int tqiStructLen, int tqiMomLen, bool useStructure, int pivotLen, bool useRsi, int rsiLen, int rsiOB, int rsiOS, int rsiLookback, bool useVol, int volLen, int minScore, bool showRisk, double slAtrMult, string tpMode, double tp1R, double tp2R, double tp3R, int labelOffset, bool showHits, int tradeMaxAge, double dynTpTqiWeight, double dynTpVolWeight, double dynTpMinScale, double dynTpMaxScale, double dynTpFloorR1, double dynTpCeilR3, bool useAutoCalib, int calibWindow, double calibBadR, double calibGoodR, double calibStepQ, int calibCooldown, double calibMinQ, double calibMaxQ, bool showBands, bool showSignals, bool showBackground, bool enableAlerts)
        {
            return gbSATS(Input, preset, atrLen, baseMultiplier, useAdaptive, erLength, adaptStrength, atrBaselineLen, useTqi, qualityStrength, qualityCurve, multSmooth, useAsymBands, asymStrength, useEffAtr, useCharFlip, charFlipMinAge, charFlipHigh, charFlipLow, tqiWeightEr, tqiWeightVol, tqiWeightStruct, tqiWeightMom, tqiStructLen, tqiMomLen, useStructure, pivotLen, useRsi, rsiLen, rsiOB, rsiOS, rsiLookback, useVol, volLen, minScore, showRisk, slAtrMult, tpMode, tp1R, tp2R, tp3R, labelOffset, showHits, tradeMaxAge, dynTpTqiWeight, dynTpVolWeight, dynTpMinScale, dynTpMaxScale, dynTpFloorR1, dynTpCeilR3, useAutoCalib, calibWindow, calibBadR, calibGoodR, calibStepQ, calibCooldown, calibMinQ, calibMaxQ, showBands, showSignals, showBackground, enableAlerts);
        }

        public gbSATS gbSATS(ISeries<double> input, string preset, int atrLen, double baseMultiplier, bool useAdaptive, int erLength, double adaptStrength, int atrBaselineLen, bool useTqi, double qualityStrength, double qualityCurve, bool multSmooth, bool useAsymBands, double asymStrength, bool useEffAtr, bool useCharFlip, int charFlipMinAge, double charFlipHigh, double charFlipLow, double tqiWeightEr, double tqiWeightVol, double tqiWeightStruct, double tqiWeightMom, int tqiStructLen, int tqiMomLen, bool useStructure, int pivotLen, bool useRsi, int rsiLen, int rsiOB, int rsiOS, int rsiLookback, bool useVol, int volLen, int minScore, bool showRisk, double slAtrMult, string tpMode, double tp1R, double tp2R, double tp3R, int labelOffset, bool showHits, int tradeMaxAge, double dynTpTqiWeight, double dynTpVolWeight, double dynTpMinScale, double dynTpMaxScale, double dynTpFloorR1, double dynTpCeilR3, bool useAutoCalib, int calibWindow, double calibBadR, double calibGoodR, double calibStepQ, int calibCooldown, double calibMinQ, double calibMaxQ, bool showBands, bool showSignals, bool showBackground, bool enableAlerts)
        {
            if (cachegbSATS != null)
                for (int idx = 0; idx < cachegbSATS.Length; idx++)
                    if (cachegbSATS[idx] != null && cachegbSATS[idx].Preset == preset && cachegbSATS[idx].AtrLen == atrLen && cachegbSATS[idx].BaseMultiplier == baseMultiplier && cachegbSATS[idx].UseAdaptive == useAdaptive && cachegbSATS[idx].ErLength == erLength && cachegbSATS[idx].AdaptStrength == adaptStrength && cachegbSATS[idx].AtrBaselineLen == atrBaselineLen && cachegbSATS[idx].UseTqi == useTqi && cachegbSATS[idx].QualityStrength == qualityStrength && cachegbSATS[idx].QualityCurve == qualityCurve && cachegbSATS[idx].MultSmooth == multSmooth && cachegbSATS[idx].UseAsymBands == useAsymBands && cachegbSATS[idx].AsymStrength == asymStrength && cachegbSATS[idx].UseEffAtr == useEffAtr && cachegbSATS[idx].UseCharFlip == useCharFlip && cachegbSATS[idx].CharFlipMinAge == charFlipMinAge && cachegbSATS[idx].CharFlipHigh == charFlipHigh && cachegbSATS[idx].CharFlipLow == charFlipLow && cachegbSATS[idx].TqiWeightEr == tqiWeightEr && cachegbSATS[idx].TqiWeightVol == tqiWeightVol && cachegbSATS[idx].TqiWeightStruct == tqiWeightStruct && cachegbSATS[idx].TqiWeightMom == tqiWeightMom && cachegbSATS[idx].TqiStructLen == tqiStructLen && cachegbSATS[idx].TqiMomLen == tqiMomLen && cachegbSATS[idx].UseStructure == useStructure && cachegbSATS[idx].PivotLen == pivotLen && cachegbSATS[idx].UseRsi == useRsi && cachegbSATS[idx].RsiLen == rsiLen && cachegbSATS[idx].RsiOB == rsiOB && cachegbSATS[idx].RsiOS == rsiOS && cachegbSATS[idx].RsiLookback == rsiLookback && cachegbSATS[idx].UseVol == useVol && cachegbSATS[idx].VolLen == volLen && cachegbSATS[idx].MinScore == minScore && cachegbSATS[idx].ShowRisk == showRisk && cachegbSATS[idx].SlAtrMult == slAtrMult && cachegbSATS[idx].TpMode == tpMode && cachegbSATS[idx].Tp1R == tp1R && cachegbSATS[idx].Tp2R == tp2R && cachegbSATS[idx].Tp3R == tp3R && cachegbSATS[idx].LabelOffset == labelOffset && cachegbSATS[idx].ShowHits == showHits && cachegbSATS[idx].TradeMaxAge == tradeMaxAge && cachegbSATS[idx].DynTpTqiWeight == dynTpTqiWeight && cachegbSATS[idx].DynTpVolWeight == dynTpVolWeight && cachegbSATS[idx].DynTpMinScale == dynTpMinScale && cachegbSATS[idx].DynTpMaxScale == dynTpMaxScale && cachegbSATS[idx].DynTpFloorR1 == dynTpFloorR1 && cachegbSATS[idx].DynTpCeilR3 == dynTpCeilR3 && cachegbSATS[idx].UseAutoCalib == useAutoCalib && cachegbSATS[idx].CalibWindow == calibWindow && cachegbSATS[idx].CalibBadR == calibBadR && cachegbSATS[idx].CalibGoodR == calibGoodR && cachegbSATS[idx].CalibStepQ == calibStepQ && cachegbSATS[idx].CalibCooldown == calibCooldown && cachegbSATS[idx].CalibMinQ == calibMinQ && cachegbSATS[idx].CalibMaxQ == calibMaxQ && cachegbSATS[idx].ShowBands == showBands && cachegbSATS[idx].ShowSignals == showSignals && cachegbSATS[idx].ShowBackground == showBackground && cachegbSATS[idx].EnableAlerts == enableAlerts && cachegbSATS[idx].EqualsInput(input))
                        return cachegbSATS[idx];

            return CacheIndicator<gbSATS>(new gbSATS()
            {
                Preset           = preset,
                AtrLen           = atrLen,
                BaseMultiplier   = baseMultiplier,
                UseAdaptive      = useAdaptive,
                ErLength         = erLength,
                AdaptStrength    = adaptStrength,
                AtrBaselineLen   = atrBaselineLen,
                UseTqi           = useTqi,
                QualityStrength  = qualityStrength,
                QualityCurve     = qualityCurve,
                MultSmooth       = multSmooth,
                UseAsymBands     = useAsymBands,
                AsymStrength     = asymStrength,
                UseEffAtr        = useEffAtr,
                UseCharFlip      = useCharFlip,
                CharFlipMinAge   = charFlipMinAge,
                CharFlipHigh     = charFlipHigh,
                CharFlipLow      = charFlipLow,
                TqiWeightEr      = tqiWeightEr,
                TqiWeightVol     = tqiWeightVol,
                TqiWeightStruct  = tqiWeightStruct,
                TqiWeightMom     = tqiWeightMom,
                TqiStructLen     = tqiStructLen,
                TqiMomLen        = tqiMomLen,
                UseStructure     = useStructure,
                PivotLen         = pivotLen,
                UseRsi           = useRsi,
                RsiLen           = rsiLen,
                RsiOB            = rsiOB,
                RsiOS            = rsiOS,
                RsiLookback      = rsiLookback,
                UseVol           = useVol,
                VolLen           = volLen,
                MinScore         = minScore,
                ShowRisk         = showRisk,
                SlAtrMult        = slAtrMult,
                TpMode           = tpMode,
                Tp1R             = tp1R,
                Tp2R             = tp2R,
                Tp3R             = tp3R,
                LabelOffset      = labelOffset,
                ShowHits         = showHits,
                TradeMaxAge      = tradeMaxAge,
                DynTpTqiWeight   = dynTpTqiWeight,
                DynTpVolWeight   = dynTpVolWeight,
                DynTpMinScale    = dynTpMinScale,
                DynTpMaxScale    = dynTpMaxScale,
                DynTpFloorR1     = dynTpFloorR1,
                DynTpCeilR3      = dynTpCeilR3,
                UseAutoCalib     = useAutoCalib,
                CalibWindow      = calibWindow,
                CalibBadR        = calibBadR,
                CalibGoodR       = calibGoodR,
                CalibStepQ       = calibStepQ,
                CalibCooldown    = calibCooldown,
                CalibMinQ        = calibMinQ,
                CalibMaxQ        = calibMaxQ,
                ShowBands        = showBands,
                ShowSignals      = showSignals,
                ShowBackground   = showBackground,
                EnableAlerts     = enableAlerts,
            }, input, ref cachegbSATS);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.gbSATS gbSATS(string preset, int atrLen, double baseMultiplier, bool useAdaptive, int erLength, double adaptStrength, int atrBaselineLen, bool useTqi, double qualityStrength, double qualityCurve, bool multSmooth, bool useAsymBands, double asymStrength, bool useEffAtr, bool useCharFlip, int charFlipMinAge, double charFlipHigh, double charFlipLow, double tqiWeightEr, double tqiWeightVol, double tqiWeightStruct, double tqiWeightMom, int tqiStructLen, int tqiMomLen, bool useStructure, int pivotLen, bool useRsi, int rsiLen, int rsiOB, int rsiOS, int rsiLookback, bool useVol, int volLen, int minScore, bool showRisk, double slAtrMult, string tpMode, double tp1R, double tp2R, double tp3R, int labelOffset, bool showHits, int tradeMaxAge, double dynTpTqiWeight, double dynTpVolWeight, double dynTpMinScale, double dynTpMaxScale, double dynTpFloorR1, double dynTpCeilR3, bool useAutoCalib, int calibWindow, double calibBadR, double calibGoodR, double calibStepQ, int calibCooldown, double calibMinQ, double calibMaxQ, bool showBands, bool showSignals, bool showBackground, bool enableAlerts)
        {
            return indicator.gbSATS(Input, preset, atrLen, baseMultiplier, useAdaptive, erLength, adaptStrength, atrBaselineLen, useTqi, qualityStrength, qualityCurve, multSmooth, useAsymBands, asymStrength, useEffAtr, useCharFlip, charFlipMinAge, charFlipHigh, charFlipLow, tqiWeightEr, tqiWeightVol, tqiWeightStruct, tqiWeightMom, tqiStructLen, tqiMomLen, useStructure, pivotLen, useRsi, rsiLen, rsiOB, rsiOS, rsiLookback, useVol, volLen, minScore, showRisk, slAtrMult, tpMode, tp1R, tp2R, tp3R, labelOffset, showHits, tradeMaxAge, dynTpTqiWeight, dynTpVolWeight, dynTpMinScale, dynTpMaxScale, dynTpFloorR1, dynTpCeilR3, useAutoCalib, calibWindow, calibBadR, calibGoodR, calibStepQ, calibCooldown, calibMinQ, calibMaxQ, showBands, showSignals, showBackground, enableAlerts);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Cbi.StrategyBase
    {
        public Indicators.gbSATS gbSATS(string preset, int atrLen, double baseMultiplier, bool useAdaptive, int erLength, double adaptStrength, int atrBaselineLen, bool useTqi, double qualityStrength, double qualityCurve, bool multSmooth, bool useAsymBands, double asymStrength, bool useEffAtr, bool useCharFlip, int charFlipMinAge, double charFlipHigh, double charFlipLow, double tqiWeightEr, double tqiWeightVol, double tqiWeightStruct, double tqiWeightMom, int tqiStructLen, int tqiMomLen, bool useStructure, int pivotLen, bool useRsi, int rsiLen, int rsiOB, int rsiOS, int rsiLookback, bool useVol, int volLen, int minScore, bool showRisk, double slAtrMult, string tpMode, double tp1R, double tp2R, double tp3R, int labelOffset, bool showHits, int tradeMaxAge, double dynTpTqiWeight, double dynTpVolWeight, double dynTpMinScale, double dynTpMaxScale, double dynTpFloorR1, double dynTpCeilR3, bool useAutoCalib, int calibWindow, double calibBadR, double calibGoodR, double calibStepQ, int calibCooldown, double calibMinQ, double calibMaxQ, bool showBands, bool showSignals, bool showBackground, bool enableAlerts)
        {
            return indicator.gbSATS(Input, preset, atrLen, baseMultiplier, useAdaptive, erLength, adaptStrength, atrBaselineLen, useTqi, qualityStrength, qualityCurve, multSmooth, useAsymBands, asymStrength, useEffAtr, useCharFlip, charFlipMinAge, charFlipHigh, charFlipLow, tqiWeightEr, tqiWeightVol, tqiWeightStruct, tqiWeightMom, tqiStructLen, tqiMomLen, useStructure, pivotLen, useRsi, rsiLen, rsiOB, rsiOS, rsiLookback, useVol, volLen, minScore, showRisk, slAtrMult, tpMode, tp1R, tp2R, tp3R, labelOffset, showHits, tradeMaxAge, dynTpTqiWeight, dynTpVolWeight, dynTpMinScale, dynTpMaxScale, dynTpFloorR1, dynTpCeilR3, useAutoCalib, calibWindow, calibBadR, calibGoodR, calibStepQ, calibCooldown, calibMinQ, calibMaxQ, showBands, showSignals, showBackground, enableAlerts);
        }

        public Indicators.gbSATS gbSATS(ISeries<double> input, string preset, int atrLen, double baseMultiplier, bool useAdaptive, int erLength, double adaptStrength, int atrBaselineLen, bool useTqi, double qualityStrength, double qualityCurve, bool multSmooth, bool useAsymBands, double asymStrength, bool useEffAtr, bool useCharFlip, int charFlipMinAge, double charFlipHigh, double charFlipLow, double tqiWeightEr, double tqiWeightVol, double tqiWeightStruct, double tqiWeightMom, int tqiStructLen, int tqiMomLen, bool useStructure, int pivotLen, bool useRsi, int rsiLen, int rsiOB, int rsiOS, int rsiLookback, bool useVol, int volLen, int minScore, bool showRisk, double slAtrMult, string tpMode, double tp1R, double tp2R, double tp3R, int labelOffset, bool showHits, int tradeMaxAge, double dynTpTqiWeight, double dynTpVolWeight, double dynTpMinScale, double dynTpMaxScale, double dynTpFloorR1, double dynTpCeilR3, bool useAutoCalib, int calibWindow, double calibBadR, double calibGoodR, double calibStepQ, int calibCooldown, double calibMinQ, double calibMaxQ, bool showBands, bool showSignals, bool showBackground, bool enableAlerts)
        {
            return indicator.gbSATS(input, preset, atrLen, baseMultiplier, useAdaptive, erLength, adaptStrength, atrBaselineLen, useTqi, qualityStrength, qualityCurve, multSmooth, useAsymBands, asymStrength, useEffAtr, useCharFlip, charFlipMinAge, charFlipHigh, charFlipLow, tqiWeightEr, tqiWeightVol, tqiWeightStruct, tqiWeightMom, tqiStructLen, tqiMomLen, useStructure, pivotLen, useRsi, rsiLen, rsiOB, rsiOS, rsiLookback, useVol, volLen, minScore, showRisk, slAtrMult, tpMode, tp1R, tp2R, tp3R, labelOffset, showHits, tradeMaxAge, dynTpTqiWeight, dynTpVolWeight, dynTpMinScale, dynTpMaxScale, dynTpFloorR1, dynTpCeilR3, useAutoCalib, calibWindow, calibBadR, calibGoodR, calibStepQ, calibCooldown, calibMinQ, calibMaxQ, showBands, showSignals, showBackground, enableAlerts);
        }
    }
}

#endregion
