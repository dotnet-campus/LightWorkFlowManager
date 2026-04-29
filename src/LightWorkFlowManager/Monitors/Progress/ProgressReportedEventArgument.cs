namespace DC.LightWorkFlowManager.Monitors;

/// <summary>
/// 表示进度上报事件参数。
/// </summary>
/// <typeparam name="T">随进度一同上报的数据类型。</typeparam>
public readonly record struct ProgressReportedEventArgument<T>
{
    /// <summary>
    /// 初始化进度上报事件参数。
    /// </summary>
    /// <param name="progressPercentage">当前进度值。</param>
    /// <param name="value">随进度上报的数据。</param>
    public ProgressReportedEventArgument(ProgressPercentage progressPercentage, T? value)
    {
        ProgressPercentage = progressPercentage;
        Value = value;
    }

    /// <summary>
    /// 获取当前进度值。
    /// </summary>
    public ProgressPercentage ProgressPercentage { get; init; }

    /// <summary>
    /// 获取随进度上报的数据。
    /// </summary>
    public T? Value { get; init; }
}
