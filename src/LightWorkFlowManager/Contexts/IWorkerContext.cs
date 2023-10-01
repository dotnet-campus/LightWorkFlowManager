using DC.LightWorkFlowManager.Exceptions;

namespace DC.LightWorkFlowManager.Contexts;

/// <summary>
/// 工作器上下文信息
/// </summary>
/// 上下文信息是带有数据的，基本上只和当前的工作过程有关的数据。和依赖注入的服务是两个不同的方向，这里的上下文信息更多的是保存一些和当前正在工作的过程有关的数据，多个不同的任务的数据不互通，相互是分开的
public interface IWorkerContext
{
    /// <summary>
    /// 获取上下文信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>如果获取不到，返回空</returns>
    T? GetContext<T>();

    /// <summary>
    /// 设置上下文信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    void SetContext<T>(T context);
}

/// <summary>
/// 工作器上下文信息扩展类型
/// </summary>
public static class MessageContextExtension
{
    /// <summary>
    /// 获取一定存在的上下文信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="workerContext"></param>
    /// <returns></returns>
    /// <exception cref="WorkerContextNotFoundException">如果上下文信息不存在，就抛出异常</exception>
    public static T GetEnsureContext<T>(this IWorkerContext workerContext)
    {
        var context = workerContext.GetContext<T>();

        if (context == null)
        {
            throw new WorkerContextNotFoundException(typeof(T).FullName!);
        }

        return context;
    }
}
