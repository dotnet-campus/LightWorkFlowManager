using System;
using System.Diagnostics.CodeAnalysis;
using LightWorkFlowManager.Contexts;

namespace LightWorkFlowManager.Protocols;

public class WorkerResult
{
    public WorkerResult(bool isSuccess, WorkFlowErrorCode errorCode, bool canRetry)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        CanRetry = canRetry;

        if (!isSuccess && errorCode.Code == WorkFlowErrorCode.Ok)
        {
            throw new ArgumentException($"失败时，禁止设置错误码为成功", nameof(errorCode));
        }
    }

    public virtual bool IsSuccess { get; }

    public bool IsFail => !IsSuccess;

    public WorkFlowErrorCode ErrorCode { get; }

    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool CanRetry { get; }

    public static WorkerResult Success() => new WorkerResult(true, WorkFlowErrorCode.Ok, false);
    public static WorkerResult Fail(WorkFlowErrorCode errorCode, bool canRetry=true) => new WorkerResult(false, errorCode, canRetry);

    public override string ToString()
    {
        if (IsSuccess)
        {
            return $"[Ok]";
        }
        else
        {
            return $"[Fail] {ErrorCode}";
        }
    }
}

public class WorkerResult<T> : WorkerResult
{
    public WorkerResult(T result) : base(isSuccess: true, WorkFlowErrorCode.Ok, canRetry: false)
    {
        Result = result;
    }

    public WorkerResult(WorkFlowErrorCode errorCode, bool canRetry) : base(isSuccess: false, errorCode, canRetry)
    {
        Result = default;
    }

    [MemberNotNullWhen(true, nameof(Result))]
    public override bool IsSuccess => Result != null;
    public T? Result { get; }

    public static implicit operator WorkerResult<T>(T workerResult)
    {
        return new WorkerResult<T>(workerResult);
    }

    public static implicit operator T?(WorkerResult<T> workerResult) => workerResult.Result;

    public override string ToString()
    {
        if (IsSuccess)
        {
            return $"[Ok] {Result}";
        }
        else
        {
            return $"[Fail] {ErrorCode}";
        }
    }
}
