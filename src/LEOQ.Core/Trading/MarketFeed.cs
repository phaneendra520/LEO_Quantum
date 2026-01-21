using System;
using System.Collections.Generic;

namespace LEOQ.Core.Trading;

/// <summary>
/// Synthetic market price series generator (random walk).
/// </summary>
public static class MarketFeed
{
    public static IReadOnlyList<double> GenerateRandomWalk(int steps, double startPrice = 100.0, double sigma = 0.001, int? seed = null)
    {
        if (steps <= 1) throw new ArgumentOutOfRangeException(nameof(steps));
        if (startPrice <= 0) throw new ArgumentOutOfRangeException(nameof(startPrice));
        if (sigma < 0) throw new ArgumentOutOfRangeException(nameof(sigma));

        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        var prices = new double[steps];
        prices[0] = startPrice;

        for (var i = 1; i < steps; i++)
        {
            // Small Gaussian-ish shock (Box-Muller)
            var u1 = 1.0 - rnd.NextDouble();
            var u2 = 1.0 - rnd.NextDouble();
            var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            var shock = sigma * z;

            prices[i] = Math.Max(0.01, prices[i - 1] * (1.0 + shock));
        }

        return prices;
    }

    public static IReadOnlyList<double> ReturnsFromPrices(IReadOnlyList<double> prices)
    {
        if (prices.Count < 2) return Array.Empty<double>();
        var rets = new double[prices.Count - 1];
        for (var i = 1; i < prices.Count; i++)
        {
            rets[i - 1] = (prices[i] - prices[i - 1]) / prices[i - 1];
        }
        return rets;
    }
}
