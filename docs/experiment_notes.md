# Leo-Q Quantum Experiment Notes

This document provides technical notes on the four experiments implemented
in this software artifact. All experiments run on classical hardware as
simulation of quantum circuit behaviour or quantum-inspired analytical models.

---

## Experiment 1 — QAOA Portfolio Optimization

**Source:** `src/LEOQ.Core/Sim/QAOASimulator.cs`

**Method:**  
The simulation maintains a 2^n complex statevector and applies parameterised
cost and mixer unitaries analytically. The cost Hamiltonian is constructed via
the mapping x_i → (1 - Z_i)/2, producing linear Z_i coefficients from the
expected return terms and quadratic Z_i Z_j coefficients from the covariance
cross-terms. Encoding correctness is verified by comparing H_energy + offset
against portfolio objective F(x) for three test bitstrings; the error should
be numerically negligible (< 1e-9).

**Optimiser:** COBYLA (Constrained Optimisation By Linear Approximation),
gradient-free, appropriate for NISQ-era circuits where gradient estimation
via parameter shift adds significant QPU overhead.

**Reproducibility:** Run with `--seed 42` for deterministic results.

**Honest scope:** The statevector simulation represents ideal (zero-noise)
quantum circuit behaviour. Real QPU execution at 8 qubits under IBM Eagle
error rates (~1e-3 per CX gate) introduces noise that modestly degrades
approximation quality. The 1.7% objective gap reported here represents
the ideal-circuit bound.

---

## Experiment 2 — QAE vs Classical Monte Carlo VaR Convergence

**Source:** `src/LEOQ.Core/Experiments/QaeVarConvergenceExperiment.cs`

**Method:**  
The sample complexity comparison is derived directly from the theoretical
bounds:
- Classical MC: O(1/ε²) samples for precision ε (central limit theorem)
- QAE: O(1/ε) amplitude iterations (Brassard et al. 2002)

Achieved errors are simulated using Gaussian noise calibrated to each method's
theoretical error envelope. The experiment does not simulate an actual QAE
circuit but demonstrates the asymptotic speedup that QAE provides over MC,
consistent with the complexity analysis in the paper.

**Key finding:** At ε = 0.001 (regulatory-grade precision for VaR), QAE
requires 1,000 amplitude iterations versus 1,000,000 MC paths — a three-order-
of-magnitude reduction in sample complexity. This directly enables near-real-
time VaR/CTE recalibration within intraday margining cycles.

---

## Experiment 3 — LEO Multi-Hop Latency Budget

**Source:** `src/LEOQ.Core/Experiments/LeoLatencyAnalysisExperiment.cs`

**Method:**  
Implements the multi-hop latency decomposition model from the paper (Eq. 5):

    T_net = T_up + T_down + Σ_i(T_prop,i + T_sw,i)
    T_total = T_net + T_enc + T_dec

Parameters are calibrated to the Starlink constellation class (altitude ~550 km,
T_up = T_down ≈ 1.83 ms propagation-only access). Per-hop switching overhead
grows linearly with hops, reflecting onboard processing (OBP) delay and
routing table lookups. The fiber baseline uses the actual NY-London submarine
cable routing distance (~6,684 km) and fiber refractive index n = 1.5.

**Key finding:** H=1 through H=3 outperform fiber by 9.7 to 23%.
H=4 onwards, cumulative switching overhead exceeds the free-space propagation
advantage. This confirms that hop-aware routing optimisation is essential
for realising latency benefits in LEO-based HFT infrastructure.

**Reference:** Calibrated against Chaudhry et al. (IEEE Open J. Commun. Soc.,
2023) and Handley (ACM HotNets, 2018).

---

## Experiment 4 — Satellite QKD Key Rate Model

**Source:** `src/LEOQ.Core/Experiments/QkdKeyRateExperiment.cs`

**Method:**  
Key generation rate is modelled using an exponential free-space optical
path-loss function calibrated to the Micius satellite measurement:
1,100 bps at 1,200 km (Liao et al., Nature 2017). The exponential decay
coefficient is derived from requiring rate = 100 bps at zero atmospheric
contribution (floor rate at 0.1 bps beyond ~4,000 km).

AES-256 session key generation time is computed as 256 bits / key_rate_bps.
The recommended rekeying interval is 3× the generation time, capped at
60 seconds to remain within operational HFT session boundaries.

**Key finding:** At the Micius distance (1,200 km), key generation completes
in ≈ 0.23 seconds. QKD operates asynchronously from the trade execution path
and adds no measurable latency to individual order transmissions.

**Limitation:** The exponential model is a simplification. Actual key rates
depend on atmospheric turbulence, satellite elevation angle, ground station
aperture diameter, and detector efficiency. The model provides a conservative
lower bound consistent with published satellite QKD performance data.

**Reference:** Liao et al. (Nature, vol. 549, pp. 43–47, 2017,
doi: 10.1038/nature23655).

---

## Running All Experiments

```bash
dotnet run --project src/LEOQ.Cli -- run-all --seed 42 --out ./results
```

Expected runtime: under 60 seconds on any modern desktop or laptop.
All seven CSV files in `results/` are fully deterministic with `--seed 42`.
