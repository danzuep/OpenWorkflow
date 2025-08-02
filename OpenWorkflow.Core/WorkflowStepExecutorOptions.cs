using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public class WorkflowStepExecutorOptions : IOptions<WorkflowStepExecutorOptions>
    {
        public int MaxAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 500;

        public virtual WorkflowStepExecutorOptions Value => this;

        public WorkflowStepExecutorOptions Copy() =>
            (WorkflowStepExecutorOptions)this.MemberwiseClone();

        public override string ToString() =>
            $"MaxAttempts={MaxAttempts}, RetryDelayMs={RetryDelayMs}";
    }
}