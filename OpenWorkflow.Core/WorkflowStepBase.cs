using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public abstract class WorkflowStepBase : WorkflowStepExecutorOptions, IWorkflowStep, IAsyncDisposable
    {
        private StepStatus _status = StepStatus.Pending;
        public StepStatus Status
        {
            get => _status;
            set => SetStatus(value);
        }
        public string Id => _options.Id;
        protected readonly WorkflowStepBaseOptions _options;
        public IReadOnlyList<IWorkflowStep> Children => _children.AsReadOnly();
        public IReadOnlyList<RequirementOptions>? Prerequisites => _requirements.AsReadOnly();

        private bool _disposed;
        private readonly List<IWorkflowStep> _children = new();
        private readonly List<RequirementOptions> _requirements;
        protected readonly ILogger<WorkflowStepBase> _logger;

        protected WorkflowStepBase(IOptions<WorkflowStepBaseOptions> options, ILogger<WorkflowStepBase> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requirements = [.. _options.Requirements];
        }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);

        protected static readonly Dictionary<StepStatus, StepStatus[]> _validTransitions = new()
        {
            { StepStatus.Pending, new[] { StepStatus.Running, StepStatus.Failed, StepStatus.Skipped } },
            { StepStatus.Running, new[] { StepStatus.Pending, StepStatus.Failed, StepStatus.Passed } }
        };

        protected void SetStatus(StepStatus requestedStatus)
        {
            if (!_validTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(requestedStatus))
            {
                _logger.LogWarning("Workflow step cannot transition to {RequestedStatus} from {CurrentStatus}", requestedStatus, Status);
                return;
            }

            _status = requestedStatus;
            _logger.LogDebug("Workflow step status set to {Status}", _status);

            // Propagate to children if appropriate
            foreach (var child in _children)
            {
                try
                {
                    child.Status = requestedStatus;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to set status on child step {ChildId} of step {StepId}", child.Id, Id);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            foreach (var child in _children)
            {
                try
                {
                    if (child is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    else if (child is IDisposable disposable)
                        disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing child step {ChildId} of step {StepId}", child.Id, Id);
                }
            }
            _children.Clear();

            await DisposeAsyncInternal().ConfigureAwait(false);

            _disposed = true;
        }

        protected virtual ValueTask DisposeAsyncInternal() => ValueTask.CompletedTask;
    }
}