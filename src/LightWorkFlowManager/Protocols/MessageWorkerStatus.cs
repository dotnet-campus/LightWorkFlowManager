using System;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Workers;

namespace DC.LightWorkFlowManager.Protocols;

/// <summary>
/// 表示工作流管理器的当前执行状态。
/// </summary>
public class MessageWorkerStatus
{
    /// <summary>
    /// 获取当前状态是否为失败。
    /// </summary>
    public bool IsFail => Status != WorkFlowErrorCode.Ok;

    /// <summary>
    /// 获取当前记录的状态码。
    /// </summary>
    public WorkFlowErrorCode Status { get; private set; } = WorkFlowErrorCode.Ok;

    /// <summary>
    /// 获取或设置最后一次异常。
    /// </summary>
    public Exception? LastException { get; set; }

    /// <summary>
    /// 失败的工作器
    /// </summary>
    public IMessageWorker? FailWorker { get; private set; }

    /// <summary>
    /// 设置当前状态码。
    /// </summary>
    /// <param name="errorCode">要设置的状态码。</param>
    public void SetErrorCode(WorkFlowErrorCode errorCode) => Status = errorCode;

    /// <summary>
    /// 在当前尚未失败时尝试记录失败状态。
    /// </summary>
    /// <param name="errorCode">失败状态码。</param>
    /// <param name="failWorker">触发失败的工作器。</param>
    /// <returns>如果成功记录失败状态则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
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

    /// <inheritdoc />
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
