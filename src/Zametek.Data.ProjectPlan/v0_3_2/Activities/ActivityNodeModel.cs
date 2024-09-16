﻿using Zametek.Maths.Graphs;

namespace Zametek.Data.ProjectPlan.v0_3_2
{
    [Serializable]
    public record ActivityNodeModel
    {
        public NodeType NodeType { get; init; }

        public ActivityModel Content { get; init; } = new ActivityModel();

        public List<int> IncomingEdges { get; init; } = [];

        public List<int> OutgoingEdges { get; init; } = [];
    }
}
