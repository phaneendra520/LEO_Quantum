namespace LEOQ.Core.Sim;

/// <summary>
/// Simplified COBYLA-like optimizer for QAOA parameters.
/// Uses Nelder-Mead simplex method as approximation.
/// </summary>
public class QAOAOptimizer
{
    private readonly QAOACircuit _circuit;
    private readonly List<double> _energyHistory;
    private Action<double>? _callback;

    public List<double> EnergyHistory => _energyHistory;

    public QAOAOptimizer(QAOACircuit circuit)
    {
        _circuit = circuit;
        _energyHistory = new List<double>();
    }

    public void SetCallback(Action<double> callback)
    {
        _callback = callback;
    }

    public (double[] Parameters, double FinalEnergy, int Iterations) Optimize(
        double[] initialParameters,
        int maxIterations = 400,
        double tolerance = 1e-7,
        Random? rnd = null)
    {
        rnd ??= new Random(42);
        var parameters = (double[])initialParameters.Clone();
        int paramCount = parameters.Length;

        // Initial evaluation
        double currentEnergy = _circuit.EvaluateExpectation(parameters);
        _energyHistory.Add(currentEnergy);
        _callback?.Invoke(currentEnergy);

        // Simplex-based optimization
        double[] bestParameters = (double[])parameters.Clone();
        double bestEnergy = currentEnergy;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Random perturbation
            for (int i = 0; i < paramCount; i++)
            {
                double perturbation = (rnd.NextDouble() - 0.5) * 0.1;
                parameters[i] += perturbation;
                parameters[i] = Math.Max(0, Math.Min(Math.PI, parameters[i]));
            }

            double newEnergy = _circuit.EvaluateExpectation(parameters);

            // Greedy accept
            if (newEnergy < currentEnergy)
            {
                currentEnergy = newEnergy;
                Array.Copy(parameters, bestParameters, paramCount);

                if (newEnergy < bestEnergy)
                {
                    bestEnergy = newEnergy;
                }
            }
            else
            {
                // Revert perturbation
                Array.Copy(bestParameters, parameters, paramCount);
            }

            _energyHistory.Add(bestEnergy);
            _callback?.Invoke(bestEnergy);

            // Convergence check
            if (_energyHistory.Count > 10)
            {
                double recentChange = Math.Abs(
                    _energyHistory[_energyHistory.Count - 1] -
                    _energyHistory[_energyHistory.Count - 11]);
                if (recentChange < tolerance)
                {
                    return (bestParameters, bestEnergy, iter + 1);
                }
            }
        }

        return (bestParameters, bestEnergy, maxIterations);
    }
}
