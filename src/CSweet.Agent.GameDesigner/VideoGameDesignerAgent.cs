using CrosswiredStudios.VideoGame.AgentKit;

namespace CSweet.Agent.GameDesigner;

/// <summary>
/// Accountable specialist for gameplay rules and systems. The shared AgentKit supplies the
/// project-scoped board, document, evidence, messaging, and restart-safe mechanics.
/// </summary>
public sealed class VideoGameDesignerAgent : VideoGameSpecialistAgentBase
{
    public override string AgentId => "com.csweet.video-game-designer";
    public override string Version => "2.2.1";

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
