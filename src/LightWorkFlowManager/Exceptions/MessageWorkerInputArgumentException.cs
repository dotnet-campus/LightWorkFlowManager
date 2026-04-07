using DC.LightWorkFlowManager.Contexts;

namespace DC.LightWorkFlowManager.Exceptions;

/// <summary>
/// 工作过程传入的参数异常，证明前置步骤或用户输入数据有误，抛出此异常则不再重试 Worker 任务
/// </summary>
public class MessageWorkerInputArgumentException : MessageWorkerException
{
    /// <summary>
    /// 使用指定错误码初始化输入参数异常。
    /// </summary>
    /// <param name="errorCode">输入参数错误对应的工作流错误码。</param>
    public MessageWorkerInputArgumentException(WorkFlowErrorCode errorCode) : base(errorCode,canRetryWorker: false)
    {
    }
}
