using System;
using System.Linq;

namespace LEOQ.Core.Sim;

/// <summary>
/// Builds a simple demo LEO mesh topology.
/// 
/// Model:
/// - Satellites are named SAT-00..SAT-(N-1)
/// - A ring is created (each satellite connects to +/- ringLinks)
/// - Optional chord links can be added via <see cref="AddRandomChords"/>
/// </summary>
public static class TopologyBuilder
{
    public static Graph BuildRingMesh(int nSatellites, int ringLinks = 1)
    {
        if (nSatellites < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(nSatellites), "nSatellites must be >= 4");
        }

        if (ringLinks < 1 || ringLinks >= nSatellites / 2)
        {
            throw new ArgumentOutOfRangeException(nameof(ringLinks), "ringLinks must be between 1 and floor(N/2)-1");
        }

        var g = new Graph();

        for (var i = 0; i < nSatellites; i++)
        {
            g.GetOrAdd(SatId(i));
        }

        // Ring connectivity
        for (var i = 0; i < nSatellites; i++)
        {
            for (var k = 1; k <= ringLinks; k++)
            {
                var j = (i + k) % nSatellites;
                g.AddUndirectedEdge(SatId(i), SatId(j), new LinkAttributes());
            }
        }

        return g;
    }

    public static void AddRandomChords(Graph g, int chordCount, int? seed = null)
    {
        if (chordCount <= 0)
        {
            return;
        }

        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        var idArray = g.Nodes.Keys.ToArray();

        var attempts = 0;
        var added = 0;

        while (added < chordCount && attempts < chordCount * 50)
        {
            attempts++;
            var a = idArray[rnd.Next(idArray.Length)];
            var b = idArray[rnd.Next(idArray.Length)];

            if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (g.HasEdge(a, b))
            {
                continue;
            }

            g.AddUndirectedEdge(a, b, new LinkAttributes());
            added++;
        }
    }

    public static string SatId(int i) => $"SAT-{i:00}";
}
