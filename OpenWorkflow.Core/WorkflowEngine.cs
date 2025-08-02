using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly Dictionary<string, IWorkflowStep> _steps = new();
        private readonly IWorkflowStepExecutor _workflowStepExecutor;
        private readonly ILogger _logger;
        private readonly IResourceManager _resourceManager;

        public WorkflowEngine(IWorkflowStepExecutor workflowStepExecutor, ILogger logger, IResourceManager resourceManager)
        {
            _workflowStepExecutor = workflowStepExecutor ?? throw new ArgumentNullException(nameof(workflowStepExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        }

        public async Task RunAsync(IEnumerable<IWorkflowStep> steps, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(steps);
            var stepsToRetry = new List<IWorkflowStep>();
            foreach (var step in steps)
            {
                _steps.Add(step.Id, step);
            }
            await InternalRunAsync(steps, cancellationToken).ConfigureAwait(false);
        }

        private readonly static StepStatus[] _validStatuses =
        {
            StepStatus.Passed,
            StepStatus.Skipped
        };

        private bool PrerequisitesPassed(IWorkflowStep step)
        {
            if (step.Status != StepStatus.Pending)
            {
                return false;
            }
            if (step.Prerequisites == null || !(step.Prerequisites.Count > 0))
            {
                return true;
            }
            foreach (var prerequisite in step.Prerequisites)
            {
                if (!_steps.TryGetValue(prerequisite.Id, out var dependency) ||
                    dependency.Status != StepStatus.Passed)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task InternalRunAsync(IEnumerable<IWorkflowStep> steps, CancellationToken cancellationToken = default)
        {
            (var stepsToRun, var stepsToRecheck) = FilterSteps(steps, cancellationToken);
            var tasks = stepsToRun.ConvertAll(s => _workflowStepExecutor.ExecuteAsync(s, cancellationToken));
            while (tasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);

                if (completedTask.Status == TaskStatus.RanToCompletion)
                {
                    _logger.LogInformation("Completed: {TaskId}", completedTask.Id);
                }
                else if (completedTask.Status == TaskStatus.Canceled)
                {
                    _logger.LogInformation("Canceled: {TaskId}", completedTask.Id);
                }
                else if (completedTask.Status == TaskStatus.Faulted)
                {
                    _logger.LogInformation("Faulted: {TaskId} with exception: {ExceptionMessage}", completedTask.Id, completedTask.Exception?.InnerException?.Message);
                }

                (stepsToRun, stepsToRecheck) = FilterSteps(stepsToRecheck, cancellationToken);
                tasks.AddRange(stepsToRun.ConvertAll(s => _workflowStepExecutor.ExecuteAsync(s, cancellationToken)));
            }

            if (stepsToRecheck.Count > 0)
            {
                _logger.LogWarning("Some steps failed prerequisites, retrying: {Steps}", string.Join(", ", stepsToRecheck.Select(s => s.Id)));
            }
        }

        private (List<IWorkflowStep>,List<IWorkflowStep>) FilterSteps(IEnumerable<IWorkflowStep> steps, CancellationToken cancellationToken = default)
        {
            var stepsToRun = new List<IWorkflowStep>();
            var stepsToRecheck = new List<IWorkflowStep>();
            foreach (var step in steps)
            {
                if (PrerequisitesPassed(step))
                {
                    stepsToRun.Add(step);
                }
                else
                {
                    stepsToRecheck.Add(step);
                }
            }
            return (stepsToRun, stepsToRecheck);
        }
    }
}