using System;
using System.Collections.Generic;
using LEOQ.Core.Sim;
using LEOQ.Core.Util;

namespace LEOQ.Core.Routing;

/// <summary>
/// Latency-aware routing: minimize total propagation delay.
/// Requires edges to have DelayMs set.
/// </summary>
public sealed class LatencyAwareRouter : IRouter
{
    public string Name => "latency-aware";

    public IReadOnlyList<string> Route(Graph g, string src, string dst)
    {
        return Pathfinding.DijkstraShortestPath(
            neighbors: u => g.Nodes[u].Neighbors.Keys,
            edgeCost: (u, v) => Math.Max(0.0, g.Nodes[u].Neighbors[v].DelayMs),
            src: src,
            dst: dst);
    }
}
