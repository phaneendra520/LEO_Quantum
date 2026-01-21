using System;
using System.Collections.Generic;
using System.Linq;

namespace LEOQ.Core.Metrics;

public static class Stats
{
    public static double Mean(IEnumerable<double> xs)
    {
        var arr = xs as double[] ?? xs.ToArray();
        return arr.Length == 0 ? 0.0 : arr.Average();
    }

    public static double Percentile(IEnumerable<double> xs, double p)
    {
        var arr = xs as double[] ?? xs.ToArray();
        if (arr.Length == 0) return 0.0;
        Array.Sort(arr);
        p = Math.Clamp(p, 0.0, 1.0);
        var idx = (int)Math.Round((arr.Length - 1) * p);
        idx = Math.Clamp(idx, 0, arr.Length - 1);
        return arr[idx];
    }
}
