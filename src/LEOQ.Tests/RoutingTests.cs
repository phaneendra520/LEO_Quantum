using System.Linq;
using LEOQ.Core.Experiments;
using LEOQ.Core.Routing;
using LEOQ.Core.Sim;
using Xunit;

namespace LEOQ.Tests;

public sealed class RoutingTests
{
    [Fact]
    public void RingMesh_ShouldProvidePaths()
    {
        var g   = TopologyBuilder.BuildRingMesh(nSatellites: 12, ringLinks: 1);
        LatencyModel.AttachSyntheticLinkAttributes(g, seed: 1);
        var src = TopologyBuilder.SatId(0);
        var dst = TopologyBuilder.SatId(7);
        Assert.True(new BaselineRouter().Route(g, src, dst).Count >= 2);
        Assert.True(new LatencyAwareRouter().Route(g, src, dst).Count >= 2);
        Assert.True(new RiskAwareRouter().Route(g, src, dst).Count >= 2);
    }

    [Fact]
    public void LatencyAware_ShouldNotBeWorseThanBaseline_OnAverage()
    {
        var g      = TopologyBuilder.BuildRingMesh(nSatellites: 24, ringLinks: 1);
        TopologyBuilder.AddRandomChords(g, chordCount: 5, seed: 2);
        LatencyModel.AttachSyntheticLinkAttributes(g, seed: 2);
        var rnd     = new System.Random(2);
        var idArray = g.Nodes.Keys.ToArray();
        double baseSum = 0, latSum = 0;
        for (var i = 0; i < 30; i++)
        {
            var src = idArray[rnd.Next(idArray.Length)];
            var dst = idArray[rnd.Next(idArray.Length)];
            if (src == dst) { i--; continue; }
            baseSum += LatencyModel.PathDelayMs(g, new BaselineRouter().Route(g, src, dst));
            latSum  += LatencyModel.PathDelayMs(g, new LatencyAwareRouter().Route(g, src, dst));
        }
        Assert.True(latSum <= baseSum * 1.10);
    }
}

public sealed class ExperimentTests
{
    [Fact]
    public void QaeExperiment_ShouldProduceSevenRows()
    {
        var rows = QaeVarConvergenceExperiment.Run();
        Assert.Equal(7, rows.Count);
    }

    [Fact]
    public void QaeExperiment_SpeedupShouldBeQuadratic()
    {
        var rows = QaeVarConvergenceExperiment.Run();
        var row  = rows.First(r => r.PrecisionTarget == 0.01);
        Assert.True(row.SpeedupFactor > 50.0);
    }

    [Fact]
    public void LeoLatency_FiberRttShouldBeInExpectedRange()
    {
        var rows  = LeoLatencyAnalysisExperiment.Run();
        var fiber = rows.First(r => r.Label == "Fiber");
        Assert.True(fiber.RttMs > 50.0 && fiber.RttMs < 75.0);
    }

    [Fact]
    public void LeoLatency_H3ShouldOutperformFiber()
    {
        var rows  = LeoLatencyAnalysisExperiment.Run();
        var fiber = rows.First(r => r.Label == "Fiber");
        var h3    = rows.First(r => r.Label == "LEO H=3");
        Assert.True(h3.TotalOneWay < fiber.TotalOneWay);
    }

    [Fact]
    public void Qkd_MiciusDistanceShouldBeFeasible()
    {
        var rows   = QkdKeyRateExperiment.Run();
        var micius = rows.First(r => r.DistanceKm == 1_200);
        Assert.True(micius.OperationallyFeasible);
        Assert.True(micius.KeyGenTimeSec < 1.0);
    }

    [Fact]
    public void Qkd_KeyRateShouldDecreaseWithDistance()
    {
        var rows = QkdKeyRateExperiment.Run().OrderBy(r => r.DistanceKm).ToList();
        for (int i = 1; i < rows.Count; i++)
            Assert.True(rows[i].KeyRateBps <= rows[i - 1].KeyRateBps);
    }
}
