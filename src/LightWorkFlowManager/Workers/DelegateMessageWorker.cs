using System;
using System.Threading.Tasks;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Protocols;

namespace DC.LightWorkFlowManager.Workers;

public class DelegateMessageWorker<TInput> : MessageWorker<TInput>
{
    public DelegateMessageWorker(Func<TInput, ValueTask> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        _messageTask = messageTask;
        _workerName = workerName;

        CanRunWhenFail = canRunWhenFail;
    }

    public override string WorkerName => _workerName ?? base.WorkerName;

    private readonly string? _workerName;

    protected override async ValueTask<WorkerResult> DoAsync(TInput input)
    {
        await _messageTask(input);
        return WorkerResult.Success();
    }

    private readonly Func<TInput, ValueTask> _messageTask;
}

public class DelegateMessageWorker<TInput, TOutput> : MessageWorker<TInput, TOutput>
{
    public DelegateMessageWorker(Func<TInput, TOutput> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        _workerName = workerName;
        _messageTask = input =>
        {
            var output = messageTask(input);
            return ValueTask.FromResult(output);
        };

        CanRunWhenFail = canRunWhenFail;
    }

    public DelegateMessageWorker(Func<TInput, ValueTask<TOutput>> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        _messageTask = messageTask;
        _workerName = workerName;

        CanRunWhenFail = canRunWhenFail;
    }

    protected override async ValueTask<WorkerResult<TOutput>> DoInnerAsync(TInput input)
    {
        return await _messageTask(input);
    }

    private readonly Func<TInput, ValueTask<TOutput>> _messageTask;

    public override string WorkerName => _workerName ?? base.WorkerName;

    private readonly string? _workerName;
}

public class DelegateMessageWorker : MessageWorkerBase
{
    public DelegateMessageWorker(Action<IWorkerContext> messageAction, string? workerName = null, bool canRunWhenFail=false)
    {
        _messageTask = c =>
        {
            messageAction(c);
            return ValueTask.FromResult(WorkerResult.Success());
        };
        _workerName = workerName;

        CanRunWhenFail = canRunWhenFail;
    }

    public DelegateMessageWorker(Func<IWorkerContext, ValueTask> messageTask, string? workerName = null, bool canRunWhenFail=false)
    {
        _messageTask = async c =>
        {
            await messageTask(c);
            return WorkerResult.Success();
        };
        _workerName = workerName;

        CanRunWhenFail = canRunWhenFail;
    }

    public DelegateMessageWorker(Func<IWorkerContext, ValueTask<WorkerResult>> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        _messageTask = messageTask;
        _workerName = workerName;

        CanRunWhenFail = canRunWhenFail;
    }

    public override string WorkerName => _workerName ?? base.WorkerName;

    private readonly string? _workerName;

    private readonly Func<IWorkerContext, ValueTask<WorkerResult>> _messageTask;

    public override ValueTask<WorkerResult> Do(IWorkerContext context)
    {
        return _messageTask(context);
    }
}
