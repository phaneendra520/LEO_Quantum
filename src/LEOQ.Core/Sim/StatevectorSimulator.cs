namespace LEOQ.Core.Sim;

/// <summary>
/// Simple statevector simulator for QAOA circuits.
/// Supports Hadamard, RZ, RX, and CNOT gates.
/// </summary>
public class StatevectorSimulator
{
    private double[] _statevector;
    private int _numQubits;

    public StatevectorSimulator(int numQubits)
    {
        _numQubits = numQubits;
        int dim = 1 << numQubits;
        _statevector = new double[dim];
        _statevector[0] = 1.0; // |00...0>
    }

    public double[] GetStatevector() => (double[])_statevector.Clone();

    public void ApplyHadamardLayer()
    {
        int dim = 1 << _numQubits;
        var newState = new double[dim];
        double factor = 1.0 / Math.Sqrt(2.0);

        for (int i = 0; i < dim; i++)
        {
            for (int q = 0; q < _numQubits; q++)
            {
                int flipped = i ^ (1 << q);
                newState[i] += factor * _statevector[flipped];
            }
        }

        _statevector = newState;
    }

    public void ApplyRZ(int qubit, double angle)
    {
        int dim = 1 << _numQubits;
        double cos = Math.Cos(angle / 2.0);
        double sinI = -Math.Sin(angle / 2.0);

        for (int i = 0; i < dim; i++)
        {
            int bit = (i >> qubit) & 1;
            if (bit == 0)
                _statevector[i] *= cos;
            else
                _statevector[i] *= (cos + sinI); // e^(-i*angle/2) ≈ cos - i*sin
        }
    }

    public void ApplyRX(int qubit, double angle)
    {
        int dim = 1 << _numQubits;
        double cos = Math.Cos(angle / 2.0);
        double sinI = -Math.Sin(angle / 2.0);

        var newState = new double[dim];

        for (int i = 0; i < dim; i++)
        {
            int bit = (i >> qubit) & 1;
            int flipped = i ^ (1 << qubit);

            if (bit == 0)
                newState[i] += cos * _statevector[i] + sinI * _statevector[flipped];
            else
                newState[i] += cos * _statevector[i] + sinI * _statevector[flipped];
        }

        _statevector = newState;
    }

    public void ApplyCNOT(int control, int target)
    {
        int dim = 1 << _numQubits;
        var newState = new double[dim];

        for (int i = 0; i < dim; i++)
        {
            int controlBit = (i >> control) & 1;
            if (controlBit == 1)
            {
                int flipped = i ^ (1 << target);
                newState[flipped] = _statevector[i];
            }
            else
            {
                newState[i] = _statevector[i];
            }
        }

        _statevector = newState;
    }

    public double ExpectationValue(HamiltonianBuilder hamiltonian)
    {
        return hamiltonian.EvaluateExpectationValue(_statevector, _numQubits);
    }

    public Dictionary<string, int> Sample(int shots, Random rnd)
    {
        var counts = new Dictionary<string, int>();
        double[] probabilities = _statevector.Select(a => a * a).ToArray();

        // Cumulative probability
        var cumProb = new double[probabilities.Length];
        cumProb[0] = probabilities[0];
        for (int i = 1; i < probabilities.Length; i++)
            cumProb[i] = cumProb[i - 1] + probabilities[i];

        for (int shot = 0; shot < shots; shot++)
        {
            double r = rnd.NextDouble();
            int index = Array.BinarySearch(cumProb, r);
            if (index < 0) index = ~index;
            if (index >= cumProb.Length) index = cumProb.Length - 1;

            string bitstring = Convert.ToString(index, 2).PadLeft(_numQubits, '0');

            if (counts.ContainsKey(bitstring))
                counts[bitstring]++;
            else
                counts[bitstring] = 1;
        }

        return counts;
    }
}
