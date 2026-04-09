using System.Threading.Tasks;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Exceptions;
using DC.LightWorkFlowManager.Protocols;

namespace DC.LightWorkFlowManager.Workers;

/// <summary>
/// 表示需要单个输入参数的工作器基类。
/// </summary>
/// <typeparam name="TInput">输入参数类型。</typeparam>
public abstract class MessageWorker<TInput> : MessageWorker
{
    /// <inheritdoc />
    protected sealed override ValueTask<WorkerResult> DoInnerAsync(IWorkerContext context)
    {
        var input = context.GetContext<TInput>();

        if (input == null)
        {
            throw new MessageWorkerInputNotFoundException($"Do not find {typeof(TInput)} in {WorkerName} worker. 无法在{WorkerName}找到{typeof(TInput)}输入，请确保前置步骤完成输出或初始化进行输入");
        }

        return DoInnerAsync(input);
    }

    protected abstract ValueTask<WorkerResult> DoInnerAsync(TInput input);
}