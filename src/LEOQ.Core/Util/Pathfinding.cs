using System;
using System.Collections.Generic;

namespace LEOQ.Core.Util;

/// <summary>
/// Simple pathfinding helpers (BFS and Dijkstra).
/// </summary>
public static class Pathfinding
{
    /// <summary>
    /// Computes an unweighted shortest path (fewest hops) using BFS.
    /// Returns an empty list if no path exists.
    /// </summary>
    public static List<string> BfsShortestPath(Func<string, IEnumerable<string>> neighbors, string src, string dst)
    {
        var prev = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [src] = null,
        };

        var q = new Queue<string>();
        q.Enqueue(src);

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (u.Equals(dst, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            foreach (var v in neighbors(u))
            {
                if (prev.ContainsKey(v))
                {
                    continue;
                }

                prev[v] = u;
                q.Enqueue(v);
            }
        }

        return Reconstruct(prev, src, dst);
    }

    /// <summary>
    /// Computes a weighted shortest path using Dijkstra.
    /// Returns an empty list if no path exists.
    /// </summary>
    public static List<string> DijkstraShortestPath(
        Func<string, IEnumerable<string>> neighbors,
        Func<string, string, double> edgeCost,
        string src,
        string dst)
    {
        var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            [src] = 0.0,
        };

        var prev = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [src] = null,
        };

        var pq = new PriorityQueue<string, double>();
        pq.Enqueue(src, 0.0);

        while (pq.TryDequeue(out var u, out var d))
        {
            if (d > dist[u])
            {
                continue;
            }

            if (u.Equals(dst, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            foreach (var v in neighbors(u))
            {
                var nd = d + edgeCost(u, v);
                if (!dist.TryGetValue(v, out var best) || nd < best)
                {
                    dist[v] = nd;
                    prev[v] = u;
                    pq.Enqueue(v, nd);
                }
            }
        }

        return Reconstruct(prev, src, dst);
    }

    private static List<string> Reconstruct(Dictionary<string, string?> prev, string src, string dst)
    {
        if (!prev.ContainsKey(dst))
        {
            return new List<string>();
        }

        var path = new List<string>();
        string? cur = dst;
        while (cur is not null)
        {
            path.Add(cur);
            cur = prev[cur];
        }

        path.Reverse();

        // Sanity: ensure starts with src
        if (path.Count == 0 || !path[0].Equals(src, StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>();
        }

        return path;
    }
}
