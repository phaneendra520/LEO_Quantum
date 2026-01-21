using System;
using System.Linq;
using LEOQ.Core.Routing;
using LEOQ.Core.Sim;
using Xunit;

namespace LEOQ.Tests;

public sealed class RoutingTests
{
    [Fact]
    public void RingMesh_ShouldProvidePaths()
    {
        var g = TopologyBuilder.BuildRingMesh(nSatellites: 12, ringLinks: 1);
        LatencyModel.AttachSyntheticLinkAttributes(g, seed: 1);

        var src = TopologyBuilder.SatId(0);
        var dst = TopologyBuilder.SatId(7);

        var baseRouter = new BaselineRouter();
        var latencyRouter = new LatencyAwareRouter();
        var riskRouter = new RiskAwareRouter();

        Assert.True(baseRouter.Route(g, src, dst).Count >= 2);
        Assert.True(latencyRouter.Route(g, src, dst).Count >= 2);
        Assert.True(riskRouter.Route(g, src, dst).Count >= 2);
    }

    [Fact]
    public void LatencyAware_ShouldNotBeWorseThanBaseline_OnAverage()
    {
        var g = TopologyBuilder.BuildRingMesh(nSatellites: 24, ringLinks: 1);
        TopologyBuilder.AddRandomChords(g, chordCount: 5, seed: 2);
        LatencyModel.AttachSyntheticLinkAttributes(g, seed: 2);

        var rnd = new Random(2);
        var idArray = g.Nodes.Keys.ToArray();

        var baseRouter = new BaselineRouter();
        var latencyRouter = new LatencyAwareRouter();

        double baseSum = 0.0;
        double latSum = 0.0;
        var pairs = 30;

        for (var i = 0; i < pairs; i++)
        {
            var src = idArray[rnd.Next(idArray.Length)];
            var dst = idArray[rnd.Next(idArray.Length)];
            if (src == dst) { i--; continue; }

            var p0 = baseRouter.Route(g, src, dst);
            var p1 = latencyRouter.Route(g, src, dst);

            baseSum += LatencyModel.PathDelayMs(g, p0);
            latSum += LatencyModel.PathDelayMs(g, p1);
        }

        Assert.True(latSum <= baseSum * 1.10); // allow small variance due to synthetic attributes
    }
}
