using Leyi.Domain.Mastery;
using Leyi.Domain.Policies;

namespace Leyi.Domain.Sessions.Planning;

/// <summary>
/// SessionPlanner (Domain Model §13, ASE §55.1, AS-G5, AS-G7, AS-G9, AS-G10, AS-G12, AS-G18–G20): pure and deterministic.
/// Turns a <see cref="CandidateSet"/> into an ordered plan within <c>floor(duration × 60 × EffectiveBudgetRatio)</c> seconds.
/// </summary>
/// <remarks>
/// Passes, each over candidates in <see cref="CandidateRanking"/> order (assessments and NEW bundles merged by rank key):
/// <list type="number">
/// <item>Core review: the best DueNow/Overdue block that can be taken (mandatory when one fits).</item>
/// <item>Weak pass: weak-skill blocks until the soft weak target (seconds of weak-dimension activities) is reached or none
/// can be taken.</item>
/// <item>Main pass: every remaining non-secondary block that can be taken; the scan continues past blocks that do not fit.</item>
/// <item>DueToday pass: secondary-only blocks with the remaining budget.</item>
/// <item>X1: unless debt is Critical, a NEW bundle may exceed the phase / 3-minute cap only while the live X1 condition
/// holds — no non-NEW block can be taken in the current selection; non-NEW blocks are re-checked after every such bundle.
/// The budget stays absolute.</item>
/// <item>D2/D3 (best effort): deterministic swaps that raise the number of distinct assessment types under the ordinary
/// rules only (never beyond the NEW cap, never a secondary-only block in place of a main one, never the last core review,
/// never half a bundle), each swap followed by a refill of the freed budget (main, DueToday, live X1).</item>
/// <item>Fallback ladder (only when nothing was selected): current-lesson reinforcement, strong-vocabulary validation,
/// relevant practice — nearest reached lesson first, then rank; the first step that selects something wins.</item>
/// </list>
/// "Can be taken" = not selected, fits the remaining budget, D5 (no second assessment for a KU and primary dimension),
/// NEW-cap rules for bundles, and the whole selection still orderable for D1/D4 (<see cref="SessionActivityOrderer"/>);
/// nothing is selected and later dropped except by a D2/D3 swap. The final order is the orderer's order of the final
/// selection. Diagnostics are computed from the final selection and never steer it.
/// </remarks>
public sealed class SessionPlanner
{
    /// <summary>D2/D3 apply from these durations (rule names in <see cref="DiversityRules"/>; ASE §31).</summary>
    private const int FiveMinutes = 5;

    private const int TenMinutes = 10;

    private const int SecondsPerMinute = 60;

    private readonly SessionPolicy sessionPolicy;
    private readonly EvidencePolicy evidencePolicy;

    public SessionPlanner(SessionPolicy sessionPolicy, EvidencePolicy evidencePolicy)
    {
        this.sessionPolicy = sessionPolicy ?? throw new ArgumentNullException(nameof(sessionPolicy));
        this.evidencePolicy = evidencePolicy ?? throw new ArgumentNullException(nameof(evidencePolicy));
    }

    public static SessionPlanner V1 { get; } = new(SessionPolicy.V1, EvidencePolicy.V1);

    /// <summary>Effective activity budget of a session duration (ASE §5): <c>floor(minutes × 60 × ratio)</c>.</summary>
    public int EffectiveBudgetSeconds(int durationMinutes) =>
        (int)decimal.Floor(durationMinutes * SecondsPerMinute * sessionPolicy.EffectiveBudgetRatio);

    public SessionPlanResult Plan(CandidateSet candidates, SessionPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(request);
        if (!sessionPolicy.AllowedDurationsMinutes.Contains(request.DurationMinutes))
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.DurationMinutes, "Not an allowed session duration.");
        }

        var budget = EffectiveBudgetSeconds(request.DurationMinutes);
        var isCritical = request.DebtLevel == ReviewDebtLevel.Critical;
        var newCap = (int)decimal.Floor(budget * sessionPolicy.NewShareCap(request.Phase, request.DebtLevel, request.DurationMinutes));
        var weakTarget = candidates.HasWeakSkillDimensions
            ? (int)decimal.Floor(budget * sessionPolicy.WeakSkillTargetShare(request.Phase))
            : 0;
        var typeTarget = request.DurationMinutes >= TenMinutes ? sessionPolicy.Diversity.MinTypesFromTenMinutes
            : request.DurationMinutes >= FiveMinutes ? sessionPolicy.Diversity.MinTypesFromFiveMinutes
            : 0;

        var normalBlocks = candidates.Assessments.Select(candidate => PlanBlock.FromAssessment(candidate, sessionPolicy))
            .Concat(candidates.NewVocabulary.Select(candidate => PlanBlock.FromNewBundle(candidate, sessionPolicy, evidencePolicy)))
            .OrderBy(block => block.RankKey, CandidateRanking.Instance)
            .ToList();
        var mainBlocks = normalBlocks.Where(block => !block.IsSecondaryOnly).ToList();
        var secondaryBlocks = normalBlocks.Where(block => block.IsSecondaryOnly).ToList();
        var nonNewBlocks = mainBlocks.Where(block => !block.IsNew).Concat(secondaryBlocks).ToList();
        var newBlocks = mainBlocks.Where(block => block.IsNew).ToList();

        var selection = new Selection(budget, newCap, isCritical, sessionPolicy.Diversity);

        // 1. Core review slot.
        var core = mainBlocks.FirstOrDefault(block => block.IsCoreReview && selection.CanTake(block, ignoreNewCap: false));
        if (core is not null)
        {
            selection.Take(core);
        }

        // 2. Weak pass (soft target).
        while (selection.WeakUsed < weakTarget
               && mainBlocks.FirstOrDefault(block => block.IsWeakSkill && selection.CanTake(block, ignoreNewCap: false)) is { } weak)
        {
            selection.Take(weak);
        }

        // 3. Main pass, 4. DueToday pass, 5. X1.
        void FillNormally()
        {
            TakeAll(selection, mainBlocks);
            TakeAll(selection, secondaryBlocks);
            FillThroughX1(selection, nonNewBlocks, newBlocks);
        }

        FillNormally();

        // 6. D2/D3 best effort, refilling after each swap.
        ImproveTypeDiversity(selection, normalBlocks, typeTarget, FillNormally);

        int? fallbackStep = null;
        if (selection.Blocks.Count == 0)
        {
            fallbackStep = RunFallback(selection, candidates.Fallback, typeTarget);
            if (fallbackStep is null)
            {
                return SessionPlanResult.NoEligibleContent;
            }
        }

        var ordered = SessionActivityOrderer.TryOrder(selection.Blocks, sessionPolicy.Diversity)
            ?? throw new InvalidOperationException("The accepted selection must stay orderable.");
        var activities = ordered
            .SelectMany(block => block.Activities.Select(activity => (Block: block, Activity: activity)))
            .Select((item, index) => new PlannedActivity(
                index,
                item.Activity.KnowledgeUnitId,
                item.Activity.ActivityType,
                item.Activity.PrimaryDimension,
                item.Activity.Role,
                item.Activity.ContentVersion,
                item.Activity.DurationSeconds,
                item.Activity.Score,
                item.Activity.Reasons,
                item.Block.IsNew ? item.Block.KnowledgeUnitId : null))
            .ToList();

        var diagnostics = new SessionPlanDiagnostics(
            request.Phase,
            request.DebtLevel,
            budget,
            newCap,
            selection.NewUsed,
            weakTarget,
            selection.WeakUsed,
            selection.Blocks.Any(block => block.IsCoreReview),
            X1Applied: selection.NewUsed > newCap,
            fallbackStep,
            selection.DistinctAssessmentTypes,
            typeTarget);
        return SessionPlanResult.Created(new SessionPlan(activities, activities.Sum(activity => activity.EstimatedDurationSeconds), diagnostics));
    }

    /// <summary>Takes, in order, every block that can be taken under the ordinary rules; whether any was taken.</summary>
    private static bool TakeAll(Selection selection, IEnumerable<PlanBlock> blocks)
    {
        var tookAny = false;
        foreach (var block in blocks)
        {
            if (selection.CanTake(block, ignoreNewCap: false))
            {
                selection.Take(block);
                tookAny = true;
            }
        }

        return tookAny;
    }

    /// <summary>
    /// X1 (AS-G5): adds NEW bundles beyond the ordinary cap one at a time, each only while <see cref="CanUseX1Now"/> holds
    /// for the current selection; non-NEW blocks are taken first again after every bundle (a bundle can make another
    /// non-NEW block orderable).
    /// </summary>
    private static void FillThroughX1(Selection selection, IReadOnlyList<PlanBlock> nonNewBlocks, IReadOnlyList<PlanBlock> newBlocks)
    {
        while (!selection.IsCritical)
        {
            TakeAll(selection, nonNewBlocks);
            if (!CanUseX1Now(selection, nonNewBlocks))
            {
                continue;
            }

            if (newBlocks.FirstOrDefault(block => selection.CanTake(block, ignoreNewCap: true)) is not { } bundle)
            {
                return;
            }

            selection.Take(bundle);
        }
    }

    /// <summary>Live X1 condition: debt is not Critical and no non-NEW block can be taken in the current selection.</summary>
    private static bool CanUseX1Now(Selection selection, IReadOnlyList<PlanBlock> nonNewBlocks) =>
        !selection.IsCritical && nonNewBlocks.All(block => !selection.CanTake(block, ignoreNewCap: false));

    /// <summary>
    /// D2/D3 (best effort, AS-G9): while below the target, take the highest-priority unselected block bringing a new
    /// assessment type — directly, or in place of the lowest-priority selected block that may be replaced — under the ordinary
    /// rules only (NEW cap, Critical, budget, D5, D1/D4). After a swap, <paramref name="refill"/> uses the freed budget. Each
    /// change raises the distinct type count and a refill only adds blocks, so the loop ends.
    /// </summary>
    private static void ImproveTypeDiversity(Selection selection, IReadOnlyList<PlanBlock> pool, int typeTarget, Action refill)
    {
        var priority = new Dictionary<PlanBlock, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < pool.Count; index++)
        {
            priority.Add(pool[index], index);
        }

        while (selection.DistinctAssessmentTypes < typeTarget && TryImproveOnce(selection, pool, priority) is { } swapped)
        {
            if (swapped)
            {
                refill();
            }
        }
    }

    /// <summary>One diversity improvement: null when none is possible, otherwise whether it was a swap.</summary>
    private static bool? TryImproveOnce(Selection selection, IReadOnlyList<PlanBlock> pool, Dictionary<PlanBlock, int> priority)
    {
        var presentTypes = selection.AssessmentTypes;
        foreach (var incoming in pool.Where(block => !selection.Contains(block) && block.AssessmentTypes.Any(type => !presentTypes.Contains(type))))
        {
            if (selection.CanTake(incoming, ignoreNewCap: false))
            {
                selection.Take(incoming);
                return false;
            }

            var outgoingOrder = selection.Blocks.OrderByDescending(block => priority[block]).ToList();
            foreach (var outgoing in outgoingOrder.Where(outgoing => IsReplaceable(selection, outgoing, incoming)))
            {
                selection.Remove(outgoing);
                if (selection.CanTake(incoming, ignoreNewCap: false))
                {
                    selection.Take(incoming);
                    return true;
                }

                selection.Take(outgoing);
            }
        }

        return null;
    }

    /// <summary>
    /// A selected block a diversity swap may give up for <paramref name="incoming"/>: not the last core review, all its
    /// assessment types still present without it, and — for secondary-only incoming content — itself secondary-only.
    /// </summary>
    private static bool IsReplaceable(Selection selection, PlanBlock outgoing, PlanBlock incoming)
    {
        if (outgoing.IsCoreReview && selection.Blocks.Count(block => block.IsCoreReview) == 1)
        {
            return false;
        }

        if (incoming.IsSecondaryOnly && !outgoing.IsSecondaryOnly)
        {
            return false;
        }

        var others = selection.Blocks.Where(block => !ReferenceEquals(block, outgoing)).SelectMany(block => block.AssessmentTypes).ToHashSet();
        return outgoing.AssessmentTypes.All(others.Contains);
    }

    /// <summary>
    /// Fallback ladder (AS-G10, AS-G19), used only when nothing was selected. Each step orders its candidates nearest reached
    /// lesson first, then by rank. Returns the step that selected something.
    /// </summary>
    private int? RunFallback(Selection selection, IReadOnlyList<FallbackCandidate> fallback, int typeTarget)
    {
        Func<FallbackCandidate, bool>[] steps =
        [
            candidate => candidate.IsCurrentLesson,
            candidate => candidate.KnowledgeState is KnowledgeState.Strong or KnowledgeState.Mastered,
            _ => true,
        ];

        for (var step = 0; step < steps.Length; step++)
        {
            var blocks = fallback.Where(steps[step])
                .OrderBy(candidate => candidate.LessonDistanceFromCurrent)
                .ThenBy(candidate => candidate.Candidate.RankKey, CandidateRanking.Instance)
                .Select(candidate => PlanBlock.FromAssessment(candidate.Candidate, sessionPolicy))
                .ToList();
            if (TakeAll(selection, blocks))
            {
                ImproveTypeDiversity(selection, blocks, typeTarget, () => TakeAll(selection, blocks));
                return step + 1;
            }
        }

        return null;
    }

    /// <summary>Selection state of one planning call.</summary>
    private sealed class Selection
    {
        private readonly int budget;
        private readonly int newCap;
        private readonly DiversityRules diversity;
        private readonly List<PlanBlock> blocks = [];

        public Selection(int budget, int newCap, bool isCritical, DiversityRules diversity)
        {
            this.budget = budget;
            this.newCap = newCap;
            IsCritical = isCritical;
            this.diversity = diversity;
        }

        /// <summary>Review debt is Critical: no NEW content at all (AS-G5).</summary>
        public bool IsCritical { get; }

        public IReadOnlyList<PlanBlock> Blocks => blocks;

        public int Used => blocks.Sum(block => block.Cost);

        public int NewUsed => blocks.Where(block => block.IsNew).Sum(block => block.Cost);

        public int WeakUsed => blocks.Sum(block => block.WeakSkillSeconds);

        public HashSet<Learning.ActivityType> AssessmentTypes => blocks.SelectMany(block => block.AssessmentTypes).ToHashSet();

        public int DistinctAssessmentTypes => AssessmentTypes.Count;

        public bool Contains(PlanBlock block) => blocks.Any(selected => ReferenceEquals(selected, block));

        /// <summary>
        /// Hard feasibility of adding <paramref name="block"/>; <paramref name="ignoreNewCap"/> is passed only by X1 while its
        /// live condition holds (Critical still forbids NEW).
        /// </summary>
        public bool CanTake(PlanBlock block, bool ignoreNewCap)
        {
            if (Contains(block) || Used + block.Cost > budget)
            {
                return false;
            }

            var usedDimensions = blocks.SelectMany(selected => selected.AssessmentDimensions.Select(dimension => (selected.KnowledgeUnitId, dimension))).ToHashSet();
            if (block.AssessmentDimensions.Any(dimension => usedDimensions.Contains((block.KnowledgeUnitId, dimension))))
            {
                return false;
            }

            if (block.IsNew && (IsCritical || (!ignoreNewCap && NewUsed + block.Cost > newCap)))
            {
                return false;
            }

            return SessionActivityOrderer.TryOrder([.. blocks, block], diversity) is not null;
        }

        public void Take(PlanBlock block) => blocks.Add(block);

        public void Remove(PlanBlock block) => blocks.RemoveAll(selected => ReferenceEquals(selected, block));
    }
}
