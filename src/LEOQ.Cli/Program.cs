using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LEOQ.Core.Crypto;
using LEOQ.Core.Experiments;
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
                "bench"        => RunBench(opt),
                "dataset"      => RunDataset(opt),
                "backtest"     => RunBacktest(opt),
                "crypto-demo"  => RunCryptoDemo(opt),
                "qaoa"         => RunQAOA(opt),
                "exp-qae"      => RunQaeExperiment(opt),
                "exp-latency"  => RunLatencyExperiment(opt),
                "exp-qkd"      => RunQkdExperiment(opt),
                "run-all"      => RunAllExperiments(opt),
                _              => Unknown(cmd),
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
        Console.WriteLine("LEO-Q Quantum Simulation Framework");
        Console.WriteLine();

        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/LEOQ.Cli -- <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Core Commands:");
        Console.WriteLine("  bench        Compare routing policies (BaselineRouter vs LatencyAware vs RiskAware)");
        Console.WriteLine("  dataset      Generate CSV dataset of LEO routing samples");
        Console.WriteLine("  backtest     Run toy trading backtest with network latency influence");
        Console.WriteLine("  crypto-demo  Demonstrate QKD/PQC stub session encryption");
        Console.WriteLine("  qaoa         Run QAOA portfolio optimization (Exp 1 — main paper result)");
        Console.WriteLine();
        Console.WriteLine("Quantum Experiments:");
        Console.WriteLine("  exp-qae      Experiment 2: QAE vs Classical MC VaR convergence speedup");
        Console.WriteLine("  exp-latency  Experiment 3: LEO multi-hop latency budget (H=1..6 vs fiber)");
        Console.WriteLine("  exp-qkd      Experiment 4: Satellite QKD key rate and session rekeying model");
        Console.WriteLine("  run-all      Run all four experiments and export all results to --out");
        Console.WriteLine();
        Console.WriteLine("Common Options:");
        Console.WriteLine("  --sats <int>     Number of satellites (default 24)");
        Console.WriteLine("  --seed <int>     Random seed for reproducibility (default 42)");
        Console.WriteLine("  --out  <path>    Output directory (default ./results)");
        Console.WriteLine();
        Console.WriteLine("QAOA Options:");
        Console.WriteLine("  --layers <int>   QAOA layers P (default 2)");
        Console.WriteLine("  --shots  <int>   Measurement shots (default 4096)");
    }

    private static (Graph G, Random Rnd) BuildDefaultGraph(Args opt)
    {
        var sats   = opt.Int("sats", 24);
        var ring   = opt.Int("ring", 1);
        var chords = opt.Int("chords", 0);
        var seed   = opt.TryInt("seed");

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

        var ids     = g.Nodes.Keys.ToArray();
        var results = new Dictionary<string, List<double>>();
        foreach (var r in routers) results[r.Name] = new List<double>();

        for (var i = 0; i < pairs; i++)
        {
            var src = ids[rnd.Next(ids.Length)];
            var dst = ids[rnd.Next(ids.Length)];
            if (src.Equals(dst, StringComparison.OrdinalIgnoreCase)) { i--; continue; }

            foreach (var r in routers)
            {
                var path = r.Route(g, src, dst);
                var d    = LatencyModel.PathDelayMs(g, path, includeJitter: true, seed: rnd.Next());
                results[r.Name].Add(d);
            }
        }

        Console.WriteLine("Routing Benchmark Summary (ms)");
        Console.WriteLine($"Satellites: {g.Nodes.Count}  Pairs: {pairs}");
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
        var rows   = DatasetGenerator.Generate(g, router, samples, seed: opt.TryInt("seed"));

        Csv.WriteRows(outPath,
            header: new[] { "hops", "distance_km", "delay_ms" },
            rows:   rows.Select(r => new object[] { r.Hops, r.DistanceKm, r.DelayMs }));

        Console.WriteLine($"Dataset written: {outPath}  ({rows.Count} rows)");
        return 0;
    }

    private static int RunBacktest(Args opt)
    {
        var (g, rnd) = BuildDefaultGraph(opt);
        var steps = opt.Int("steps", 2000);
        var every = opt.Int("every", 10);

        var prices = MarketFeed.GenerateRandomWalk(steps, seed: opt.TryInt("seed"));
        var router = new LatencyAwareRouter();
        var ids    = g.Nodes.Keys.ToArray();
        var delays = new List<double>();

        for (var i = 0; i < 200; i++)
        {
            var src = ids[rnd.Next(ids.Length)];
            var dst = ids[rnd.Next(ids.Length)];
            if (src.Equals(dst, StringComparison.OrdinalIgnoreCase)) { i--; continue; }

            var path = router.Route(g, src, dst);
            var d    = LatencyModel.PathDelayMs(g, path, includeJitter: true, seed: rnd.Next());
            delays.Add(Math.Max(0.0, d));
        }

        var (_, summary) = Backtester.Run(prices, delays, orderEvery: every);

        Console.WriteLine("Trading Impact Summary");
        Console.WriteLine($"Orders          : {summary.Orders}");
        Console.WriteLine($"Avg delay (ms)  : {summary.AvgDelayMs:F4}");
        Console.WriteLine($"Avg slippage    : {summary.AvgSlippageAbs:F6} abs  ({summary.AvgSlippagePct * 100.0:F6}%)");
        Console.WriteLine($"VaR 99 (demo)   : {summary.Var99:F6}");
        return 0;
    }

    private static int RunCryptoDemo(Args opt)
    {
        var msg     = opt.String("msg", "Hello from LEO-Q secure session");
        var key     = QkdStub.GenerateSharedKey(lengthBytes: 32, seed: opt.TryInt("seed"));
        var session = new SecureSession(key);

        var c = session.Protect(msg);
        var p = session.Unprotect(c);

        Console.WriteLine("QKD Crypto Stub Demo");
        Console.WriteLine($"Plain : {msg}");
        Console.WriteLine($"Cipher: {c}");
        Console.WriteLine($"Back  : {p}");
        return 0;
    }

    private static int RunQAOA(Args opt)
    {
        var layers    = opt.Int("layers", 2);
        var shots     = opt.Int("shots", 4096);
        var outputDir = opt.String("out", "./results");
        var seed      = opt.TryInt("seed");

        var simulator = new QAOASimulator(qaoapLayers: layers, shots: shots, seed: seed);
        simulator.Run(outputDir);
        return 0;
    }

    private static int RunQaeExperiment(Args opt)
    {
        var outDir = opt.String("out", "./results");
        var rows   = QaeVarConvergenceExperiment.Run();
        QaeVarConvergenceExperiment.ExportCsv(Path.Combine(outDir, "qae_var_convergence.csv"), rows);
        return 0;
    }

    private static int RunLatencyExperiment(Args opt)
    {
        var outDir = opt.String("out", "./results");
        var rows   = LeoLatencyAnalysisExperiment.Run();
        LeoLatencyAnalysisExperiment.ExportCsv(Path.Combine(outDir, "leo_latency_budget.csv"), rows);
        return 0;
    }

    private static int RunQkdExperiment(Args opt)
    {
        var outDir = opt.String("out", "./results");
        var rows   = QkdKeyRateExperiment.Run();
        QkdKeyRateExperiment.ExportCsv(Path.Combine(outDir, "qkd_key_rate_model.csv"), rows);
        return 0;
    }

    private static int RunAllExperiments(Args opt)
    {
        var outDir = opt.String("out", "./results");
        Console.WriteLine("================================================================");
        Console.WriteLine("  LEO-Q: Running All Four Quantum Experiments");
        Console.WriteLine("  Output directory: " + outDir);
        Console.WriteLine("================================================================");

        // Experiment 1: QAOA Portfolio Optimization
        Console.WriteLine("\n--- Experiment 1: QAOA Portfolio Optimization ---");
        var sim = new QAOASimulator(qaoapLayers: opt.Int("layers", 2),
                                    shots: opt.Int("shots", 4096),
                                    seed: opt.TryInt("seed"));
        sim.Run(outDir);

        // Experiment 2: QAE VaR Convergence
        Console.WriteLine("\n--- Experiment 2: QAE vs Classical MC VaR Convergence ---");
        var qaeRows = QaeVarConvergenceExperiment.Run();
        QaeVarConvergenceExperiment.ExportCsv(Path.Combine(outDir, "qae_var_convergence.csv"), qaeRows);

        // Experiment 3: LEO Latency Budget
        Console.WriteLine("\n--- Experiment 3: LEO Multi-Hop Latency Budget ---");
        var latRows = LeoLatencyAnalysisExperiment.Run();
        LeoLatencyAnalysisExperiment.ExportCsv(Path.Combine(outDir, "leo_latency_budget.csv"), latRows);

        // Experiment 4: QKD Key Rate Model
        Console.WriteLine("\n--- Experiment 4: Satellite QKD Key Rate Model ---");
        var qkdRows = QkdKeyRateExperiment.Run();
        QkdKeyRateExperiment.ExportCsv(Path.Combine(outDir, "qkd_key_rate_model.csv"), qkdRows);

        Console.WriteLine("\n================================================================");
        Console.WriteLine("  All experiments complete. Results written to: " + outDir);
        Console.WriteLine("================================================================");
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
            var val = (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                      ? args[++i] : "true";
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
