# LightWorkFlowManager

轻量的工作过程管理

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

## 相似的项目

https://github.com/danielgerlag/workflow-core