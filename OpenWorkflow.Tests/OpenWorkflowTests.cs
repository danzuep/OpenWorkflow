using Microsoft.Extensions.Logging;
using OpenWorkflow.Tests;

namespace OpenWorkflow.Core.Tests
{
    public class FlowMasterEngineWithDI_Tests
    {
        [Fact]
        public async Task WorkflowEngine_LoadFromOptions_Run()
        {
            // Simulated appsettings-like options for steps
            List<WorkflowStepTestOptions> workflowOptions = new List<WorkflowStepTestOptions>
            {
                new WorkflowStepTestOptions
                {
                    Id = "Load_WI1",
                    Order = 3,
                    DurationMs = 1000,
                    Requirements = new List<RequirementOptions>
                    {
                        new() { Type = ResourceType.DevicePort, Count = 1 }
                    }
                },
                new WorkflowStepTestOptions
                {
                    Id = "Process_WI1",
                    Order = 2,
                    DurationMs = 1500,
                    FailProbability = 0.05,
                    Requirements = new List<RequirementOptions>
                    {
                        new() { Type = ResourceType.License, Count = 1 },
                        new() { Type = ResourceType.StepId, Id = "Load_WI1" }
                    }
                },
                new WorkflowStepTestOptions
                {
                    Id = "WaitForApproval_WI1",
                    Order = 1,
                    WaitDurationMs = 2000,
                    Requirements = new List<RequirementOptions>
                    {
                        new() { Type = ResourceType.StepId, Id = "Process_WI1" }
                    }
                }
            };

            // Resources available in the system
            var resources = new List<RequirementOptions>
            {
                new() { Type = ResourceType.License, Count = 1 },
                new() { Type = ResourceType.DevicePort, Count = 2 }
            };

            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.SetMinimumLevel(LogLevel.Information).AddDebug().AddConsole());
            var logger = loggerFactory.CreateLogger("FlowMaster");

            var resourceManager = new ResourceManager(logger);
            await resourceManager.AddResourcesAsync(resources, CancellationToken.None);

            var factory = new WorkflowStepFactory(resourceManager, loggerFactory);

            // Build root steps from options (builds full dependency graph)
            var opts = workflowOptions.Select(w => w as WorkflowStepBaseOptions).ToArray();
            var rootSteps = await WorkflowStepBuilder.BuildStepsFromOptionsAsync<WorkflowStep>(opts, factory, logger, resourceManager);

            // Recursively collect all steps into a flat list (for engine)
            var allStepsFlat = new List<IWorkflowStep>();
            void CollectSteps(IWorkflowStep step)
            {
                if (allStepsFlat.Contains(step)) return;
                allStepsFlat.Add(step);
                foreach (var child in step.Children)
                    CollectSteps(child);
            }
            foreach (var root in rootSteps)
                CollectSteps(root);

            // Initialize all steps asynchronously if needed
            foreach (var step in allStepsFlat)
            {
                if (step is IAsyncInitialize asyncInit)
                {
                    await asyncInit.InitializeAsync();
                }
            }

            var executorOptions = new WorkflowStepExecutorOptions
            {
                MaxAttempts = 3,
                RetryDelayMs = 500
            };

            var executor = new RetryWorkflowStepExecutor(executorOptions, logger, resourceManager);
            var engine = new WorkflowEngine(executor, logger, resourceManager);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            logger.LogInformation("Starting FlowMaster workflow execution from options...");

            await engine.RunAsync(allStepsFlat, cts.Token);

            logger.LogInformation("FlowMaster workflow execution completed.");

            var validator = new WorkflowStepValidator();
            foreach (var step in allStepsFlat)
            {
                var validationResult = validator.Validate(step);
                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        logger.LogError("Validation error for step {StepId}, {PropertyName} property error: {ErrorMessage}", step.Id, error.PropertyName, error.ErrorMessage);
                    }
                }
            }

            // Print summary
            foreach (var root in rootSteps)
                PrintStepSummary(root, 0, logger);
        }

        private static void PrintStepSummary(IWorkflowStep step, int indent, ILogger logger)
        {
            logger.LogInformation("{Indent}- Step {StepId}: {Status}",
                new string(' ', indent * 2), step.Id, step.Status);
            if (step.Children == null) return;
            foreach (var child in step.Children)
                PrintStepSummary(child, indent + 1, logger);
        }
    }
}