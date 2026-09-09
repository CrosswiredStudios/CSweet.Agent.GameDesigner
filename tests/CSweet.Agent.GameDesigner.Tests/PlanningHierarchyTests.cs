using System.Text.Json;
using CSweet.Agent.SDK;
using CrosswiredStudios.VideoGame.AgentKit;
using CrosswiredStudios.VideoGame.Contracts;

namespace CSweet.Agent.GameDesigner.Tests;

public sealed class PlanningHierarchyTests
{
    [Fact]
    public async Task PlanningProposalHasCompleteMilestoneFeatureLeafHierarchy()
    {
        var self = new AgentCoordinationParticipant(Guid.NewGuid(), Guid.NewGuid(), "Designer", "Designer");
        var producer = new AgentCoordinationParticipant(Guid.NewGuid(), Guid.NewGuid(), "Producer", "Producer");
        var cycle = new GameProductionPlanningCycleV1(Guid.NewGuid(), Guid.NewGuid(), 12, Guid.NewGuid(),
            "profile", Guid.NewGuid(), 1, "digest", "concept", "vision-approved", "cycle");
        var request = new AgentCoordinationTurnRequest(Guid.NewGuid(), 1, 2, "Plan", "Accepted loop", [], self, producer, false,
            [new(Guid.NewGuid(), 0, producer.OrganizationUserId, "Continue", "Plan", DateTimeOffset.UtcNow,
                new("video-game.production.planning-cycle.v1", "1.0", "cycle", 1, true,
                    JsonSerializer.SerializeToElement(cycle), "digest"))]) {
            SourceKind = "Board",
            WorkContext = new(Guid.NewGuid(), cycle.WorkstreamId, Guid.NewGuid(), Guid.NewGuid(), null, null, null, Guid.NewGuid(), null, null)
        };
        var result = await new VideoGameDesignerAgent().HandleCoordinationTurnAsync(request, new AgentTestRuntime().CreateContext(), default);
        Assert.Equal("Completed", result.Disposition);
        var proposal = result.Artifact!.Payload.Deserialize<GameDesignerBacklogProposalV1>()!;
        var milestone = Assert.Single(proposal.PlayerOutcomes, x => x.WorkItemTypeKey == VideoGameWorkItemTypeKeys.Milestone);
        var feature = Assert.Single(proposal.PlayerOutcomes, x => x.WorkItemTypeKey == VideoGameWorkItemTypeKeys.Feature);
        Assert.Null(milestone.ParentProposalKey);
        Assert.Equal(milestone.ProposalKey, feature.ParentProposalKey);
        Assert.All(proposal.PlayerOutcomes.Where(x => x != milestone && x != feature),
            x => Assert.Equal(feature.ProposalKey, x.ParentProposalKey));
        Assert.All(proposal.PlayerOutcomes.SelectMany(x => x.DependencyProposalKeys),
            key => Assert.Contains(proposal.PlayerOutcomes, x => x.ProposalKey == key));
        Assert.Equal(cycle, proposal.Cycle);
    }
}
