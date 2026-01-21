using System.Collections.Generic;
using LEOQ.Core.Sim;

namespace LEOQ.Core.Routing;

/// <summary>
/// Common routing interface.
/// </summary>
public interface IRouter
{
    /// <summary>
    /// Computes a route (sequence of node IDs) from <paramref name="src"/> to <paramref name="dst"/>.
    /// Returns an empty list if no route exists.
    /// </summary>
    IReadOnlyList<string> Route(Graph g, string src, string dst);

    string Name { get; }
}
