using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Exceptions;
using DC.LightWorkFlowManager.Protocols;

using System;
using System.Threading.Tasks;

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
        var input = GetInputContext<TInput>();

        return DoInnerAsync(input);
    }

    protected abstract ValueTask<WorkerResult> DoInnerAsync(TInput input);

    /// <summary>
    /// 使用指定输入参数运行工作器。
    /// </summary>
    /// <param name="input">工作器输入参数。</param>
    /// <returns>带输出结果的工作器执行结果。</returns>
    public ValueTask<WorkerResult> RunAsync(TInput input)
    {
        ThrowNotManager();

        SetContext(input);
        return RunAsync();
    }

    /// <summary>
    /// 使用当前上下文中的参数转换为输入后运行工作器。
    /// </summary>
    /// <typeparam name="TArgument">当前上下文中的参数类型。</typeparam>
    /// <param name="converter">将上下文参数转换为输入参数的委托。</param>
    /// <returns>带输出结果的工作器执行结果。</returns>
    public ValueTask<WorkerResult> RunAsync<TArgument>(Func<TArgument, TInput> converter)
    {
        var argument = GetInputContext<TArgument>();
        var input = converter(argument);
        return RunAsync(input);
    }
}