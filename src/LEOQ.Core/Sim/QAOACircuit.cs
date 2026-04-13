namespace LEOQ.Core.Sim;

/// <summary>
/// QAOA circuit construction and evaluation.
/// </summary>
public class QAOACircuit
{
    private readonly int _numQubits;
    private readonly int _numLayers;
    private readonly HamiltonianBuilder _hamiltonian;

    public QAOACircuit(int numQubits, int numLayers, HamiltonianBuilder hamiltonian)
    {
        _numQubits = numQubits;
        _numLayers = numLayers;
        _hamiltonian = hamiltonian;
    }

    public double EvaluateExpectation(double[] parameters)
    {
        if (parameters.Length != 2 * _numLayers)
            throw new ArgumentException($"Expected {2 * _numLayers} parameters, got {parameters.Length}");

        var simulator = new StatevectorSimulator(_numQubits);

        // Initial superposition
        simulator.ApplyHadamardLayer();

        // QAOA layers
        for (int layer = 0; layer < _numLayers; layer++)
        {
            double gamma = parameters[layer];
            double beta = parameters[_numLayers + layer];

            // Cost Hamiltonian
            foreach (var term in _hamiltonian.Terms)
            {
                ApplyCostHamiltonian(simulator, term, gamma);
            }

            // Mixer Hamiltonian (RX on all qubits)
            for (int q = 0; q < _numQubits; q++)
            {
                simulator.ApplyRX(q, 2 * beta);
            }
        }

        return simulator.ExpectationValue(_hamiltonian);
    }

    private void ApplyCostHamiltonian(StatevectorSimulator simulator, PauliTerm term, double gamma)
    {
        int[] zQubits = FindZQubits(term.PauliString);

        double angle = 2 * gamma * term.Coefficient;

        if (zQubits.Length == 1)
        {
            simulator.ApplyRZ(zQubits[0], angle);
        }
        else if (zQubits.Length == 2)
        {
            simulator.ApplyCNOT(zQubits[0], zQubits[1]);
            simulator.ApplyRZ(zQubits[1], angle);
            simulator.ApplyCNOT(zQubits[0], zQubits[1]);
        }
    }

    private int[] FindZQubits(string pauliString)
    {
        var zQubits = new List<int>();
        for (int i = 0; i < pauliString.Length; i++)
        {
            if (pauliString[i] == 'Z')
                zQubits.Add(i);
        }
        return zQubits.ToArray();
    }

    public Dictionary<string, int> MeasureQubits(double[] parameters, int shots, Random rnd)
    {
        var simulator = new StatevectorSimulator(_numQubits);
        simulator.ApplyHadamardLayer();

        for (int layer = 0; layer < _numLayers; layer++)
        {
            double gamma = parameters[layer];
            double beta = parameters[_numLayers + layer];

            foreach (var term in _hamiltonian.Terms)
            {
                ApplyCostHamiltonian(simulator, term, gamma);
            }

            for (int q = 0; q < _numQubits; q++)
            {
                simulator.ApplyRX(q, 2 * beta);
            }
        }

        return simulator.Sample(shots, rnd);
    }
}
