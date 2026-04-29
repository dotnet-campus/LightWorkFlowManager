namespace DC.LightWorkFlowManager.Monitors;

/// <summary>
/// 表示子进度合成器的注册信息。
/// </summary>
public readonly record struct SubProgressCompositorInfo
{
    /// <summary>
    /// 初始化子进度合成器信息。
    /// </summary>
    /// <param name="name">子进度名称。</param>
    /// <param name="weight">子进度权重。</param>
    public SubProgressCompositorInfo(string name, double weight)
    {
        Name = name;
        Weight = weight;
    }

    /// <summary>
    /// 获取子进度名称。
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 获取子进度权重。
    /// </summary>
    public double Weight { get; init; }
}
