using System;
using System.Runtime.Serialization;

namespace DC.LightWorkFlowManager.Exceptions;

/// <summary>
/// 表示工作过程的异常
/// </summary>
public abstract class WorkFlowException : Exception, IWorkFlowException
{
    /// <summary>
    /// 初始化工作流异常。
    /// </summary>
    protected WorkFlowException()
    {
    }

    /// <summary>
    /// 使用序列化信息初始化工作流异常。
    /// </summary>
    /// <param name="info">保存序列化对象数据的对象。</param>
    /// <param name="context">有关源或目标的上下文信息。</param>
    protected WorkFlowException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <summary>
    /// 使用指定错误消息初始化工作流异常。
    /// </summary>
    /// <param name="message">异常消息。</param>
    protected WorkFlowException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和内部异常初始化工作流异常。
    /// </summary>
    /// <param name="message">异常消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    protected WorkFlowException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
