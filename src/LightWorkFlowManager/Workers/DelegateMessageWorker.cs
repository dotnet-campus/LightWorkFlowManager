using System;
using System.Threading.Tasks;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Protocols;

namespace DC.LightWorkFlowManager.Workers;

/// <summary>
/// 表示基于委托实现的单输入工作器。
/// </summary>
/// <typeparam name="TInput">输入参数类型。</typeparam>
public class DelegateMessageWorker<TInput> : MessageWorker<TInput>
{
    /// <summary>
    /// 使用异步委托初始化工作器。
    /// </summary>
    /// <param name="messageTask">实际执行逻辑。</param>
    /// <param name="workerName">工作器名称。</param>
    /// <param name="canRunWhenFail">当前置步骤失败时是否仍可运行。</param>
    public DelegateMessageWorker(Func<TInput, ValueTask> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        _messageTask = messageTask;
        _workerName = workerName;

        CanRunWhenFail = canRunWhenFail;
    }

    /// <inheritdoc />
    public override string WorkerName => _workerName ?? base.WorkerName;

    private readonly string? _workerName;

    protected override async ValueTask<WorkerResult> DoAsync(TInput input)
    {
        await _messageTask(input);
        return WorkerResult.Success();
    }

    private readonly Func<TInput, ValueTask> _messageTask;
}

/// <summary>
/// 表示基于委托实现的输入输出工作器。
/// </summary>
/// <typeparam name="TInput">输入参数类型。</typeparam>
/// <typeparam name="TOutput">输出参数类型。</typeparam>
public class DelegateMessageWorker<TInput, TOutput> : MessageWorker<TInput, TOutput>
{
    /// <summary>
    /// 使用同步委托初始化工作器。
    /// </summary>
    /// <param name="messageTask">实际执行逻辑。</param>
    /// <param name="workerName">工作器名称。</param>
    /// <param name="canRunWhenFail">当前置步骤失败时是否仍可运行。</param>
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

    /// <summary>
    /// 使用异步委托初始化工作器。
    /// </summary>
    /// <param name="messageTask">实际执行逻辑。</param>
    /// <param name="workerName">工作器名称。</param>
    /// <param name="canRunWhenFail">当前置步骤失败时是否仍可运行。</param>
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

    /// <inheritdoc />
    public override string WorkerName => _workerName ?? base.WorkerName;

    private readonly string? _workerName;
}

/// <summary>
/// 表示基于委托实现的无固定输入工作器。
/// </summary>
public class DelegateMessageWorker : MessageWorkerBase
{
    /// <summary>
    /// 使用同步委托初始化工作器。
    /// </summary>
    /// <param name="messageAction">实际执行逻辑。</param>
    /// <param name="workerName">工作器名称。</param>
    /// <param name="canRunWhenFail">当前置步骤失败时是否仍可运行。</param>
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

    /// <summary>
    /// 使用异步委托初始化工作器。
    /// </summary>
    /// <param name="messageTask">实际执行逻辑。</param>
    /// <param name="workerName">工作器名称。</param>
    /// <param name="canRunWhenFail">当前置步骤失败时是否仍可运行。</param>
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

    /// <summary>
    /// 使用返回执行结果的异步委托初始化工作器。
    /// </summary>
    /// <param name="messageTask">实际执行逻辑。</param>
    /// <param name="workerName">工作器名称。</param>
    /// <param name="canRunWhenFail">当前置步骤失败时是否仍可运行。</param>
    public DelegateMessageWorker(Func<IWorkerContext, ValueTask<WorkerResult>> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        _messageTask = messageTask;
        _workerName = workerName;

        CanRunWhenFail = canRunWhenFail;
    }

    /// <inheritdoc />
    public override string WorkerName => _workerName ?? base.WorkerName;

    private readonly string? _workerName;

    private readonly Func<IWorkerContext, ValueTask<WorkerResult>> _messageTask;

    /// <inheritdoc />
    public override ValueTask<WorkerResult> DoAsync(IWorkerContext context)
    {
        return _messageTask(context);
    }
}
