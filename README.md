# Leo-Q: Quantum-Secured LEO Satellite Network — Simulation Framework

[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.XXXXXXXX.svg)](10.5281/zenodo.18752219)
[![License: CC BY 4.0](https://img.shields.io/badge/License-CC%20BY%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by/4.0/)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com)

## Overview

This repository contains the reference implementation and simulation framework for the Leo-Q architecture described in:

> Phaneendra Vayu Kumar Yerra, **"Leo-Q: Quantum-Secured Low Earth Orbit Satellite Network for High-Frequency Trading and Real-Time Portfolio Optimization,"** *IEEE Access*, Manuscript ID 2026-10381 (under review).

The framework provides four reproducible quantum simulation experiments supporting the empirical claims of the paper, implemented in C# on .NET 8. All computations are classical simulations of quantum circuit behaviour — no physical quantum hardware is required to reproduce the results.

---

## Research Context

Modern high-frequency trading (HFT) infrastructure faces two binding constraints: the physical propagation latency of terrestrial fiber networks, and the computational cost of real-time portfolio risk analytics. The Leo-Q architecture addresses both simultaneously through three co-designed layers: LEO free-space optical inter-satellite routing, quantum algorithm acceleration (QAE, HHL, QAOA), and satellite-based Quantum Key Distribution (QKD).

This software artifact provides analytical and simulation evidence for three of those layers.

---

## Repository Structure

```
leo-q-quantum-sim/
├── README.md                          This file
├── CITATION.cff                       Machine-readable citation metadata
├── LICENSE                            Creative Commons Attribution 4.0
├── LEOQ.sln                           .NET solution file
├── global.json                        SDK version pin (.NET 8)
│
├── src/
│   ├── LEOQ.Core/                     Core simulation library
│   │   ├── Sim/                       QAOA circuit, statevector, Hamiltonian
│   │   ├── Routing/                   LEO satellite routing algorithms
│   │   ├── Metrics/                   VaR, slippage, statistical utilities
│   │   ├── Crypto/                    QKD and PQC session stubs
│   │   ├── Trading/                   Backtester and market feed
│   │   ├── Util/                      CSV export, pathfinding
│   │   └── Experiments/               Four quantum experiment modules
│   │       ├── QaeVarConvergenceExperiment.cs   (Experiment 2)
│   │       ├── LeoLatencyAnalysisExperiment.cs  (Experiment 3)
│   │       └── QkdKeyRateExperiment.cs          (Experiment 4)
│   │
│   ├── LEOQ.Cli/                      Command-line runner
│   │   └── Program.cs
│   │
│   └── LEOQ.Tests/                    xUnit test suite (8 tests)
│       └── RoutingTests.cs
│
├── experiments/
│   ├── 01_qaoa_portfolio/             Experiment 1 description
│   ├── 02_qae_var_convergence/        Experiment 2 description
│   ├── 03_leo_latency_analysis/       Experiment 3 description
│   └── 04_qkd_key_rate_model/         Experiment 4 description
│
├── results/                           Pre-computed outputs (all reproducible)
│   ├── qaoa_results.csv               QAOA top-12 portfolio allocations
│   ├── convergence_history.csv        QAOA energy per optimizer iteration
│   ├── nisq_report.csv                NISQ hardware compliance metrics
│   ├── asset_inclusion.csv            Asset marginal inclusion probabilities
│   ├── qae_var_convergence.csv        QAE vs MC sample complexity comparison
│   ├── leo_latency_budget.csv         LEO H=1..6 vs fiber latency breakdown
│   └── qkd_key_rate_model.csv         QKD key rate vs satellite distance
│
└── data/
    └── portfolio/                     Portfolio calibration data
        └── leoq_dataset_sample.csv
```

---

## Experiments

### Experiment 1 — QAOA Portfolio Optimization

**Command:** `dotnet run --project src/LEOQ.Cli -- qaoa --out ./results`

Implements the Quantum Approximate Optimization Algorithm (QAOA) for an 8-asset sector portfolio using a classical statevector simulation. The Hamiltonian encoding maps the binary portfolio selection objective F(x) = -μ·x + (λ/2)xᵀΣx into an Ising cost Hamiltonian via xᵢ → (1−Zᵢ)/2.

**Key results (paper Section VI.G, Table V):**
- 8 logical qubits, P=2 QAOA layers, circuit depth 68, 112 CX gates
- COBYLA optimizer converges in ~325 iterations
- Portfolio objective within **1.7%** of classical brute-force optimum (F* = −0.118)
- All NISQ constraints satisfied: qubits < 10, depth < 10³, CX gates < 500

**Paper claim supported:** Section V.I Table IV, Section VI.G

---

### Experiment 2 — QAE vs Classical Monte Carlo VaR Convergence

**Command:** `dotnet run --project src/LEOQ.Cli -- exp-qae --out ./results`

Demonstrates the quadratic speedup of Quantum Amplitude Estimation over classical Monte Carlo for Value-at-Risk estimation across seven precision targets (ε = 0.1 to 0.001).

| Precision ε | MC Samples | QAE Iterations | Speedup |
|-------------|-----------|----------------|---------|
| 0.100       | 100       | 10             | 10×     |
| 0.010       | 10,000    | 100            | 100×    |
| 0.001       | 1,000,000 | 1,000          | 1,000×  |

**Paper claim supported:** Section VI.B — "QAE reduces nested simulation runtime by three orders of magnitude at ε = 10⁻³"

---

### Experiment 3 — LEO Multi-Hop Latency Budget Analysis

**Command:** `dotnet run --project src/LEOQ.Cli -- exp-latency --out ./results`

Systematically evaluates end-to-end one-way latency for H = 1..6 inter-satellite hops on the New York to London corridor (geodesic 5,570 km), implementing the decomposition model T_net = T_up + T_down + Σᵢ(T_prop,i + T_sw,i).

| Scenario | One-Way (ms) | RTT (ms) | vs Fiber |
|----------|-------------|---------|---------|
| Fiber    | 30.6        | 61.2    | baseline |
| LEO H=1  | 18.9        | 37.8    | −38.2%  |
| LEO H=3  | 23.1        | 46.3    | −24.5%  |
| LEO H=6  | 75.1        | 150.2   | +145%   |

**Paper claim supported:** Section VI.A — "25–42% one-way latency reduction relative to terrestrial fiber" and Table III.

---

### Experiment 4 — Satellite QKD Key Rate and Session Model

**Command:** `dotnet run --project src/LEOQ.Cli -- exp-qkd --out ./results`

Models satellite QKD key generation rates at distances from 500 km to 4,000 km, calibrated against Micius satellite measurements (Liao et al., Nature 2017). Demonstrates operational feasibility of the rekeying interval design for HFT trade channel security.

**Key results:**
- At 1,200 km (Micius distance): key rate ≈ 1.1 kbps, AES-256 key generated in ≈ 0.23 seconds
- Rekeying interval of 1–60 seconds is achievable at all operationally relevant distances
- QKD operates asynchronously from trade execution — zero latency impact on individual orders

**Paper claim supported:** Section V.G and Section VIII.B

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (free, cross-platform)
- No additional packages required — all dependencies are NuGet-resolved on first build

### Run All Experiments (Single Command)

```bash
git clone https://github.com/phaneendra520/leo-q-framework
cd leo-q-framework
dotnet run --project src/LEOQ.Cli -- run-all --out ./results --seed 42
```

This reproduces all four experiments and writes seven CSV files to `./results/`.

### Run Individual Experiments

```bash
# Experiment 1: QAOA portfolio optimization
dotnet run --project src/LEOQ.Cli -- qaoa --layers 2 --shots 4096 --seed 42

# Experiment 2: QAE vs Monte Carlo VaR convergence
dotnet run --project src/LEOQ.Cli -- exp-qae

# Experiment 3: LEO latency budget (H=1..6 hops vs fiber)
dotnet run --project src/LEOQ.Cli -- exp-latency

# Experiment 4: QKD key rate model
dotnet run --project src/LEOQ.Cli -- exp-qkd

# LEO routing benchmark (BaselineRouter vs LatencyAware vs RiskAware)
dotnet run --project src/LEOQ.Cli -- bench --sats 24 --pairs 20 --seed 42
```

### Run Tests

```bash
dotnet test src/LEOQ.Tests
```

All 8 tests should pass. Tests are deterministic when seeded and validate the core numerical claims.

---

## Reproducibility

All experiments use deterministic random seeds (default: 42). Passing `--seed 42` to any command guarantees bit-for-bit identical results across platforms and .NET runtime versions within the same major version.

The pre-computed results in `/results/` were generated with:
```
dotnet run --project src/LEOQ.Cli -- run-all --out ./results --seed 42
```
on .NET 8.0.x (pinned in `global.json`).

---

## Implementation Notes

**QAOA Circuit (Experiment 1):** The statevector simulation maintains a 2^n complex amplitude vector and applies parameterised cost and mixer unitaries exactly. This reproduces ideal (zero-noise) quantum circuit behaviour. Gate counts and circuit depth reported in the NISQ compliance table are exact counts from the constructed circuit.

**QAE Convergence (Experiment 2):** The sample complexity comparison uses the theoretical bounds O(1/ε²) for classical Monte Carlo and O(1/ε) for QAE, with realistic noise added via Box-Muller sampling to demonstrate achieved error under the complexity predictions.

**LEO Latency (Experiment 3):** The model implements the multi-hop decomposition from the paper (Equation 5) using measured speed-of-light constants and empirical switching overhead ranges from Table I of the paper.

**QKD Key Rate (Experiment 4):** The distance-rate model is calibrated to the published Micius result (1.1 kbps at 1,200 km) using an exponential free-space optical path loss model.

---

## Citation

If you use this software in your research, please cite:

```bibtex
@software{yerra2026leoq_dotnet,
  author    = {Yerra, Phaneendra Vayu Kumar},
  title     = {{Leo-Q: Quantum-Secured LEO Satellite Network — .NET Simulation Framework}},
  year      = {2026},
  publisher = {Zenodo},
  doi       = {10.5281/zenodo.18752219},
  url       = {https://github.com/phaneendra520/leo-q-framework}
}
```


---

## Author

**Phaneendra Vayu Kumar Yerra**  
Vice President, Global Markets Technology, Bank of America  
IEEE Member | ORCID: [0009-0006-9165-3331](https://orcid.org/0009-0006-9165-3331)  
Affiliated Researcher, University of the Cumberlands

---

## License

This software is released under the [Creative Commons Attribution 4.0 International License](https://creativecommons.org/licenses/by/4.0/). You are free to share and adapt the material for any purpose, provided appropriate credit is given.
