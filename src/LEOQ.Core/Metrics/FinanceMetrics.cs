using System;
using System.Collections.Generic;
using System.Linq;

namespace LEOQ.Core.Metrics;

public static class FinanceMetrics
{
    /// <summary>
    /// Absolute slippage = executed - intended.
    /// </summary>
    public static double SlippageAbs(double intendedPrice, double executedPrice)
        => executedPrice - intendedPrice;

    public static double SlippagePct(double intendedPrice, double executedPrice)
        => intendedPrice == 0 ? 0.0 : (executedPrice - intendedPrice) / intendedPrice;

    /// <summary>
    /// Very simple parametric VaR estimate using historical returns (non-normalized).
    /// VaR at confidence alpha (e.g., 0.99) is the (1-alpha) percentile of losses.
    /// </summary>
    public static double ValueAtRisk(IEnumerable<double> returns, double confidence = 0.99)
    {
        var rets = returns as double[] ?? returns.ToArray();
        if (rets.Length == 0) return 0.0;

        // losses: negative returns
        var losses = rets.Select(r => -r).ToArray();
        Array.Sort(losses);
        var p = 1.0 - Math.Clamp(confidence, 0.0, 1.0);
        var idx = (int)Math.Round((losses.Length - 1) * p);
        idx = Math.Clamp(idx, 0, losses.Length - 1);
        return losses[idx];
    }
}
