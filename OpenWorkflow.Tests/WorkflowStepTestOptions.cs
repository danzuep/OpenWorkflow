using Microsoft.Extensions.Options;
using OpenWorkflow.Core;

namespace OpenWorkflow.Tests
{
    public class WorkflowStepTestOptions : WorkflowStepOptions, IOptions<WorkflowStepTestOptions>
    {
        public new string StepType { get; } = nameof(WorkflowStepTestOptions)[..^_optionsLength];
        public int DurationMs { get; set; } = 800; // For ProcessingStep
        public double FailProbability { get; set; } = 0.1; // For ProcessingStep
        public int WaitDurationMs { get; set; } = 2000; // For WaitForEventStep

        public override WorkflowStepTestOptions Value => this;
    }
}