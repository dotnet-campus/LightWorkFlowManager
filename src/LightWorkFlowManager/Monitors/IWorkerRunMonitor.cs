using System;
using LightWorkFlowManager.Protocols;
using LightWorkFlowManager.Workers;

namespace LightWorkFlowManager.Monitors;

public interface IWorkerRunMonitor
{
    void OnWorkerStart(IMessageWorker worker);
    void OnWorkerFinish(IMessageWorker worker, WorkerResult result);
    void OnWorkerException(IMessageWorker worker, Exception exception);
}
