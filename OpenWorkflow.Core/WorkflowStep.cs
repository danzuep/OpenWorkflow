using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public sealed class WorkflowStep : WorkflowStepBase
    {
        public Func<CancellationToken, ILogger, Task>? ActionToExecute { get; set; }

        public WorkflowStep(
            IOptions<WorkflowStepBaseOptions> options,
            ILogger<WorkflowStepBase> logger)
            : base(options, logger)
        {
            var stepOptions = options?.Value as WorkflowStepOptions;
            ActionToExecute = stepOptions?.ActionToExecute;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (ActionToExecute == null)
            {
                _logger.LogWarning("No ActionToExecute configured for ProcessingStep {StepId}. Nothing to run.", _options.Id);
                return;
            }

            _logger.LogInformation("ProcessingStep {StepId} starting action.", _options.Id);
            await ActionToExecute.Invoke(cancellationToken, _logger);
            _logger.LogInformation("ProcessingStep {StepId} finished action.", _options.Id);
        }
    }
}