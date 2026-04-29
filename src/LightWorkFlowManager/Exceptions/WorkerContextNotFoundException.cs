namespace DC.LightWorkFlowManager.Exceptions;

/// <summary>
/// 表示无法从工作器上下文中找到指定类型数据的异常。
/// </summary>
public class WorkerContextNotFoundException : WorkFlowException
{
    /// <summary>
    /// 使用缺失的上下文键初始化异常。
    /// </summary>
    /// <param name="key">缺失的上下文键。</param>
    public WorkerContextNotFoundException(string key)
    {
        Key = key;
    }

    /// <summary>
    /// 获取缺失的上下文键。
    /// </summary>
    public string Key { get; }

    /// <inheritdoc />
    public override string Message => $"Can not find {Key}";
}
