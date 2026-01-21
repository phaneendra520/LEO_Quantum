using System;
using System.Collections.Generic;
using System.Linq;
using LEOQ.Core.Routing;

namespace LEOQ.Core.Sim;

/// <summary>
/// Generates a simple dataset of path features -> end-to-end delay.
/// </summary>
public static class DatasetGenerator
{
    public sealed record Row(int Hops, double DistanceKm, double DelayMs);

    public static IReadOnlyList<Row> Generate(Graph g, IRouter router, int samples, int? seed = null)
    {
        if (samples <= 0) throw new ArgumentOutOfRangeException(nameof(samples));

        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        var ids = g.Nodes.Keys.ToArray();

        var rows = new List<Row>(samples);

        for (var i = 0; i < samples; i++)
        {
            var src = ids[rnd.Next(ids.Length)];
            var dst = ids[rnd.Next(ids.Length)];
            if (src.Equals(dst, StringComparison.OrdinalIgnoreCase))
            {
                i--; // retry
                continue;
            }

            var path = router.Route(g, src, dst);
            if (path.Count < 2)
            {
                i--; // disconnected
                continue;
            }

            var hops = path.Count - 1;
            double distKm = 0.0;
            for (var k = 0; k < path.Count - 1; k++)
            {
                distKm += g.Nodes[path[k]].Neighbors[path[k + 1]].DistanceKm;
            }

            var delay = LatencyModel.PathDelayMs(g, path, includeJitter: false);
            rows.Add(new Row(hops, distKm, delay));
        }

        return rows;
    }
}
