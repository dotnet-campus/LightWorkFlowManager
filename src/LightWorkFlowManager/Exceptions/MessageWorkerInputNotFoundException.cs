using System;

namespace DC.LightWorkFlowManager.Exceptions;

/// <summary>
/// 找不到输入的异常，基本上就是业务代码写错
/// </summary>
public class MessageWorkerInputNotFoundException : InvalidOperationException, IWorkFlowException
{
    /// <summary>
    /// 使用指定错误消息初始化异常。
    /// </summary>
    /// <param name="message">异常消息。</param>
    public MessageWorkerInputNotFoundException(string? message) : base(message)
    {
    }
}
