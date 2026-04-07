using System;
using DC.LightWorkFlowManager.Protocols;
using DC.LightWorkFlowManager.Workers;

namespace DC.LightWorkFlowManager.Monitors;

/// <summary>
/// 提供工作器执行过程的监控回调。
/// </summary>
public interface IWorkerRunMonitor
{
    /// <summary>
    /// 在工作器开始执行时调用。
    /// </summary>
    /// <param name="worker">即将执行的工作器。</param>
    void OnWorkerStart(IMessageWorker worker);

    /// <summary>
    /// 在工作器执行完成时调用。
    /// </summary>
    /// <param name="worker">已执行完成的工作器。</param>
    /// <param name="result">工作器执行结果。</param>
    void OnWorkerFinish(IMessageWorker worker, WorkerResult result);

    /// <summary>
    /// 在工作器执行出现异常时调用。
    /// </summary>
    /// <param name="worker">发生异常的工作器。</param>
    /// <param name="exception">执行过程中抛出的异常。</param>
    void OnWorkerException(IMessageWorker worker, Exception exception);
}
