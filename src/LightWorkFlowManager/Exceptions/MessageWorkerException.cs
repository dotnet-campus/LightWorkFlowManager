using System;
using LightWorkFlowManager.Contexts;

namespace LightWorkFlowManager.Exceptions;

/// <summary>
/// 工作过程的异常，使用用来扔出明确异常，打断后续执行
/// </summary>
public class MessageWorkerException : WorkFlowException
{
    /// <summary>
    /// 工作过程的异常
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="canRetryWorker">默认 false 表示不能重试</param>
    public MessageWorkerException(WorkFlowErrorCode errorCode, bool canRetryWorker = false)
    {
        ErrorCode = errorCode;
        CanRetryWorker = canRetryWorker;
    }

    /// <summary>
    /// 工作过程的异常
    /// </summary>
    public MessageWorkerException(WorkFlowErrorCode errorCode, Exception innerException) : base(errorCode.Message, innerException)
    {
        ErrorCode = errorCode;
        CanRetryWorker = false;
    }

    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool CanRetryWorker { get; }
    public WorkFlowErrorCode ErrorCode { get; }

    public override string Message => ErrorCode.Message;
}
