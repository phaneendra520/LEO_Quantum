namespace LEOQ.Core.Trading;

public enum Side
{
    Buy,
    Sell,
}

public sealed record Order(
    Side Side,
    int SubmitIndex,
    int ExecuteIndex,
    double IntendedPrice,
    double ExecutedPrice,
    double DelayMs);

public sealed record BacktestSummary(
    int Orders,
    double AvgDelayMs,
    double AvgSlippageAbs,
    double AvgSlippagePct,
    double Var99);
