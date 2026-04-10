using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Workers;

namespace DC.LightWorkFlowManager.Protocols;

/// <summary>
/// 表示工作器执行结果。
/// </summary>
public class WorkerResult
{
    /// <summary>
    /// 创建工作器执行结果。
    /// </summary>
    /// <param name="isSuccess">是否执行成功。</param>
    /// <param name="errorCode">执行失败时的错误码。</param>
    /// <param name="canRetry">执行失败后是否允许重试。</param>
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

    /// <summary>
    /// 获取当前执行是否成功。
    /// </summary>
    public virtual bool IsSuccess { get; }

    /// <summary>
    /// 获取当前执行是否失败。
    /// </summary>
    public bool IsFail => !IsSuccess;

    /// <summary>
    /// 获取执行失败时的错误码。
    /// </summary>
    public WorkFlowErrorCode ErrorCode { get; }

    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool CanRetry { get; }

    /// <summary>
    /// 创建一个成功结果。
    /// </summary>
    /// <returns>表示成功的执行结果。</returns>
    public static WorkerResult Success() => new WorkerResult(true, WorkFlowErrorCode.Ok, false);

    /// <summary>
    /// 创建一个失败结果。
    /// </summary>
    /// <param name="errorCode">失败时的错误码。</param>
    /// <param name="canRetry">失败后是否允许重试。</param>
    /// <returns>表示失败的执行结果。</returns>
    public static WorkerResult Fail(WorkFlowErrorCode errorCode, bool canRetry=true) => new WorkerResult(false, errorCode, canRetry);

    public WorkerResult<T> AsFail<T>()
    {
        if (IsSuccess)
        {
            throw new InvalidOperationException($"仅当 Result 为失败时，才能作为另一个失败的结果");
        }

        return new WorkerResult<T>(ErrorCode, CanRetry);
    }

    /// <inheritdoc />
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

/// <summary>
/// 表示带返回值的工作器执行结果。
/// </summary>
/// <typeparam name="T">结果值类型。</typeparam>
public class WorkerResult<T> : WorkerResult
{
    /// <summary>
    /// 创建一个成功的执行结果。
    /// </summary>
    /// <param name="result">工作器输出结果。</param>
    public WorkerResult(T result) : base(isSuccess: true, WorkFlowErrorCode.Ok, canRetry: false)
    {
        Result = result;
    }

    /// <summary>
    /// 创建一个失败的执行结果。
    /// </summary>
    /// <param name="errorCode">失败时的错误码。</param>
    /// <param name="canRetry">失败后是否允许重试。</param>
    public WorkerResult(WorkFlowErrorCode errorCode, bool canRetry) : base(isSuccess: false, errorCode, canRetry)
    {
        Result = default;
    }

    /// <summary>
    /// 获取当前执行是否成功。
    /// </summary>
    [MemberNotNullWhen(true, nameof(Result))]
    public override bool IsSuccess => Result != null;

    /// <summary>
    /// 获取工作器输出结果。
    /// </summary>
    public T? Result { get; }

    // 为什么不做链调用呢？ 这是因为代码实际写起来不好看：
    // WorkerResult<F1> r1 = xx;
    // var r2 = r1.RunWorker<Worker1, F2>();
    // 这时可以看到在 RunWorker 里面必须加上 TOutput 参数，导致实际不好写，所以就不做链调用了
    // 决定在带 Input 的 MessageWorker 的 RunAsync 方法提供 WorkerResult<T> 作为入参，这样的结果可能更好
#if false
    private WorkerResult<TOutput> RunWorker<TWorker, TOutput>()
        where TWorker : MessageWorker<T, TOutput>
    {
        throw null;
    }
#endif

    /// <summary>
    /// 将此结果转换为另一个结果
    /// </summary>
    /// <remarks>
    /// 无需提前判断结果是否为成功，可自动当成功时才执行转换。对 IsFail 的结果调用转换，结果也是返回一个失败的结果，且错误码和是否可重试与原结果一致  
    /// </remarks>
    /// <typeparam name="TOther"></typeparam>
    /// <param name="converter">转换器，当结果为成功时才会被调用</param>
    /// <returns></returns>
    public WorkerResult<TOther> Convert<TOther>(Func<T, TOther> converter)
    {
        if (IsSuccess)
        {
            return converter(Result);
        }
        else
        {
            return AsFail<TOther>();
        }
    }

    /// <summary>
    /// 将输出结果隐式转换为成功的执行结果。
    /// </summary>
    /// <param name="workerResult">输出结果。</param>
    /// <returns>包装后的执行结果。</returns>
    public static implicit operator WorkerResult<T>(T workerResult)
    {
        return new WorkerResult<T>(workerResult);
    }

    /// <summary>
    /// 从执行结果中隐式获取输出结果。
    /// </summary>
    /// <param name="workerResult">执行结果。</param>
    /// <returns>输出结果。</returns>
    public static implicit operator T?(WorkerResult<T> workerResult) => workerResult.Result;

    /// <inheritdoc />
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
