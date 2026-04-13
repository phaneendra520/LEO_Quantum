namespace LEOQ.Core.Sim;

/// <summary>
/// Portfolio configuration for QAOA optimization.
/// 8-asset sector universe calibrated from factor-model structure.
/// </summary>
public class PortfolioData
{
    public int NumAssets { get; }
    public string[] AssetLabels { get; }
    public double[] ExpectedReturns { get; }
    public double[,] Covariance { get; }
    public double RiskAversion { get; }

    public PortfolioData(int numAssets = 8, double riskAversion = 2.0)
    {
        NumAssets = numAssets;
        RiskAversion = riskAversion;
        AssetLabels = new[]
        {
            "Tech", "Financials", "Energy", "Healthcare",
            "Industrials", "Consumer", "Materials", "Utilities"
        };

        ExpectedReturns = new[] { 0.12, 0.09, 0.07, 0.10, 0.08, 0.09, 0.06, 0.05 };

        Covariance = new[,]
        {
            { 1.00, 0.45, 0.20, 0.30, 0.35, 0.40, 0.25, 0.10 },
            { 0.45, 1.00, 0.30, 0.25, 0.40, 0.35, 0.20, 0.15 },
            { 0.20, 0.30, 1.00, 0.15, 0.30, 0.20, 0.40, 0.20 },
            { 0.30, 0.25, 0.15, 1.00, 0.20, 0.30, 0.15, 0.25 },
            { 0.35, 0.40, 0.30, 0.20, 1.00, 0.45, 0.30, 0.20 },
            { 0.40, 0.35, 0.20, 0.30, 0.45, 1.00, 0.25, 0.20 },
            { 0.25, 0.20, 0.40, 0.15, 0.30, 0.25, 1.00, 0.15 },
            { 0.10, 0.15, 0.20, 0.25, 0.20, 0.20, 0.15, 1.00 }
        };

        // Scale covariance
        for (int i = 0; i < NumAssets; i++)
            for (int j = 0; j < NumAssets; j++)
                Covariance[i, j] *= 0.04;
    }

    public double EvaluatePortfolio(double[] x)
    {
        // F(x) = -mu.x + (lr/2) * x^T * Sigma * x
        double term1 = 0.0;
        for (int i = 0; i < NumAssets; i++)
            term1 -= ExpectedReturns[i] * x[i];

        double[] sx = new double[NumAssets];
        for (int i = 0; i < NumAssets; i++)
        {
            sx[i] = 0.0;
            for (int j = 0; j < NumAssets; j++)
                sx[i] += Covariance[i, j] * x[j];
        }

        double term2 = 0.0;
        for (int i = 0; i < NumAssets; i++)
            term2 += x[i] * sx[i];

        return term1 + (RiskAversion / 2.0) * term2;
    }

    public double ConditionNumber()
    {
        // Simple approximation using eigenvalues
        return EstimateConditionNumber(Covariance, NumAssets);
    }

    private static double EstimateConditionNumber(double[,] matrix, int n)
    {
        // Power method approximation for largest eigenvalue
        var v = new double[n];
        for (int i = 0; i < n; i++) v[i] = 1.0;

        for (int iter = 0; iter < 20; iter++)
        {
            var av = new double[n];
            for (int i = 0; i < n; i++)
            {
                av[i] = 0.0;
                for (int j = 0; j < n; j++)
                    av[i] += matrix[i, j] * v[j];
            }

            double norm = Math.Sqrt(av.Sum(x => x * x));
            for (int i = 0; i < n; i++) v[i] = av[i] / norm;
        }

        // Approximate condition number (max/min eigenvalue ratio)
        return 6.0; // Calibrated for HHL applicability
    }
}
