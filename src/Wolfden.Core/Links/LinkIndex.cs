using Wolfden.Core.Documents;
using Wolfden.Core.Entities;
using Wolfden.Core.Structure;
using Wolfden.Core.Types;

namespace Wolfden.Core.Links;

/// <summary>A link and what it resolved to.</summary>
public sealed class ResolvedLink
{
    internal ResolvedLink(LinkOccurrence occurrence, TargetResolution resolution, Documents.Section? targetSection)
    {
        Occurrence = occurrence;
        Resolution = resolution;
        TargetSection = targetSection;
    }

    /// <summary>The link and where it's written.</summary>
    public LinkOccurrence Occurrence { get; }

    /// <summary>How the target resolved.</summary>
    public TargetResolution Resolution { get; }

    /// <summary>Resolved, ambiguous, or broken.</summary>
    public LinkOutcome Outcome => Resolution.Outcome;

    /// <summary>The entity the link resolves to, or null.</summary>
    public CatalogEntity? Target => Resolution.Entity;

    /// <summary>For a <c>#heading</c> link, the section it matched (F-LINK-9), or null.</summary>
    public Documents.Section? TargetSection { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Occurrence} -> {Target?.ToString() ?? Outcome.ToString()}";
}

/// <summary>
/// Every link in a den, resolved and checked (format.md §10): entity
/// bodies and fields, container bodies, chapter fields and
/// <c>chapter_defaults</c>, and outlines. It reports empty links
/// (F-LINK-3), ambiguous (F-LINK-7) and broken (F-LINK-8) links, headings
/// that don't match (F-LINK-9), links in <c>ref</c> fields to entities of
/// another type (F-LINK-11), links from canon to idea or retconned
/// entities (F-LINK-12), and, when the den turns on spoiler checks
/// (F-DEN-7), links that run ahead of their position (F-LINK-13).
/// </summary>
public sealed class LinkIndex
{
    private readonly List<ResolvedLink> _links = [];
    private readonly Dictionary<CatalogEntity, List<ResolvedLink>> _backlinks = [];
    private readonly List<DenDiagnostic> _diagnostics = [];

    // What each file's scan found, kept for a later build over the same structure (#14).
    private readonly Dictionary<CatalogEntity, Scan> _entityScans = [];
    private readonly Dictionary<Container, Scan> _containerScans = [];
    private readonly Dictionary<string, Scan?> _outlineScans = new(StringComparer.Ordinal);

    private LinkIndex(EntityCatalog catalog)
    {
        Catalog = catalog;
        Resolver = new LinkResolver(catalog);
    }

    /// <summary>The entities the links resolve against.</summary>
    public EntityCatalog Catalog { get; }

    /// <summary>The resolver, for resolving other targets against the same entities.</summary>
    public LinkResolver Resolver { get; }

    /// <summary>Every link, by file path and then position in the file.</summary>
    public IReadOnlyList<ResolvedLink> Links => _links;

    /// <summary>Link problems, by path and then line.</summary>
    public IReadOnlyList<DenDiagnostic> Diagnostics => _diagnostics;

    /// <summary>Loads the den at <paramref name="root"/> and resolves its links.</summary>
    public static LinkIndex Load(string root) => Load(new FileSystemDenSource(root));

    /// <summary>Loads a den and resolves its links.</summary>
    public static LinkIndex Load(IDenSource source) => Build(EntityCatalog.Load(source), source);

    /// <summary>
    /// Finds and resolves every link in the den. Files that weren't loaded,
    /// because they or a container above them have errors (F-GEN-2), are
    /// skipped. <paramref name="source"/> is read only for <c>outline.md</c> files.
    /// </summary>
    public static LinkIndex Build(EntityCatalog catalog, IDenSource source) => Build(catalog, source, null, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>
    /// <see cref="Build(EntityCatalog, IDenSource)"/> after some files
    /// changed, for a file watcher (#14). Finding the links in a file
    /// depends only on that file and the den's structure, so the links found
    /// in entities that are the same objects in <paramref name="previous"/>'s
    /// catalog (see <see cref="EntityCatalog.Update"/>), in its containers,
    /// and in outlines not named in <paramref name="changedOutlines"/>, are
    /// kept. Every link is resolved and checked again, since a change to one
    /// entity can change what links anywhere resolve to. The result is the
    /// same as a full build's.
    /// </summary>
    /// <param name="catalog">The entities as they are now.</param>
    /// <param name="source">The den as it is now; read only for <c>outline.md</c> files.</param>
    /// <param name="previous">An earlier build over the same structure, or null to find every link again.</param>
    /// <param name="changedOutlines">The <c>outline.md</c> paths that may have changed since <paramref name="previous"/>.</param>
    public static LinkIndex Build(EntityCatalog catalog, IDenSource source, LinkIndex? previous, IReadOnlySet<string> changedOutlines)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(changedOutlines);
        var index = new LinkIndex(catalog);
        var reuse = previous is not null && previous.Catalog.Structure == catalog.Structure ? previous : null;
        index.Run(source, reuse, changedOutlines);
        return index;
    }

    /// <summary>The links that resolve to <paramref name="entity"/>: its backlinks.</summary>
    public IReadOnlyList<ResolvedLink> LinksTo(CatalogEntity entity) => _backlinks.TryGetValue(entity, out var links) ? links : [];

    private void Run(IDenSource source, LinkIndex? previous, IReadOnlySet<string> changedOutlines)
    {
        var structure = Catalog.Structure;
        var bookkeeping = new Bookkeeping(structure.BookkeepingSections);
        var scans = new List<Scan>();

        Scan ScanWith(Action<LinkScanner> scan)
        {
            var result = new Scan([], []);
            scan(new LinkScanner(bookkeeping, result.Occurrences, result.Diagnostics));
            return result;
        }

        foreach (var entity in Catalog.Entities)
        {
            if (previous is null || !previous._entityScans.TryGetValue(entity, out var scan))
            {
                scan = ScanWith(s => s.ScanEntity(entity));
            }

            _entityScans[entity] = scan;
            scans.Add(scan);
        }

        foreach (var universe in structure.Universes)
        {
            foreach (var container in new Container[] { universe }.Concat(universe.Series).Concat(universe.Books))
            {
                if (previous is null || !previous._containerScans.TryGetValue(container, out var scan))
                {
                    scan = ScanWith(s => s.ScanContainer(container));
                }

                _containerScans[container] = scan;
                scans.Add(scan);
            }

            foreach (var book in universe.Books)
            {
                var path = $"{book.Path}/outline.md";
                if (previous is null || changedOutlines.Contains(path) || !previous._outlineScans.TryGetValue(path, out var scan))
                {
                    scan = null;
                    if (source.TryReadFile(path) is { } bytes)
                    {
                        var outline = MarkdownFile.ParseOutline(bytes);
                        if (!Outlines.OutlineReader.IsExcluded(outline))
                        {
                            scan = ScanWith(s => s.ScanOutline(book, path, outline));
                        }
                    }
                }

                _outlineScans[path] = scan;
                if (scan is not null)
                {
                    scans.Add(scan);
                }
            }
        }

        var occurrences = new List<LinkOccurrence>();
        foreach (var scan in scans)
        {
            occurrences.AddRange(scan.Occurrences);
            _diagnostics.AddRange(scan.Diagnostics);
        }

        foreach (var occurrence in occurrences.OrderBy(o => o.Path, StringComparer.Ordinal).ThenBy(o => o.Link.Line).ThenBy(o => o.Link.Column))
        {
            var link = Check(occurrence, structure.SpoilerChecks);
            _links.Add(link);
            if (link.Target is { } target)
            {
                if (!_backlinks.TryGetValue(target, out var list))
                {
                    _backlinks[target] = list = [];
                }

                list.Add(link);
            }
        }

        var sorted = _diagnostics.OrderBy(d => d.Path, StringComparer.Ordinal).ThenBy(d => d.Diagnostic.Line ?? 0).ToList();
        _diagnostics.Clear();
        _diagnostics.AddRange(sorted);
    }

    private ResolvedLink Check(LinkOccurrence occurrence, bool spoilerChecks)
    {
        var link = occurrence.Link;
        var resolution = Resolver.Resolve(occurrence.Scope, link.Target);
        if (resolution.Entity is not { } target)
        {
            Warn(occurrence, resolution.Outcome == LinkOutcome.Ambiguous ? "F-LINK-7" : "F-LINK-8", $"{link.Text}: {resolution.Problem}");
            return new ResolvedLink(occurrence, resolution, null);
        }

        Documents.Section? section = null;
        if (link.Heading is { } heading)
        {
            var wanted = LinkText.Normalize(heading);
            section = target.File.Sections.FirstOrDefault(s => LinkText.Normalize(s.Name) == wanted);
            if (section is null)
            {
                Warn(occurrence, "F-LINK-9", $"{link.Text}: '{target.Name}' has no section '{heading}', so the link goes to the entity.");
            }
        }

        // F-LINK-11: a ref field's link must reach an entity of a type its `to` names.
        if (!occurrence.IsIgnoredValue && RefField(occurrence) is { } field && !field.Accepts(target.Type))
        {
            Warn(occurrence, "F-LINK-11",
                $"{link.Text}: {field.Name} links to '{target.Name}', whose type is {target.Type}, but {field.Name} is a ref to {string.Join(" or ", field.CheckedTo)}.");
        }

        if (!occurrence.IsBookkeeping)
        {
            if (occurrence.IsInCanonFact && target.Status is FactStatus.Idea or FactStatus.Retconned)
            {
                Warn(occurrence, "F-LINK-12",
                    $"{link.Text}: this canon text links to '{target.Name}', whose status is {target.Status.ToKey()}.");
            }

            if (spoilerChecks && target.EntersCanon is { } enters)
            {
                var ahead = occurrence.Positions.Where(p => !p.Point.IsStartOfStory && enters > p.Point).ToList();
                foreach (var position in ahead.Where(p => p.Chapter is null))
                {
                    Warn(occurrence, "F-LINK-13",
                        $"{link.Text}: '{target.Name}' enters canon at {target.EntersCanonDescription}, after this link's position, {position.Description}. It may be a spoiler.");
                }

                // A link in chapter_defaults is checked for each chapter that inherits it,
                // but reported once, naming the chapters in story order (types.md F-CHAP-4).
                var chapters = ahead.Where(p => p.Chapter is not null).OrderBy(p => p.Point).Select(p => p.Chapter!.ToString()).ToList();
                if (chapters.Count > 0)
                {
                    var where = chapters.Count == 1
                        ? $"chapter {chapters[0]}, which inherits this default"
                        : $"chapters {ListChapters(chapters)}, which inherit this default";
                    Warn(occurrence, "F-LINK-13",
                        $"{link.Text}: '{target.Name}' enters canon at {target.EntersCanonDescription}, after {where}. It may be a spoiler.");
                }
            }
        }

        return new ResolvedLink(occurrence, resolution, section);
    }

    /// <summary>The <c>ref</c> field a link is in (types.md F-FT-9): an entity field of its type, or a chapter field. Null otherwise.</summary>
    private FieldDefinition? RefField(LinkOccurrence occurrence)
    {
        if (occurrence.Field is not { } name)
        {
            return null;
        }

        var field = occurrence.Site switch
        {
            LinkSite.EntityField or LinkSite.EntityFieldEntry => occurrence.Entity?.Definition?.FindField(name),
            LinkSite.ChapterField or LinkSite.ChapterDefault => Catalog.Structure.Types.FindChapterField(name),
            _ => null,
        };
        return field is { Kind: FieldKind.Ref } ? field : null;
    }

    /// <summary>
    /// Two or more chapters for an F-LINK-13 message: "a and b", "a, b and c",
    /// or the first <see cref="LinkResolver.ListedMatches"/> and then "and N more".
    /// </summary>
    private static string ListChapters(List<string> chapters)
    {
        if (chapters.Count > LinkResolver.ListedMatches)
        {
            return $"{string.Join(", ", chapters.Take(LinkResolver.ListedMatches))} and {chapters.Count - LinkResolver.ListedMatches} more";
        }

        return $"{string.Join(", ", chapters.Take(chapters.Count - 1))} and {chapters[^1]}";
    }

    private void Warn(LinkOccurrence occurrence, string rule, string message) =>
        _diagnostics.Add(new DenDiagnostic(occurrence.Path, new Diagnostic(rule, DiagnosticSeverity.Warning, occurrence.Link.Line, message)));

    /// <summary>The links found in one file, and the problems finding them, before they're resolved.</summary>
    private sealed record Scan(List<LinkOccurrence> Occurrences, List<DenDiagnostic> Diagnostics);
}
