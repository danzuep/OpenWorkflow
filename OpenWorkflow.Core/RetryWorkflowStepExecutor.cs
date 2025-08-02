using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public class RetryWorkflowStepExecutor : IWorkflowStepExecutor
    {
        public int AttemptCount { get; private set; } = 1;

        private readonly WorkflowStepExecutorOptions _options;
        private readonly ILogger _logger;
        private readonly IResourceManager _resourceManager;

        public RetryWorkflowStepExecutor(IOptions<WorkflowStepExecutorOptions> options, ILogger logger, IResourceManager resourceManager)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        }

        public async Task ExecuteAsync(IWorkflowStep step, CancellationToken cancellationToken = default)
        {
            if (step == null || step.Status >= StepStatus.Running)
                return;

            while (AttemptCount <= _options.MaxAttempts)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    SetWorkflowStepStatus(step, StepStatus.Cancelled);
                    _logger.LogInformation("Step {StepId} cancelled before attempt {AttemptCount}", step.Id, AttemptCount);
                    return;
                }

                SetWorkflowStepStatus(step, StepStatus.Running);
                _logger.LogInformation("Step {StepId} started. Attempt {AttemptCount}/{MaxAttempts}", step.Id, AttemptCount, _options.MaxAttempts);

                try
                {
                    if (step.Prerequisites != null && step.Prerequisites.Count > 0)
                    {
                        if (!await _resourceManager.TryReserveResourcesAsync(step.Prerequisites, cancellationToken).ConfigureAwait(false))
                        {
                            SetWorkflowStepStatus(step, StepStatus.Failed);
                            _logger.LogInformation("Step {StepId} failed due to unmet prerequisites.", step.Id);
                            return;
                        }
                    }

                    await step.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                    if (step.Children != null && step.Children.Any())
                    {
                        var executor = new RetryWorkflowStepExecutor(_options, _logger, _resourceManager);
                        var engine = new WorkflowEngine(executor, _logger, _resourceManager);
                        await engine.RunAsync(step.Children, cancellationToken);
                    }

                    SetWorkflowStepStatus(step, StepStatus.Passed);
                    _logger.LogInformation("Step {StepId} passed.", step.Id);
                    return;
                }
                catch (OperationCanceledException)
                {
                    SetWorkflowStepStatus(step, StepStatus.Cancelled);
                    _logger.LogInformation("Step {StepId} cancelled during execution.", step.Id);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Step {StepId} failed on attempt {AttemptCount}.", step.Id, AttemptCount);

                    if (AttemptCount >= _options.MaxAttempts)
                    {
                        SetWorkflowStepStatus(step, StepStatus.Failed);
                        _logger.LogError("Step {StepId} failed after {AttemptCount} attempts.", step.Id, AttemptCount);
                        return;
                    }
                    AttemptCount++;
                    try
                    {
                        await Task.Delay(_options.RetryDelayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        SetWorkflowStepStatus(step, StepStatus.Cancelled);
                        _logger.LogInformation("Step {StepId} cancelled during retry wait.", step.Id);
                        return;
                    }
                }
            }
        }

        private void SetWorkflowStepStatus(IWorkflowStep step, StepStatus requestedStatus)
        {
            _logger.LogDebug("Workflow executor requested to set status to {Status}", requestedStatus);
            step.Status = requestedStatus;
        }
    }
}