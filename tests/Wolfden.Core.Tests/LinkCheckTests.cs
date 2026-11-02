using Wolfden.Core.Links;
using static Wolfden.Core.Tests.LinkDens;

namespace Wolfden.Core.Tests;

/// <summary>
/// The link checks (format.md §10.3): links to idea and retconned entities
/// from canon (F-LINK-12), and the spoiler check (F-LINK-13), with the
/// positions F-LINK-13 gives each kind of link.
/// </summary>
public class LinkCheckTests
{
    private const string Writer = LongDark + "/entities/characters/writer.md";

    /// <summary>For each link label, how many warnings of <paramref name="rule"/> its line got.</summary>
    private static Dictionary<string, int> Warned(LinkIndex index, string rule)
    {
        var lines = index.Diagnostics.Where(d => d.Diagnostic.RuleId == rule).GroupBy(d => (d.Path, d.Diagnostic.Line)).ToDictionary(g => g.Key, g => g.Count());
        return index.Links
            .Where(l => l.Occurrence.Link.Label is not null)
            .ToDictionary(l => l.Occurrence.Link.Label!, l => lines.GetValueOrDefault((l.Occurrence.Path, (int?)l.Occurrence.Link.Line)));
    }

    /// <summary>
    /// "Late" enters canon at ash:2. A writer entity from ember:1 links to
    /// it from every kind of place, each link labelled by where it is.
    /// </summary>
    private static MemoryDenSource SpoilerDen(bool spoilerChecks = true)
    {
        const string writerFields = """
            fields:
              ally: "[[late|plain field]]"
              posting:
                - { value: "[[late|entry early]]", from: "ember:2" }
                - { value: "[[late|entry late]]", from: "ash:3" }
                - { value: "[[late|entry unpositioned]]" }
                - { value: "[[late|entry unresolved]]", from: "ash:9" }
            """;
        const string writerBody = """
            [[late|lead]]

            ## Early
            [[late|section default]]

            ## Later
            <!-- wolfden: from=ash:2 -->
            [[late|section late]]

            ### Sub early
            <!-- wolfden: from=ember:2 -->
            [[late|subsection inherits]]

            ## Unresolved
            <!-- wolfden: from=ash:9 -->
            [[late|section unresolved]]

            ## Change log
            [[late|change log]]

            ### Inside the log
            <!-- wolfden: canon=true -->
            [[late|inside the log]]

            ## Notes
            <!-- wolfden: canon=false -->
            [[late|canon false]]

            ## Scratch
            [[late|den pattern]]
            """;
        const string thread = """
            type: thread
            name: Thread
            status: canon
            from: "ember:1"
            fields:
              paid_off: "ash:3"
            """;
        const string threadBody = """
            ## Payoff
            [[late|payoff]]

            ### Detail
            [[late|payoff detail]]
            """;
        const string earlyPayoff = """
            ## Payoff
            <!-- wolfden: from=ember:2 -->
            [[late|explicit payoff]]
            """;

        return Den(spoilerChecks, "bookkeeping_sections: [Scratch]\n")
            .Entity(LongDark, "late", Character("Late", "from: \"ash:2\""))
            .Entity(LongDark, "writer", Character("Writer", "from: \"ember:1\"", writerFields), writerBody)
            .Entity(LongDark, "thread", thread, threadBody, folder: "threads")
            .Entity(LongDark, "early-payoff", thread.Replace("name: Thread", "name: Early payoff", StringComparison.Ordinal), earlyPayoff, folder: "threads")
            .Entity(LongDark, "candidate-paid-off", thread.Replace("name: Thread", "name: Candidate paid off", StringComparison.Ordinal)
                .Replace("paid_off: \"ash:3\"", "paid_off: { value: \"ash:3\", status: candidate }", StringComparison.Ordinal),
                "## Payoff\n[[late|candidate paid off]]\n", folder: "threads")
            .Entity(LongDark, "not-a-thread", Character("Not a thread", "from: \"ember:1\"", "fields:", "  paid_off: \"ash:3\""), "## Payoff\n[[late|not a thread]]\n")
            .Entity(Starfall, "timeless", Character("Timeless"), "[[late|universe default]]\n")
            .Entity(Ash, "in-ash", Character("In Ash"), "[[late|ash default]]\n")
            .Entity(Ash, "lost", Character("Lost", "from: \"ash:9\""), "[[late|from unresolved]]\n")
            .Entity(Moon, "moon-born", Character("Moon Born"))
            .Entity(Starfall, "unplaced", Character("Unplaced", "from: \"ash:9\""))
            .Entity(Ember, "pointer", Character("Pointer", "from: \"ember:1\""), "[[moon-born|to a later book]]\n[[unplaced|to an unresolved from]]\n")
            .With($"{Starfall}/universe.md", "---\nname: Starfall\norder: [long-dark, a-quiet-moon]\n---\n[[late|universe body]]\n")
            .With($"{LongDark}/series.md", "---\nname: The Long Dark\nbooks: [ember, ash]\nchapter_defaults:\n  when: \"[[late|series default]]\"\n---\n")
            .With($"{Ember}/book.md", """
                ---
                name: Ember
                chapter_defaults: { pov: "[[late|ember default]]" }
                chapters:
                  - { key: prologue, fields: { pov: [] } }
                  - { key: "1", fields: { pov: "[[late|chapter field]]" } }
                  - "2"
                  - "3"
                ---
                """)
            .With($"{Ember}/outline.md", """
                # Ember outline

                [[late|outline notes]]

                | row | chapter | beat | status |
                |---|---|---|---|
                | 1 | 1 | [[late\|row chapter 1]] | |
                | 2 | | [[late\|row no chapter]] | |
                | 3 | 9 | [[late\|row bad chapter]] | |
                | x | 1 | [[late\|row bad id]] | |
                """)
            .With($"{Ash}/outline.md", "| row | chapter | beat |\n|---|---|---|\n| 10 | 2 | [[late\\|row ash 2]] |\n");
    }

    [Theory]
    [InlineData("lead", 1)]
    [InlineData("plain field", 1)]
    [InlineData("entry early", 1)]
    [InlineData("entry late", 0)]
    [InlineData("entry unpositioned", 0)]
    [InlineData("entry unresolved", 0)]
    [InlineData("section default", 1)]
    [InlineData("section late", 0)]
    [InlineData("subsection inherits", 0)]
    [InlineData("section unresolved", 0)]
    [InlineData("change log", 0)]
    [InlineData("inside the log", 0)]
    [InlineData("canon false", 0)]
    [InlineData("den pattern", 0)]
    [InlineData("payoff", 0)]
    [InlineData("payoff detail", 0)]
    [InlineData("explicit payoff", 1)]
    [InlineData("candidate paid off", 1)]
    [InlineData("not a thread", 1)]
    [InlineData("universe default", 0)]
    [InlineData("ash default", 1)]
    [InlineData("from unresolved", 0)]
    [InlineData("to a later book", 1)]
    [InlineData("to an unresolved from", 0)]
    [InlineData("universe body", 0)]
    [InlineData("chapter field", 1)]
    [InlineData("ember default", 1)]
    [InlineData("series default", 1)]
    [InlineData("outline notes", 0)]
    [InlineData("row chapter 1", 1)]
    [InlineData("row no chapter", 1)]
    [InlineData("row bad chapter", 1)]
    [InlineData("row bad id", 0)]
    [InlineData("row ash 2", 0)]
    public void SpoilerChecksUseEachLinksPosition(string label, int warnings)
    {
        var index = LinkIndex.Load(SpoilerDen());

        Assert.Equal(warnings, Warned(index, "F-LINK-13")[label]);
        Assert.All(index.Diagnostics, d => Assert.Equal("F-LINK-13", d.Diagnostic.RuleId));
    }

    [Fact]
    public void SpoilerChecksAreOffUnlessTheDenTurnsThemOn()
    {
        Assert.Empty(LinkIndex.Load(SpoilerDen(spoilerChecks: false)).Diagnostics);
        Assert.Empty(LinkIndex.Load(SpoilerDen().With("den.yaml", "format: 1\n")).Diagnostics);
    }

    [Fact]
    public void SpoilerWarningsNameBothPositions()
    {
        var index = LinkIndex.Load(SpoilerDen());
        string Message(string label) => index.Diagnostics.Single(d =>
            d.Diagnostic.Line == index.Links.Single(l => l.Occurrence.Link.Label == label).Occurrence.Link.Line
            && d.Path == index.Links.Single(l => l.Occurrence.Link.Label == label).Occurrence.Path).Diagnostic.Message;

        Assert.Equal("[[late|lead]]: 'Late' enters canon at ash:2, after this link's position, ember:1. It may be a spoiler.", Message("lead"));
        Assert.Contains("after this link's position, the start of book 'ember'", Message("row no chapter"), StringComparison.Ordinal);
        Assert.Contains("'Moon Born' enters canon at the start of book 'a-quiet-moon'", Message("to a later book"), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Scenario", "SC-CHECK-44")]
    public void ADefaultIsOneSpoilerWarningThatNamesItsChapters()
    {
        var index = LinkIndex.Load(SpoilerDen());
        string Message(string path, string label) => Assert.Single(index.Diagnostics,
            d => d.Path == path && d.Diagnostic.Message.Contains(label, StringComparison.Ordinal)).Diagnostic.Message;

        Assert.Equal("[[late|ember default]]: 'Late' enters canon at ash:2, after chapters ember:2 and ember:3, which inherit this default. It may be a spoiler.",
            Message($"{Ember}/book.md", "ember default"));
        Assert.Equal("[[late|series default]]: 'Late' enters canon at ash:2, after chapters ember:prologue, ember:1, ember:2, ember:3 and ash:1, which inherit this default. It may be a spoiler.",
            Message($"{LongDark}/series.md", "series default"));
    }

    [Fact]
    [Trait("Scenario", "SC-CHECK-44")]
    public void ADefaultAheadOfMoreThanFiveChaptersNamesTheFirstFive()
    {
        var den = Den()
            .Entity(LongDark, "late", Character("Late", "from: \"ash:2\""))
            .With($"{Ember}/book.md", "---\nname: Ember\nchapter_defaults: { pov: \"[[late]]\" }\nchapters: [1, 2, 3, 4, 5, 6, 7, 8]\n---\n");

        var warning = Assert.Single(LinkIndex.Load(den).Diagnostics).Diagnostic;

        Assert.Equal("F-LINK-13", warning.RuleId);
        Assert.Equal("[[late]]: 'Late' enters canon at ash:2, after chapters ember:1, ember:2, ember:3, ember:4, ember:5 and 3 more, which inherit this default. It may be a spoiler.",
            warning.Message);
    }

    [Fact]
    public void LinksKnowTheirSites()
    {
        var index = LinkIndex.Load(SpoilerDen());
        LinkOccurrence At(string label) => index.Links.Single(l => l.Occurrence.Link.Label == label).Occurrence;

        Assert.Equal(LinkSite.EntityLead, At("lead").Site);
        Assert.Equal(LinkSite.EntityField, At("plain field").Site);
        Assert.Equal(LinkSite.EntityFieldEntry, At("entry early").Site);
        Assert.Equal("posting", At("entry early").Field);
        Assert.Equal(LinkSite.EntitySection, At("subsection inherits").Site);
        Assert.Equal("Sub early", At("subsection inherits").Section!.Name);
        Assert.True(At("change log").IsBookkeeping);
        Assert.True(At("inside the log").IsBookkeeping);
        Assert.False(At("section late").IsBookkeeping);
        Assert.Equal(LinkSite.ContainerBody, At("universe body").Site);
        Assert.Equal(LinkSite.ChapterField, At("chapter field").Site);
        Assert.Equal("1", At("chapter field").Chapter!.Key);
        Assert.Equal(LinkSite.ChapterDefault, At("ember default").Site);
        Assert.Equal(LinkSite.OutlineRow, At("row chapter 1").Site);
        Assert.Equal("1", At("row chapter 1").OutlineRow);
        Assert.Equal(LinkSite.OutlineNotes, At("outline notes").Site);
        Assert.Equal(LinkSite.OutlineNotes, At("row bad id").Site);
        Assert.Equal("ash:2", Assert.Single(At("subsection inherits").Positions).Description);
    }

    [Fact]
    public void FrontmatterLinksPointAtTheirText()
    {
        var index = LinkIndex.Load(SpoilerDen());
        var link = index.Links.Single(l => l.Occurrence.Link.Label == "entry late").Occurrence.Link;

        Assert.Equal(10, link.Line);
        Assert.Equal(17, link.Column);
        Assert.Equal("[[late|entry late]]", link.Text);
    }

    /// <summary>"Dropped" is retconned and "Maybe" is an idea. Each link is labelled by where it is.</summary>
    private static LinkIndex CanonDen()
    {
        const string fields = """
            fields:
              ally: "[[dropped|plain field]]"
              posting:
                - { value: "[[dropped|candidate entry]]", status: candidate }
                - { value: "[[dropped|canon entry]]", from: "ember:2" }
            """;
        const string body = """
            [[dropped|lead]]

            ## Options
            <!-- wolfden: status=candidate -->
            [[dropped|candidate section]]

            ### Inner
            <!-- wolfden: status=canon -->
            [[maybe|canon under candidate]]

            ## Settled
            [[maybe|canon section]]

            ## Session notes
            [[dropped|session notes]]
            """;

        var source = Den()
            .Entity(Starfall, "dropped", Character("Dropped", "status: retconned"))
            .Entity(Starfall, "maybe", Character("Maybe", "status: idea"))
            .Entity(Starfall, "draft", Character("Draft", "status: draft"))
            .Entity(Starfall, "writer", Character("Writer", fields), body)
            .Entity(Starfall, "dreamer", Character("Dreamer", "status: idea"), "[[maybe|idea lead]]\n[[draft|to a draft]]\n")
            .With($"{Starfall}/universe.md", "---\nname: Starfall\norder: [long-dark, a-quiet-moon]\n---\n[[dropped|container body]]\n")
            .With($"{Ember}/book.md", "---\nname: Ember\nchapters:\n  - { key: \"1\", fields: { pov: \"[[dropped|chapter field]]\" } }\n---\n")
            .With($"{Ember}/outline.md", """
                | row | beat | status |
                |---|---|---|
                | 1 | [[maybe\|draft row]] | |
                | 2 | [[maybe\|candidate row]] | Candidate |
                | 3 | [[maybe\|retconned row]] | retconned |
                | 4 | [[maybe\|unknown status row]] | maybe |
                """);
        return LinkIndex.Load(source);
    }

    [Theory]
    [InlineData("plain field", 1)]
    [InlineData("candidate entry", 0)]
    [InlineData("canon entry", 1)]
    [InlineData("lead", 1)]
    [InlineData("candidate section", 0)]
    [InlineData("canon under candidate", 0)]
    [InlineData("canon section", 1)]
    [InlineData("session notes", 0)]
    [InlineData("idea lead", 0)]
    [InlineData("to a draft", 0)]
    [InlineData("container body", 0)]
    [InlineData("chapter field", 1)]
    [InlineData("draft row", 1)]
    [InlineData("candidate row", 0)]
    [InlineData("retconned row", 0)]
    [InlineData("unknown status row", 1)]
    public void CanonLinksToIdeasAndRetconsAreWarnings(string label, int warnings)
    {
        var index = CanonDen();

        Assert.Equal(warnings, Warned(index, "F-LINK-12")[label]);
        Assert.All(index.Diagnostics, d => Assert.Equal("F-LINK-12", d.Diagnostic.RuleId));
    }

    [Fact]
    public void ACanonLinkWarningNamesTheStatus()
    {
        var warning = CanonDen().Diagnostics.First(d => d.Diagnostic.Message.StartsWith("[[dropped|lead]]", StringComparison.Ordinal));

        Assert.Equal("[[dropped|lead]]: this canon text links to 'Dropped', whose status is retconned.", warning.Diagnostic.Message);
    }

    [Fact]
    public void LinksInIgnoredChapterFieldValuesResolveButArentChecked()
    {
        // types.md F-CHAP-3: an entry-shaped chapter-field value is ignored, as
        // if the field were left out, in a chapter item and in chapter_defaults alike.
        var source = Den()
            .Entity(LongDark, "late", Character("Late", "status: retconned", "from: \"ash:2\""))
            .With($"{Ember}/book.md", """
                ---
                name: Ember
                chapter_defaults: { tense: { value: "[[late|ignored default]]" } }
                chapters:
                  - { key: "1", fields: { pov: { value: "[[late|ignored entry]]" } } }
                  - { key: "2", fields: { pov: ["[[late|listed entry]]", { value: x }] } }
                  - { key: "3", fields: { pov: "[[nobody|still resolved]]" } }
                ---
                """);
        var index = LinkIndex.Load(source);
        LinkOccurrence At(string label) => index.Links.Single(l => l.Occurrence.Link.Label == label).Occurrence;

        foreach (var label in new[] { "ignored default", "ignored entry", "listed entry" })
        {
            Assert.True(At(label).IsIgnoredValue);
            Assert.Empty(At(label).Positions);
            Assert.False(At(label).IsInCanonFact);
            Assert.Equal("Late", index.Links.Single(l => l.Occurrence.Link.Label == label).Target!.Name);
        }

        // Only the broken link is reported: no F-LINK-12 or F-LINK-13 for the ignored ones.
        Assert.Equal("F-LINK-8", Assert.Single(index.Diagnostics).Diagnostic.RuleId);
    }
}
