using System.Collections.Generic;
using LEOQ.Core.Sim;
using LEOQ.Core.Util;

namespace LEOQ.Core.Routing;

/// <summary>
/// Baseline routing: unweighted shortest path (fewest hops).
/// </summary>
public sealed class BaselineRouter : IRouter
{
    public string Name => "baseline-hops";

    public IReadOnlyList<string> Route(Graph g, string src, string dst)
    {
        return Pathfinding.BfsShortestPath(
            neighbors: u => g.Nodes[u].Neighbors.Keys,
            src: src,
            dst: dst);
    }
}
