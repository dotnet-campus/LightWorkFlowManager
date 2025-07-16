using System;
using DC.LightWorkFlowManager.Monitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

namespace LightWorkFlowManager.Tests;

[TestClass]
public class ProgressCompositorTest
{
    [ContractTestCase]
    public void TestSubProgressReport()
    {
        "自身也上报进度的情况下，存在两个各占一半比例的子进度，两个子进度和自身进度都为百分之50时，实际进度为百分之75的值".Test(() =>
        {
            var progressCompositor = new ProgressCompositor<int>("Xxx");
            var registerSubProgressCompositors = progressCompositor.RegisterSubProgressCompositors(new SubProgressCompositorInfo("1", 50), new SubProgressCompositorInfo("2", 50));

            // 自身进度为百分之50时
            var selfProcess = 0.5;
            progressCompositor.Report(new ProgressPercentage(selfProcess));

            // 两个子进度为百分之50时
            registerSubProgressCompositors[0].Report(new ProgressPercentage(0.5));
            registerSubProgressCompositors[1].Report(new ProgressPercentage(0.5));

            Assert.IsTrue(Math.Abs(progressCompositor.CurrentProgress.Value - (selfProcess + 0.5 / 2)) < 0.01);
        });

        "存在两个各占一半比例的子进度，首个子进度为百分之50时，实际进度为百分之25的值".Test(() =>
        {
            var progressCompositor = new ProgressCompositor<int>("Xxx");
            var registerSubProgressCompositors = progressCompositor.RegisterSubProgressCompositors(new SubProgressCompositorInfo("1", 50), new SubProgressCompositorInfo("2", 50));

            registerSubProgressCompositors[0].Report(new ProgressPercentage(0.5));

            Assert.IsTrue(Math.Abs(progressCompositor.CurrentProgress.Value - 0.5 / 2) < 0.01);
        });
    }
}