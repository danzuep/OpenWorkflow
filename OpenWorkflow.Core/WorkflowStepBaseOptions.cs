using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    /// <summary>
    /// Options class for workflow step configuration
    /// </summary>
    public class WorkflowStepBaseOptions : IOptions<WorkflowStepBaseOptions>
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? ParentId { get; set; }
        public int Order { get; set; }
        public string? StepType { get; set; }
        public List<WorkflowStepBaseOptions> Children { get; set; } = new();
        public List<RequirementOptions> Requirements { get; set; } = new();

        public virtual WorkflowStepBaseOptions Value => this;

        public WorkflowStepBaseOptions Copy()
        {
            var copy = (WorkflowStepBaseOptions)this.MemberwiseClone();
            copy.Children = Children.ConvertAll(child => child.Copy());
            copy.Requirements = Requirements.ConvertAll(req => req.Copy());
            return copy;
        }

        public override string ToString() =>
            $"Id={Id}, StepType={StepType}, ChildrenCount={Children.Count}, RequirementsCount={Requirements.Count}";
    }
}