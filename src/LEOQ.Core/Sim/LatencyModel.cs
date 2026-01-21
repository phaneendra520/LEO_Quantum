using System;

namespace LEOQ.Core.Sim;

/// <summary>
/// Functions for attaching synthetic distances, propagation delay and jitter.
/// </summary>
public static class LatencyModel
{
    /// <summary>
    /// Speed of light in vacuum (m/s). Used as a base for propagation delay.
    /// In fiber, effective speed is ~2e8 m/s; in space it's closer to c.
    /// </summary>
    private const double SpeedOfLightMps = 299_792_458.0;

    /// <summary>
    /// Attach demo link distances to all edges in the graph.
    /// 
    /// Each edge is assigned:
    /// - DistanceKm: sampled from a range
    /// - DelayMs: computed propagation delay
    /// - JitterStdMs: optional jitter standard deviation
    /// </summary>
    public static void AttachSyntheticLinkAttributes(Graph g, double minDistanceKm = 800, double maxDistanceKm = 1800, double jitterStdMs = 0.05, int? seed = null)
    {
        if (minDistanceKm <= 0 || maxDistanceKm <= minDistanceKm)
        {
            throw new ArgumentOutOfRangeException(nameof(minDistanceKm), "Distance range is invalid.");
        }

        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();

        foreach (var node in g.Nodes.Values)
        {
            foreach (var kv in node.Neighbors)
            {
                // Only set once per undirected edge by ordering keys.
                if (string.Compare(node.Id, kv.Key, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    continue;
                }

                var attrs = kv.Value;
                attrs.DistanceKm = minDistanceKm + rnd.NextDouble() * (maxDistanceKm - minDistanceKm);
                attrs.DelayMs = PropagationDelayMs(attrs.DistanceKm, medium: PropagationMedium.Space);
                attrs.JitterStdMs = Math.Max(0, jitterStdMs);
            }
        }
    }

    public static double PropagationDelayMs(double distanceKm, PropagationMedium medium)
    {
        var speed = medium switch
        {
            PropagationMedium.Space => SpeedOfLightMps,
            PropagationMedium.Fiber => 2.0e8, // typical effective speed in fiber
            _ => SpeedOfLightMps,
        };

        var distanceM = distanceKm * 1000.0;
        var seconds = distanceM / speed;
        return seconds * 1000.0;
    }

    public static double SampleJitterMs(Random rnd, double stdMs)
    {
        // Box-Muller transform for standard normal
        var u1 = 1.0 - rnd.NextDouble();
        var u2 = 1.0 - rnd.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return z * stdMs;
    }

    public static double PathDelayMs(Graph g, IReadOnlyList<string> path, bool includeJitter = false, int? seed = null)
    {
        if (path.Count < 2)
        {
            return 0.0;
        }

        var rnd = includeJitter ? (seed.HasValue ? new Random(seed.Value) : new Random()) : null;
        double total = 0.0;

        for (var i = 0; i < path.Count - 1; i++)
        {
            var u = path[i];
            var v = path[i + 1];
            var attrs = g.Nodes[u].Neighbors[v];
            total += attrs.DelayMs;

            if (includeJitter && rnd is not null && attrs.JitterStdMs > 0)
            {
                total += SampleJitterMs(rnd, attrs.JitterStdMs);
            }
        }

        return total;
    }
}

public enum PropagationMedium
{
    Space,
    Fiber,
}
