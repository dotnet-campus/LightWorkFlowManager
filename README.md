# LightWorkFlowManager

| [中文文档](./README.md) | [English Document](./README.en-US.md) |

轻量的工作过程管理

[![](https://img.shields.io/nuget/v/dotnetCampus.LightWorkFlowManager.svg)](https://www.nuget.org/packages/dotnetCampus.LightWorkFlowManager)
[![Github issues](https://img.shields.io/github/issues/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/issues)
[![Github forks](https://img.shields.io/github/forks/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/network/members)
[![Github stars](https://img.shields.io/github/stars/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/stargazers)
[![Top language](https://img.shields.io/github/languages/top/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/)
[![Github license](https://img.shields.io/github/license/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/)

世界上有许多现有的工作过程管理，那为什么还重新造了一个？没啥原因，自己造的用的顺手

本工作过程管理库有什么特色？

- 支持链调用，可全程串行编写工作过程，无需分支判断逻辑
- 自动输入输出参数传递，可手动可自动
- 内建各种辅助机制：
  - 自动失败重试
  - 自动错误传递
  - 内建错误码机制
- 可观测执行状态： IWorkerRunMonitor 机制
- 高定制模式
  - 可重写定制上下文管理
  - 可重写工作器执行管理
  - 可重写异常捕获与记录

为了不和旧代码冲突，这里命名 Worker 为 MessageWorker 类型

## 使用方法

1、 创建 MessageWorkerManager 对象

```csharp
            // 每个任务一个 TaskId 号
            string taskId = Guid.NewGuid().ToString();
            // 相同类型的任务采用相同的名字，比如这是做 PPT 解析的任务
            string taskName = "PPT 解析";
            // 提供容器
            IServiceScope serviceScope = serviceProvider.CreateScope();

            var workerManager = new MessageWorkerManager(taskId, taskName, serviceScope);
```

2、 定义 Worker 工作器

```csharp
record InputType();

record OutputType();

class FooWorker : MessageWorker<InputType, OutputType>
{
    protected override ValueTask<WorkerResult<OutputType>> DoInnerAsync(InputType input)
    {
        ...
    }
}
```

每个工作器都可以声明其输入和输出类型，输入类型将会自动由框架注入，输出类型将会自动存储到框架里面

请确保定义的工作器注册到容器里面，推荐使用 `AddTransient` 做瞬时注入或 `AddScoped` 做作用域注入；最好**不要**使用 `AddSingleton` 做单例注入，避免多次工作流状态混乱

3、 执行 Worker 工作器

```csharp
            var result = await workerManager
                .GetWorker<FooWorker>()
                .RunAsync();
```

以上代码也可以简写为以下代码

```csharp
            var result = await workerManager.RunWorker<FooWorker>();
```

## 机制和功能

### 工作器参数

工作器参数通过 MessageWorkerManager 工作器管理的 IWorkerContext 上下文读取。每个工作器的可返回值类型都会自动被设置到 IWorkerContext 上下文里面。如此即可自动实现上一个 Worker 的输出作为下一个 Worker 的输入

在每个工作器里面，都可以通过 `SetContext` 设置上下文信息

在开始执行工作器时，还可以手动设置输入参数，如以下例子

```csharp
 // 例子1：先获取工作器，再赋值给到工作器的执行方法

            await Manager
                .GetWorker<FooWorker>()
                .RunAsync(new InputType());

 // 例子2： 通过 SetContext 进行设置参数，再执行工作器
           await Manager
                .SetContext(new InputType())
                .RunWorker<FooWorker>();
```

如果有些工作器之间的输入和输出参数需要进行转换，也可以 `SetContext` 传入转换委托进行参数转换

```csharp
 // 以下例子将当前上下文里的 Foo1Type 类型转换为 FooWorker 需要的 Foo2Type 参数
           await Manager
                .SetContext((Foo1Type foo1) => ConvertFoo1ToFoo2(foo1))
                .RunWorker<FooWorker>();
```

#### 链路调用

在实际业务里，通常会连续调用多个 Worker 工作器，将上一步的输出作为下一步的输入。框架支持将整个调用过程串起来，形成一个清晰的链路。

如以下例子：

```csharp
        WorkerResult<Info2> step1Result = await messageWorkerManager
            .GetWorker<Worker1>()
            .RunAsync(new Info1());

        WorkerResult<Info3> step2Result = await messageWorkerManager
            .GetWorker<Worker2>()
            .RunAsync(step1Result);

        // 假定中间没有 Worker3 ，此时可以手动转换参数
        WorkerResult<Info4> info4Result = step2Result.Convert((Info3 info3) => new Info4());

        WorkerResult<Info5> step3Result = await messageWorkerManager
            .GetWorker<Worker4>()
            .RunAsync(info4Result);

        // 注： 假定在 Worker5 里面将返回失败结果
        WorkerResult<Info6> step4Result = await messageWorkerManager
            .GetWorker<Worker5>()
            .RunAsync(step3Result);

        // 尽管 Worker5 失败了，但是其结果依然可以传递给 Worker6 作为入参。这样的设计可以减少大量的错误判断分支代码，尽量确保整个工作链路串行编写。收到带上失败状态的入参时， 不会真的执行 `Worker6` 而是直接将错误继续往后传递
        WorkerResult<Info7> step5Result = await messageWorkerManager
            .GetWorker<Worker6>()
            .RunAsync(step4Result);

        // 链路最后拿到的结果，会和 MessageWorkerStatus 状态保持一致
        Assert.AreEqual(messageWorkerManager.MessageWorkerStatus.Status, step5Result.ErrorCode);
        // 同时在 MessageWorkerStatus 也会记录最初失败的 Worker 信息
        // 如下所示，最后一步的结果是由 Worker6 输出的 `step5Result`，但记录的失败工作器信息依然是最初失败的 Worker5 信息
        Assert.AreEqual(nameof(Worker5), messageWorkerManager.MessageWorkerStatus.FailWorker?.WorkerName);
```

在 `RunAsync` 方法里面，既可以传入明确的输入参数类型，如 `RunAsync(new Info1())` ，也可以直接传入上一步执行得到的结果类型，如 `RunAsync(step1Result)` 。这样在串联多个 Worker 时，代码会比较自然。

如果中间某一步的输入输出类型对不上，可以和上文工作器参数一节一样，先进行一次转换，再继续往下执行。以上例子里的 `step2Result.Convert((Info3 info3) => new Info4())` 就是如此。

当某一个 Worker 执行失败时，后续链路依然可以继续拿到一个 `WorkerResult<T>` 对象。例如 `Worker5` 返回失败之后，依然可以拿到 `WorkerResult<Info6> step4Result` 。但这个结果已经带上失败状态，再继续传给 `Worker6` 执行时，`Worker6` 不会真的被执行，而是直接将错误继续往后传递。

因此链路最后拿到的结果，会和 `messageWorkerManager.MessageWorkerStatus.Status` 保持一致。也就是可以通过 `Assert.AreEqual(messageWorkerManager.MessageWorkerStatus.Status, step5Result.ErrorCode);` 这样的方式，确认最终结果就是当前工作流管理器记录下来的状态。

同时，在 `messageWorkerManager.MessageWorkerStatus.FailWorker` 里面，还会记录最初失败的是哪一个 Worker 。如以上例子，在 `Worker5` 首次失败之后，即可通过 `Assert.AreEqual(nameof(Worker5), messageWorkerManager.MessageWorkerStatus.FailWorker?.WorkerName);` 确认失败工作器信息。

以下是各示例工作器类型的代码定义：

```csharp
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
```

### 异常中断和重试

每个 Worker 都可以返回 WorkerResult 类型的返回值，可以在返回值里面告知框架层是否当前的 Worker 执行成功。在执行失败时，可以赋值错误码，方便定位调试或输出。在执行失败时，可以返回告知框架层是否需要重试

中断后续的工作器执行有两个方法：

方法1： 通过返回状态为失败的 WorkerResult 返回值。一旦工作管理器的状态为 IsFail 状态，将会阻止所有的没有标记 CanRunWhenFail 为 true 的工作器的执行。换句话说就是除了哪些不断成功或失败状态都要执行的 Worker 工作器之外，其他的工作器都将不会执行，包括 SetContext 里面的委托转换也不会执行

方法2： 通过抛出异常的方式，通过 dotnet 里面的异常可以让后续逻辑炸掉不再执行

以上两个方法都是框架推荐使用的。框架设计的偏好是如果在重视性能的情况下，尽量使用方法1的方式中断执行。如果是在复杂的业务逻辑，有大量业务逻辑穿插在工作过程之外，则可以方便通过方法2进行中断

### 在 Worker 里执行其他 Worker 工作器

在一个 Worker 里面，可以执行其他的 Worker 工作器，如此可以比较自由的实现分支逻辑，套娃决定执行工作器

例子如下：

假定有一个 Worker2 工作器，定义如下：

```csharp
    class Worker2 : MessageWorker<InputType, OutputType>
    {
        protected override ValueTask<WorkerResult<OutputType>> DoInnerAsync(InputType input)
        {
            return SuccessTask(new OutputType());
        }
    }

    record InputType();

    record OutputType();
```

有另一个 Worker1 工作器，可以在 Worker1 里面执行 Worker2 工作器：

```csharp
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
```

### 委托工作器

有一些非常小且轻的逻辑，也想加入到工作过程里面，但不想为此定义一个单独的工作器。可以试试委托工作器，如以下代码例子

```csharp
            var delegateMessageWorker = new DelegateMessageWorker(_ =>
            {
                // 在这里编写委托工作器的业务内容
            });

            var result = await messageWorkerManager.RunWorker(delegateMessageWorker);
```

如果连 DelegateMessageWorker 都不想创建，那也可以直接使用 MessageWorkerManager 的 RunWorker 方法传入委托，如以下代码例子

```csharp
            await messageWorkerManager.RunWorker((IWorkerContext context) =>
            {
                // 在这里编写委托工作器的业务内容
            });
```

### 内建机制



`MessageWorker` 除了约定工作器的执行方式之外，还内建了一些在自己编写工作器时可直接使用的机制，用来减少样板代码。

- 可重写工作器名称。默认情况下，`WorkerName` 使用当前类型名，如果希望在日志或调试时显示更明确的名称，可以重写 `WorkerName` 属性。
- 可以为工作器设置自己能否被重试。`CanRetry` 默认值为 `true`，如某个工作器失败后不适合重复执行，可以自行设置为 `false`。如果要在某次失败时精确指定是否允许重试，也可以通过 `Fail(errorCode, canRetry)` 或 `FailTask(errorCode, canRetry)` 返回结果。
- 可设置当前置工作器执行失败时，当前工作器是否依然继续执行。`CanRunWhenFail` 默认值为 `false`，如果当前工作器属于收尾、记录日志、清理现场之类的逻辑，可以将其设置为 `true`。
- 如果有多个输出内容，可以通过 `SetContext` 将额外结果写入上下文，供后续工作器继续读取，不必只依赖单一输出值。
- 如果执行成功且没有返回值，可以直接调用 `Success()` 返回成功结果。如果执行方法里面没有真正的异步逻辑，也可以直接调用 `SuccessTask()` 获取返回值。
- 如果执行失败，可以调用 `Fail` 或 `FailTask` 快速返回失败结果。
- 对于继承自 `MessageWorker<TInput, TOutput>` 的类型，还可以使用 `Success(TOutput output)` 直接返回带输出的成功结果；如果当前拿到的是一个失败结果，也可以通过 `WorkerResult<TOutput> Fail(WorkerResult failResult)` 将其转换成当前输出类型的失败结果。
- 可以用 `WorkerResult.AsFail<T>` 方法将一个失败状态转换为另一个类型的失败状态，错误码和是否允许重试等信息会被保留。
- 可以用 `RegisterOnDispose` 系列方法注册当整个 `MessageWorkerManager` 被 `Dispose` 时执行的逻辑，例如清理临时文件、删除文件夹或执行其他收尾工作。

## 相似的项目

https://github.com/danielgerlag/workflow-core