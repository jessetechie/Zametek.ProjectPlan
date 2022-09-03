﻿namespace Zametek.Data.ProjectPlan.v0_3_0
{
    [Serializable]
    public record ResourceScheduleModel
    {
        public ResourceModel Resource { get; init; } = new ResourceModel();

        public List<v0_2_1.ScheduledActivityModel> ScheduledActivities { get; init; } = new List<v0_2_1.ScheduledActivityModel>();

        public int FinishTime { get; init; }
    }
}
