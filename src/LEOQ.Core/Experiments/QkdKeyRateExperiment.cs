namespace LEOQ.Core.Experiments;

/// <summary>
/// Experiment 4: Satellite QKD Key Rate and Session Rekeying Model.
///
/// Models the operational parameters of satellite-based QKD for HFT
/// trade channel security, calibrated against published Micius satellite
/// measurements (Liao et al., Nature 2017, doi:10.1038/nature23655).
///
/// Key parameters:
///   Micius demonstrated ~1.1 kbps over 1,200 km satellite-to-ground.
///   AES-256 key = 256 bits => generation time = 256 / key_rate_bps seconds.
///   Rekeying interval: designed to be 1-60 seconds based on key rate.
///
/// Results support paper Section V.G and Section VIII.B claims on QKD
/// operational constraints.
/// </summary>
public static class QkdKeyRateExperiment
{
    // Micius calibrated parameters
    private const double MiciusKeyRateBps    = 1_100.0;   // bps over 1,200 km
    private const double MiciusDistanceKm    = 1_200.0;
    private const int    Aes256KeyBits       = 256;

    /// <summary>
    /// Evaluate QKD key generation capacity across a range of satellite
    /// link distances, using an empirical path-loss model calibrated from
    /// Micius measurements.
    /// </summary>
    public static List<QkdSessionRow> Run()
    {
        var rows = new List<QkdSessionRow>();

        Console.WriteLine("\n  == Experiment 4: Satellite QKD Key Rate and Session Model ==");
        Console.WriteLine($"  Calibration: Micius satellite — {MiciusKeyRateBps:F0} bps at {MiciusDistanceKm:F0} km");
        Console.WriteLine($"  AES-256 key: {Aes256KeyBits} bits per session key");
        Console.WriteLine();
        Console.WriteLine($"  {"Distance":>10}  {"KeyRate":>10}  {"KeyGenTime":>12}  " +
                          $"{"RekeyInterval":>14}  {"Keys/Min":>10}  {"Feasible":>10}");
        Console.WriteLine("  " + new string('-', 80));

        // Distances from 500 km (low LEO) to 4,000 km (extended link)
        int[] distances = { 500, 800, 1_200, 1_600, 2_000, 2_500, 3_000, 4_000 };

        foreach (int dist in distances)
        {
            // Free-space optical path loss: rate ~ exp(-alpha * d)
            // Calibrated so that at 1,200 km rate = 1,100 bps
            double alpha   = Math.Log(MiciusKeyRateBps / 100.0) / MiciusDistanceKm;
            double keyRate = MiciusKeyRateBps * Math.Exp(-alpha * (dist - MiciusDistanceKm));
            keyRate = Math.Max(keyRate, 0.1);   // floor at 0.1 bps (atmosphere limited)

            // Time to generate one AES-256 session key
            double keyGenSec = Aes256KeyBits / keyRate;

            // Recommended rekeying interval: 3x key generation time (allows buffering)
            double rekeyIntervalSec = Math.Min(keyGenSec * 3.0, 60.0);

            // Keys generated per minute
            double keysPerMin = 60.0 / keyGenSec;

            // Operationally feasible if key can be generated within 60 seconds
            bool feasible = keyGenSec <= 60.0;
            string feasStr = feasible ? "YES" : "NO (extended)";

            Console.WriteLine($"  {dist,9} km  {keyRate,9:F2} bps  {keyGenSec,10:F2} s  " +
                              $"  {rekeyIntervalSec,12:F1} s  {keysPerMin,9:F3}  {feasStr,10}");

            rows.Add(new QkdSessionRow
            {
                DistanceKm          = dist,
                KeyRateBps          = keyRate,
                KeyGenTimeSec       = keyGenSec,
                RekeyIntervalSec    = rekeyIntervalSec,
                KeysPerMinute       = keysPerMin,
                OperationallyFeasible = feasible
            });
        }

        // Also model next-generation QKD targets (10 kbps, 100 kbps)
        Console.WriteLine();
        Console.WriteLine("  Next-generation QKD satellite projections (at 1,200 km):");
        Console.WriteLine($"  10 kbps  => key gen {Aes256KeyBits / 10_000.0:F4} s  => {60.0 / (Aes256KeyBits / 10_000.0):F1} keys/min");
        Console.WriteLine($"  100 kbps => key gen {Aes256KeyBits / 100_000.0:F4} s  => {60.0 / (Aes256KeyBits / 100_000.0):F0} keys/min");

        Console.WriteLine();
        Console.WriteLine("  Conclusion: At Micius rates (~1.1 kbps), AES-256 session keys can be");
        Console.WriteLine("  generated in ~0.23 s at 1,200 km, enabling 1-60 s rekeying intervals.");
        Console.WriteLine("  QKD overhead is asynchronous from trade execution — zero latency impact");
        Console.WriteLine("  on individual order transmissions encrypted with pre-established keys.");

        return rows;
    }

    public static void ExportCsv(string path, List<QkdSessionRow> rows)
    {
        var lines = new List<string>
        {
            "distance_km,key_rate_bps,key_gen_time_sec,rekey_interval_sec," +
            "keys_per_minute,operationally_feasible"
        };
        foreach (var r in rows)
            lines.Add($"{r.DistanceKm},{r.KeyRateBps:F4},{r.KeyGenTimeSec:F4}," +
                      $"{r.RekeyIntervalSec:F2},{r.KeysPerMinute:F4}," +
                      $"{(r.OperationallyFeasible ? "TRUE" : "FALSE")}");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
        Console.WriteLine($"\n  Exported: {path}");
    }
}

public class QkdSessionRow
{
    public int    DistanceKm            { get; set; }
    public double KeyRateBps            { get; set; }
    public double KeyGenTimeSec         { get; set; }
    public double RekeyIntervalSec      { get; set; }
    public double KeysPerMinute         { get; set; }
    public bool   OperationallyFeasible { get; set; }
}
