using System;
using DC.LightWorkFlowManager.Protocols;
using DC.LightWorkFlowManager.Workers;

namespace DC.LightWorkFlowManager.Monitors;

public interface IWorkerRunMonitor
{
    void OnWorkerStart(IMessageWorker worker);
    void OnWorkerFinish(IMessageWorker worker, WorkerResult result);
    void OnWorkerException(IMessageWorker worker, Exception exception);
}
