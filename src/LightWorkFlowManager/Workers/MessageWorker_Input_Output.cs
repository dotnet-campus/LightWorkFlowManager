using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Exceptions;
using DC.LightWorkFlowManager.Protocols;

using System;
using System.Threading.Tasks;

namespace DC.LightWorkFlowManager.Workers;

/// <summary>
/// 表示带输入与输出参数的工作器基类。
/// </summary>
/// <typeparam name="TInput">输入参数类型。</typeparam>
/// <typeparam name="TOutput">输出参数类型。</typeparam>
public abstract class MessageWorker<TInput, TOutput> : MessageWorker
{
    /// <inheritdoc />
    protected sealed override async ValueTask<WorkerResult> DoInnerAsync(IWorkerContext context)
    {
        var input = context.GetContext<TInput>();

        if (input == null)
        {
            throw new MessageWorkerInputNotFoundException($"Do not find {typeof(TInput)} in {WorkerName} worker. 无法在{WorkerName}找到{typeof(TInput)}输入，请确保前置步骤完成输出或初始化进行输入");
        }

        WorkerResult<TOutput> output = await DoInnerAsync(input);

        if (output.IsSuccess)
        {
            CurrentContext.SetContext(output.Result);
        }

        return output;
    }

    protected abstract ValueTask<WorkerResult<TOutput>> DoInnerAsync(TInput input);

    /// <summary>
    /// 使用当前上下文中的参数转换为输入后运行工作器。
    /// </summary>
    /// <typeparam name="TArgument">当前上下文中的参数类型。</typeparam>
    /// <param name="converter">将上下文参数转换为输入参数的委托。</param>
    /// <returns>带输出结果的工作器执行结果。</returns>
    public ValueTask<WorkerResult<TOutput>> RunAsync<TArgument>(Func<TArgument, TInput> converter)
    {
        var argument = CurrentContext.GetEnsureContext<TArgument>();
        var input = converter(argument);
        return RunAsync(input);
    }

    /// <summary>
    /// 使用指定输入参数运行工作器。
    /// </summary>
    /// <param name="input">工作器输入参数。</param>
    /// <returns>带输出结果的工作器执行结果。</returns>
    public ValueTask<WorkerResult<TOutput>> RunAsync(TInput input)
    {
        ThrowNotManager();

        SetContext(input);
        return RunAsync();
    }

    /// <summary>
    /// 使用当前上下文运行工作器并返回输出结果。
    /// </summary>
    /// <returns>带输出结果的工作器执行结果。</returns>
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

    /// <summary>
    /// 返回且标记失败
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="canRetry">是否需要重试</param>
    /// <returns></returns>
    protected new WorkerResult<TOutput> Fail(WorkFlowErrorCode errorCode, bool canRetry = true)
    {
        return new WorkerResult<TOutput>(errorCode, canRetry);
    }

    protected WorkerResult<TOutput> Fail(WorkerResult failResult)
    {
        return failResult.AsFail<TOutput>();
    }

    protected new ValueTask<WorkerResult<TOutput>> FailTask(WorkFlowErrorCode errorCode, bool canRetry = true) 
        => ValueTask.FromResult(Fail(errorCode, canRetry));

    protected ValueTask<WorkerResult<TOutput>> FailTask(WorkerResult failResult)
    {
        return FailTask(failResult.ErrorCode, failResult.CanRetry);
    }

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