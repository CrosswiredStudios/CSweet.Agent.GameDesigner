using CrosswiredStudios.VideoGame.AgentKit;
using CSweet.Agent.SDK;
using Microsoft.Extensions.AI;
using System.Text.Json;
using CSweet.Agent.SDK.VideoGame.Contracts;

namespace CSweet.Agent.GameDesigner;

/// <summary>
/// Accountable specialist for gameplay rules and systems. The shared AgentKit supplies the
/// project-scoped board, document, evidence, messaging, and restart-safe mechanics.
/// </summary>
public sealed class VideoGameDesignerAgent : VideoGameSpecialistAgentBase
{
    internal const int DefaultContextWindowTokens = 256_000;
    internal const int DefaultOutputTokens = 128_000;
    private const int MinimumOutputTokens = 1_000;
    public override string AgentId => "com.csweet.video-game-designer";
    public override string Version => "3.1.0";
    protected override AgentConfigurationBuilder Configure(AgentConfigurationBuilder builder) =>
        base.Configure(builder)
            .Number("maxContextWindowTokens", "Maximum context-window tokens", required: true,
                description: "Planning ceiling for Game Designer model requests; set this no higher than the selected model's real context window.",
                minimum: 16_000, step: 1_000,
                defaultValue: DefaultContextWindowTokens)
            .Number("maxOutputTokens", "Maximum output tokens", required: true,
                description: "Budget for each Game Designer model response, including reasoning. Set this within the selected model and provider's supported limits.",
                minimum: MinimumOutputTokens, step: 1_000,
                defaultValue: DefaultOutputTokens,
                lessThanFieldKey: "maxContextWindowTokens");

    protected override ChatOptions? ResponseOptions() =>
        new() { MaxOutputTokens = ResolveOutputTokens(Settings) };

    internal static int ResolveOutputTokens(AgentSettings settings)
    {
        var contextWindow = Math.Max(settings.GetInt32("maxContextWindowTokens", DefaultContextWindowTokens),
            MinimumOutputTokens + 1);
        var output = Math.Max(settings.GetInt32("maxOutputTokens", DefaultOutputTokens),
            MinimumOutputTokens);
        return Math.Min(output, contextWindow - 1);
    }

    public override async Task<AgentCoordinationTurnResult> HandleCoordinationTurnAsync(AgentCoordinationTurnRequest request,
        AgentRuntimeContext context, CancellationToken token)
    {
        var turn = request.Transcript.LastOrDefault(t => t.Artifact != null);
        if (turn?.Artifact is not { Type: "video-game.design.consult.v1" } artifact)
            return await base.HandleCoordinationTurnAsync(request, context, token);
        var question = artifact.Payload.Deserialize<GameDesignConsultation>();
        if (question == null || turn.SpeakerOrganizationUserId != request.Counterpart.OrganizationUserId ||
            request.WorkContext?.WorkstreamId != question.WorkstreamId || request.BoardSource?.BoardId != question.BoardId ||
            string.IsNullOrWhiteSpace(question.StoryKey) || string.IsNullOrWhiteSpace(question.Question))
            return AgentCoordinationTurnResult.Blocked("Design advice needs the Producer's scoped question.");
        var board = await context.Platform.Work.ReadBoardAsync(question.BoardId, token);
        if (board.Board.ManagerOrganizationUserId != request.Counterpart.OrganizationUserId || board.Board.WorkstreamId != question.WorkstreamId)
            return AgentCoordinationTurnResult.Blocked("Only the current board manager can commission a Story consultation.");
        var document = await context.Platform.Artifacts.GetAsync(question.BriefId, token);
        var revision = document.Revisions.SingleOrDefault(r => r.Id == question.RevisionId && r.ContentSha256 == question.RevisionDigest && r.Status == "Accepted");
        if (revision == null || document.WorkstreamId != question.WorkstreamId)
            return AgentCoordinationTurnResult.Blocked("The approved source for this question is unavailable.");
        var client = context.CreateChatClient(new AgentLlmSelection(Settings.GetGuid("llmProviderId") ??
            throw new InvalidOperationException("Configure the design model provider."), Settings.GetString("llmModel")));
        var advice = await AgentModelJson.ReadAsync(client, [
            new(ChatRole.System, "Answer this one game design question in the approved brief. Return JSON with assessment, proposedChange and evidenceNeeded. Attribute design judgment, identify hypotheses and propose a bounded check. Only management can approve a scope change."),
            new(ChatRole.User, JsonSerializer.Serialize(new { question.Question, question.StoryKey, brief = revision.Content }))
        ], new AgentModelJsonRequest<GameDesignAdvice>("Story design consultation") { Options = ResponseOptions(),
            Normalize = a => string.IsNullOrWhiteSpace(a.Assessment) || string.IsNullOrWhiteSpace(a.EvidenceNeeded)
                ? throw new AgentModelJsonException("Supply an assessment and the evidence needed.") : a }, token);
        return AgentCoordinationTurnResult.Completed(advice.Assessment, new("video-game.design.advice.v1", "1.0", artifact.Key,
            1, true, JsonSerializer.SerializeToElement(advice)));
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
