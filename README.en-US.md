# LightWorkFlowManager
## Lightweight Work Process Management

### What are the features of this workflow management library?
- Supports chain calls, allowing you to write the entire workflow serially without additional branch judgment logic
- Automatic input/output parameter passing, supports both manual and explicit assignment modes
- Built-in auxiliary mechanisms:
  - Automatic failure retry
  - Automatic error propagation
  - Built-in error code system
- Observable execution status via the `IWorkerRunMonitor` mechanism
- Highly customizable:
  - Overridable custom context management
  - Overridable worker execution management
  - Overridable exception capture and logging

To avoid conflicts with existing code, the base Worker type is named `MessageWorker` in this library.

---

## Usage
### 1. Create a MessageWorkerManager instance
```csharp
// Each task is assigned a unique TaskId
string taskId = Guid.NewGuid().ToString();
// Tasks of the same type share the same name, e.g. for PPT parsing tasks
string taskName = "PPT Parsing";
// Provide DI service scope
IServiceScope serviceScope = serviceProvider.CreateScope();

var workerManager = new MessageWorkerManager(taskId, taskName, serviceScope);
```

### 2. Define MessageWorker workers
```csharp
record InputType();

record OutputType();

class FooWorker : MessageWorker<InputType, OutputType>
{
    protected override ValueTask<WorkerResult<OutputType>> DoInnerAsync(InputType input)
    {
        // Your business logic here
    }
}
```

Each worker can declare its input and output types. The input type will be automatically injected by the framework, and the output type will be automatically stored in the framework context.

> Please make sure to register your defined workers to the DI container. We recommend using `AddTransient` (transient injection) or `AddScoped` (scoped injection). **Do NOT use `AddSingleton` (singleton injection)** to avoid state confusion across multiple workflow runs.

### 3. Execute workers
```csharp
var result = await workerManager
    .GetWorker<FooWorker>()
    .RunAsync();
```

The above code can also be abbreviated as:
```csharp
var result = await workerManager.RunWorker<FooWorker>();
```

---

## Mechanisms & Features
### Worker Parameters
Worker parameters are read from the `IWorkerContext` managed by `MessageWorkerManager`. The return value type of each worker will be automatically set to the `IWorkerContext`, so the output of the previous worker can be automatically used as the input of the next worker.

You can also set context information explicitly via `SetContext` in any worker.

You can manually pass input parameters when starting to execute a worker, as shown in the following examples:
```csharp
// Example 1: Get the worker first, then pass the parameter to the execution method
await Manager
    .GetWorker<FooWorker>()
    .RunAsync(new InputType());

// Example 2: Set parameters via SetContext before executing the worker
await Manager
    .SetContext(new InputType())
    .RunWorker<FooWorker>();
```

If you need to convert parameters between the input and output of different workers, you can also pass a conversion delegate to `SetContext`:
```csharp
// The following example converts the Foo1Type in the current context to the Foo2Type required by FooWorker
await Manager
    .SetContext((Foo1Type foo1) => ConvertFoo1ToFoo2(foo1))
    .RunWorker<FooWorker>();
```

#### Chained Calls
In actual business scenarios, you usually need to call multiple workers continuously, using the output of the previous step as the input of the next step. The framework supports concatenating the entire call process into a clear chain.

Example:
```csharp
WorkerResult<Info2> step1Result = await messageWorkerManager
    .GetWorker<Worker1>()
    .RunAsync(new Info1());

WorkerResult<Info3> step2Result = await messageWorkerManager
    .GetWorker<Worker2>()
    .RunAsync(step1Result);

// Assume there is no Worker3 in between, we can convert parameters manually
WorkerResult<Info4> info4Result = step2Result.Convert((Info3 info3) => new Info4());

WorkerResult<Info5> step3Result = await messageWorkerManager
    .GetWorker<Worker4>()
    .RunAsync(info4Result);

// Note: Assume Worker5 returns a failed result
WorkerResult<Info6> step4Result = await messageWorkerManager
    .GetWorker<Worker5>()
    .RunAsync(step3Result);

// Even if Worker5 fails, its result can still be passed to Worker6 as input. This design eliminates a large amount of error judgment branch code, ensuring the entire workflow chain is written serially. When receiving an input with a failure status, `Worker6` will not actually be executed, and the error will be directly propagated further
WorkerResult<Info7> step5Result = await messageWorkerManager
    .GetWorker<Worker6>()
    .RunAsync(step4Result);

// The final result of the chain is consistent with the MessageWorkerStatus
Assert.AreEqual(messageWorkerManager.MessageWorkerStatus.Status, step5Result.ErrorCode);
// MessageWorkerStatus also records the information of the first failed worker
// As shown below, the final result step5Result is output by Worker6, but the recorded failed worker information is still the first failed Worker5
Assert.AreEqual(nameof(Worker5), messageWorkerManager.MessageWorkerStatus.FailWorker?.WorkerName);
```

In the `RunAsync` method, you can either pass an explicit input parameter like `RunAsync(new Info1())`, or directly pass the result of the previous step like `RunAsync(step1Result)`, which makes the code for concatenating multiple workers very natural.

If the input/output types do not match in a certain step, you can perform a conversion first before continuing execution, just like the `step2Result.Convert((Info3 info3) => new Info4())` in the above example.

When a worker fails to execute, subsequent links can still get a `WorkerResult<T>` object. For example, after `Worker5` returns a failure, you can still get `WorkerResult<Info6> step4Result`, but this result already carries a failure status. When it is passed to `Worker6` for execution, `Worker6` will not be actually executed, and the error will be directly propagated further.

Definition of the sample workers in the above example:
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

---

### Exception Interruption and Retry
Each worker can return a `WorkerResult` type value, which tells the framework whether the current worker executed successfully. When execution fails, you can assign an error code for easy debugging and output, and specify whether the framework needs to retry.

There are two ways to interrupt the execution of subsequent workers:
1. Return a `WorkerResult` with a failed status. Once the worker manager is in `IsFail` state, it will block the execution of all workers that are not marked with `CanRunWhenFail = true`. In other words, except for workers that need to run regardless of success or failure status, all other workers will not be executed, including the delegate conversion in `SetContext`.
2. Throw an exception, which will stop all subsequent logic execution according to .NET's exception mechanism.

Both methods are recommended by the framework. For performance-sensitive scenarios, method 1 is preferred. For complex business logic with a large amount of code outside the workflow, method 2 allows for more convenient interruption.

---

### Execute Other Workers Inside a Worker
You can execute other workers inside a worker, which allows you to implement branch logic and nested execution flexibly.

Example:
First define Worker2:
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

Then execute Worker2 inside Worker1:
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

---

### Delegate Worker
For very small and lightweight logic that you want to add to the workflow without defining a separate worker, you can use a delegate worker:
```csharp
var delegateMessageWorker = new DelegateMessageWorker(_ =>
{
    // Your business logic here
});

var result = await messageWorkerManager.RunWorker(delegateMessageWorker);
```

If you don't even want to create a `DelegateMessageWorker` instance, you can directly pass a delegate to the `RunWorker` method of `MessageWorkerManager`:
```csharp
await messageWorkerManager.RunWorker((IWorkerContext context) =>
{
    // Your business logic here
});
```

---

### Built-in Mechanisms
In addition to standardizing the execution method of workers, `MessageWorker` also has built-in mechanisms that you can use directly when writing workers to reduce boilerplate code:
- Customizable worker name: By default, `WorkerName` uses the current type name. If you want a clearer name in logs or debugging, you can override the `WorkerName` property.
- Configurable retry support: `CanRetry` defaults to `true`. If a worker is not suitable for repeated execution after failure, you can set it to `false`. To specify retry permission precisely for a single failure, you can also return the result via `Fail(errorCode, canRetry)` or `FailTask(errorCode, canRetry)`.
- Configurable execution on upstream failure: `CanRunWhenFail` defaults to `false`. If the worker is used for finalization, logging, resource cleanup and other scenarios that need to run regardless of upstream status, you can set it to `true`.
- Multiple output support: You can write additional results to the context via `SetContext` for subsequent workers to read, no need to rely on only a single output value.
- Simplified success return: If execution succeeds and there is no return value, you can directly call `Success()` to return a success result. If there is no real asynchronous logic in the execution method, you can also call `SuccessTask()` directly to get the return value.
- Simplified failure return: If execution fails, you can call `Fail()` or `FailTask()` to quickly return a failure result.
- Generic worker helper methods: For types inheriting from `MessageWorker<TInput, TOutput>`, you can use `Success(TOutput output)` to directly return a success result with output; if you get a failure result from another call, you can convert it to a failure result of the current output type via `WorkerResult<TOutput> Fail(WorkerResult failResult)`.
- Failure result conversion: You can use the `WorkerResult.AsFail<T>` method to convert a failure state to a failure state of another type, while retaining information such as error code and retry permission.
- Dispose registration: You can use the `RegisterOnDispose` series of methods to register logic to be executed when the entire `MessageWorkerManager` is disposed, such as cleaning temporary files, deleting folders, or performing other finalization work.