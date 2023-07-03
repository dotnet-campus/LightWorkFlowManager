using System;
using LightWorkFlowManager.Contexts;
using LightWorkFlowManager.Workers;

namespace LightWorkFlowManager.Protocols;

public class MessageWorkerStatus
{
    public bool IsFail => Status != WorkFlowErrorCode.Ok;

    public WorkFlowErrorCode Status { get; private set; } = WorkFlowErrorCode.Ok;

    public Exception? LastException { get; set; }

    /// <summary>
    /// 失败的工作器
    /// </summary>
    public IMessageWorker? FailWorker { get; private set; }

    public void SetErrorCode(WorkFlowErrorCode errorCode) => Status = errorCode;

    public bool TrySetErrorCode(WorkFlowErrorCode errorCode, IMessageWorker failWorker)
    {
        if (IsFail)
        {
            return false;
        }

        Status = errorCode;
        FailWorker = failWorker;

        return true;
    }

    public override string ToString()
    {
        if (IsFail)
        {
            return $"[{Status.Code}] {Status.Message} {LastException}";
        }
        else
        {
            return "Ok";
        }
    }
}
