using System;
using System.Runtime.Serialization;

namespace DC.LightWorkFlowManager.Exceptions;

/// <summary>
/// 表示工作过程的异常
/// </summary>
public abstract class WorkFlowException : Exception, IWorkFlowException
{
    protected WorkFlowException()
    {
    }

    protected WorkFlowException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    protected WorkFlowException(string? message) : base(message)
    {
    }

    protected WorkFlowException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
