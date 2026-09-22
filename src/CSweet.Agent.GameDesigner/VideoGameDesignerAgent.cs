using CrosswiredStudios.VideoGame.AgentKit;
using CSweet.Agent.SDK;
using Microsoft.Extensions.AI;

namespace CSweet.Agent.GameDesigner;

/// <summary>
/// Accountable specialist for gameplay rules and systems. The shared AgentKit supplies the
/// project-scoped board, document, evidence, messaging, and restart-safe mechanics.
/// </summary>
public sealed class VideoGameDesignerAgent : VideoGameSpecialistAgentBase
{
    internal const int DefaultContextWindowTokens = 128_000;
    internal const int DefaultOutputTokens = 16_000;
    private const int MinimumOutputTokens = 1_000;
    private const int MaximumOutputTokens = 200_000;
    public override string AgentId => "com.csweet.video-game-designer";
    public override string Version => "2.3.1";
    protected override AgentConfigurationBuilder Configure(AgentConfigurationBuilder builder) =>
        base.Configure(builder)
            .Number("maxContextWindowTokens", "Maximum context-window tokens", required: true,
                description: "Planning ceiling for Game Designer model requests; set this no higher than the selected model's real context window.",
                minimum: 16_000, maximum: 2_000_000, step: 1_000,
                defaultValue: DefaultContextWindowTokens)
            .Number("maxOutputTokens", "Maximum output tokens", required: true,
                description: "Budget for each Game Designer model response, including reasoning. The provider may impose a lower ceiling.",
                minimum: MinimumOutputTokens, maximum: MaximumOutputTokens, step: 1_000,
                defaultValue: DefaultOutputTokens,
                lessThanFieldKey: "maxContextWindowTokens");

    protected override ChatOptions? ResponseOptions() =>
        new() { MaxOutputTokens = ResolveOutputTokens(Settings) };

    internal static int ResolveOutputTokens(AgentSettings settings)
    {
        var contextWindow = Math.Max(settings.GetInt32("maxContextWindowTokens", DefaultContextWindowTokens),
            MinimumOutputTokens + 1);
        var output = Math.Clamp(settings.GetInt32("maxOutputTokens", DefaultOutputTokens),
            MinimumOutputTokens, MaximumOutputTokens);
        return Math.Min(output, contextWindow - 1);
    }

    protected override string RoleKey => "game-designer";
    protected override string ArtifactTypeKey => "video-game.gameplay-systems-design.v1";
    protected override string RolePrompt => """
        Own gameplay systems, mechanics, controls as game rules, progression, balance, prototype
        hypotheses, tuning variables, and systemic content rules. Ground every rule in the accepted
        game vision and assigned evidence. Make the design implementable and falsifiable. Define
        player actions, state transitions, feedback, failure/recovery, measurable targets, edge cases,
        instrumentation, and validation criteria. Identify dependencies on engineering, level, UI/UX,
        narrative, art, audio, QA, and playtest specialists without taking over their deliverables.
        """;

    protected override IReadOnlyList<string> RequiredSections =>
    [
        "Design Decisions",
        "Core Loop",
        "Gameplay Systems",
        "Progression",
        "Balance Model",
        "Prototype Hypotheses",
        "Content Rules",
        "Instrumentation",
        "Acceptance Criteria",
        "Dependencies and Risks"
    ];
}
