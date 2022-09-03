﻿namespace Zametek.Common.ProjectPlan
{
    [Serializable]
    public record ResourceSettingsModel
    {
        public List<ResourceModel> Resources { get; init; } = new List<ResourceModel>();

        public double DefaultUnitCost { get; init; }

        public bool AreDisabled { get; init; }
    }
}
