using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWorkflow.Core
{
    public class StepCompletedRequirement : IPrerequisite
    {
        public int RetryDelayMs { get; set; } = 500;

        private readonly IWorkflowStep _dependencyStep;

        public StepCompletedRequirement(IWorkflowStep dependencyStep)
        {
            _dependencyStep = dependencyStep ?? throw new ArgumentNullException(nameof(dependencyStep));
        }

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            // Wait until the dependency step is not Pending or Running
            while (_dependencyStep.Status == StepStatus.Pending || _dependencyStep.Status == StepStatus.Running)
            {
                await Task.Delay(RetryDelayMs, cancellationToken);
            }

            // Requirement satisfied if dependency passed
            return _dependencyStep.Status == StepStatus.Passed;
        }
    }
}