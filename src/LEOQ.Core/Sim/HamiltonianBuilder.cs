namespace LEOQ.Core.Sim;

/// <summary>
/// Represents a Pauli term in the Ising Hamiltonian.
/// </summary>
public class PauliTerm
{
    public string PauliString { get; set; } // e.g., "IIZIIZ" (I, X, Y, Z)
    public double Coefficient { get; set; }

    public PauliTerm(string pauliString, double coefficient)
    {
        PauliString = pauliString;
        Coefficient = coefficient;
    }
}

/// <summary>
/// Builds the cost Hamiltonian for portfolio optimization.
/// H_C = sum_i a_i Z_i + sum_{i<j} b_ij Z_i Z_j
/// </summary>
public class HamiltonianBuilder
{
    public List<PauliTerm> Terms { get; }

    public HamiltonianBuilder()
    {
        Terms = new List<PauliTerm>();
    }

    public static HamiltonianBuilder BuildFromPortfolio(PortfolioData portfolio)
    {
        var builder = new HamiltonianBuilder();
        int n = portfolio.NumAssets;
        double lr = portfolio.RiskAversion;
        var mu = portfolio.ExpectedReturns;
        var sigma = portfolio.Covariance;

        // Single-qubit Z terms: a_i = mu_i/2 - (lr/4) * sum_j sigma_ij
        for (int i = 0; i < n; i++)
        {
            double sumSigma = 0.0;
            for (int j = 0; j < n; j++)
                sumSigma += sigma[i, j];

            double coeff = mu[i] / 2.0 - (lr / 4.0) * sumSigma;

            if (Math.Abs(coeff) > 1e-12)
            {
                var pauliStr = new char[n];
                for (int k = 0; k < n; k++)
                    pauliStr[k] = 'I';
                pauliStr[n - 1 - i] = 'Z'; // Reverse indexing

                builder.Terms.Add(new PauliTerm(new string(pauliStr), coeff));
            }
        }

        // Two-qubit ZZ terms: b_ij = (lr/4) * sigma_ij
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                double coeff = (lr / 4.0) * sigma[i, j];

                if (Math.Abs(coeff) > 1e-12)
                {
                    var pauliStr = new char[n];
                    for (int k = 0; k < n; k++)
                        pauliStr[k] = 'I';
                    pauliStr[n - 1 - i] = 'Z';
                    pauliStr[n - 1 - j] = 'Z';

                    builder.Terms.Add(new PauliTerm(new string(pauliStr), coeff));
                }
            }
        }

        return builder;
    }

    public double EvaluateExpectationValue(double[] statevector, int numQubits)
    {
        double expectation = 0.0;

        foreach (var term in Terms)
        {
            expectation += term.Coefficient * EvaluatePauliTerm(statevector, term.PauliString, numQubits);
        }

        return expectation;
    }

    private static double EvaluatePauliTerm(double[] statevector, string pauliStr, int numQubits)
    {
        double expect = 0.0;
        int dim = 1 << numQubits;

        for (int i = 0; i < dim; i++)
        {
            for (int j = 0; j < dim; j++)
            {
                // Check if measurement would give consistent eigenvalue
                int eigenvalueProduct = 1;
                bool isEigenvector = true;

                for (int q = 0; q < numQubits; q++)
                {
                    if (pauliStr[q] == 'Z')
                    {
                        int bi = (i >> q) & 1;
                        int bj = (j >> q) & 1;
                        if (bi != bj)
                        {
                            isEigenvector = false;
                            break;
                        }
                        eigenvalueProduct *= (1 - 2 * bi); // +1 for |0>, -1 for |1>
                    }
                    else if (pauliStr[q] != 'I')
                    {
                        isEigenvector = false;
                        break;
                    }
                }

                if (isEigenvector)
                {
                    expect += statevector[i] * statevector[j] * eigenvalueProduct;
                }
            }
        }

        return Math.Abs(expect) > 1e-14 ? expect : 0.0;
    }

    public double ComputeHamiltonianEnergy(string bitstring, int numQubits)
    {
        double energy = 0.0;

        foreach (var term in Terms)
        {
            double eigenvalue = 1.0;
            for (int i = 0; i < numQubits; i++)
            {
                if (term.PauliString[i] == 'Z')
                {
                    int bit = int.Parse(bitstring[numQubits - 1 - i].ToString());
                    eigenvalue *= (1 - 2 * bit); // +1 for |0>, -1 for |1>
                }
            }
            energy += term.Coefficient * eigenvalue;
        }

        return energy;
    }
}
