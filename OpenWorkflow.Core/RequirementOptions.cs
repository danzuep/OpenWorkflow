using System;
using Microsoft.Extensions.Options;

namespace OpenWorkflow.Core
{
    public class RequirementOptions : IOptions<RequirementOptions>
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ResourceType Type { get; set; }
        public int Count { get; set; } = 1;

        public RequirementOptions Value => this;

        public RequirementOptions Copy() =>
            (RequirementOptions)MemberwiseClone();

        public override string ToString() =>
            $"Id={Id}, Type={Type}, Count={Count}";
    }
}