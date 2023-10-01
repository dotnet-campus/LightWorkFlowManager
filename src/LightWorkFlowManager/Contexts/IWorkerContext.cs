using DC.LightWorkFlowManager.Exceptions;

namespace DC.LightWorkFlowManager.Contexts;

public interface IWorkerContext
{
    T? GetContext<T>();
    void SetContext<T>(T context);
}

public static class MessageContextExtension
{
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
