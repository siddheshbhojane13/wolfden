using Wolfden.Core.Entities;
using Wolfden.Core.Structure;

namespace Wolfden.Core.Links;

/// <summary>Which tier of F-LINK-6 a link matched in.</summary>
public enum LinkMatchKind
{
    /// <summary>The target is the entity's id.</summary>
    Id,

    /// <summary>The target matches the entity's slug.</summary>
    Slug,

    /// <summary>The target matches the entity's name.</summary>
    Name,

    /// <summary>The target matches one of the entity's aliases.</summary>
    Alias,
}

/// <summary>What became of a link (format.md §10.2).</summary>
public enum LinkOutcome
{
    /// <summary>The link resolves to one entity.</summary>
    Resolved,

    /// <summary>Several entities match and none is nearest in the scope chain (F-LINK-7).</summary>
    Ambiguous,

    /// <summary>No entity matches (F-LINK-8).</summary>
    Broken,
}

/// <summary>The result of resolving a link target from a scope.</summary>
public sealed class TargetResolution
{
    internal TargetResolution(LinkOutcome outcome, CatalogEntity? entity, LinkMatchKind? matchedBy, IReadOnlyList<CatalogEntity> matches, string? problem)
    {
        Outcome = outcome;
        Entity = entity;
        MatchedBy = matchedBy;
        Matches = matches;
        Problem = problem;
    }

    /// <summary>Resolved, ambiguous, or broken.</summary>
    public LinkOutcome Outcome { get; }

    /// <summary>The entity the link resolves to, or null.</summary>
    public CatalogEntity? Entity { get; }

    /// <summary>The tier that matched, or null if nothing matched. A link whose winner has a duplicate id (F-ID-3) is broken but has a tier.</summary>
    public LinkMatchKind? MatchedBy { get; }

    /// <summary>Every match in the winning tier, by path. For an ambiguous link, the candidates.</summary>
    public IReadOnlyList<CatalogEntity> Matches { get; }

    /// <summary>Why the link doesn't resolve, for the F-LINK-7 or F-LINK-8 warning, or null if it resolves.</summary>
    public string? Problem { get; }
}

/// <summary>
/// Resolves link targets to entities (format.md §10.2): by id, then slug,
/// then name, then alias, among the entities of the linking file's
/// universe, with the nearest scope in the scope chain breaking ties.
/// </summary>
public sealed class LinkResolver
{
    private readonly EntityCatalog _catalog;
    /// <summary>
    /// How many matches an F-LINK-7 message lists before "and N more". An
    /// F-LINK-13 message for a <c>chapter_defaults</c> link lists the same
    /// number of chapters (types.md F-CHAP-4).
    /// </summary>
    internal const int ListedMatches = 5;

    private readonly Dictionary<Universe, Candidates> _targets = [];
    private readonly Dictionary<Universe, Dictionary<string, string>> _excluded = [];
    private readonly Dictionary<(Container Scope, string Target), TargetResolution> _cache = [];

    /// <summary>A resolver over the entities of <paramref name="catalog"/>.</summary>
    public LinkResolver(EntityCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        foreach (var universe in catalog.Structure.Universes)
        {
            // Entities with a duplicate id are candidates too: a link whose
            // winner has one is broken (F-ID-3), rather than falling through
            // to some other entity.
            _targets[universe] = new Candidates(catalog.InUniverse(universe));
            var excluded = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entity in catalog.ExcludedEntities.Where(e => e.Universe == universe))
            {
                foreach (var key in entity.MatchKeys)
                {
                    excluded.TryAdd(key, entity.Path);
                }
            }

            _excluded[universe] = excluded;
        }
    }

    /// <summary>
    /// Resolves <paramref name="target"/>, as written in a link, from a file
    /// whose scope is <paramref name="scope"/> (F-LINK-5 to F-LINK-8).
    /// </summary>
    public TargetResolution Resolve(Container scope, string target)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(target);
        target = target.Trim();
        if (!_cache.TryGetValue((scope, target), out var resolution))
        {
            resolution = Compute(scope, target);
            _cache[(scope, target)] = resolution;
        }

        return resolution;
    }

    private TargetResolution Compute(Container scope, string target)
    {
        var universe = scope.Universe;
        var (kind, matches) = _targets.TryGetValue(universe, out var candidates) ? candidates.Match(target) : (null, []);
        if (kind is not { } matchedBy)
        {
            return new TargetResolution(LinkOutcome.Broken, null, null, [], BrokenMessage(universe, target));
        }

        if (matches.Count == 1)
        {
            return Winner(matches[0], matchedBy, matches, target);
        }

        // F-LINK-7: the match in the nearest scope of the scope chain wins.
        // The chain has at most three scopes, so this is one pass over the matches.
        var chain = scope.ScopeChain;
        var nearest = new List<CatalogEntity>();
        var best = int.MaxValue;
        foreach (var match in matches)
        {
            var distance = IndexOf(chain, match.Scope);
            if (distance < 0 || distance > best)
            {
                continue;
            }

            if (distance < best)
            {
                best = distance;
                nearest.Clear();
            }

            nearest.Add(match);
        }

        if (nearest is [var winner])
        {
            return Winner(winner, matchedBy, matches, target);
        }

        // F-ID-3: copies with a duplicated id are out of link resolution, so
        // when every tied match is one, nothing can be picked and the link
        // is broken. "Link by slug or id" couldn't help: a slug link to a
        // copy is broken too, and the id link is the one that's tied (#82).
        var tied = nearest.Count > 0 ? nearest : matches;
        if (tied.All(_catalog.HasDuplicateId))
        {
            return new TargetResolution(LinkOutcome.Broken, null, matchedBy, matches,
                $"'{target}' would match {tied.Count} entities by {matchedBy.ToString().ToLowerInvariant()} ({List(tied)}), but each of their ids is used by more than one file (F-ID-3), so links don't reach them until the ids are unique. Give each copy a new id.");
        }

        var listed = List(matches);
        var why = nearest.Count == 0
            ? "and none of them is in this file's scope chain"
            : $"and {nearest.Count} of them are in the nearest scope, {Describe(nearest[0].Scope)}";
        return new TargetResolution(LinkOutcome.Ambiguous, null, matchedBy, matches,
            $"'{target}' is ambiguous: it matches {matches.Count} entities by {matchedBy.ToString().ToLowerInvariant()} ({listed}), {why}. Link by slug or id to pick one.");
    }

    /// <summary>
    /// The link reaches <paramref name="winner"/>, unless its id is
    /// duplicated: then the link is broken (F-ID-3), not resolved to
    /// whatever would match next.
    /// </summary>
    private TargetResolution Winner(CatalogEntity winner, LinkMatchKind matchedBy, IReadOnlyList<CatalogEntity> matches, string target)
    {
        if (!_catalog.HasDuplicateId(winner))
        {
            return new TargetResolution(LinkOutcome.Resolved, winner, matchedBy, matches, null);
        }

        return new TargetResolution(LinkOutcome.Broken, null, matchedBy, matches,
            $"'{target}' would match '{winner.Name}' ({winner.Path}), but that entity's id is used by more than one file (F-ID-3), so links don't reach it until the id is unique.");
    }

    /// <summary>The matches for a message, up to <see cref="ListedMatches"/> of them and then "and N more".</summary>
    private static string List(IReadOnlyList<CatalogEntity> matches)
    {
        var listed = string.Join("; ", matches.Take(ListedMatches).Select(m => $"'{m.Name}' ({m.Path})"));
        return matches.Count > ListedMatches ? $"{listed}; and {matches.Count - ListedMatches} more" : listed;
    }

    private static int IndexOf(IReadOnlyList<Container> chain, Container scope)
    {
        for (var i = 0; i < chain.Count; i++)
        {
            if (chain[i] == scope)
            {
                return i;
            }
        }

        return -1;
    }

    private static string Describe(Container c) => $"{c.Kind.ToString().ToLowerInvariant()} '{c.Key}'";

    /// <summary>
    /// How a broken link's message ends when its hint names an entity in
    /// another universe (F-LINK-8): <c>It matches only '&lt;name&gt;' in universe
    /// '&lt;key&gt;'; links don't cross universes.</c> No other message ends so,
    /// so a reader that mustn't name another universe, such as a server
    /// pinned to one (#175), can tell the hint by it, and give
    /// <see cref="BrokenMessageStart"/> alone instead.
    /// </summary>
    public const string CrossUniverseHintEnd = "; links don't cross universes.";

    /// <summary>
    /// The start of a broken link's message (F-LINK-8), before any hint:
    /// that <paramref name="target"/> (as written; it's trimmed) matches no
    /// entity in universe <paramref name="universeKey"/>.
    /// </summary>
    public static string BrokenMessageStart(string universeKey, string target)
    {
        ArgumentNullException.ThrowIfNull(universeKey);
        ArgumentNullException.ThrowIfNull(target);
        return $"'{target.Trim()}' matches no entity in universe '{universeKey}'.";
    }

    /// <summary>The F-LINK-8 message, with a hint when the target matches something links can't reach.</summary>
    private string BrokenMessage(Universe universe, string target)
    {
        var message = BrokenMessageStart(universe.Key, target);
        var normalized = LinkText.Normalize(target);
        if (_excluded.TryGetValue(universe, out var excluded)
            && (excluded.TryGetValue(normalized, out var errorPath) || excluded.TryGetValue(target, out errorPath)))
        {
            return $"{message} It would match {errorPath}, but that file has errors, so it isn't indexed (F-GEN-2).";
        }

        foreach (var other in _catalog.Structure.Universes.Where(u => u != universe))
        {
            if (_targets[other].Match(target) is (not null, var elsewhere))
            {
                return $"{message} It matches only '{elsewhere[0].Name}' in universe '{other.Key}'{CrossUniverseHintEnd}";
            }
        }

        var container = universe.Series.Cast<Container>().Concat(universe.Books)
            .FirstOrDefault(c => LinkText.Normalize(c.Key) == normalized || LinkText.Normalize(c.Name) == normalized);
        if (container is not null)
        {
            return $"{message} It matches {Describe(container)} ('{container.Name}'), but containers aren't link targets yet.";
        }

        return message;
    }

    /// <summary>The entities links can match in one universe, by tier.</summary>
    private sealed class Candidates
    {
        private readonly Dictionary<string, List<CatalogEntity>> _byId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<CatalogEntity>> _bySlug = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<CatalogEntity>> _byName = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<CatalogEntity>> _byAlias = new(StringComparer.Ordinal);

        public Candidates(IEnumerable<CatalogEntity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.Id is { } id)
                {
                    Add(_byId, id, entity);
                }

                if (entity.NormalizedSlug is { } slug)
                {
                    Add(_bySlug, slug, entity);
                }

                Add(_byName, entity.NormalizedName, entity);
                foreach (var alias in entity.NormalizedAliases)
                {
                    Add(_byAlias, alias, entity);
                }
            }
        }

        /// <summary>The first tier with any match (F-LINK-6), and its matches, or null if none matches.</summary>
        public (LinkMatchKind? Kind, IReadOnlyList<CatalogEntity> Matches) Match(string target) =>
            LinkMatching.FirstTier(target, k => Lookup(_byId, k), k => Lookup(_bySlug, k), k => Lookup(_byName, k), k => Lookup(_byAlias, k));

        private static IReadOnlyList<CatalogEntity> Lookup(Dictionary<string, List<CatalogEntity>> index, string key) =>
            index.TryGetValue(key, out var list) ? list : [];

        private static void Add(Dictionary<string, List<CatalogEntity>> index, string key, CatalogEntity entity)
        {
            if (!index.TryGetValue(key, out var list))
            {
                index[key] = list = [];
            }

            // One entity counts once, even if two of its aliases normalize alike.
            if (!list.Contains(entity))
            {
                list.Add(entity);
            }
        }
    }
}
