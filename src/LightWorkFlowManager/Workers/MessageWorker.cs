using System;
using System.Threading.Tasks;
using LightWorkFlowManager.Contexts;
using LightWorkFlowManager.Exceptions;
using LightWorkFlowManager.Protocols;

namespace LightWorkFlowManager.Workers;

public abstract class MessageWorker<TInput> : MessageWorkerBase
{
    public sealed override ValueTask<WorkerResult> Do(IWorkerContext context)
    {
        var input = context.GetContext<TInput>();

        if (input == null)
        {
            throw new MessageWorkerInputNotFoundException($"Do not find {typeof(TInput)} in {WorkerName} worker. 无法在{WorkerName}找到{typeof(TInput)}输入，请确保前置步骤完成输出或初始化进行输入");
        }

        return DoAsync(input);
    }

    protected abstract ValueTask<WorkerResult> DoAsync(TInput input);
}

public abstract class MessageWorker<TInput, TOutput> : MessageWorker<TInput>
{
    protected sealed override async ValueTask<WorkerResult> DoAsync(TInput input)
    {
        WorkerResult<TOutput> output = await DoInnerAsync(input);

        if (output.IsSuccess)
        {
            CurrentContext.SetContext(output.Result);
        }

        return output;
    }

    public ValueTask<WorkerResult<TOutput>> RunAsync<TArgument>(Func<TArgument, TInput> converter)
    {
        var argument = CurrentContext.GetEnsureContext<TArgument>();
        var input = converter(argument);
        return RunAsync(input);
    }

    public ValueTask<WorkerResult<TOutput>> RunAsync(TInput input)
    {
        ThrowNotManager();

        SetContext(input);
        return RunAsync();
    }

    public new async ValueTask<WorkerResult<TOutput>> RunAsync()
    {
        ThrowNotManager();

        var manager = Manager;
        await manager.RunWorker(this);
        if (Status.IsFail)
        {
            return manager.GetFailResult<TOutput>();
        }
        else
        {
            return GetEnsureContext<TOutput>();
        }
    }

    protected abstract ValueTask<WorkerResult<TOutput>> DoInnerAsync(TInput input);

    /// <summary>
    /// 返回且标记失败
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="retry">是否需要重试</param>
    /// <returns></returns>
    protected WorkerResult<TOutput> Fail(WorkFlowErrorCode errorCode, bool retry = true)
    {
        return new WorkerResult<TOutput>(errorCode, retry);
    }

    protected ValueTask<WorkerResult<TOutput>> FailTask(WorkFlowErrorCode errorCode, bool retry = true) 
        => ValueTask.FromResult(Fail(errorCode, retry));

    protected WorkerResult<TOutput> Success(TOutput output)
    {
        return new WorkerResult<TOutput>(output);
    }

    protected ValueTask<WorkerResult<TOutput>> SuccessTask(TOutput output)
    {
        var result = Success(output);
        return ValueTask.FromResult(result);
    }
}
