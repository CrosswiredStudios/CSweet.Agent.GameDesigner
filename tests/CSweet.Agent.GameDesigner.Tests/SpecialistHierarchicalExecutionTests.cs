using System.Reflection;
using System.Text.Json;
using CSweet.WorkManagement.Contracts;
using CrosswiredStudios.VideoGame.AgentKit;

namespace HierarchicalDeliveryConformance;

public sealed class SpecialistHierarchicalExecutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanonicalWireInputAcceptsInitialTraversalAndRoutesExactArtifactToQa(bool hierarchical)
    {
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var packageId = Guid.NewGuid();
        var member = new ArtifactPackageMemberDigest(Guid.NewGuid(), Guid.NewGuid(), "approved-input", new string('a', 64));
        var planning = new WorkItemPlanningSpecification(["Deliver exact artifact"], ["Meets criterion"])
        {
            DelegationRecommendations = [new("specialist-execution", "specialist", ["work.execution.run.v2"], null, true, "Assigned discipline")],
            ArtifactPackageDigest = new(packageId, 1, ArtifactPackageDigestCalculator.Calculate(packageId, 1, [member]), DateTimeOffset.UtcNow, [member])
        };
        var input = new WorkExecutionInputV1(Guid.NewGuid(), Guid.NewGuid(), 1, planning)
        {
            AssignmentRequirements = new("specialist", [], [], ["work.execution.run.v2"]),
            AssignmentSelection = new(Guid.NewGuid(), 1, new string('b', 64), [], new string('c', 64), DateTimeOffset.UtcNow)
        };
        var delivery = new WorkItemDeliverySpecification(Guid.Empty, planning.Requirements, planning.AcceptanceCriteria)
            { DeliveryKind = "Artifact", DeliveryPlanId = hierarchical ? Guid.NewGuid() : null };
        var task = new WorkExecutionAssignmentV1(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "BOARD", "BOARD-1", Guid.NewGuid(), "specialist-execution", 0, 1,
            DateTimeOffset.UtcNow.AddHours(1), "Deliver exact revision", JsonSerializer.SerializeToElement(new { delivery }, json),
            JsonSerializer.SerializeToElement(input, json), [], []);
        var v2 = WorkExecutionAssignmentV2.FromTask(task, input.WorkstreamId!.Value, delivery.DeliveryPlanId, 1);
        var canonical = v2.ToTaskAssignment();
        Assert.Equal(input.WorkstreamId, SpecialistAssignmentValidator.Validate(canonical, "specialist").WorkstreamId);
        var artifact = new SpecialistDelivery("Delivered exact revision", Guid.NewGuid(), Guid.NewGuid(), new string('d', 64), [], []);
        var outcome = (WorkExecutionOutcomeV1)typeof(VideoGameSpecialistAgentBase).GetMethod("CompletedOutcome", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [canonical, artifact])!;
        Assert.Equal(hierarchical ? "artifact-delivered" : "completed", outcome.OutcomeCode);
        Assert.Equal(artifact.RevisionId, outcome.Output.GetProperty("RevisionId").GetGuid());
        Assert.Equal(artifact.Sha256, outcome.Output.GetProperty("Sha256").GetString());
        Assert.Equal(hierarchical ? "artifact-revision" : "ArtifactRevision", Assert.Single(outcome.Evidence).Kind);
    }
}
