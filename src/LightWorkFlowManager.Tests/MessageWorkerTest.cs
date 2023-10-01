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
public class MessageWorkerTest
{
    [ContractTestCase]
    public void RunWorkerOnWorker()
    {
        "可以在一个 Worker 里面套另一个 Worker 的运行".Test(async () =>
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddTransient<Worker1>()
                .AddTransient<Worker2>()
                .AddLogging();

            await using var serviceProvider = serviceCollection.BuildServiceProvider();
            var serviceScope = serviceProvider.CreateScope();

            await using var messageWorkerManager = new MessageWorkerManager(taskId: Guid.NewGuid().ToString(), taskName: "TestRunWorkerOnWorker", serviceScope);

            var result = await messageWorkerManager.RunWorker<Worker1>();
            Assert.AreEqual(true, result.IsSuccess);
        });
    }

    class Worker1 : MessageWorkerBase
    {
        public override async ValueTask<WorkerResult> Do(IWorkerContext context)
        {
            await Manager
                .GetWorker<Worker2>()
                .RunAsync(new InputType());

            return WorkerResult.Success();
        }
    }

    class Worker2 : MessageWorker<InputType, OutputType>
    {
        protected override ValueTask<WorkerResult<OutputType>> DoInnerAsync(InputType input)
        {
            return SuccessTask(new OutputType());
        }
    }

    record InputType();

    record OutputType();
}