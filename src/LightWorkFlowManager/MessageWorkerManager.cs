using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using LightWorkFlowManager.Contexts;
using LightWorkFlowManager.Exceptions;
using LightWorkFlowManager.Monitors;
using LightWorkFlowManager.Protocols;
using LightWorkFlowManager.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LightWorkFlowManager;

/// <summary>
/// 工作器管理
/// </summary>
public class MessageWorkerManager : IAsyncDisposable
{
    /// <summary>
    /// 创建工作器管理
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="taskName">任务名，任务类型，如 PDF 解析或 PPT 解析等</param>
    /// <param name="serviceScope"></param>
    /// <param name="retryCount">每个工作器的失败重试次数，默认三次</param>
    /// <param name="context">参数上下文信息</param>
    /// <param name="workerRunMonitor"></param>
    public MessageWorkerManager(string taskId, string taskName, IServiceScope serviceScope, int retryCount = 3,
        IWorkerContext? context = null, IWorkerRunMonitor? workerRunMonitor = null)
    {
        _serviceScope = serviceScope;
        context ??= new WorkerContext();

        ServiceProvider = serviceScope.ServiceProvider;
        TaskId = taskId;
        TaskName = taskName;
        Context = context;
        RetryCount = retryCount;

        context.SetContext(this);
        context.SetContext(MessageWorkerStatus);
        context.SetContext(serviceScope);

        Logger = ServiceProvider.GetRequiredService<ILogger<MessageWorkerManager>>();
        WorkerRunMonitor = workerRunMonitor;
    }

    private readonly IServiceScope _serviceScope;

    /// <summary>
    /// 任务 Id 用于追踪
    /// </summary>
    public string TaskId { get; }

    /// <summary>
    /// 任务名，任务类型，如 ENBX 解析或 PPT 解析等
    /// </summary>
    public string TaskName { get; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// 服务提供器
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 参数上下文
    /// </summary>
    public IWorkerContext Context { get; }

    /// <summary>
    /// 当前的状态
    /// </summary>
    public MessageWorkerStatus MessageWorkerStatus { get; set; } = new MessageWorkerStatus();

    /// <summary>
    /// 用来监控执行状态
    /// </summary>
    protected IWorkerRunMonitor? WorkerRunMonitor { get; set; }

    /// <summary>
    /// 工作器执行栈，用于调试，以及用于清理
    /// </summary>
    /// 清理采用后执行的先清理的方式
    private readonly Stack<IMessageWorker> _workerStack = new Stack<IMessageWorker>();

    /// <summary>
    /// 日志
    /// </summary>
    public ILogger Logger { get; protected set; }

    /// <summary>
    /// 设置上下文信息。设计上要求一个类型对应一个参数，不允许相同的类型作为不同的参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <returns></returns>
    public MessageWorkerManager SetContext<T>(T context)
    {
        Context.SetContext(context);
        return this;
    }

    /// <summary>
    /// 根据现有的参数设置上下文信息
    /// </summary>
    /// <typeparam name="TInput">现有的参数类型</typeparam>
    /// <typeparam name="TOutput">转换后的参数类型</typeparam>
    /// <param name="worker"></param>
    /// <remarks>如果前置步骤失败，即 <see cref="MessageWorkerStatus"/> 为 IsFail 时，将不执行委托内容</remarks>
    /// <returns></returns>
    public MessageWorkerManager SetContext<TInput, TOutput>(Func<TInput, TOutput> worker)
    {
        if (MessageWorkerStatus.IsFail)
        {
            // 失败就不给转换
            return this;
        }

        var input = Context.GetEnsureContext<TInput>();
        var output = worker(input);
        Context.SetContext(output);
        return this;
    }

    /// <summary>
    /// 获取工作器，获取到的工作器将会被注入信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetWorker<T>() where T : IMessageWorker
    {
        var messageWorker = ServiceProvider.GetRequiredService<T>();
        SetManager(messageWorker as IMessageWorkerManagerSensitive);
        return messageWorker;
    }

    /// <summary>
    /// 执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker<TInput, TOutput>(Func<TInput, TOutput> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker<TInput, TOutput>(messageTask, workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    ///  执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker<TInput, TOutput>(Func<TInput, ValueTask<TOutput>> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker<TInput, TOutput>(messageTask, workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    ///  执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker<TInput>(Func<TInput, ValueTask> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker<TInput>(messageTask, workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    ///  执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker(Func<ValueTask> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker(_ => messageTask(), workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    ///  执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker<TInput>(Action<TInput> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker<TInput>(input =>
        {
            messageTask(input);
            return ValueTask.CompletedTask;
        }, workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    ///  执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker(Action<IWorkerContext> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker(messageTask, workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    ///  执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker(Action messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker(_ => messageTask(), workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    ///  执行委托工作器，执行的内容为 <paramref name="messageTask"/> 参数的内容
    /// </summary>
    /// <param name="messageTask"></param>
    /// <param name="workerName">此委托代表的工作器名，用于调试和埋点上报</param>
    /// <param name="canRunWhenFail">是否在当前 <see cref="MessageWorkerManager"/> 前置步骤已失败时，依然可以执行。默认为 false 表示在前置步骤失败时，不执行</param>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker(Func<IWorkerContext, ValueTask> messageTask, string? workerName = null, bool canRunWhenFail = false)
    {
        var worker = new DelegateMessageWorker(messageTask, workerName ?? messageTask.Method.DeclaringType?.FullName, canRunWhenFail);
        return RunWorker(worker);
    }

    /// <summary>
    /// 执行工作器
    /// </summary>
    /// <typeparam name="TWorker"></typeparam>
    /// <returns></returns>
    public ValueTask<WorkerResult> RunWorker<TWorker>() where TWorker : IMessageWorker
    {
        var worker = ServiceProvider.GetRequiredService<TWorker>();
        return RunWorker(worker);
    }

    /// <summary>
    /// 执行工作器
    /// </summary>
    /// <param name="worker"></param>
    /// <returns></returns>
    public virtual async ValueTask<WorkerResult> RunWorker(IMessageWorker worker)
    {
        SetManager(worker as IMessageWorkerManagerSensitive);
        _workerStack.Push(worker);
        worker.TaskId = TaskId;

        if (MessageWorkerStatus.IsFail)
        {
            // 如果当前的状态是失败，且当前的 Worker 不能在失败时运行，那就返回吧
            if (!worker.CanRunWhenFail)
            {
                IgnoreWorkerRunOnFailStatus(worker);

                return GetFailResult();
            }
        }

        try
        {
            var result = await RunWithRetry();
            OnWorkerRunFinish(worker, result);

            return result;
        }
        catch (Exception e)
        {
            OnWorkerRunException(worker, e);
            MessageWorkerStatus.LastException = e;

            // 继续对外抛出
            throw;
        }

        // 运行工作任务的核心入口
        async ValueTask<WorkerResult> RunWorkerCoreInner()
        {
            WorkerRunMonitor?.OnWorkerStart(worker);
            try
            {
                // 可以在这里打断点，调试被执行的逻辑
                var result = await RunWorkerCore(worker);
                WorkerRunMonitor?.OnWorkerFinish(worker, result);
                return result;
            }
            catch (Exception e)
            {
                WorkerRunMonitor?.OnWorkerException(worker, e);
                throw;
            }
        }

        async ValueTask<WorkerResult> RunWithRetry()
        {
            Exception? exception = null;

            for (int i = 0; i < RetryCount; i++)
            {
                var isFirstRun = i == 0;
                // 是否最后一次执行
                bool isLastRun = i == RetryCount - 1;

                if (!isFirstRun)
                {
                    // 非首次执行，等待一下吧
                    await Task.Delay(worker.RetryDelayTime);
                }

                try
                {
                    var workerResult = await RunWorkerCoreInner();
                    if
                    (
                        // 不是最后一次执行的状态，才可以允许回到循环重新执行
                        !isLastRun
                        // 如果是失败，那才会有重试的需求。成功就立刻返回
                        && workerResult.IsFail
                        // 如果失败记录里面允许重试的话，才可以进行重试。例如有些是明确的参数错误，重试也没有用的，具体工作器返回时将会记录不能重试，此时就听工作器的，不重试
                        && workerResult.CanRetry
                        // 如果要重试，还需要判断一下工作器本身是否支持重试。理论上如果返回结果是可以重试，基本工作器都可以重试
                        && worker.CanRetry
                    )
                    {
                        // 如果失败了，且可以重试的，那就执行继续逻辑吧
                        continue;
                    }
                    else
                    {
                        return workerResult;
                    }
                }
                catch (MessageWorkerInputNotFoundException)
                {
                    // 输入参数不存在了，那就是阻断异常，基本都是业务代码写错的
                    throw;
                }
                catch (Exception e)
                {
                    exception = e;

                    // 如果是最后一次执行，那就立刻抛出，不进行等待
                    // 如果 Worker 自身，或者是框架不允许重试，那就不重试
                    if (isLastRun || !worker.CanRetry || !CanRetry(exception, i))
                    {
                        throw;
                    }
                }
            }

            Debug.Assert(exception is not null);
            ExceptionDispatchInfo.Throw(exception);
            // 理论上是不会进入这里
            throw exception;
        }
    }

    #region 框架重写

    /// <summary>
    /// 实际执行工作器的方法
    /// </summary>
    /// 方便业务方重写，从而方便打断点调试
    /// <param name="worker"></param>
    /// <returns></returns>
    protected virtual ValueTask<WorkerResult> RunWorkerCore(IMessageWorker worker)
        // 可以在这里打断点，调试被执行的逻辑
        => worker.Do(Context);

    /// <summary>
    /// 在当前是失败的状态下，跳过工作器的执行
    /// </summary>
    /// <param name="worker"></param>
    protected virtual void IgnoreWorkerRunOnFailStatus(IMessageWorker worker)
    {
        //Logger.CCloudInfo($"Fail run {worker.WorkerName}. {MessageWorkerStatus}");
    }

    /// <summary>
    /// 当工作器开始执行触发
    /// </summary>
    /// <param name="worker"></param>
    protected virtual void OnWorkerRunStart(IMessageWorker worker)
    {
    }

    /// <summary>
    /// 当工作器执行触发，经过重试的结果
    /// </summary>
    /// <param name="worker"></param>
    /// <param name="result"></param>
    protected virtual void OnWorkerRunFinish(IMessageWorker worker, WorkerResult result)
    {
        if (result.IsFail && !MessageWorkerStatus.IsFail)
        {
            // 失败了，记录一下
            RecordWorkerError(worker, result.ErrorCode);
        }
    }

    /// <summary>
    /// 当工作器执行异常时触发。只是用来做记录，不能用来吃掉异常
    /// </summary>
    /// <param name="worker"></param>
    /// <param name="e"></param>
    protected virtual void OnWorkerRunException(IMessageWorker worker, Exception e)
    {
        if (e is MessageWorkerException messageWorkerException)
        {
            RecordWorkerError(worker, messageWorkerException.ErrorCode);
        }
        //else if (e is MessageWorkerInputNotFoundException)
        //{
        //    RecordError(ResponseErrorCode.UnknownBusinessInternalError.AppendMessage(e.ToString()));
        //}
        else
        {
            RecordWorkerError(worker, new WorkFlowErrorCode(-1, e.ToString()));
        }
    }

    protected virtual void RecordWorkerError(IMessageWorker worker, WorkFlowErrorCode errorCode)
    {
        if (!MessageWorkerStatus.IsFail)
        {
            var appendMessage = $"FailWorker:{worker.WorkerName}";
            errorCode = errorCode.AppendMessage(appendMessage);

            MessageWorkerStatus.TrySetErrorCode(errorCode, worker);
        }
    }

    #endregion


    private void SetManager(IMessageWorkerManagerSensitive? messageWorkerManagerSensitive)
        => messageWorkerManagerSensitive?.SetMessageWorkerManager(this);

    /// <summary>
    /// 框架判断能否重试
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="retryCount"></param>
    /// <returns></returns>
    protected virtual bool CanRetry(Exception exception, int retryCount)
    {
        if (exception is MessageWorkerException messageWorker)
        {
            return messageWorker.CanRetryWorker;
        }

        return true;
    }

    private WorkerResult GetFailResult()
    {
        if (!MessageWorkerStatus.IsFail)
        {
            throw new InvalidOperationException($"只有已经失败时，才能获取失败结果");
        }

        return new WorkerResult(isSuccess: false, MessageWorkerStatus.Status,
            // 已经失败了的，就不需要重试
            canRetry: false);
    }

    internal WorkerResult<T> GetFailResult<T>()
    {
        if (!MessageWorkerStatus.IsFail)
        {
            throw new InvalidOperationException($"只有已经失败时，才能获取失败结果");
        }

        return new WorkerResult<T>(MessageWorkerStatus.Status,
            // 已经失败了的，就不需要重试
            canRetry: false);
    }

    public async ValueTask DisposeAsync()
    {
        while (_workerStack.TryPop(out var worker))
        {
            try
            {
                await worker.OnDisposeAsync(Context);
            }
            catch
            {
                // Ignore
            }
        }

        _serviceScope.Dispose();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var name = TaskName;
        string status;
        if (MessageWorkerStatus.IsFail)
        {
            status = $"[Fail] {MessageWorkerStatus.Status}";
        }
        else
        {
            status = "[OK]";
        }

        return $"[{name}] {status} WorkerList:{string.Join('-', _workerStack.Reverse().Select(worker => worker.WorkerName))}";
    }
}
