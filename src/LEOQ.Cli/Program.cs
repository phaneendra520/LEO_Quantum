using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LEOQ.Core.Crypto;
using LEOQ.Core.Metrics;
using LEOQ.Core.Routing;
using LEOQ.Core.Sim;
using LEOQ.Core.Trading;
using LEOQ.Core.Util;

namespace LEOQ.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        var cmd = args[0].Trim().ToLowerInvariant();
        var opt = Args.Parse(args.Skip(1).ToArray());

        try
        {
            return cmd switch
            {
                "bench" => RunBench(opt),
                "dataset" => RunDataset(opt),
                "backtest" => RunBacktest(opt),
                "crypto-demo" => RunCryptoDemo(opt),
                _ => Unknown(cmd),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }

    private static int Unknown(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("LEO-Q CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/LEOQ.Cli -- <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  bench        Compare routing policies over random pairs");
        Console.WriteLine("  dataset      Generate CSV dataset for ML training");
        Console.WriteLine("  backtest     Run toy trading backtest influenced by network delay");
        Console.WriteLine("  crypto-demo  Demonstrate QKD/PQC stubs using SecureSession");
        Console.WriteLine();
        Console.WriteLine("Common options:");
        Console.WriteLine("  --sats <int>         Number of satellites (default 24)");
        Console.WriteLine("  --ring <int>         Ring links per node (default 1)");
        Console.WriteLine("  --chords <int>       Random chord links (default 0)");
        Console.WriteLine("  --seed <int>         Random seed (optional)");
        Console.WriteLine();
        Console.WriteLine("bench options:");
        Console.WriteLine("  --pairs <int>        Number of random src/dst pairs (default 20)");
        Console.WriteLine();
        Console.WriteLine("dataset options:");
        Console.WriteLine("  --samples <int>      Rows to generate (default 1000)");
        Console.WriteLine("  --out <path>         Output CSV path (default data/leoq_dataset.csv)");
        Console.WriteLine();
        Console.WriteLine("backtest options:");
        Console.WriteLine("  --steps <int>        Price steps (default 2000)");
        Console.WriteLine("  --every <int>        Submit order every N steps (default 10)");
    }

    private static (Graph G, Random Rnd) BuildDefaultGraph(Args opt)
    {
        var sats = opt.Int("sats", 24);
        var ring = opt.Int("ring", 1);
        var chords = opt.Int("chords", 0);
        var seed = opt.TryInt("seed");

        var g = TopologyBuilder.BuildRingMesh(sats, ringLinks: ring);
        if (chords > 0) TopologyBuilder.AddRandomChords(g, chords, seed);

        LatencyModel.AttachSyntheticLinkAttributes(g, seed: seed);

        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        return (g, rnd);
    }

    private static int RunBench(Args opt)
    {
        var (g, rnd) = BuildDefaultGraph(opt);
        var pairs = opt.Int("pairs", 20);

        var routers = new IRouter[]
        {
            new BaselineRouter(),
            new LatencyAwareRouter(),
            new RiskAwareRouter(degreePenaltyWeight: 0.10),
        };

        var ids = g.Nodes.Keys.ToArray();
        var results = new Dictionary<string, List<double>>();
        foreach (var r in routers) results[r.Name] = new List<double>();

        for (var i = 0; i < pairs; i++)
        {
            var src = ids[rnd.Next(ids.Length)];
            var dst = ids[rnd.Next(ids.Length)];
            if (src.Equals(dst, StringComparison.OrdinalIgnoreCase))
            {
                i--; continue;
            }

            foreach (var r in routers)
            {
                var path = r.Route(g, src, dst);
                var d = LatencyModel.PathDelayMs(g, path, includeJitter: true, seed: rnd.Next());
                results[r.Name].Add(d);
            }
        }

        Console.WriteLine("Routing Benchmark Summary (ms)");
        Console.WriteLine($"Satellites: {g.Nodes.Count}, Pairs: {pairs}");
        Console.WriteLine();

        foreach (var kv in results)
        {
            var xs = kv.Value;
            Console.WriteLine($"{kv.Key}");
            Console.WriteLine($"  mean: {Stats.Mean(xs):F4}");
            Console.WriteLine($"  p95 : {Stats.Percentile(xs, 0.95):F4}");
            Console.WriteLine($"  p99 : {Stats.Percentile(xs, 0.99):F4}");
            Console.WriteLine($"  max : {xs.Max():F4}");
        }

        return 0;
    }

    private static int RunDataset(Args opt)
    {
        var (g, _) = BuildDefaultGraph(opt);
        var samples = opt.Int("samples", 1000);
        var outPath = opt.String("out", Path.Combine("data", "leoq_dataset.csv"));

        var router = new LatencyAwareRouter();
        var rows = DatasetGenerator.Generate(g, router, samples, seed: opt.TryInt("seed"));

        Csv.WriteRows(
            outPath,
            header: new[] { "hops", "distance_km", "delay_ms" },
            rows: rows.Select(r => new object[] { r.Hops, r.DistanceKm, r.DelayMs }));

        Console.WriteLine($"Dataset written: {outPath}");
        Console.WriteLine($"Rows: {rows.Count}");
        return 0;
    }

    private static int RunBacktest(Args opt)
    {
        var (g, rnd) = BuildDefaultGraph(opt);
        var steps = opt.Int("steps", 2000);
        var every = opt.Int("every", 10);

        // Generate price series
        var prices = MarketFeed.GenerateRandomWalk(steps, seed: opt.TryInt("seed"));

        // Sample delays from latency-aware routes
        var router = new LatencyAwareRouter();
        var ids = g.Nodes.Keys.ToArray();
        var delays = new List<double>();
        for (var i = 0; i < 200; i++)
        {
            var src = ids[rnd.Next(ids.Length)];
            var dst = ids[rnd.Next(ids.Length)];
            if (src.Equals(dst, StringComparison.OrdinalIgnoreCase))
            {
                i--; continue;
            }

            var path = router.Route(g, src, dst);
            var d = LatencyModel.PathDelayMs(g, path, includeJitter: true, seed: rnd.Next());
            delays.Add(Math.Max(0.0, d));
        }

        var (_, summary) = Backtester.Run(prices, delays, orderEvery: every);

        Console.WriteLine("Trading Impact Summary");
        Console.WriteLine($"Orders: {summary.Orders}");
        Console.WriteLine($"Avg delay (ms): {summary.AvgDelayMs:F4}");
        Console.WriteLine($"Avg slippage (abs): {summary.AvgSlippageAbs:F6}");
        Console.WriteLine($"Avg slippage (%): {summary.AvgSlippagePct * 100.0:F6}%");
        Console.WriteLine($"VaR 99 (demo): {summary.Var99:F6}");

        return 0;
    }

    private static int RunCryptoDemo(Args opt)
    {
        var msg = opt.String("msg", "Hello from LEO-Q secure session");
        var key = QkdStub.GenerateSharedKey(lengthBytes: 32, seed: opt.TryInt("seed"));
        var session = new SecureSession(key);

        var c = session.Protect(msg);
        var p = session.Unprotect(c);

        Console.WriteLine("Crypto Demo (STUBS ONLY)");
        Console.WriteLine($"Plain : {msg}");
        Console.WriteLine($"Cipher: {c}");
        Console.WriteLine($"Back  : {p}");

        return 0;
    }
}

internal sealed class Args
{
    private readonly Dictionary<string, string> _kv;

    private Args(Dictionary<string, string> kv) => _kv = kv;

    public static Args Parse(string[] args)
    {
        var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("--", StringComparison.Ordinal)) continue;
            var key = a[2..];
            var val = (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) ? args[++i] : "true";
            kv[key] = val;
        }
        return new Args(kv);
    }

    public string String(string key, string defaultValue)
        => _kv.TryGetValue(key, out var v) ? v : defaultValue;

    public int Int(string key, int defaultValue)
        => _kv.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : defaultValue;

    public int? TryInt(string key)
        => _kv.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : null;
}
