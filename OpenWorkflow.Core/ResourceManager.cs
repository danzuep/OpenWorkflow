using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpenWorkflow.Core
{
    /// <summary>
    /// Manages access to all resources.
    /// </summary>
    public class ResourceManager : IResourceManager, IDisposable
    {
        private readonly Dictionary<ResourceType, int> _availableResources = new();
        private readonly SemaphoreSlim _mutex = new(1, 1);
        private readonly ILogger _logger;

        public ResourceManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddResourcesAsync(IEnumerable<RequirementOptions> requirements, CancellationToken cancellationToken = default)
        {
            await _mutex.WaitAsync(cancellationToken);
            try
            {
                foreach (var requirement in requirements ?? Enumerable.Empty<RequirementOptions>())
                {
                    if (_availableResources.ContainsKey(requirement.Type))
                        _availableResources[requirement.Type] += requirement.Count;
                    else
                        _availableResources.Add(requirement.Type, requirement.Count);

                    _logger.LogInformation("Added {Quantity} units of resource '{ResourceId}'. Total: {Remaining}",
                        requirement.Count, requirement.Type, _availableResources[requirement.Type]);
                }
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> TryReserveResourcesAsync(IEnumerable<RequirementOptions> requirements, CancellationToken cancellationToken)
        {
            await _mutex.WaitAsync(cancellationToken);
            try
            {
                if (requirements == null || !requirements.Any())
                    return true;

                foreach (var req in requirements)
                {
                    if (!_availableResources.TryGetValue(req.Type, out var available) || available < req.Count)
                    {
                        _logger.LogInformation("Not enough of resource '{ResourceId}'. Required: {Required}, Available: {Available}",
                            req.Type, req.Count, available);
                        return false;
                    }
                }

                foreach (var req in requirements)
                {
                    _availableResources[req.Type] -= req.Count;
                    _logger.LogInformation("Reserved {Quantity} units of resource '{ResourceId}'. Remaining: {Remaining}",
                        req.Count, req.Type, _availableResources[req.Type]);
                }

                return true;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task ReleaseResourcesAsync(IEnumerable<RequirementOptions> requirements, CancellationToken cancellationToken = default)
        {
            await _mutex.WaitAsync(cancellationToken);
            try
            {
                if (requirements == null)
                    return;

                foreach (var req in requirements)
                {
                    if (_availableResources.TryGetValue(req.Type, out var current))
                    {
                        _availableResources[req.Type] = current + req.Count;
                    }
                    else
                    {
                        _availableResources[req.Type] = req.Count;
                    }
                    _logger.LogInformation("Released {Quantity} units of resource '{ResourceId}'. Total available: {Total}",
                        req.Count, req.Type, _availableResources[req.Type]);
                }
            }
            finally
            {
                _mutex.Release();
            }
        }

        public void Dispose()
        {
            _mutex.Dispose();
        }
    }
}