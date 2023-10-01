namespace DC.LightWorkFlowManager.Exceptions;

public class WorkerContextNotFoundException : WorkFlowException
{
    public WorkerContextNotFoundException(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public override string Message => $"Can not find {Key}";
}
