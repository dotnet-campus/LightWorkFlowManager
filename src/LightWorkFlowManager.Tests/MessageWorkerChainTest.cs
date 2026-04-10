using DC.LightWorkFlowManager;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Protocols;
using DC.LightWorkFlowManager.Workers;

namespace LightWorkFlowManager.Tests;

[TestClass]
public class MessageWorkerChainTest
{
    [TestMethod]
    public async Task TestChainCallMessageWorker()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddTransient<Worker1>();
        serviceCollection.AddTransient<Worker2>();
        //serviceCollection.AddTransient<Worker3>();
        serviceCollection.AddTransient<Worker4>();
        serviceCollection.AddTransient<Worker5>();
        serviceCollection.AddTransient<Worker6>();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IServiceScope serviceScope = serviceProvider.CreateScope();

        var taskId = Guid.NewGuid().ToString();
        var taskName = nameof(TestChainCallMessageWorker);
        await using var messageWorkerManager = new MessageWorkerManager(taskId, taskName, serviceScope);

        WorkerResult<Info2> step1Result = await messageWorkerManager
            .GetWorker<Worker1>()
            .RunAsync(new Info1());

        WorkerResult<Info3> step2Result = await messageWorkerManager
            .GetWorker<Worker2>()
            .RunAsync(step1Result);

        // Not exist worker3
        WorkerResult<Info4> info4Result = step2Result.Convert((Info3 info3) => new Info4());

        WorkerResult<Info5> step3Result = await messageWorkerManager
            .GetWorker<Worker4>()
            .RunAsync(info4Result);

        WorkerResult<Info6> step4Result = await messageWorkerManager
            .GetWorker<Worker5>()
            .RunAsync(step3Result);

        WorkerResult<Info7> step5Result = await messageWorkerManager
            .GetWorker<Worker6>()
            .RunAsync(step4Result);

        Assert.AreEqual(messageWorkerManager.MessageWorkerStatus.Status, step5Result.ErrorCode);
    }

    class Worker1 : MessageWorker<Info1, Info2>
    {
        protected override async ValueTask<WorkerResult<Info2>> DoInnerAsync(Info1 input)
        {
            await Task.CompletedTask;
            return new Info2();
        }
    }

    class Worker2 : MessageWorker<Info2, Info3>
    {
        protected override async ValueTask<WorkerResult<Info3>> DoInnerAsync(Info2 input)
        {
            await Task.CompletedTask;
            return new Info3();
        }
    }

    class Worker4 : MessageWorker<Info4, Info5>
    {
        protected override async ValueTask<WorkerResult<Info5>> DoInnerAsync(Info4 input)
        {
            await Task.CompletedTask;
            return new Info5();
        }
    }

    class Worker5 : MessageWorker<Info5, Info6>
    {
        protected override async ValueTask<WorkerResult<Info6>> DoInnerAsync(Info5 input)
        {
            await Task.CompletedTask;
            return Fail(new WorkFlowErrorCode(123, "The error message"), canRetry: false);
        }
    }

    class Worker6 : MessageWorker<Info6, Info7>
    {
        protected override async ValueTask<WorkerResult<Info7>> DoInnerAsync(Info6 input)
        {
            await Task.CompletedTask;
            return new Info7();
        }
    }

    record Info1();
    record Info2();
    record Info3();
    record Info4();
    record Info5();
    record Info6();
    record Info7();
}