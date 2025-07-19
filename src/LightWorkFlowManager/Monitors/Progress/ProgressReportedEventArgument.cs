namespace DC.LightWorkFlowManager.Monitors;

public readonly record struct ProgressReportedEventArgument<T>(ProgressPercentage ProgressPercentage, T? Value);