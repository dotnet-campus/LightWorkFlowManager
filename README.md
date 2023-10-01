# LightWorkFlowManager

轻量的工作过程管理

[![](https://img.shields.io/nuget/v/dotnetCampus.LightWorkFlowManager.svg)](https://www.nuget.org/packages/dotnetCampus.LightWorkFlowManager)
[![Github issues](https://img.shields.io/github/issues/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/issues)
[![Github forks](https://img.shields.io/github/forks/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/network/members)
[![Github stars](https://img.shields.io/github/stars/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/stargazers)
[![Top language](https://img.shields.io/github/languages/top/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/)
[![Github license](https://img.shields.io/github/license/dotnet-campus/LightWorkFlowManager)](https://github.com/dotnet-campus/LightWorkFlowManager/)

世界上有许多现有的工作过程管理，那为什么还重新造了一个？没啥原因，自己造的用的顺手

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

## 相似的项目

https://github.com/danielgerlag/workflow-core