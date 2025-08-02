using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public class ResourceRequirement : IPrerequisite
    {
        private readonly IResourceManager _resourceManager;
        private readonly RequirementOptions _options;

        public ResourceRequirement(IOptions<RequirementOptions> options, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            return await _resourceManager.TryReserveResourcesAsync([_options], cancellationToken);
        }
    }
}