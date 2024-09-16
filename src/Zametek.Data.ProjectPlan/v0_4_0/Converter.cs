﻿using AutoMapper;

namespace Zametek.Data.ProjectPlan.v0_4_0
{
    public static class Converter
    {
        public static ProjectPlanModel Upgrade(
            IMapper mapper,
            v0_3_2.ProjectPlanModel projectPlan)
        {
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(projectPlan);

            return new ProjectPlanModel
            {
                ProjectStart = projectPlan.ProjectStart,
                DependentActivities = mapper.Map<List<v0_3_2.DependentActivityModel>, List<DependentActivityModel>>(projectPlan.DependentActivities),
                ArrowGraphSettings = projectPlan.ArrowGraphSettings ?? new v0_1_0.ArrowGraphSettingsModel(),
                ResourceSettings = mapper.Map<v0_3_2.ResourceSettingsModel, ResourceSettingsModel>(projectPlan.ResourceSettings ?? new v0_3_2.ResourceSettingsModel()),
                WorkStreamSettings = new v0_3_2.WorkStreamSettingsModel(),
                GraphCompilation = mapper.Map<v0_3_2.GraphCompilationModel, GraphCompilationModel>(projectPlan.GraphCompilation ?? new v0_3_2.GraphCompilationModel()),
                ArrowGraph = mapper.Map<v0_3_2.ArrowGraphModel, ArrowGraphModel>(projectPlan.ArrowGraph ?? new v0_3_2.ArrowGraphModel()),
                HasStaleOutputs = projectPlan.HasStaleOutputs,
            };
        }
    }
}
