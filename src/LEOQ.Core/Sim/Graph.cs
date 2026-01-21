using System.Collections.Generic;

namespace LEOQ.Core.Sim;

/// <summary>
/// Minimal undirected graph model used by the LEO-Q prototype.
/// 
/// We intentionally avoid external graph libraries so that the solution is
/// easy to build and review.
/// </summary>
public sealed class Graph
{
    private readonly Dictionary<string, Node> _nodes = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, Node> Nodes => _nodes;

    public Node GetOrAdd(string id)
    {
        if (_nodes.TryGetValue(id, out var node))
        {
            return node;
        }

        node = new Node(id);
        _nodes[id] = node;
        return node;
    }

    /// <summary>
    /// Adds an undirected edge between two nodes. Multiple edges are not allowed.
    /// If the edge already exists, its attributes are overwritten.
    /// </summary>
    public void AddUndirectedEdge(string a, string b, LinkAttributes attrs)
    {
        var na = GetOrAdd(a);
        var nb = GetOrAdd(b);

        na.Neighbors[b] = attrs;
        nb.Neighbors[a] = attrs;
    }

    public bool HasEdge(string a, string b)
        => _nodes.ContainsKey(a) && _nodes[a].Neighbors.ContainsKey(b);

    public int Degree(string nodeId)
        => _nodes.TryGetValue(nodeId, out var n) ? n.Neighbors.Count : 0;

    public sealed class Node
    {
        public Node(string id)
        {
            Id = id;
        }

        public string Id { get; }

        /// <summary>
        /// Neighbor node id -> link attributes.
        /// </summary>
        public Dictionary<string, LinkAttributes> Neighbors { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Attributes for a link (inter-satellite or other) used for latency calculations.
/// </summary>
public sealed class LinkAttributes
{
    /// <summary>
    /// Physical distance in kilometers.
    /// </summary>
    public double DistanceKm { get; set; }

    /// <summary>
    /// Propagation delay in milliseconds (computed from DistanceKm and medium).
    /// </summary>
    public double DelayMs { get; set; }

    /// <summary>
    /// Optional per-link jitter (standard deviation) in milliseconds.
    /// </summary>
    public double JitterStdMs { get; set; }
}
