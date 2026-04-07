using System;

namespace DC.LightWorkFlowManager.Monitors;

/// <summary>
/// 进度百分比 0-1 范围
/// </summary>
public readonly record struct ProgressPercentage
{
    /// <summary>
    /// 进度百分比 0-1 范围
    /// </summary>
    /// <param name="value">进度值，范围在 0 到 1 之间。</param>
    public ProgressPercentage(double value)
    {
        Value = value;

        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Range of {nameof(ProgressPercentage)} is between 0 to 1");
        }
    }

    /// <summary>
    /// 获取当前进度值。
    /// </summary>
    public double Value { get; init; }

    /// <summary>
    /// 获取最小进度值。
    /// </summary>
    public static ProgressPercentage MinValue => new ProgressPercentage(0);

    /// <summary>
    /// 获取最大进度值。
    /// </summary>
    public static ProgressPercentage MaxValue => new ProgressPercentage(1);
}