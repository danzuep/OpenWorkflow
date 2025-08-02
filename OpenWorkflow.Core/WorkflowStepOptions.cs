using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public class WorkflowStepOptions : WorkflowStepBaseOptions, IOptions<WorkflowStepOptions>
    {
        protected static readonly int _optionsLength = "Options".Length;
        public new string StepType { get; } = nameof(WorkflowStepOptions)[..^_optionsLength];

        /// <summary>
        /// The async action that represents the step work.
        /// Receives CancellationToken and ILogger.
        /// </summary>
        public Func<CancellationToken, ILogger, Task>? ActionToExecute { get; set; }

        public override WorkflowStepOptions Value => this;
    }
}