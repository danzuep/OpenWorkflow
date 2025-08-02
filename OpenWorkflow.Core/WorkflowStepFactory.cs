using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public class WorkflowStepFactory : IWorkflowStepFactory
    {
        protected readonly ILogger<WorkflowStepBase> _logger;
        private readonly IResourceManager _resourceManager;
        private bool _initialized;

        public WorkflowStepFactory(IResourceManager resourceManager, ILoggerFactory? loggerFactory = null)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _logger = loggerFactory?.CreateLogger<WorkflowStepBase>() ?? NullLogger<WorkflowStepBase>.Instance;
        }

        public async Task<IWorkflowStep> CreateAsync<T>(WorkflowStepBaseOptions options, CancellationToken cancellationToken = default) where T : IWorkflowStep
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var typeName = typeof(T).Name;
            IWorkflowStep step = typeName switch
            {
                nameof(WorkflowStep) => new WorkflowStep(options, _logger),
                _ => throw new ArgumentException($"Unknown step type '{options.StepType}'")
            };

            //await InitializeAsync(step, cancellationToken);

            return step;
        }
    }
}