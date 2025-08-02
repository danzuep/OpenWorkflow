using System;
using System.Collections.Generic;

namespace OpenWorkflow.Core
{
    /// <summary>
    /// WorkItem representing a unit of workflow
    /// </summary>
    public class WorkItem
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public List<WorkflowStepBase> RootSteps { get; } = new();
    }
}