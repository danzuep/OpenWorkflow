# Prompt for Large Language Model: Generic C# Workflow Execution Framework for Task Orchestration

**Context:**

I want to develop a flexible and extensible C# workflow execution framework designed to manage and orchestrate complex asynchronous task sequences. The framework should support:

- **Parallel execution** of tasks where dependencies allow.
- **Task dependencies**, ensuring tasks only start after their prerequisites complete.
- **Hierarchical tasks**, where a task can have child tasks that run sequentially or in parallel.
- Management of **multiple independent workflows** simultaneously.
- Comprehensive **logging, error handling, and status reporting** for each task and workflow.
- Extensibility to add custom task types (e.g., hardware operations, communication, loading processes, measurements).

---

**Requirements:**

1. **Task Model:**
   - Each task has:
     - A unique ID or name.
     - Optional parent task (for hierarchy).
     - Zero or more child tasks.
     - A list of dependencies (tasks that must complete before it can run).
     - Status: Pending, Running, Passed, Failed, Skipped, Cancelled.
     - An asynchronous execution method.
   - Tasks can run in parallel if no dependencies block them.

2. **Workflow Model:**
   - Represents a single workflow instance.
   - Contains root tasks representing the workflow’s entry points.
   - Can run independently from other workflows.

3. **Execution Engine:**
   - Manages scheduling and execution of tasks across workflows.
   - Respects dependencies and parent-child relationships.
   - Supports parallel execution both across workflows and within a workflow.
   - Provides real-time status updates and progress reporting.

4. **Resource Management:**
   - Supports resource acquisition and release for tasks that require exclusive or shared resources.
   - Ensures tasks only execute when required resources are available.

5. **Logging & Reporting:**
   - Logs task start, completion, and errors.
   - Aggregates results per workflow.
   - Provides summary reports.

---

**Deliverable:**

Please design and provide:

- C# interfaces and classes for tasks, workflows, resource management, and the execution engine, guided by the following concepts:
  - `IWorkflowStep` for task definition,
  - `IWorkflowEngine` for orchestration,
  - `IResourceManager` for resource lifecycle,
  - and related supporting interfaces.
- An example implementation of task scheduling and execution that respects dependencies, supports parallelism, and manages resources.
- Sample code demonstrating:
  - Creating multiple workflows,
  - Defining tasks with dependencies and child tasks,
  - Running workflows concurrently,
  - Logging and reporting results.

---

**Additional Notes:**

- Use `async`/`await` for asynchronous task execution.
- Employ concurrency primitives like `Task`, `Task.WhenAll`, or other suitable patterns.
- Ensure modularity and maintainability.
- Include comments explaining key architectural decisions.
- Demonstrate error handling and cancellation support.
- No UI is required — console output is sufficient.

---

**Example Usage Scenario:**

- Workflow 1:
  - Task A (e.g., resource acquisition)
  - Task B depends on A
  - Task C depends on B
- Workflow 2:
  - Task D (e.g., initialization)
  - Task E depends on D
  - Task F depends on E
- Tasks A and D run in parallel.
- Tasks B and E run in parallel after their respective prerequisites.
- Tasks C and F run last.

---

**Please provide the C# code for this generic workflow execution framework now.**