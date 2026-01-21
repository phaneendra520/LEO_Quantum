using System;
using System.Collections.Generic;
using LEOQ.Core.Sim;
using LEOQ.Core.Util;

namespace LEOQ.Core.Routing;

/// <summary>
/// Risk-aware routing: minimize a composite cost = delay + penalty.
/// 
/// The penalty is a simple proxy for "risk" (e.g., congestion, exposure, fragility).
/// In this prototype we approximate risk by preferring paths that avoid high-degree
/// nodes (hubs), using a degree-based penalty.
/// 
/// This is a research-motivated heuristic and not a claim of real network risk.
/// </summary>
public sealed class RiskAwareRouter : IRouter
{
    public string Name => "risk-aware";

    /// <summary>
    /// Multiplier applied to degree penalty.
    /// Larger values make the router more "risk-averse".
    /// </summary>
    public double DegreePenaltyWeight { get; }

    public RiskAwareRouter(double degreePenaltyWeight = 0.10)
    {
        DegreePenaltyWeight = Math.Max(0.0, degreePenaltyWeight);
    }

    public IReadOnlyList<string> Route(Graph g, string src, string dst)
    {
        return Pathfinding.DijkstraShortestPath(
            neighbors: u => g.Nodes[u].Neighbors.Keys,
            edgeCost: (u, v) =>
            {
                var delay = Math.Max(0.0, g.Nodes[u].Neighbors[v].DelayMs);
                var penalty = DegreePenaltyWeight * (g.Degree(v));
                return delay + penalty;
            },
            src: src,
            dst: dst);
    }
}
