# LEO-Q .NET Solution (VS Code / Visual Studio Ready)

This repository is a **self-contained .NET solution** that implements a research prototype for **LEO‑Q**:

- A simplified **LEO satellite mesh simulator** (topology, latency, jitter)
- Multiple **routing policies** (baseline, latency-aware, risk-aware)
- **Quantum-secured session scaffolding** (QKD/PQC stubs; *not production crypto*)
- A toy **trading impact sandbox** (delayed execution → slippage)
- A **dataset generator** and **ML.NET regression trainer** for latency prediction

## Important Note (Security)
The crypto components in this repository are **stubs for interface demonstration only**. They do **not** provide real security.

## Repo Structure

- `src/LEOQ.Core`  — core library (topology, routing, crypto stubs, trading, metrics)
- `src/LEOQ.Cli`   — console app (benchmarks, dataset generation, demos)
- `src/LEOQ.ML`    — ML.NET training app (regression model + model export)
- `src/LEOQ.Tests` — unit tests (xUnit)
- `data/`          — sample datasets and outputs

## Prerequisites

- Install the **.NET SDK (recommended: .NET 8)**
- VS Code extensions:
  - **C# Dev Kit** (Microsoft)
  - **.NET Install Tool** (optional)

## Quick Start (VS Code)

1) Open this folder in VS Code
2) Restore and build:

```bash
dotnet restore
dotnet build
```

3) Run a routing benchmark:

```bash
dotnet run --project src/LEOQ.Cli -- bench --sats 24 --pairs 10
```

4) Generate a dataset (CSV):

```bash
dotnet run --project src/LEOQ.Cli -- dataset --out data/leoq_dataset.csv --samples 500
```

5) Train the ML model (ML.NET):

```bash
dotnet run --project src/LEOQ.ML -- --data data/leoq_dataset.csv --model data/Model.zip
```

## CLI Commands

### `bench`
Compares baseline vs latency-aware vs risk-aware routing across random pairs.

### `dataset`
Generates a synthetic CSV dataset:

Columns: `hops`, `distance_km`, `delay_ms`

### `backtest`
Runs a toy trading backtest where execution delay impacts fill price and slippage.

### `crypto-demo`
Demonstrates the QKD/PQC stubs through a `SecureSession` wrapper.


## Build Notes

This solution was created using standard .NET project formats. If your machine has .NET 8 installed, it should build and run without special setup.

If you want, I can also generate a GitHub Actions workflow (`.github/workflows/ci.yml`) to run tests automatically.
