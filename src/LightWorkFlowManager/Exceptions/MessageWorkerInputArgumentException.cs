using DC.LightWorkFlowManager.Contexts;

namespace DC.LightWorkFlowManager.Exceptions;

/// <summary>
/// 工作过程传入的参数异常，证明前置步骤或用户输入数据有误，抛出此异常则不再重试 Worker 任务
/// </summary>
public class MessageWorkerInputArgumentException : MessageWorkerException
{
    public MessageWorkerInputArgumentException(WorkFlowErrorCode errorCode) : base(errorCode,canRetryWorker: false)
    {
    }
}
