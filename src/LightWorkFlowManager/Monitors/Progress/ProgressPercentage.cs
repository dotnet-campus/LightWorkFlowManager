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
    /// <param name="value"></param>
    public ProgressPercentage(double value)
    {
        Value = value;

        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Range of {nameof(ProgressPercentage)} is between 0 to 1");
        }
    }

    public double Value { get; init; }

    public static ProgressPercentage MinValue => new ProgressPercentage(0);

    public static ProgressPercentage MaxValue => new ProgressPercentage(1);
}