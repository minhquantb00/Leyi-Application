using Leyi.Domain.Learning;
using Leyi.Domain.Policies;

namespace Leyi.Domain.Sessions.Planning;

/// <summary>
/// Orders selected blocks so the flattened activity sequence respects D1 (at most <c>MaxSameTypeInARow</c> activities of
/// one ActivityType in a row, exposure included) and D4 (at most <c>MaxSameKnowledgeInARow</c> activities of one knowledge
/// unit in a row) with every NEW bundle kept whole and adjacent (AS-G9).
/// </summary>
/// <remarks>
/// <para>
/// Deterministic, bounded search (no Random, no factorial enumeration):
/// <list type="number">
/// <item>Run-count precheck: a type (or knowledge unit) with <c>c</c> of the <c>N</c> activities needs
/// <c>c ≤ max × (N − c + 1)</c>; sets failing it are rejected at once.</item>
/// <item>Symmetry reduction: blocks are grouped into classes of interchangeable blocks — same activity-type sequence and,
/// when the knowledge unit appears in more than one block, the same knowledge unit. A unit owned by one block can never
/// meet itself across blocks, so such blocks only differ by type sequence. Inside a class, blocks are used in rank order.</item>
/// <item>Depth-first search over class choices, children in the rank order of each class's next block, with a memo of
/// failed states (remaining count per class + run tail). The first leaf found is therefore the rank-lexicographically first
/// valid order: the planner's order wherever D1/D4 allow (AS-G9).</item>
/// </list>
/// The state space is at most Π(classSize + 1) × tails and the search stops after <see cref="MaxSearchExpansions"/>
/// expanded states, so the cost per call is bounded (O(MaxSearchExpansions × classes)) whatever the session size. Within
/// that bound the search is exact. Only a set needing more expansions is (conservatively) reported as not orderable; a
/// returned order is always re-validated activity by activity.
/// </para>
/// </remarks>
public static class SessionActivityOrderer
{
    /// <summary>Upper bound of expanded search states per call (technical bound, not a business rule).</summary>
    public const int MaxSearchExpansions = 20_000;

    /// <summary>The blocks in a valid order, or null when no order is found.</summary>
    public static IReadOnlyList<PlanBlock>? TryOrder(IReadOnlyCollection<PlanBlock> blocks, DiversityRules rules)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(rules);

        var ranked = blocks.OrderBy(block => block.RankKey, CandidateRanking.Instance).ToList();
        if (!SatisfiesRunCounts(ranked, rules))
        {
            return null;
        }

        var order = new Search(ranked, rules).Run();
        return order is not null && IsValid(order, rules) ? order : null;
    }

    /// <summary>Whether the flattened activities respect D1 and D4.</summary>
    public static bool IsValid(IEnumerable<PlanBlock> orderedBlocks, DiversityRules rules)
    {
        ArgumentNullException.ThrowIfNull(orderedBlocks);
        ArgumentNullException.ThrowIfNull(rules);

        Tail? tail = Tail.Empty;
        foreach (var block in orderedBlocks)
        {
            tail = tail.Value.Append(block, rules);
            if (tail is null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Necessary condition: runs of one key separated by at least one other activity.</summary>
    private static bool SatisfiesRunCounts(List<PlanBlock> blocks, DiversityRules rules)
    {
        var activities = blocks.SelectMany(block => block.Activities).ToList();
        var total = activities.Count;
        return activities.GroupBy(activity => activity.ActivityType).All(group => Fits(group.Count(), rules.MaxSameTypeInARow))
               && activities.GroupBy(activity => activity.KnowledgeUnitId).All(group => Fits(group.Count(), rules.MaxSameKnowledgeInARow));

        bool Fits(int count, int maxInARow) => count <= maxInARow * (total - count + 1);
    }

    private sealed class Search
    {
        private readonly DiversityRules rules;
        private readonly List<List<PlanBlock>> classes = [];
        private readonly HashSet<Guid> sharedKnowledgeUnits;
        private readonly Dictionary<Guid, int> sharedIndex;
        private readonly int[] used;
        private readonly HashSet<string> failed = [];
        private readonly List<PlanBlock> order = [];
        private readonly int total;
        private int expansions;

        public Search(List<PlanBlock> ranked, DiversityRules rules)
        {
            this.rules = rules;
            total = ranked.Count;
            sharedKnowledgeUnits = ranked.GroupBy(block => block.KnowledgeUnitId).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
            sharedIndex = sharedKnowledgeUnits.Order().Select((unit, index) => (unit, index)).ToDictionary(item => item.unit, item => item.index);

            // Classes in rank order of their best block; members in rank order.
            var classIndex = new Dictionary<string, int>();
            foreach (var block in ranked)
            {
                var key = ClassKey(block);
                if (!classIndex.TryGetValue(key, out var index))
                {
                    index = classes.Count;
                    classIndex.Add(key, index);
                    classes.Add([]);
                }

                classes[index].Add(block);
            }

            used = new int[classes.Count];
        }

        public List<PlanBlock>? Run() => Extend(Tail.Empty) ? order : null;

        private bool Extend(Tail tail)
        {
            if (order.Count == total)
            {
                return true;
            }

            var state = StateKey(tail);
            if (failed.Contains(state) || ++expansions > MaxSearchExpansions)
            {
                return false;
            }

            // Children: each class with a block left, in the rank order of its next block.
            var children = new List<int>(classes.Count);
            for (var index = 0; index < classes.Count; index++)
            {
                if (used[index] < classes[index].Count)
                {
                    children.Add(index);
                }
            }

            children.Sort((left, right) => CandidateRanking.Instance.Compare(classes[left][used[left]].RankKey, classes[right][used[right]].RankKey));
            foreach (var index in children)
            {
                var block = classes[index][used[index]];
                if (tail.Append(block, rules) is not { } next)
                {
                    continue;
                }

                used[index]++;
                order.Add(block);
                if (Extend(next))
                {
                    return true;
                }

                order.RemoveAt(order.Count - 1);
                used[index]--;
                if (expansions > MaxSearchExpansions)
                {
                    return false;
                }
            }

            failed.Add(state);
            return false;
        }

        private string ClassKey(PlanBlock block) =>
            (sharedKnowledgeUnits.Contains(block.KnowledgeUnitId) ? block.KnowledgeUnitId.ToString("N") : "-")
            + ":" + string.Join(",", block.Activities.Select(activity => (int)activity.ActivityType));

        /// <summary>
        /// Exact state identity: blocks used per class (one char each; at most a few hundred blocks) + run tail. A unit owned
        /// by one block is recorded as "none", since nothing left can continue its run.
        /// </summary>
        private string StateKey(Tail tail)
        {
            var lastKnowledgeClass = tail.LastKnowledge is { } unit && sharedKnowledgeUnits.Contains(unit) ? sharedIndex[unit] : -1;
            return string.Create(used.Length + 4, (used, tail, lastKnowledgeClass), static (span, state) =>
            {
                for (var index = 0; index < state.used.Length; index++)
                {
                    span[index] = (char)state.used[index];
                }

                span[^4] = (char)((int?)state.tail.LastType + 1 ?? 0);
                span[^3] = (char)state.tail.TypeRun;
                span[^2] = (char)(state.lastKnowledgeClass + 1);
                span[^1] = (char)state.tail.KnowledgeRun;
            });
        }
    }

    /// <summary>Run lengths at the end of the sequence built so far.</summary>
    private readonly record struct Tail(ActivityType? LastType, int TypeRun, Guid? LastKnowledge, int KnowledgeRun)
    {
        public static Tail Empty => default;

        /// <summary>The tail after appending the block, or null when D1/D4 break.</summary>
        public Tail? Append(PlanBlock block, DiversityRules rules)
        {
            var tail = this;
            foreach (var activity in block.Activities)
            {
                var typeRun = activity.ActivityType == tail.LastType ? tail.TypeRun + 1 : 1;
                var knowledgeRun = activity.KnowledgeUnitId == tail.LastKnowledge ? tail.KnowledgeRun + 1 : 1;
                if (typeRun > rules.MaxSameTypeInARow || knowledgeRun > rules.MaxSameKnowledgeInARow)
                {
                    return null;
                }

                tail = new Tail(activity.ActivityType, typeRun, activity.KnowledgeUnitId, knowledgeRun);
            }

            return tail;
        }
    }
}
