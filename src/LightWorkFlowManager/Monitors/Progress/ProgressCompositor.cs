using System;
using System.Collections.Generic;

namespace DC.LightWorkFlowManager.Monitors;

/// <summary>
/// 进度合成器，允许包含多个子进度
/// </summary>
/// <typeparam name="T"></typeparam>
/// 规则：
/// - 可以注册多个子进度，每个子进度都有自己的权值
/// - 子进度的进度贡献到上级进度时，将叠加上子进度自己的权值。如占比一半权值的子进度，就最多只贡献一半的进度
/// - 自身可以报告进度。当自身报告进度时，除去自身进度外的剩余进度将由子进度贡献。如带两个各占一半权值的子进度时，两个子进度当前进度都是百分之50的值，自身进度报告是百分之50时，当前进度=自身进度+（剩余进度x （子进度1.进度值x子进度1.权重比例 + 子进度2.进度值x子进度2.权重比例）） = 自身进度（0.5）+（剩余进度（1-0.5）x （子进度1.进度值（0.5）x子进度1.权重比例（0.5） + 子进度2.进度值（0.5）x子进度2.权重比例（0.5）））=0.5+（0.5x（0.5x0.5+0.5x0.5））=0.75
public class ProgressCompositor<T>
{
    /// <summary>
    /// 创建进度合成器
    /// </summary>
    /// <param name="name"></param>
    public ProgressCompositor(string name)
    {
        Name = name;
        _subProgressCompositorReportedEventHandler = SubProgressCompositor_Reported;
    }

    /// <summary>
    /// 进度名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 当前进度值
    /// </summary>
    public ProgressPercentage CurrentProgress
    {
        get
        {
            if (_subProgressCompositorDictionary.Count > 0)
            {
                double selfValue = _selfProgressPercentage.Value;

                var value = selfValue;

                var remain = ProgressPercentage.MaxValue.Value - selfValue;
                if (remain > 0)
                {
                    var totalWeight = 0d;
                    foreach (var subProgressCompositorInfo in _subProgressCompositorDictionary.Keys)
                    {
                        totalWeight += subProgressCompositorInfo.Weight;
                    }

                    var subValue = 0d;

                    foreach (var (subInfo, subProgress) in _subProgressCompositorDictionary)
                    {
                        subValue += subProgress.CurrentProgress.Value * (subInfo.Weight / totalWeight);
                    }

                    value += remain * subValue;
                }

                value = Math.Clamp(value, 0, 1);

                return new ProgressPercentage(value);
            }

            return _selfProgressPercentage;
        }
    }

    public IReadOnlyList<ProgressCompositor<T>> RegisterSubProgressCompositors(
        params SubProgressCompositorInfo[] subList) => RegisterSubProgressCompositors((IReadOnlyList<SubProgressCompositorInfo>) subList);

    public IReadOnlyList<ProgressCompositor<T>> RegisterSubProgressCompositors(IReadOnlyList<SubProgressCompositorInfo> subList)
    {
        var subProgressList = new ProgressCompositor<T>[subList.Count];

        _subProgressCompositorDictionary.EnsureCapacity(_subProgressCompositorDictionary.Count + subList.Count);

        for (var i = 0; i < subList.Count; i++)
        {
            var info = subList[i];
            var progressCompositor = RegisterSubProgressCompositor(info);
            subProgressList[i] = progressCompositor;
        }

        return subProgressList;
    }

    public ProgressCompositor<T> RegisterSubProgressCompositor(SubProgressCompositorInfo subProgressCompositor)
    {
        var progressCompositor = new ProgressCompositor<T>(subProgressCompositor.Name);
        _subProgressCompositorDictionary[subProgressCompositor] = progressCompositor;

        progressCompositor.Reported += _subProgressCompositorReportedEventHandler;
        return progressCompositor;
    }

    private void SubProgressCompositor_Reported(object? sender, ProgressReportedEventArgument<T> e)
    {
        _currentValue = e.Value;

        OnReported();
    }

    private readonly Dictionary<SubProgressCompositorInfo, ProgressCompositor<T>> _subProgressCompositorDictionary = new();

    /// <summary>
    /// 上报进度
    /// </summary>
    /// <param name="percentage"></param>
    /// <param name="value"></param>
    public void Report(ProgressPercentage percentage, T? value = default)
    {
        _selfProgressPercentage = percentage;
        _currentValue = value;

        OnReported();
    }

    private ProgressPercentage _selfProgressPercentage = default;
    private T? _currentValue;

    private readonly EventHandler<ProgressReportedEventArgument<T>> _subProgressCompositorReportedEventHandler;

    /// <summary>
    /// 上报增量进度，将叠加上当前的进度
    /// </summary>
    /// <param name="percentage"></param>
    /// <param name="value"></param>
    public void ReportIncreased(ProgressPercentage percentage, T? value = default)
    {
        var currentPercentageValue = _selfProgressPercentage.Value + percentage.Value;
        currentPercentageValue = Math.Clamp(currentPercentageValue, 0, 1);
        Report(new ProgressPercentage(currentPercentageValue), value);
    }

    private void OnReported()
    {
        Reported?.Invoke(this, new ProgressReportedEventArgument<T>(CurrentProgress, _currentValue));
    }

    /// <summary>
    /// 进度变更时触发
    /// </summary>
    public event EventHandler<ProgressReportedEventArgument<T>>? Reported;
}