using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWorkflow.Core
{
    public enum ResourceType
    {
        Unknown,
        StepId,
        DevicePort,
        NetworkPort,
        License,
        // Add more as needed
    }

    public enum StepStatus
    {
        Pending,
        Running,
        Passed,
        Failed,
        Skipped,
        Cancelled
    }

    public interface IAsyncInitialize
    {
        /// <summary>
        /// Asynchronously initializes the object, potentially performing setup tasks.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the initialization to complete.</param>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        ValueTask InitializeAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Composite pattern for workflow steps.
    /// </summary>
    public interface IWorkflowStep
    {
        string Id { get; }
        StepStatus Status { get; set; }
        IReadOnlyList<IWorkflowStep>? Children { get; }
        IReadOnlyList<RequirementOptions>? Prerequisites { get; }
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Strategy pattern for encapsulating retry logic or policies.
    /// Command pattern for encapsulating workflow step execution.
    /// </summary>
    public interface IWorkflowStepExecutor
    {
        Task ExecuteAsync(IWorkflowStep step, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Orchestration pattern for managing the execution of workflow steps.
    /// Defines the contract for a workflow engine capable of running workflows asynchronously.
    /// </summary>
    public interface IWorkflowEngine
    {
        /// <summary>
        /// Executes the workflow asynchronously until completion or cancellation.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the workflow to complete.</param>
        /// <returns>A task representing the asynchronous run operation.</returns>
        Task RunAsync(IEnumerable<IWorkflowStep> steps, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Defines a factory interface for creating workflow step instances.
    /// Responsible for encapsulating the creation logic of <see cref="IWorkflowStep"/> objects,
    /// potentially configuring them based on provided options and logging requirements.
    /// </summary>
    public interface IWorkflowStepFactory
    {
        /// <summary>
        /// Creates a new <see cref="IWorkflowStep"/> instance configured with the specified options and logger.
        /// </summary>
        /// <param name="options">The options used to configure the workflow step.</param>
        /// <returns>A configured <see cref="IWorkflowStep"/> instance.</returns>
        Task<IWorkflowStep> CreateAsync<T>(WorkflowStepBaseOptions options, CancellationToken cancellationToken = default) where T : IWorkflowStep;
    }

    /// <summary>
    /// Strategy pattern for encapsulating prerequisites requirements and dependencies.
    /// </summary>
    public interface IPrerequisite
    {
        /// <summary>
        /// Checks asynchronously if the requirement is currently satisfied.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if requirement is satisfied; otherwise false.</returns>
        Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Adaptor pattern for managing resource lifecycle.
    /// Defines methods for managing the requirements of resources required by workflow tasks.
    /// Responsible for acquiring/locking and releasing resources in an asynchronous manner.
    /// </summary>
    public interface IResourceManager
    {
        Task AddResourcesAsync(IEnumerable<RequirementOptions> requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to acquire the specified resources asynchronously.
        /// This method should return true if all requested resources are successfully acquired;
        /// otherwise, false.
        /// </summary>
        /// <param name="requirements">The collection of resource requirements to acquire.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the operation to complete.</param>
        /// <returns>A task that represents the asynchronous operation, with a boolean result indicating success.</returns>
        Task<bool> TryReserveResourcesAsync(IEnumerable<RequirementOptions> requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases the specified resources asynchronously, making them available for other operations.
        /// </summary>
        /// <param name="requirements">The collection of resource requirements to release.</param>
        /// <returns>A task that represents the asynchronous release operation.</returns>
        Task ReleaseResourcesAsync(IEnumerable<RequirementOptions> requirements, CancellationToken cancellationToken = default);
    }
}