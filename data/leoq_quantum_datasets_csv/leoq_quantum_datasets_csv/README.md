# LEO-Q Quantum Component Test Datasets (CSV)
Generated: 2026-01-28T02:03:38.868390Z

These datasets are synthetic and intended for benchmarking and unit/integration tests for:
- QAE-driven VaR / tail-risk estimation
- QAOA portfolio optimization instances
- QKD/PQC session overhead simulation
- End-to-end decision-to-execute cycle experiments (compute + route + overhead)
- Noise sensitivity profiles

All files are CSV unless noted.

## Contents
1) var_qae/
   - var_returns_small.csv (1,024 scenarios, 8 assets)
   - var_returns_medium.csv (65,536 scenarios, 32 assets)
   - var_heavytail_small.csv (20,000 scenarios, 8 assets, regimes/jumps)
   - var_loss_surface_medium.csv (~62,500 rows; (s,v,r,t) grid)

2) qaoa/
   - instance_S_mu.csv, instance_S_sigma.csv, instance_S_constraints.csv
   - instance_M_mu.csv, instance_M_sigma.csv, instance_M_constraints.csv
   - instance_L_mu.csv, instance_L_sigma.csv, instance_L_constraints.csv
   - sigma_regime1.csv, sigma_regime2.csv, sigma_regime3.csv, regime_schedule.csv

3) qkd/
   - qkd_session_overhead.csv (10,000 sessions)

4) e2e/
   - routing_quantum_e2e.csv (10,000 pairs)

5) noise/
   - quantum_noise_profiles.csv (60 profiles)
