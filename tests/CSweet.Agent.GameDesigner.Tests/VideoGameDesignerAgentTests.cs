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
        Assert.Equal("1.0.0", agent.Version);
        Assert.Equal("video-game.game-designer.execute.v1", agent.PrimaryCapability);
    }

    [Fact]
    public void TypedAssignmentRequiresProjectBoardContextAndExactHashes()
    {
        var assignment = ValidAssignment() with
        {
            Documents = [new ExactArtifactInput(Guid.NewGuid(), Guid.NewGuid(), "not-a-hash", "video-game.vision.v1", "vision")]
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
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "csweet-plugin.json"));
        var manifest = await AgentManifestLoader.LoadAsync(path, CancellationToken.None);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Contains(manifest.Provides, capability => capability.Name == "video-game.game-designer.execute.v1");
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.Empty(json.RootElement.GetProperty("events").GetProperty("subscribes").EnumerateArray());
        Assert.Equal("None", json.RootElement.GetProperty("webAccess").GetProperty("mode").GetString());
        Assert.Empty(VideoGameSpecialistConformance.ValidateManifest(
            path, "com.csweet.video-game-designer", "game-designer", "video-game.game-designer.execute.v1"));
        var requires = json.RootElement.GetProperty("requires").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()!).ToList();
        Assert.Contains("work.item.comment", requires);
        Assert.Contains("platform.artifact.read.v1", requires);
        Assert.DoesNotContain(requires, x => x.Contains("filesystem", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(requires, x => x.Contains("spend", StringComparison.OrdinalIgnoreCase));
    }

    private static SpecialistWorkAssignment ValidAssignment()
    {
        var organizationId = Guid.NewGuid();
        var workstreamId = Guid.NewGuid();
        return new SpecialistWorkAssignment(
            new AgentWorkContext(organizationId, workstreamId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                null, null, Guid.NewGuid(), null, "video-game-production.v2"),
            1, "game-designer", "video-game.feature.v1", "Design the core interaction.",
            [new ExactArtifactInput(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), "video-game.vision.v1", "vision")],
            [], Guid.NewGuid(), Guid.NewGuid());
    }
}
