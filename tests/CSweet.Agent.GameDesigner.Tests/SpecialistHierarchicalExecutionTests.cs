using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CSweet.Agent.SDK;
using System.Text.Json;
using CSweet.WorkManagement.Contracts;
using CrosswiredStudios.VideoGame.AgentKit;

namespace HierarchicalDeliveryConformance;

public sealed class SpecialistHierarchicalExecutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanonicalWireInputAcceptsInitialTraversalAndRoutesExactArtifactToQa(bool hierarchical)
    {
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var packageId = Guid.NewGuid();
        var member = new ArtifactPackageMemberDigest(Guid.NewGuid(), Guid.NewGuid(), "approved-input", new string('a', 64));
        var planning = new WorkItemPlanningSpecification(["Deliver exact artifact"], ["Meets criterion"])
        {
            DelegationRecommendations = [new("specialist-execution", "specialist", ["work.execution.run"], null, true, "Assigned discipline")],
            ArtifactPackageDigest = new(packageId, 1, ArtifactPackageDigestCalculator.Calculate(packageId, 1, [member]), DateTimeOffset.UtcNow, [member])
        };
        var input = new WorkExecutionInputV1(Guid.NewGuid(), Guid.NewGuid(), 1, planning)
        {
            AssignmentRequirements = new("specialist", [], [], ["work.execution.run"]),
            AssignmentSelection = new(Guid.NewGuid(), 1, new string('b', 64), [], new string('c', 64), DateTimeOffset.UtcNow)
        };
        var delivery = new WorkItemDeliverySpecification(Guid.Empty, planning.Requirements, planning.AcceptanceCriteria)
            { DeliveryKind = "Artifact", DeliveryPlanId = hierarchical ? Guid.NewGuid() : null };
        var task = new WorkExecutionAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Task", null, 0, Guid.NewGuid(), Guid.NewGuid(), 1, "BOARD-1", Guid.NewGuid(), "specialist-execution", 0, 1, DateTimeOffset.UtcNow.AddHours(1), "Deliver exact revision", JsonSerializer.SerializeToElement(new { delivery }, json), JsonSerializer.SerializeToElement(input, json), [], []);
        var v2 = (task with { WorkstreamId = input.WorkstreamId!.Value, DeliveryPlanId = delivery.DeliveryPlanId, ScopeRevision = 1 });
        var canonical = v2.RequireTaskScope();
        Assert.Equal(input.WorkstreamId, SpecialistAssignmentValidator.Validate(canonical, "specialist").WorkstreamId);
        var content = "Concrete discipline-owned document evidence.";
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var artifact = new SpecialistDelivery("Delivered exact revision", Guid.NewGuid(), Guid.NewGuid(), digest, [], []);
        var outcome = (WorkExecutionOutcomeV1)typeof(VideoGameSpecialistAgentBase).GetMethod("CompletedOutcome", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [canonical, artifact])!;
        Assert.Equal(hierarchical ? "artifact-delivered" : "completed", outcome.OutcomeCode);
        Assert.Equal(artifact.RevisionId, outcome.Output.GetProperty("revisionId").GetGuid());
        Assert.Equal(artifact.Sha256, outcome.Output.GetProperty("sha256").GetString());
        Assert.Equal(hierarchical ? "artifact-revision" : "ArtifactRevision", Assert.Single(outcome.Evidence).Kind);

        var persisted = JsonSerializer.Deserialize<WorkExecutionOutcomeV1>(JsonSerializer.Serialize(outcome, json), json)!;
        var qaInstallation = Guid.NewGuid();
        var reviewedItem = JsonSerializer.Deserialize<WorkItem>("{}")! with
        {
            Id = canonical.TaskItemId, PlanningRevision = 1, Planning = planning,
            StageAssignments = [new("quality", "AgentInstallation", Guid.NewGuid(), qaInstallation)]
        };
        var qa = canonical with
        {
            StageKey = "quality", StageExecutionId = Guid.NewGuid(), AttemptId = Guid.NewGuid(), PriorOutcomes = [persisted],
            Item = JsonSerializer.SerializeToElement(new { deliverySpecificationJson = JsonSerializer.Serialize(delivery, json) })
        };
        var runtime = new AgentTestRuntime()
            .RegisterCapability<WorkItemReference, WorkItem>(WorkItemCapabilities.Read, (_, _) => Task.FromResult(reviewedItem))
            .RegisterCapability<JsonElement, ArtifactDocument>(PlatformCapabilities.ArtifactRead, (request, _) =>
            {
                Assert.Equal(artifact.ArtifactId, request.GetProperty("artifactId").GetGuid());
                return Task.FromResult(new ArtifactDocument(artifact.ArtifactId, "Delivered document", "discipline", "Submitted", artifact.RevisionId,
                    artifact.RevisionId, null, null, canonical.TaskItemId,
                    [new(artifact.RevisionId, 1, null, content, digest, "Submitted", DateTimeOffset.UtcNow, null, null)]));
            })
            .RegisterCapability<JsonElement, JsonElement>(PlatformCapabilities.LlmChatStream, (_, _) => Task.FromResult(
                JsonSerializer.SerializeToElement(new { text = JsonSerializer.Serialize(new WorkArtifactQualityResult(artifact.ArtifactId, artifact.RevisionId,
                    digest, true, "Reviewed exact discipline deliverable", [new("Meets criterion", true, "Concrete evidence in exact revision")], []), json), role = "assistant" })));
        var context = runtime.CreateContext(canonical.OrganizationId.ToString(), qaInstallation.ToString());
        Assert.True(ArtifactDeliveryReview.Supports(qa));
        var result = await ArtifactDeliveryReview.ExecuteAsync(qa, context, context.CreateChatClient(new(Guid.NewGuid(), "test-model")), default);
        Assert.Equal("passed", result.Value!.Value.Deserialize<WorkExecutionOutcomeV1>(json)!.OutcomeCode);

    }
}
