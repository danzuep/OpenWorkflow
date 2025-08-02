using System.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorkflow.Tests;

namespace OpenWorkflow.Core.Tests
{
    public class WorkflowStepTests
    {
        [Fact]
        public async Task WorkflowEngine_WithPrerequisite_AwaitsPrerequisite()
        {
            List<WorkflowStepTestOptions> workflowOptions = new()
            {
                new WorkflowStepTestOptions
                {
                    Id = "A"
                },
                new WorkflowStepTestOptions
                {
                    Id = "B",
                    Requirements = new List<RequirementOptions>
                    {
                        new() { Type = ResourceType.StepId, Id = "A" }
                    }
                },
            };

            var executorOptions = new WorkflowStepExecutorOptions
            {
                MaxAttempts = 3,
                RetryDelayMs = 500
            };

            var resources = new List<RequirementOptions>
            {
                new() { Type = ResourceType.DevicePort, Count = 1 }
            };

            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.SetMinimumLevel(LogLevel.Information).AddDebug().AddConsole());
            var logger = loggerFactory.CreateLogger<WorkflowStepBase>();

            var resourceManager = new ResourceManager(logger);
            await resourceManager.AddResourcesAsync(resources, CancellationToken.None);

            var executor = new RetryWorkflowStepExecutor(executorOptions, logger, resourceManager);
            var engine = new WorkflowEngine(executor, logger, resourceManager);

            var steps = workflowOptions.ConvertAll(o => new WorkflowStep(o, logger));

            await engine.RunAsync(steps, CancellationToken.None);
        }

        [Fact]
        public async Task WorkflowEngine_WithResource_AwaitsResource()
        {
            List<WorkflowStepTestOptions> workflowOptions = new()
            {
                new WorkflowStepTestOptions
                {
                    Id = "A",
                    Requirements = new List<RequirementOptions>
                    {
                        new() { Type = ResourceType.DevicePort }
                    }
                },
                new WorkflowStepTestOptions
                {
                    Id = "B",
                    Requirements = new List<RequirementOptions>
                    {
                        new() { Type = ResourceType.DevicePort }
                    }
                },
            };

            var executorOptions = new WorkflowStepExecutorOptions();

            var resources = new List<RequirementOptions>
            {
                new() { Type = ResourceType.DevicePort, Count = 1 }
            };

            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.SetMinimumLevel(LogLevel.Information).AddDebug().AddConsole());
            var logger = loggerFactory.CreateLogger<WorkflowStepBase>();

            var resourceManager = new ResourceManager(logger);
            await resourceManager.AddResourcesAsync(resources, CancellationToken.None);

            var executor = new RetryWorkflowStepExecutor(executorOptions, logger, resourceManager);
            var engine = new WorkflowEngine(executor, logger, resourceManager);

            var steps = workflowOptions.ConvertAll(o => new WorkflowStep(o, logger));

            await engine.RunAsync(steps, CancellationToken.None);
        }
    }
}