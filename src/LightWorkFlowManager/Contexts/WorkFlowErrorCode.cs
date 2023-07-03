using System;
using System.Collections.Concurrent;

namespace LightWorkFlowManager.Contexts;

/// <summary>
/// 错误码
/// </summary>
/// 之前只是返回一个数值，不方便其他接入方了解问题原因。也不方便自己看 ES 日志，为了解决此问题，将可读的信息也记录
public readonly struct WorkFlowErrorCode : IEquatable<WorkFlowErrorCode>
{
    /// <summary>
    /// 创建错误码
    /// </summary>
    public WorkFlowErrorCode(int code, string message)
    {
        Code = code;
        Message = message;

        ErrorCodeDictionary[code] = this;
    }

    /// <summary>
    /// 错误码
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// 给人类的信息
    /// </summary>
    public string Message { get; }

    public static WorkFlowErrorCode Ok => new WorkFlowErrorCode(0, "Ok");

    public WorkFlowErrorCode AppendMessage(string? appendMessage)
    {
        if (appendMessage == null)
        {
            return this;
        }
        else
        {
            return new WorkFlowErrorCode(Code, Message + " " + appendMessage);
        }
    }

    public static implicit operator int(WorkFlowErrorCode code)
    {
        return code.Code;
    }

    public static implicit operator WorkFlowErrorCode(int code)
    {
        if (ErrorCodeDictionary.TryGetValue(code, out var value))
        {
            return value;
        }

        if (code == WorkFlowErrorCode.Ok.Code)
        {
            return new WorkFlowErrorCode(code, "Ok");
        }

        return new WorkFlowErrorCode(code, string.Empty);
    }

    public override string ToString() => $"{Code} {Message}";

    private static readonly ConcurrentDictionary<int, WorkFlowErrorCode> ErrorCodeDictionary =
        new ConcurrentDictionary<int, WorkFlowErrorCode>();

    public bool Equals(WorkFlowErrorCode other)
    {
        return Code == other.Code;
    }

    public override bool Equals(object? obj)
    {
        return obj is WorkFlowErrorCode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Code;
    }

    public static bool operator ==(WorkFlowErrorCode left, WorkFlowErrorCode right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WorkFlowErrorCode left, WorkFlowErrorCode right)
    {
        return !left.Equals(right);
    }
}
