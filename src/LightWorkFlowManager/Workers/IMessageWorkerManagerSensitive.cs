namespace LightWorkFlowManager.Workers;

/// <summary>
/// 表示对 <see cref="MessageWorkerManager"/> 需要注入
/// </summary>
internal interface IMessageWorkerManagerSensitive
{
    /// <summary>
    /// 在加入到 <see cref="MessageWorkerManager"/> 被调用
    /// </summary>
    /// <param name="manager"></param>
    void SetMessageWorkerManager(MessageWorkerManager manager);
}
