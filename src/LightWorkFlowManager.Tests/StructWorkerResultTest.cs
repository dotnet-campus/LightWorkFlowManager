using System;
using System.Threading.Tasks;
using DC.LightWorkFlowManager;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Protocols;
using DC.LightWorkFlowManager.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace LightWorkFlowManager.Tests;

[TestClass]
public class StructWorkerResultTest
{
    [ContractTestCase]
    public void TestStructResult()
    {
        "当一个 Worker 的返回值是 struct 结构体类型时，返回失败时，应该被记录为失败".Test(async () =>
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddTransient<FooWorker>()
                .AddLogging();

            await using var serviceProvider = serviceCollection.BuildServiceProvider();
            var serviceScope = serviceProvider.CreateScope();

            await using var messageWorkerManager = new MessageWorkerManager(taskId: Guid.NewGuid().ToString(), taskName: "TestRunWorkerOnWorker", serviceScope);

            var result = await messageWorkerManager.RunWorker<FooWorker>();
            Assert.AreEqual(true, result.IsFail);
        });
    }

    public class FooWorker : MessageWorker<int, bool>
    {
        protected override ValueTask<WorkerResult<bool>> DoInnerAsync(int input)
        {
            return FailTask(new WorkFlowErrorCode(-1, "Foo"), canRetry: false);
        }
    }
}