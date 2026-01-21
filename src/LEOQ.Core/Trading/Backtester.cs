using System;
using System.Collections.Generic;
using System.Linq;
using LEOQ.Core.Metrics;

namespace LEOQ.Core.Trading;

/// <summary>
/// Toy backtester: submits orders at time t and executes at time t + delay.
/// This is a simplified demonstration to connect latency distributions to execution outcomes.
/// </summary>
public static class Backtester
{
    public static (IReadOnlyList<Order> Orders, BacktestSummary Summary) Run(
        IReadOnlyList<double> prices,
        IReadOnlyList<double> delaysMs,
        int orderEvery = 10)
    {
        if (prices.Count < 2) throw new ArgumentException("prices must have at least 2 points");
        if (delaysMs.Count == 0) throw new ArgumentException("delaysMs must not be empty");
        if (orderEvery <= 0) throw new ArgumentOutOfRangeException(nameof(orderEvery));

        var orders = new List<Order>();
        var rnd = new Random(42);

        for (var t = 0; t < prices.Count; t += orderEvery)
        {
            var delay = delaysMs[rnd.Next(delaysMs.Count)];
            // Map delay (ms) to index steps using a rough scale factor.
            var stepsDelay = (int)Math.Round(delay / 1.0); // 1ms -> 1 step (demo-only)
            var exec = Math.Min(prices.Count - 1, t + Math.Max(0, stepsDelay));

            var side = rnd.NextDouble() < 0.5 ? Side.Buy : Side.Sell;
            var intended = prices[t];
            var executed = prices[exec];

            // For sells, treat slippage relative to sell direction (executed lower is worse)
            if (side == Side.Sell)
            {
                // invert for symmetry
                executed = 2 * intended - executed;
            }

            orders.Add(new Order(side, t, exec, intended, executed, delay));
        }

        var slAbs = orders.Select(o => FinanceMetrics.SlippageAbs(o.IntendedPrice, o.ExecutedPrice)).ToArray();
        var slPct = orders.Select(o => FinanceMetrics.SlippagePct(o.IntendedPrice, o.ExecutedPrice)).ToArray();
        var rets = MarketFeed.ReturnsFromPrices(prices);

        var summary = new BacktestSummary(
            Orders: orders.Count,
            AvgDelayMs: Stats.Mean(orders.Select(o => o.DelayMs)),
            AvgSlippageAbs: Stats.Mean(slAbs),
            AvgSlippagePct: Stats.Mean(slPct),
            Var99: FinanceMetrics.ValueAtRisk(rets, confidence: 0.99));

        return (orders, summary);
    }
}
