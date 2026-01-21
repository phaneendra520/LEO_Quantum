using System;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace LEOQ.ML;

internal static class Program
{
    private static int Main(string[] args)
    {
        var opt = Args.Parse(args);
        var dataPath = opt.String("data", Path.Combine("data", "leoq_dataset.csv"));
        var modelPath = opt.String("model", Path.Combine("data", "Model.zip"));

        if (!File.Exists(dataPath))
        {
            Console.Error.WriteLine($"Dataset not found: {dataPath}");
            Console.Error.WriteLine("Generate it using: dotnet run --project src/LEOQ.Cli -- dataset");
            return 2;
        }

        var ml = new MLContext(seed: 42);

        var data = ml.Data.LoadFromTextFile<LatencyRow>(dataPath, hasHeader: true, separatorChar: ',');
        var split = ml.Data.TrainTestSplit(data, testFraction: 0.2);

        var pipeline = ml.Transforms.Concatenate(
                outputColumnName: "Features",
                nameof(LatencyRow.Hops),
                nameof(LatencyRow.DistanceKm))
            .Append(ml.Regression.Trainers.FastTree(labelColumnName: nameof(LatencyRow.DelayMs), featureColumnName: "Features"));

        var model = pipeline.Fit(split.TrainSet);
        var predictions = model.Transform(split.TestSet);
        var metrics = ml.Regression.Evaluate(predictions, labelColumnName: nameof(LatencyRow.DelayMs));

        Console.WriteLine("ML.NET Latency Prediction (Regression)");
        Console.WriteLine($"Data:  {dataPath}");
        Console.WriteLine($"RMSE:  {metrics.RootMeanSquaredError:F6}");
        Console.WriteLine($"MAE:   {metrics.MeanAbsoluteError:F6}");
        Console.WriteLine($"R^2:   {metrics.RSquared:F6}");

        Directory.CreateDirectory(Path.GetDirectoryName(modelPath) ?? ".");
        ml.Model.Save(model, split.TrainSet.Schema, modelPath);
        Console.WriteLine($"Model saved: {modelPath}");

        return 0;
    }
}

public sealed class LatencyRow
{
    [LoadColumn(0)]
    public float Hops { get; set; }

    [LoadColumn(1)]
    public float DistanceKm { get; set; }

    [LoadColumn(2), ColumnName("Label")]
    public float DelayMs { get; set; }
}

internal sealed class Args
{
    private readonly System.Collections.Generic.Dictionary<string, string> _kv;

    private Args(System.Collections.Generic.Dictionary<string, string> kv) => _kv = kv;

    public static Args Parse(string[] args)
    {
        var kv = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
}
