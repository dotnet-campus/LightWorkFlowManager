using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Exceptions;
using DC.LightWorkFlowManager.Protocols;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Threading.Tasks;

namespace DC.LightWorkFlowManager.Workers;

/// <summary>
/// 工作器基类
/// </summary>
public abstract class MessageWorker : IMessageWorker, IMessageWorkerManagerSensitive
{
    /// <inheritdoc />
    public string TaskId { get; set; } = null!;

    /// <summary>
    /// 获取当前工作器名称。
    /// </summary>
    public virtual string WorkerName => GetType().Name;

    /// <summary>
    /// 设置或获取是否可以重试
    /// </summary>
    public virtual bool CanRetry { protected set; get; } = true;

    /// <summary>
    /// 获取重试前的等待时间。
    /// </summary>
    public virtual TimeSpan RetryDelayTime => TimeSpan.FromSeconds(1);

    /// <summary>
    /// 设置当前的 <see cref="IMessageWorker"/> 是否在 <see cref="MessageWorkerManager"/> 处于失败状态时能否运行
    /// </summary>
    public bool CanRunWhenFail { get; protected set; }
    // 默认遇到错误不能运行
        = false;

    /// <inheritdoc />
    ValueTask<WorkerResult> IMessageWorker.DoAsync(IWorkerContext context)
    {
        return DoInnerAsync(context);
    }

    protected abstract ValueTask<WorkerResult> DoInnerAsync(IWorkerContext context);

    protected MessageWorkerStatus Status => GetEnsureContext<MessageWorkerStatus>();
    protected IWorkerContext CurrentContext => Manager.Context;
    protected IServiceProvider ServiceProvider => Manager.ServiceProvider;

    /// <summary>
    /// 获取当前工作器使用的日志器。
    /// </summary>
    public ILogger Logger { get; private set; }
        // 框架注入
        = null!;
    protected MessageWorkerManager Manager { get; private set; }
        // 框架注入
        = null!;

    /// <summary>
    /// 从 <see cref="CurrentContext"/> 获取服务，如果获取不到，则从 <see cref="ServiceProvider"/> 获取，获取到后设置到上下文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected T GetScopeWithContext<T>() where T:notnull
    {
        var context = GetContext<T>();
        if (context is null)
        {
            context = ServiceProvider.GetRequiredService<T>();
            SetContext(context);
        }

        return context;
    }

    protected T GetEnsureContext<T>()
    {
        ThrowNotManager();
        return CurrentContext.GetEnsureContext<T>();
    }

    protected T? GetContext<T>()
    {
        ThrowNotManager();
        return CurrentContext.GetContext<T>();
    }

    protected void SetContext<T>(T context)
    {
        ThrowNotManager();
        CurrentContext.SetContext(context);
    }

    /// <summary>
    /// 获取输入的上下文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <remarks>
    /// 仅框架内容，给带 Input 的调用
    /// </remarks>
    /// <exception cref="MessageWorkerInputNotFoundException"></exception>
    private protected T GetInputContext<T>()
    {
        ThrowNotManager();
        var input = GetContext<T>();

        if (input == null)
        {
            throw new MessageWorkerInputNotFoundException($"Do not find {typeof(T)} in {WorkerName} worker. 无法在{WorkerName}找到{typeof(T)}输入，请确保前置步骤完成输出或初始化进行输入");
        }

        return input;
    }

    /// <summary>
    /// 通过当前管理器执行当前工作器。
    /// </summary>
    /// <returns>工作器执行结果。</returns>
    public async ValueTask<WorkerResult> RunAsync()
    {
        ThrowNotManager();

        var result = await Manager.RunWorker(this);
        return result;
    }

    /// <inheritdoc />
    public async ValueTask OnDisposeAsync(IWorkerContext context)
    {
        if (_onDispose != null)
        {
            await _onDispose.Invoke(context);
        }
        await OnDisposeInnerAsync(context);
    }

    protected virtual ValueTask OnDisposeInnerAsync(IWorkerContext context)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 注册释放的执行内容
    /// </summary>
    /// <param name="action"></param>
    protected void RegisterOnDispose(Action action)
    {
        _onDispose += _ =>
        {
            action();
            return ValueTask.CompletedTask;
        };
    }

    /// <summary>
    /// 注册释放的执行内容
    /// </summary>
    /// <param name="action"></param>
    protected void RegisterOnDispose(Action<IWorkerContext> action)
    {
        _onDispose += context =>
        {
            action(context);
            return ValueTask.CompletedTask;
        };
    }

    protected void RegisterOnDispose(Func<IWorkerContext, ValueTask> onDispose) => _onDispose += onDispose;

    private Func<IWorkerContext, ValueTask>? _onDispose;

    void IMessageWorkerManagerSensitive.SetMessageWorkerManager(MessageWorkerManager manager)
    {
        if (ReferenceEquals(Manager, manager))
        {
            return;
        }

        Manager = manager;

        Logger = ServiceProvider.GetRequiredService<ILogger<IMessageWorker>>();
    }

    protected void ThrowNotManager()
    {
        if (Manager == null)
        {
            throw new InvalidOperationException($"MessageWorkerManager is null. 没有注入 MessageWorkerManager 对象，请确保 {GetType().FullName} 在 MessageWorkerManager 里运行。如调用 {nameof(MessageWorkerManager.GetWorker)} 或 {nameof(MessageWorkerManager.RunWorker)} 执行");
        }
    }

    protected WorkerResult Fail(WorkFlowErrorCode errorCode, bool canRetry = true)
    {
        return WorkerResult.Fail(errorCode, canRetry);
    }

    protected ValueTask<WorkerResult> FailTask(WorkFlowErrorCode errorCode, bool canRetry = true)
        => ValueTask.FromResult(Fail(errorCode, canRetry));

    protected WorkerResult Success()
    {
        return WorkerResult.Success();
    }

    protected ValueTask<WorkerResult> SuccessTask()
    {
        return ValueTask.FromResult(Success());
    }
}
