using System;
using System.Threading.Tasks;

using LightWorkFlowManager.Contexts;
using LightWorkFlowManager.Exceptions;
using LightWorkFlowManager.Protocols;
using LightWorkFlowManager.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

namespace LightWorkFlowManager.Tests;

[TestClass]
public class MessageWorkerManagerTest
{
    private const int UnknownError = 7000;

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

    public static MessageWorkerManager GetTestMessageWorkerManager(IServiceProvider? serviceProvider = null,
        string? taskId = null, string? taskName = null, int retryCount = 3)
    {
        var messageWorkerManager = new MessageWorkerManager(taskId ?? Guid.NewGuid().ToString(), taskName ?? "Test",
            serviceProvider?.CreateScope() ?? BuildServiceProvider().CreateScope(), retryCount);
        return messageWorkerManager;
    }

    public static IServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddTransient<RequestInputMessageWorker>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    private class RequestInputMessageWorker : MessageWorker<TestInputFoo>
    {
        protected override ValueTask<WorkerResult> DoAsync(TestInputFoo input)
        {
            return ValueTask.FromResult(WorkerResult.Success());
        }
    }

    private class TestInputFoo
    {
    }

    private class FailTestMessageWorker : MessageWorkerBase
    {
        public bool Success { get; set; }

        public override ValueTask<WorkerResult> Do(IWorkerContext context)
        {
            var result = Success ? WorkerResult.Success() : WorkerResult.Fail(UnknownError);
            Success = !Success;

            return ValueTask.FromResult(result);
        }
    }
}