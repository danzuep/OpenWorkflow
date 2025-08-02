using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpenWorkflow.Core
{
    public static class WorkflowStepBuilder
    {
        /// <summary>
        /// Build steps and wire dependencies by Id.
        /// </summary>
        /// <param name="options">List of step options</param>
        /// <param name="factory">Factory for creating steps</param>
        /// <param name="logger">Logger</param>
        /// <param name="resourceManager">Resource manager</param>
        /// <returns>Root steps (not dependencies of others)</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<IList<IWorkflowStep>> BuildStepsFromOptionsAsync<T>(
            IEnumerable<WorkflowStepBaseOptions> options,
            IWorkflowStepFactory factory,
            ILogger logger,
            IResourceManager resourceManager,
            CancellationToken cancellationToken = default) where T : IWorkflowStep
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            // Flatten all steps including children
            var allOptions = FlattenSteps(options).ToArray();

            // Create all steps first, flat list
            var stepMap = new Dictionary<string, IWorkflowStep>(StringComparer.OrdinalIgnoreCase);
            var dependencyIds = new List<string>();
            foreach (var opt in allOptions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = await factory.CreateAsync<T>(opt, cancellationToken);
                stepMap.Add(step.Id, step);

                foreach (var req in opt.Requirements ?? Enumerable.Empty<RequirementOptions>())
                {
                    if (req.Type != ResourceType.StepId) continue;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (stepMap.TryGetValue(req.Id, out var depStep))
                    {
                        logger.LogInformation("Dependency '{DependencyId}' found for step '{StepId}'", depStep.Id, opt.Id);
                        dependencyIds.Add(depStep.Id);
                    }
                    else
                    {
                        logger.LogWarning("Dependency '{DependencyId}' not found for step '{StepId}'", req.Id, opt.Id);
                    }
                }
            }

            // Return only root steps (those not dependencies of others)
            var dependencyIdSet = dependencyIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var rootSteps = stepMap.Values.Where(s => !dependencyIdSet.Contains(s.Id)).ToList();

            return rootSteps;
        }

        /// <summary>
        /// Helper to flatten tree of WorkflowStepOptions recursively
        /// </summary>
        private static IEnumerable<WorkflowStepBaseOptions> FlattenSteps(IEnumerable<WorkflowStepBaseOptions> steps)
        {
            foreach (var step in steps)
            {
                yield return step;
                if (step.Children != null && step.Children.Any())
                {
                    foreach (var child in FlattenSteps(step.Children))
                    {
                        child.ParentId = step.Id;
                        yield return child;
                    }
                }
            }
        }
    }
}