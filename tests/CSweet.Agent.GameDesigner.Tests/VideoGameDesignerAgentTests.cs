using System.Text.Json;
using CSweet.Agent.SDK;
using CSweet.VideoGame.AgentKit;
using CSweet.WorkManagement.Contracts;

namespace CSweet.Agent.GameDesigner.Tests;

public sealed class VideoGameDesignerAgentTests
{
    [Fact]
    public void IdentityAndAccountabilityAreStable()
    {
        var agent = new VideoGameDesignerAgent();
        Assert.Equal("com.csweet.video-game-designer", agent.AgentId);
        Assert.Equal("2.1.0", agent.Version);
        Assert.Equal("work.execution.run.v1", agent.PrimaryCapability);
    }

    [Fact]
    public void TypedAssignmentRequiresProjectBoardContextAndExactHashes()
    {
        var assignment = ValidAssignment() with
        {
            Input = JsonSerializer.SerializeToElement(BuildInput("not-a-hash"))
        };
        Assert.Throws<ArgumentException>(() => SpecialistAssignmentValidator.Validate(assignment, "game-designer"));
        Assert.Throws<UnauthorizedAccessException>(() => SpecialistAssignmentValidator.Validate(ValidAssignment(), "level-designer"));
    }

    [Fact]
    public void SubstantiveOutputRejectsBoilerplateAndUnresolvedPlaceholders()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SubstantiveOutputValidator.RequireSubstantiveMarkdown("# Design\nTBD", "Core Loop"));
        var content = string.Join('\n', [
            "# Design Decisions", "# Core Loop", "# Gameplay Systems", "# Progression",
            "# Balance Model", "# Prototype Hypotheses", "# Content Rules", "# Instrumentation",
            "# Acceptance Criteria", "# Dependencies and Risks", new string('x', 1200)
        ]);
        SubstantiveOutputValidator.RequireSubstantiveMarkdown(content, "Core Loop", "Balance Model");
    }

    [Fact]
    public async Task ManifestUsesOnlyTypedBoardWorkflowAndNoAmbientAuthority()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "csweet-plugin.json");
        var manifest = await AgentManifestLoader.LoadAsync(path, CancellationToken.None);
        Assert.Equal("2.1.0", manifest.Version);
        Assert.Contains(manifest.Provides, capability => capability.Name == "work.execution.run.v1");
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.Contains(json.RootElement.GetProperty("events").GetProperty("subscribes").EnumerateArray(),
            value => value.GetString() == "com.csweet.agent.coordination.turn-requested.v1");
        Assert.Equal("None", json.RootElement.GetProperty("webAccess").GetProperty("mode").GetString());
        Assert.Empty(VideoGameSpecialistConformance.ValidateManifest(
            path, "com.csweet.video-game-designer", "game-designer", "work.execution.run.v1"));
        var requires = json.RootElement.GetProperty("requires").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()!).ToList();
        Assert.Contains("work.item.comment", requires);
        Assert.Contains("platform.artifact.read.v1", requires);
        Assert.DoesNotContain(requires, x => x.Contains("filesystem", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(requires, x => x.Contains("spend", StringComparison.OrdinalIgnoreCase));
    }

    private static WorkExecutionAssignmentV1 ValidAssignment()
    {
        return new WorkExecutionAssignmentV1(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), 1, "GAME", "GAME-1", Guid.NewGuid(), "design", 1, 1,
            DateTimeOffset.UtcNow.AddMinutes(5), "Design the core interaction.",
            JsonSerializer.SerializeToElement(new { status = "InProgress" }),
            JsonSerializer.SerializeToElement(BuildInput(new string('a', 64))), [], []);
    }

    private static WorkExecutionInputV1 BuildInput(string memberDigest)
    {
        var packageId = Guid.NewGuid();
        var members = new[]
        {
            new ArtifactPackageMemberDigest(Guid.NewGuid(), Guid.NewGuid(), "video-game.vision.v1", memberDigest)
        };
        var packageDigest = memberDigest.Length == 64
            ? ArtifactPackageDigestCalculator.Calculate(packageId, 1, members)
            : new string('b', 64);
        return new WorkExecutionInputV1(Guid.NewGuid(), Guid.NewGuid(), 1,
            new WorkItemPlanningSpecification(["Design"], ["Approved"])
            {
                DelegationRecommendations =
                [new WorkTechnicalDelegationRecommendation("design", "game-designer", [], null, true, "Role-owned design")],
                ArtifactPackageDigest = new ArtifactPackageDigest(packageId, 1, packageDigest,
                    DateTimeOffset.UtcNow, members)
            });
    }
}
