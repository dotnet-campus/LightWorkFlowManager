using System;
using System.Threading.Tasks;
using DC.LightWorkFlowManager;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Exceptions;
using DC.LightWorkFlowManager.Protocols;
using DC.LightWorkFlowManager.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

namespace LightWorkFlowManager.Tests;

/// <summary>
/// `MessageWorkerManager` 相关测试。
/// </summary>
[TestClass]
public class MessageWorkerManagerTest
{
    private const int UnknownError = 7000;

    /// <summary>
    /// 验证工作器管理器在失败、重试与异常场景下的行为。
    /// </summary>
    [ContractTestCase]
    public void RunFail()
    {
        "执行工作器，抛出工作器异常，异常设置可以重试，重试返回成功，管理器状态是成功".Test(async () =>
        {
            var count = 0;
            var messageWorkerManager = GetTestMessageWorkerManager();

            var delegateMessageWorker = new DelegateMessageWorker(_ =>
            {
                count++;

                if (count == 1)
                    // 异常设置可以重试
                    throw new MessageWorkerException(UnknownError, true);
                return ValueTask.CompletedTask;
            });

            var result = await messageWorkerManager.RunWorker(delegateMessageWorker);
            Assert.AreEqual(true, result.IsSuccess);
            Assert.AreEqual(false, messageWorkerManager.MessageWorkerStatus.IsFail);
        });

        "执行工作器，不断抛出异常，可以将异常抛出管理器，管理器状态是失败".Test(async () =>
        {
            var messageWorkerManager = GetTestMessageWorkerManager();

            var delegateMessageWorker = new DelegateMessageWorker(_ =>
            {
                throw new ArgumentException();
#pragma warning disable CS0162
                return ValueTask.CompletedTask;
#pragma warning restore CS0162
            });

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await messageWorkerManager.RunWorker(delegateMessageWorker);
            });

            Assert.AreEqual(true, messageWorkerManager.MessageWorkerStatus.IsFail);
        });

        "执行工作器，先抛出任意异常，重试时返回成功，管理器状态是成功".Test(async () =>
        {
            var messageWorkerManager = GetTestMessageWorkerManager();
            var count = 0;

            var delegateMessageWorker = new DelegateMessageWorker(_ =>
            {
                count++;

                if (count == 3)
                    return ValueTask.FromResult(WorkerResult.Success());
                throw new ArgumentException();
            });

            var result = await messageWorkerManager.RunWorker(delegateMessageWorker);
            Assert.AreEqual(true, result.IsSuccess);
            Assert.AreEqual(3, count);
            Assert.AreEqual(false, messageWorkerManager.MessageWorkerStatus.IsFail);
        });

        "执行一个输入参数不存在工作器，抛出阻断异常".Test(async () =>
        {
            var messageWorkerManager = GetTestMessageWorkerManager();

            await Assert.ThrowsExceptionAsync<MessageWorkerInputNotFoundException>(async () =>
            {
                // 没有给工作器设置参数
                await messageWorkerManager.RunWorker<RequestInputMessageWorker>();
            });

            Assert.AreEqual(true, messageWorkerManager.MessageWorkerStatus.IsFail);
        });

        "执行一个一直返回 WorkerResult 失败的工作器，管理器的状态是失败".Test(async () =>
        {
            var messageWorkerManager = GetTestMessageWorkerManager();

            var delegateMessageWorker = new DelegateMessageWorker(_ =>
            {
                return ValueTask.FromResult<WorkerResult>(WorkerResult.Fail(UnknownError));
            });

            await messageWorkerManager.RunWorker(delegateMessageWorker);

            // 管理器的状态是失败
            Assert.AreEqual(true, messageWorkerManager.MessageWorkerStatus.IsFail);
        });

        "一个工作器先执行一次失败，再执行一次成功，失败通过 WorkerResult 返回，最后管理器的状态应该是成功".Test(async () =>
        {
            var messageWorkerManager = GetTestMessageWorkerManager();

            var failTestMessageWorker = new FailTestMessageWorker();
            await messageWorkerManager.RunWorker(failTestMessageWorker);

            // 最后管理器的状态应该是成功
            Assert.AreEqual(false, messageWorkerManager.MessageWorkerStatus.IsFail);
        });
    }

    [TestMethod]
    public async Task RunWorker_WithInputWorkerAndDirectInput_RunsWorkerWithProvidedInput()
    {
        var messageWorkerManager = GetTestMessageWorkerManager();

        await messageWorkerManager.RunWorker<CaptureProvidedInputWorker, DirectWorkerInput>(new DirectWorkerInput("direct-input"));

        var observedInput = messageWorkerManager.Context.GetEnsureContext<ObservedWorkerInput>();
        Assert.AreEqual("direct-input", observedInput.Value);
    }

    [TestMethod]
    public async Task RunWorker_WithInputWorkerAndConverter_RunsWorkerWithConvertedInput()
    {
        var messageWorkerManager = GetTestMessageWorkerManager();
        messageWorkerManager.SetContext(new WorkerArgument("argument"));

        await messageWorkerManager.RunWorker<CaptureConvertedInputWorker, WorkerArgument, ConvertedWorkerInput>(argument => new ConvertedWorkerInput($"{argument.Value}-converted"));

        var observedInput = messageWorkerManager.Context.GetEnsureContext<ObservedWorkerInput>();
        Assert.AreEqual("argument-converted", observedInput.Value);
    }

    [TestMethod]
    public async Task RunWorker_WithInputOutputWorkerAndDirectInput_ReturnsWorkerOutput()
    {
        var messageWorkerManager = GetTestMessageWorkerManager();

        var result = await messageWorkerManager.RunWorker<ReturnProvidedOutputWorker, DirectWorkerInput, WorkerOutput>(new DirectWorkerInput("direct-input"));

        Assert.AreEqual("output:direct-input", result.Result?.Value);
    }

    [TestMethod]
    public async Task RunWorker_WithInputOutputWorkerAndConverter_ReturnsConvertedWorkerOutput()
    {
        var messageWorkerManager = GetTestMessageWorkerManager();
        messageWorkerManager.SetContext(new WorkerArgument("argument"));

        var result = await messageWorkerManager.RunWorker<ReturnConvertedOutputWorker, WorkerArgument, ConvertedWorkerInput, WorkerOutput>(argument => new ConvertedWorkerInput($"{argument.Value}-converted"));

        Assert.AreEqual("output:argument-converted", result.Result?.Value);
    }

    /// <summary>
    /// 创建用于测试的工作器管理器。
    /// </summary>
    /// <param name="serviceProvider">可选的服务提供器。</param>
    /// <param name="taskId">可选的任务标识。</param>
    /// <param name="taskName">可选的任务名称。</param>
    /// <param name="retryCount">重试次数。</param>
    /// <returns>测试用工作器管理器实例。</returns>
    public static MessageWorkerManager GetTestMessageWorkerManager(IServiceProvider? serviceProvider = null,
        string? taskId = null, string? taskName = null, int retryCount = 3)
    {
        var messageWorkerManager = new MessageWorkerManager(taskId ?? Guid.NewGuid().ToString(), taskName ?? "Test",
            serviceProvider?.CreateScope() ?? BuildServiceProvider().CreateScope(), retryCount);
        return messageWorkerManager;
    }

    /// <summary>
    /// 构建测试用服务提供器。
    /// </summary>
    /// <returns>测试用服务提供器实例。</returns>
    public static IServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddTransient<RequestInputMessageWorker>();
        serviceCollection.AddTransient<CaptureProvidedInputWorker>();
        serviceCollection.AddTransient<CaptureConvertedInputWorker>();
        serviceCollection.AddTransient<ReturnProvidedOutputWorker>();
        serviceCollection.AddTransient<ReturnConvertedOutputWorker>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    private record DirectWorkerInput(string Value);

    private record WorkerArgument(string Value);

    private record ConvertedWorkerInput(string Value);

    private record ObservedWorkerInput(string Value);

    private record WorkerOutput(string Value);

    private class RequestInputMessageWorker : MessageWorker<TestInputFoo>
    {
        protected override ValueTask<WorkerResult> DoInnerAsync(TestInputFoo input)
        {
            return ValueTask.FromResult(WorkerResult.Success());
        }
    }

    private class TestInputFoo
    {
    }

    private class CaptureProvidedInputWorker : MessageWorker<DirectWorkerInput>
    {
        protected override ValueTask<WorkerResult> DoInnerAsync(DirectWorkerInput input)
        {
            SetContext(new ObservedWorkerInput(input.Value));
            return ValueTask.FromResult(WorkerResult.Success());
        }
    }

    private class CaptureConvertedInputWorker : MessageWorker<ConvertedWorkerInput>
    {
        protected override ValueTask<WorkerResult> DoInnerAsync(ConvertedWorkerInput input)
        {
            SetContext(new ObservedWorkerInput(input.Value));
            return ValueTask.FromResult(WorkerResult.Success());
        }
    }

    private class ReturnProvidedOutputWorker : MessageWorker<DirectWorkerInput, WorkerOutput>
    {
        protected override ValueTask<WorkerResult<WorkerOutput>> DoInnerAsync(DirectWorkerInput input)
        {
            return ValueTask.FromResult<WorkerResult<WorkerOutput>>(new WorkerOutput($"output:{input.Value}"));
        }
    }

    private class ReturnConvertedOutputWorker : MessageWorker<ConvertedWorkerInput, WorkerOutput>
    {
        protected override ValueTask<WorkerResult<WorkerOutput>> DoInnerAsync(ConvertedWorkerInput input)
        {
            return ValueTask.FromResult<WorkerResult<WorkerOutput>>(new WorkerOutput($"output:{input.Value}"));
        }
    }

    private class FailTestMessageWorker : MessageWorker
    {
        public bool Success { get; set; }

        protected override ValueTask<WorkerResult> DoInnerAsync(IWorkerContext context)
        {
            var result = Success ? WorkerResult.Success() : WorkerResult.Fail(UnknownError);
            Success = !Success;

            return ValueTask.FromResult(result);
        }
    }
}