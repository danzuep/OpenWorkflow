using FluentValidation;
using OpenWorkflow.Core;

namespace OpenWorkflow.Tests
{
    public class WorkflowStepValidator : AbstractValidator<IWorkflowStep>
    {
        internal readonly static StepStatus[] _validStatuses =
        {
            StepStatus.Passed,
            StepStatus.Skipped
        };

        public WorkflowStepValidator()
        {
            RuleFor(customer => customer.Status).Must(s => _validStatuses.Contains(s));
        }
    }
}