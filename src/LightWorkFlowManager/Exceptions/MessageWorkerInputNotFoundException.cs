using System;

namespace LightWorkFlowManager.Exceptions;

/// <summary>
/// 找不到输入的异常，基本上就是业务代码写错
/// </summary>
public class MessageWorkerInputNotFoundException : InvalidOperationException, IWorkFlowException
{
    public MessageWorkerInputNotFoundException(string? message) : base(message)
    {
    }
}
