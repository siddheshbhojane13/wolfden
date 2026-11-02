# Wolfden Writer Scenarios

This is a running list of situations that real writers run into, written
from the writer's side. Each one says what the writer does and what
Wolfden must show them. The scenarios aren't normative: the rules are in
[`format.md`](format.md) and [`types.md`](types.md). They are what the
rules have to get right in practice, and they're the starting point for
tests at every layer (Core, index, CLI, MCP, and the web UI).

## How to use this list

- Each scenario has a stable id (`SC-<AREA>-<n>`). Ids are never reused or
  renumbered. A scenario that no longer applies is marked **Withdrawn** and
  says why.
- A test that covers a scenario carries its id. In .NET tests, use
  `[Trait("Scenario", "SC-CANON-1")]`. One scenario can have tests at
  several layers.
- **Covered by** lists the test classes or files that exercise the scenario.
  "Not yet" is fine: a scenario can be written before the code exists.
- To add one, describe the situation in the writer's words, then the
  expected behavior, then the spec rules involved. Each scenario has a
  single expected outcome, so a test can check it. Invented names only. No
  real manuscripts or notes.

## The example world

The scenarios share one invented universe, and test fixtures use the same
keys:

| Key | What |
|---|---|
| `a` | The main series, five books in story order: `a1` to `a5` ("Book 1" to "Book 5") |
| `b` | A spin-off series in the same universe, after `a` in story order: `b1` |

Positions are written `a3:14` (Book 3, chapter 14), or `a3` for the start
of Book 3 (format.md §8).

## Areas

| Area | What it covers |
|---|---|
| CANON | What a canon-as-of view shows at a point in the story |
| CHECK | What `wolfden check` reports, and how it exits |
| RT | What saving a note keeps, and what an edit changes |
| CLI | Using the other `wolfden` commands: init, index, query, check --fix, new-id |
| WATCH | Keeping the index up to date while notes are edited outside Wolfden |
| MCP | An AI assistant reading the den through `wolfden mcp` |
| STORE | Saving, moving, and deleting notes through Wolfden's den store |
| HIST | Version history, checkpoints, and tags, with or without a history backend |
| WEB | Reading the den in a browser through `wolfden serve` |

## CANON: what's canon at a point in the story

Background: a canon-as-of view is decided by **position, not folder**
(maintainer decision, 2026-10-19, #87). At a given chapter it shows
everything a reader has met by then, in story order, at every scope of the
universe. In the default, unfiltered view, where an entity is filed never
decides whether the view can include it. Filing does decide which entities a
scope filter keeps (#87), and it sets where an entity with no `from` enters canon (F-ENT-6),
how views order and label it (F-VIEW-3), and which match wins when links
tie (F-LINK-7).

### SC-CANON-1. A minor character returns in a later book

A side character first appears in Book 1, filed under `a1` with no `from`.
The writer brings them back in Book 4 and never moves the file out of
Book 1's folder.

- Views at any point in Book 2, 3, or 4 include the character.
- Links from Book 4 files to the character resolve.

Spec: F-VIEW-1, F-ENT-6, F-ORD-1, F-LINK-5, F-LINK-7. Covered by:
`CanonViewScenarioTests.ASideCharacterFiledUnderBookOneReturnsInBookFourWithoutBeingMoved`.

### SC-CANON-2. A throwaway character in Book 3 comes back in Book 5

The writer meant a Book 3 character as a one-off and filed them under `a3`
with no `from`. Later they decide to bring the character back in Book 5.

- Left in place: absent from Book 1 and 2 views, present from `a3` on,
  including Book 5.
- Moved up to the series folder with `from: a3` written in: every view
  includes the character at the same positions as before the move, with the
  same values. Only the character's place in the view's order and the scope it
  shows under change (F-VIEW-3).
- Moved up with no `from`: the character inherits the start of the series,
  which is the start of Book 1, and now appears from Book 1. That's the
  specified behavior. It's why a move made by the app or MCP writes the
  inherited `from` explicitly (F-RT-5, SC-MCP-29).
- Links from Book 5 resolve in all three cases.

Spec: F-VIEW-1, F-ENT-6, F-ORD-3, F-LINK-5, F-LINK-7. Covered by:
`CanonViewScenarioTests.ThrowawayCharacterInBook3ReturnsInBook5`.

### SC-CANON-3. A character enters partway through a book

A character filed under `a1` has `from: a1:20`.

- Absent from a view at `a1:10`. Present at `a1:20` and in every later
  book.

Spec: F-ENT-6, F-VIEW-1. Covered by:
`CanonViewScenarioTests.ACharacterEntersPartwayThroughABook`.

### SC-CANON-4. A series-wide character who arrives late

A character filed at the series level (`a`) has `from: a3:14`.

- Absent at `a3:13` and from all earlier books, even though the file sits
  above every book. Present from `a3:14` on.

Spec: F-ENT-6, F-VIEW-1. Covered by:
`CanonViewScenarioTests.ASeriesLevelCharacterArrivesLateInTheSeries`.

### SC-CANON-5. A fact changes over time

A character's occupation is a temporal field with two entries: one with no
`from`, which starts at the start of the story, and a new value
`from: a2:5`.

- A Book 1 view shows the old occupation. A Book 4 view shows the new one.

Spec: F-TMP-2, F-TMP-3, F-TMP-4, F-TMP-5, F-VIEW-1. Covered by:
`CanonViewScenarioTests.AnOccupationChangesInBookTwo`.

### SC-CANON-6. A secret identity the reader learns at a set chapter

A Book 1 character has a non-temporal field for their true identity,
written as a single entry with `from: a3:20`. Here `from` means the reader
learns the fact at that point, not that the value changes.

- Views before `a3:20` leave the field out. Views from `a3:20` on show it.

Spec: F-TMP-8, F-TMP-4, F-VIEW-1. Covered by:
`CanonViewScenarioTests.AHiddenIdentityStaysHiddenUntilTheReveal`. See also #58
(who-knows-what tracking). A cover story that's later replaced by the truth
is a different case: on a temporal field it's SC-CANON-5, and in a section
it's covered by F-SEC-8 and F-SEC-9.

### SC-CANON-7. A spin-off series in the same world

Series `b` comes after series `a` in story order. Its entities have no
`from`, or a `from` inside `b`.

- Its entities are absent from views at any position in `a`, and present
  in the end-of-story view.
- A view filtered to series `a` leaves them out, even at the end of the
  story. Entities filed at universe scope stay in that filtered view.

Spec: F-VIEW-1, F-ORD-1, F-ORD-3, F-ENT-6; the filter rule is the #87
decision. Covered by:
`CanonViewScenarioTests.ASpinOffSeriesStaysOutOfTheMainSeries`.

### SC-CANON-8. Retconned and idea material stays out of canon views

One entity is marked `retconned` and another is still an `idea`.

- Neither appears in any canon-as-of or pinned canon view, including the
  end-of-story view.
- Both are still indexed, and views that ask for out-of-canon material
  (such as an idea bank) show them.

Spec: F-VIEW-1, F-VIEW-2, F-STAT-3, F-STAT-4, §9.3. Covered by:
`CanonViewScenarioTests.RetconnedAndIdeaMaterialNeverShows`, except
the out-of-canon view, which doesn't exist yet.

### SC-CANON-9. Two different side characters share a name

Book 1 and Book 2 each have their own minor character named "Officer
Hale". They're different people, each filed in their own book.

- A Book 4 view shows both, told apart by where each is filed.
- `[[Officer Hale]]` from a Book 1 file resolves to Book 1's Hale. From a
  Book 4 file it's ambiguous: a warning that lists both, and the link
  doesn't resolve.

Spec: F-VIEW-1, F-VIEW-3, F-LINK-7. Covered by:
`CanonViewScenarioTests.TwoCharactersShareANameInSiblingBooks`.

## CHECK: what `wolfden check` reports

Background: `wolfden check` (design.md §7) lists every error and warning
in a den, one per line with its file, line, and rule id, or as JSON with
`--json`. It exits 0 when there are no errors, even with warnings
(F-GEN-1), 1 when there's at least one error, and 2 when it couldn't
check the den at all: a wrong command line, a missing folder, or a
`den.yaml` that doesn't load. It reads the files as they are now, never
the index, and never changes them unless asked to with `--fix`
(SC-CLI-4).

### SC-CHECK-1. I renamed a character and old links broke

The writer renamed `mara-voss.md` to `mara-ellery.md` and changed her
`name` to "Mara Ellery" by hand, without keeping the old name as an alias.
Other notes still say `[[mara-voss]]` and `[[Mara Voss]]`.

- Each old link is a broken-link warning on its own line, and check still
  exits 0. Adding `aliases: [Mara Voss]` fixes the name link. The slug
  link stays broken until it's rewritten.

Spec: F-LINK-6, F-LINK-8, F-RT-5, F-GEN-1. Covered by:
`ContinuityCheckTests.RenamingACharacterWithoutAnAliasBreaksTheOldLinks`,
`CheckCommandTests.WarningsAloneExitZero`.

### SC-CHECK-2. A Book 2 note mentions someone who doesn't appear until Book 4

A location filed under `a2` links to a character filed under `a4` with no
`from`.

- With `spoiler_checks: true` in `den.yaml`, the link is a spoiler warning
  that says the character enters canon at the start of `a4`. Without it,
  there's no warning.

Spec: F-LINK-13, F-DEN-7, F-ENT-6. Covered by:
`ContinuityCheckTests.ABookTwoNoteThatNamesABookFourCharacterIsASpoilerOnlyWhenTheDenAsks`.

### SC-CHECK-3. A link matches two characters

Books 1 and 2 each have their own "Officer Hale". A Book 3 note links to
`[[Officer Hale]]`.

- The link is an ambiguous-link warning that names both files, and it
  doesn't resolve.

Spec: F-LINK-7. Covered by:
`ContinuityCheckTests.ALinkThatMatchesTwoCharactersIsAmbiguous`. See also
SC-CANON-9.

### SC-CHECK-4. I copied a character file and kept its id

To start a new character, the writer copied `orrin-fell.md` to
`orrin-fell-copy.md` and changed the name, but not the `id`.

- Both files get a duplicate-id warning. Until one gets a new id, links to
  either are broken, and check still exits 0. That holds even for a link
  by the shared id, or by a name both copies still have: the broken-link
  warning names the copies and says to give each copy a new id, rather
  than suggesting a link by slug or id that couldn't work.

Spec: F-ID-3, F-LINK-8. Covered by:
`ContinuityCheckTests.ACopiedFileWithTheSameIdIsCaughtOnBothCopies`,
`LinkResolutionTests.TiedCopiesWithTheSameIdBreakTheLinkAndSayToGiveThemNewIds`.

### SC-CHECK-5. I cut chapters and a fact still points at one

Book 2 had 30 chapters, and the writer cut it to 24 in `book.md`. A
character's role still changes `from: "a2:27"`.

- That line is a warning that names `a2:27` and says Book 2 has no chapter
  27. The value stays in the file, out of canon views, until it's fixed.

Spec: F-POS-4. Covered by:
`ContinuityCheckTests.AFactThatPointsAtACutChapterIsWarned`.

### SC-CHECK-6. Two values for one field claim the same chapters

A character's occupation is "deckhand" from `a1:1` until `a2:10`, and
"harbourmaster" from `a2:5`. The writer moved the promotion earlier and
forgot the `until`.

- The first entry is an overlap warning that names the second one's line
  and `from`.

Spec: F-TMP-5, F-TMP-4. Covered by:
`ContinuityCheckTests.TwoPostingsThatClaimTheSameChaptersOverlap`.

### SC-CHECK-7. A note is missing what its type requires

The den's own `ship` type requires a `class` and a `crew` field and a
`## Crew` section. A quickly written ship note has none of them.

- Each missing field and the missing section is a warning naming it.

Spec: F-FLD-3, F-SEC-5, types.md F-FT-14, F-TYPE-11. Covered by:
`ContinuityCheckTests.AShipNoteWithoutItsRequiredFieldsAndSectionIsWarned`.

### SC-CHECK-8. A typo in a note's type

A note says `type: charcter`.

- It's an unknown-type warning that quotes `charcter`. The note still
  loads, untyped, and links to it work.

Spec: F-ENT-2. Covered by:
`ContinuityCheckTests.ATypoInATypeIsAnUnknownType`.

### SC-CHECK-9. A note doesn't parse

A note's frontmatter has a YAML mistake, such as `from: a1: 3`.

- It's an error with the file and line, the note is left out, and check
  exits 1.

Spec: F-YAML-1, F-GEN-1, F-GEN-2. Covered by:
`ContinuityCheckTests.ANoteThatDoesntParseIsAnErrorAndIsLeftOut`,
`CheckCommandTests.AnErrorExitsOne`.

### SC-CHECK-10. I reordered chapters and a range now ends before it starts

A value holds `from: "a1:5"` `until: "a1:12"`. The writer moved chapter
12 before chapter 5 in `book.md`.

- The entry is a warning that says its `until` isn't after its `from`, so
  it's never in effect.

Spec: F-TMP-3, F-GEN-4. Covered by:
`ContinuityCheckTests.AnEntryWhoseUntilIsNoLongerAfterItsFromIsNeverInEffect`.

### SC-CHECK-11. A new Appearance for a time skip, without its `from`

The writer added a second `## Appearance` for the character after a time
skip, but forgot the `<!-- wolfden: from=... -->` comment under it.

- The second section is an overlap warning, since both start where the
  character enters canon. Views show both until it's fixed.

Spec: F-SEC-4, F-SEC-8. Covered by:
`ContinuityCheckTests.ASecondAppearanceWithoutAFromOverlapsTheFirst`.

### SC-CHECK-12. My AI assistant fixes what check finds

The writer asks an AI assistant to clean up the den's warnings. The
assistant runs `wolfden check --json`.

- It gets one JSON document with a version number, the counts, and each
  diagnostic's path, line (or null), severity, rule id, and message, in a
  fixed order.

Spec: F-GEN-3. Covered by:
`CheckCommandTests.JsonOutputMatchesTheGoldenFile`.

### SC-CHECK-13. A typo in a field name

A character's note says `occupaton: [deckhand]`.

- It's a warning that the character type doesn't define `occupaton`. The
  value is kept and indexed as written. The universal `row` and
  `placeholder` never get this warning, and an untyped note gets no field
  warnings at all.

Spec: F-FLD-2, F-ENT-2, types.md §5.5. Covered by:
`SpecWarningCheckTests.AFieldTheTypeDoesntDefineIsWarned`.

### SC-CHECK-14. A value that doesn't fit its field

The den's own `ship` type has a number `crew`, an enum `condition`, and a
`ref` to a captain. A ship note says `crew: "140"`, `condition: damagd`,
and `captain: Orrin Fell` without a link.

- Each value is a warning that names the field and what it should be,
  and the misspelled enum value suggests `damaged`. In a temporal list,
  each entry's value is checked. Nothing is an error, and the values are
  kept as written.

Spec: F-FLD-3, types.md F-FT-4 to F-FT-12. Covered by:
`SpecWarningCheckTests.ValuesThatDontMatchTheirFieldTypesAreWarned`.

### SC-CHECK-15. A link without quotes

A character's note says `species: [[velari]]`, which YAML reads as a list
inside a list.

- It's one warning that says to write `"[[velari]]"` in quotes, not a
  second one about the field's type.

Spec: F-FLD-6. Covered by:
`SpecWarningCheckTests.AnUnquotedLinkIsAListInsideAListAndSaysToQuoteIt`.

### SC-CHECK-16. Values that change, on a field that doesn't

The writer gave a character's `born` (not a temporal field) a list of
values from different chapters.

- It's a warning that the field isn't temporal. It's still read as
  temporal. A single value with only a `from`, such as a death the reader
  learns about later, gets no warning.

Spec: F-TMP-8, types.md F-FT-13. Covered by:
`SpecWarningCheckTests.EntriesOnAFieldThatIsntTemporalAreWarned`.

### SC-CHECK-17. A typo in an entry

A value is written `{ value: deckhand, form: "a1:3" }`.

- It's a warning that `form` isn't an entry key, so the writer sees why
  the value holds from the start of the story.

Spec: F-TMP-2. Covered by:
`SpecWarningCheckTests.AnEntryKeyThatIsntOneIsWarned`.

### SC-CHECK-18. An event points at an outline row that isn't there

An event's `row: 4` names a row the writer deleted from the outline. Another
says `row: 02`.

- Each is a warning: row 4 isn't in the series' outlines, and `02` isn't
  a row id. A row id in the outlines of two series is ambiguous for a
  universe-level note. A `row` entry with a `from` is a warning too.

Spec: F-FLD-7, F-OUT-5. Covered by:
`SpecWarningCheckTests.ARowFieldThatDoesntMatchTheOutlineIsWarned`,
`SpecWarningCheckTests.ARowIdFoundInTwoSeriesIsAmbiguousForAUniverseEntityButNotForABookEntity`.

### SC-CHECK-19. A Book 2 character who first appears in Book 3

A character filed under `a2` has `from: "a3:2"`.

- It's a warning that the `from` is outside the book the character is
  filed in. Views still use it.

Spec: F-ENT-6. Covered by:
`SpecWarningCheckTests.ABookEntityWhoseFromIsInAnotherBookIsWarned`.

### SC-CHECK-20. A subsection placed outside its section

A `### Recovery` subsection has `from=a1:5`, but its `## Shoulder injury`
starts at `a1:10`.

- It's a warning that the subsection starts before its parent, so it
  starts with its parent instead. A subsection that runs past its parent,
  or starts after its parent hands over, is a warning too.

Spec: F-SEC-9, F-SEC-8. Covered by:
`SpecWarningCheckTests.ASubsectionOutsideItsParentIsWarned`.

### SC-CHECK-21. A note saved straight into a book folder

The writer saved `scratch.md` next to `book.md`.

- It's a warning that the file is ignored, with a hint to move it into
  `entities/`. A book's `outline.md` is fine, but an `outline.md` in a
  series folder is a warning, since outlines are read only in books.

Spec: F-LAY-7, F-OUT-1. Covered by:
`SpecWarningCheckTests.AMarkdownFileDirectlyInAContainerFolderIsWarned`.

### SC-CHECK-22. A character filed with the locations

A character's note is in `entities/locations/`.

- It's a warning that names the folder it belongs in,
  `entities/characters/`. A folder that isn't any type's, such as
  `entities/crew/`, gets no warning.

Spec: F-LAY-6. Covered by:
`SpecWarningCheckTests.AnEntityInAnotherTypesFolderIsWarned`,
`SpecWarningCheckTests.AnEntityDirectlyInEntitiesIsWarned`.

### SC-CHECK-23. An old copy of the den inside the den

The writer keeps `archive/old-den/` in the den, with its own `den.yaml`.

- That `den.yaml` is a warning that dens don't nest, and it's ignored.

Spec: F-LAY-1, F-LAY-8. Covered by:
`SpecWarningCheckTests.ADenYamlBelowTheRootIsWarned`.

### SC-CHECK-24. A notes folder at the den root

The writer made `notes/` at the den root for a style guide.

- It's a warning that `notes/` is reserved and ignored for now, with a
  hint to keep notes in a universe's `entities/notes/`.

Spec: F-LAY-3. Covered by:
`SpecWarningCheckTests.NotesAndAuthorsAtTheDenRootAreReserved`.

### SC-CHECK-25. I split an outline row and typed the same id twice

Splitting row 38, the writer typed `38` twice and put row 36 after them.

- Each repeated id is a warning on every row that has it, across the
  series' outlines, and row 36 is a warning that it's out of order.

Spec: F-OUT-4, F-OUT-5. Covered by:
`SpecWarningCheckTests.OutlineRowsOutOfOrderOrRepeatedAreWarned`.

### SC-CHECK-26. Book 2's outline starts again at row 1

Book 1's outline ends at row 2, and the writer started Book 2's at row 1.

- It's a warning on Book 2's outline: row ids keep counting across a
  series, so its first row should come after Book 1's last.

Spec: F-OUT-6. Covered by:
`SpecWarningCheckTests.RowsThatRestartInTheNextBookAreWarned`.

### SC-CHECK-27. Outline rows and chapters disagree

Book 1 was cut to 24 chapters, but a row still says chapter 30, and a
beat moved up the table still says a later chapter than the rows below
it.

- The row for chapter 30 is a warning that the book has no such chapter.
  The row mapped to an earlier chapter than a row above it is a warning
  that names that row.

Spec: F-OUT-9, F-OUT-10. Covered by:
`SpecWarningCheckTests.OutlineRowsWithMissingOrOutOfOrderChaptersAreWarned`.

### SC-CHECK-28. An outline table that doesn't read as I meant

An outline row has `[[orrin-fell|Orrin]]` with an unescaped `|`, a row id
`069`, a status `maybe`, a header typed twice, and a second table that
has a `row` column but no `beat`.

- Each is a warning on its line: the extra cell and the unclosed `[[`,
  the bad row id (the row is ignored), the unknown status (read as
  `draft`), the ignored column, and "did you mean an outline table?".
  None is an error.

Spec: F-OUT-2, F-OUT-3, F-OUT-9, F-OUT-11, F-OUT-12. Covered by:
`SpecWarningCheckTests.AnOutlineTableThatDoesntReadAsMeantIsWarned`.

### SC-CHECK-29. A section comment pasted into an outline

The writer pasted `<!-- wolfden: from=a1:14.2 status=maybe -->` under a
heading in `outline.md`.

- It's a warning that wolfden comments do nothing in an outline. Its bad
  values aren't errors, and the outline's rows and links are still read.

Spec: F-SEC-10, F-GEN-2 (#80). Covered by:
`SpecWarningCheckTests.AWolfdenCommentInAnOutlineIsAWarningAndTheOutlineStillLoads`,
`SpecWarningCheckTests.AnOutlineParsedAsAnOutlineHasNoSectionCommentsOrCommentErrors`.

### SC-CHECK-30. `den.yaml` lists an author without a name

`den.yaml` says `authors: { wren-calder: Wren Calder }` instead of giving
the author a `name`.

- It's a warning in `den.yaml`. That author is skipped, and the den
  still loads. So is an author key that isn't a key, such as
  `Wren Calder`.
- A series that names the skipped author gets the F-CTR-2 warning
  (SC-CHECK-31), which says the author's identity in `den.yaml` isn't
  valid.

Spec: F-DEN-3, F-KEY-1, F-CTR-2. Covered by:
`SpecWarningCheckTests.AnAuthorsThatIsntAMappingOfNamedIdentitiesIsWarnedAndTheDenStillLoads`,
`SpecWarningCheckTests.ASkippedAuthorIsUnknownToTheContainersThatNameIt`.

### SC-CHECK-31. A series names an author `den.yaml` doesn't have

A series says `authors: [wren-calder, ash-morrow]`, and `den.yaml` lists
only `wren-calder`.

- `ash-morrow` is a warning that lists the den's authors.

Spec: F-CTR-2. Covered by:
`SpecWarningCheckTests.AContainerAuthorThatDenYamlDoesntListIsWarned`.

### SC-CHECK-32. A book file written by hand has no id

The writer made `book.md` and `universe.md` by hand, without ids.

- Each is the same no-id warning an entity gets, suggesting
  `wolfden check --fix`. In a final book, the warning says `--fix` won't
  add one until the book is unfrozen.

Spec: F-ID-2, F-ID-6, F-BOOK-1 (#100). Covered by:
`SpecWarningCheckTests.AContainerFileWithoutAnIdIsWarnedLikeAnEntity`,
`CheckCommandTests.FixAddsMissingIdsAndReportsEachFile`.

### SC-CHECK-33. I linked a shared folder of notes into my den

To share locations between two dens, the writer made
`universes/vael/entities/locations/shared` a symbolic link (or a
junction on Windows) to a folder outside the den.

- The notes in the linked folder aren't read, since Wolfden doesn't
  follow folder links, and `check` warns about the link: it isn't
  followed, so move the folder into the den or copy it there. A link
  somewhere Wolfden doesn't read, such as a `drafts/` folder at the den
  root, gets no warning.

Spec: F-LAY-2 (#102). Covered by:
`FolderLinkTests.ANoteReachedOnlyThroughALinkIsNotReadAndCheckSaysSo`,
`FolderLinkTests.LinksThatLoopAreSkippedByTheLoaderAndTheIndex`,
`FolderLinkTests.ALinkedFolderWhoseTargetWasDeletedIsWarnedAboutAndSkipped`.

### SC-CHECK-34. A canon section in a character who is still an idea

Wren is `status: idea`, but her `## Backstory` says `status=canon`, and
so does an entry in her `role`.

- Each is a warning that says it's read as `idea`, since a fact can't be
  more canon than what holds it. A subsection that only takes the status
  from its parent gets no warning of its own.

Spec: F-STAT-3, F-STAT-2. Covered by:
`SpecWarningCheckTests.ACanonFactInsideAnIdeaIsWarned`.

### SC-CHECK-35. I pinned a question that's still undecided

Orrin's `## Shoulder injury` says `status=undecided pin=true`, and a
`candidate` entry in `died` has `pin: true`.

- Each is a warning: pinned canon leaves them out until they're in
  canon. A pinned `draft` is fine, and a candidate subsection pinned only
  through its parent gets no warning of its own.

Spec: F-PIN-3, F-PIN-2. Covered by:
`SpecWarningCheckTests.APinnedFactThatsOutOfCanonIsWarned`,
`SpecWarningCheckTests.APinnedSectionInAnIdeaIsWarnedOnceForItsStatus`.

### SC-CHECK-36. I pinned the change log

The writer put `pin=true` on a character's `## Change log`, and on the
`## Changelog 2026` of `universe.md`.

- Each is a warning: bookkeeping is never pinned canon, so the pin does
  nothing. A section that matches a bookkeeping pattern but has
  `canon=true` can be pinned.

Spec: F-SEC-12. Covered by:
`SpecWarningCheckTests.PinningABookkeepingSectionIsWarned`.

### SC-CHECK-37. A story note filed inside my session notes

Under `## Session notes`, the writer added `### The reef` with
`canon=true` to keep it in the story.

- It's a warning: a subsection of a bookkeeping section is bookkeeping,
  so `canon=true` does nothing there, and the message says to move it
  out.

Spec: F-SEC-7, F-SEC-12. Covered by:
`SpecWarningCheckTests.CanonTrueUnderABookkeepingSectionIsWarned`.

### SC-CHECK-38. A thread paid off before it's planted

A thread says `planted: ["a1:5", "a1:3"]` and `paid_off: "a1:2"`.

- It's a warning on `paid_off` that names the earliest planted position.
  A `candidate` entry for the payoff, a temporal list, or a position that
  doesn't resolve isn't compared. If either side names only a book
  (`paid_off: "a1"`), only the books are compared, so a payoff later in
  the same book isn't warned.

Spec: types.md F-CORE-2. Covered by:
`SpecWarningCheckTests.AThreadPaidOffBeforeItsPlantedIsWarned`,
`SpecWarningCheckTests.APaidOffIsComparedOnlyWithPlantedPositionsThatAreSet`.

### SC-CHECK-39. An event that ends before it starts

The storm says `when: "2291-05-14"` and ends "the spring before", with
`sort: [2291, 4]`.

- It's a warning on `ends`. Dates are compared only over the numbers
  both have, so `ends: "2291-05"` with `when: "2291-05-14"` is fine, and
  a date without a sort key is skipped.

Spec: types.md F-CORE-3. Covered by:
`SpecWarningCheckTests.AnEventThatEndsBeforeItStartsIsWarned`,
`SpecWarningCheckTests.AnEventsDatesAreComparedOnlyOverTheNumbersBothHave`.

### SC-CHECK-40. A universe of thousands of standalone books

The writer keeps a long-running anthology as one universe, with 2,000
standalone books and no series, so every book's folder is in the same
`books/` folder.

- `check` and `index` take about as long per book as in a den of the
  same size whose books are spread over series: seconds, not minutes.
  Twice as many books take about twice as long.

Spec: design.md §7 (#119). Covered by:
`DenStructureTests.APathLookupListsEachFolderOnTheWayAboutOnce` (Core),
`PerformanceTests.AUniverseOfManyStandaloneBooksIsCheckedAndRebuiltQuickly`.

### SC-CHECK-41. An empty value typed right up against the bracket

The writer types `died: {value: drowned, from:}`, meaning a death with no
`from` yet, or ends an alias list with `[Voss, Orrin:]`. YAML readers
don't agree on a colon that runs straight into `}` or `]`: most take
`from` as a key with an empty value, but Wolfden's reader would keep the
colon and read the key `from:`.

- It's one F-YAML-1 error on that line that quotes `from:` and says to
  add a space after the colon, or to quote the text if the colon is part
  of it. The note is left out until it's fixed, and Wolfden won't edit
  the file. With a space (`from: }`), or before a comma (`{from:, value:
  drowned}`), every reader agrees and there's no diagnostic.

Spec: F-YAML-1. Covered by:
`QuotingCorpusTests.APlainScalarEndingInAColonBeforeAFlowIndicatorIsAnError`,
`QuotingCorpusTests.ColonsThatEveryReaderAgreesOnAreFine`,
`ContinuityCheckTests.AnEmptyFlowValueWithNoSpaceIsOneErrorAndTheNoteIsLeftOut`.

### SC-CHECK-42. Where a chapter takes place

The writer sets `setting: "[[kessan-reach]]"` in the series'
`chapter_defaults` and gives the prologue its own two places. One
chapter lists a character there by mistake. In an older den, a chapter
says `setting: Kessan Reach` as plain text, written before the core
pack had a `setting` field; in another, the den's own pack defined a
`setting` chapter field of its own before core did.

- Chapters without their own `setting` inherit the series' place, and
  the prologue lists its two. `setting: []` means no place.
- The character link gets the F-LINK-11 warning: `setting` is a ref to
  location.
- The plain-text value is an F-CHAP-3 warning and is ignored, so the
  chapter inherits.
- With the den's own definition, it's used and its values stay valid.
  The clash is one F-CHAP-2 warning, on the den's `chapter.yaml`.

Spec: types.md F-CHAP-2, F-CHAP-3, F-CHAP-5, F-PACK-11, §9.2 (#226).
Covered by: `TypeEffectsTests.ASettingLinksAChapterToItsPlacesAndInherits`,
`TypeEffectsTests.ASettingWrittenAsPlainTextBeforeCoreHadOneIsIgnoredWithAWarning`,
`PackLoadingTests.ADenLocalSettingWinsOverTheCoreOne`.

### SC-CHECK-43. A `chapter_heading` that doesn't read as I meant

The writer adds `chapter_heading` to the series and its books (#227).
The series writes `chapter_heading: pov`, without the brackets; Ember
lists `pov` twice, `pvo` by mistake, and `{ field: when }`; Ash writes
`chapter_heading: [[], ~]`; A Quiet Moon writes `chapter_heading: {
field: pov }`.

- The series' `pov` is a one-item list, `[pov]`, with no warning.
- Each of the others is an F-CTR-6 warning, and `wolfden check` still
  passes. A Quiet Moon's mapping is ignored, so it inherits the
  universe's list, if any.
- In Ember, the repeated `pov` and the mapping are ignored. `pvo` isn't
  a chapter field: the warning says so and lists the chapter fields, and
  the name is kept, so a chapter that writes its own `pvo` shows it.
- Ash's list has no entries left, so it counts as not set and Ash
  inherits the series' `[pov]`, unlike `chapter_heading: []`, which
  turns headings from fields off.

Spec: format.md F-CTR-6 (#227). Covered by:
`ChapterHeadingTests.ProblemsWithTheKeyAreWarnings` (Core).

### SC-CHECK-44. An early default runs ahead of every chapter that inherits it

The series sets `chapter_defaults: { pov: "[[mara-voss]]" }`, but Mara
doesn't enter canon until Book 2, chapter 2. Every chapter before that
inherits the default.

- With `spoiler_checks: true`, there's one spoiler warning, on the file
  that holds the default, not one per chapter. It names the chapters in
  story order, earliest first.
- Past five chapters, it names the first five and counts the rest:
  "after chapters ember:1, ember:2, ember:3, ember:4, ember:5 and 3
  more". Fixing the one default fixes them all.

Spec: types.md F-CHAP-4, F-LINK-13. Covered by:
`LinkCheckTests.ADefaultIsOneSpoilerWarningThatNamesItsChapters`,
`LinkCheckTests.ADefaultAheadOfMoreThanFiveChaptersNamesTheFirstFive`.

## RT: saving a note keeps what the writer wrote

Background: the files are the writer's, and Wolfden is only one of the
tools that edits them. When Wolfden or an AI assistant saves a note, it
changes only what the writer asked for and keeps everything else byte for
byte: comments, key order, quoting, blank lines, sections it doesn't
know, line endings, a byte order mark, and the final newline (format.md
§12). The sample dens under `samples/` are the test fixtures, so a new
sample is checked too (#18). That's why the RT scenarios use the
public-domain Dracula sample instead of the invented example world.

### SC-RT-1. I open a note and save it without changing anything

The writer opens a note and saves it, or a tool reads it and writes it
back, with no edits. Some of the writer's notes were saved on Windows,
with CRLF line endings, a byte order mark, or no newline at the end.

- The file's bytes are exactly the same as before, so version history
  shows no change.

Spec: F-RT-3, F-RT-1, F-ENC-2, F-ENC-3, F-ENC-4. Covered by:
`RoundTripTests.RepoFileRoundTrips`,
`RoundTripTests.SampleFileRoundTripsWithOtherEncodings`,
`RoundTripTests.FixtureRoundTrips`.

### SC-RT-2. I change one field and the rest of the frontmatter stays as I wrote it

The writer changes the Count's `role`, his quoted `from`, or a book's
`status` that sits just above a block of YAML comments.

- Only that value changes. The other fields, their order, the YAML
  comments, the quotes (`"dracula:1"` becomes `"dracula:2"`, still
  quoted), and the flow lists and mappings are untouched.

Spec: F-RT-1, F-RT-2. Covered by:
`SampleEditTests.ChangingOneFieldKeepsCommentsFlowValuesAndTheOtherFields`,
`SampleEditTests.ChangingAQuotedValueKeepsItsQuotes`,
`SampleEditTests.ChangingATopLevelValueKeepsTheYamlCommentsAroundIt`,
`SampleEditTests.ChangingANestedValueChangesOnlyThatValue`,
`SampleEditTests.ChangingDenYamlValuesKeepsTheRest`.

### SC-RT-3. I add a field the note didn't have

The writer gives the Count a `height`. His last field has a YAML comment
above it.

- The new field goes on its own line after the last field, so the comment
  stays with the field it describes. Nothing else moves.

Spec: F-RT-4, F-RT-2. Covered by:
`SampleEditTests.ANewFieldGoesAfterTheLastFieldAndItsComment`,
`SampleEditTests.ANewTopLevelKeyGoesAfterTheLongChaptersList`,
`SampleEditTests.ANewNestedDenYamlKeyGoesAfterItsSiblings`.

### SC-RT-4. I rewrite one section

The Count has two `## Appearance` sections, the second one for London
from chapter 13. The writer rewrites the second one.

- Only the second section's text changes. Its heading, its
  `<!-- wolfden: from=dracula:13 -->` comment, the first Appearance, and
  every other section stay as they were. Rewriting a subsection leaves its
  parent's text alone.

Spec: F-RT-2, F-SEC-4, F-SEC-6. Covered by:
`SampleEditTests.EditingOneOfTwoSameNamedSectionsLeavesTheOtherAlone`,
`SampleEditTests.EditingASubsectionKeepsItsCommentAndItsParent`,
`SampleEditTests.EditingASectionWithAListKeepsTheSectionsAroundIt`.

### SC-RT-5. I add a section at the end of a note

The writer adds `## Open questions` to Lucy's note.

- The new section goes after everything else, with a blank line before
  it. Everything above it is unchanged.

Spec: F-RT-2. Covered by:
`SampleEditTests.AddingASectionAtTheEndKeepsEverythingBeforeIt`.

### SC-RT-6. I edit a note that was saved on Windows

The note has CRLF line endings and a byte order mark, and maybe no
newline at the end. The writer changes a field, adds one, rewrites a
section, and adds a section at the end.

- The new and changed lines end in CRLF like the rest, the byte order
  mark stays, and the file still ends the way it did.

Spec: F-RT-1, F-ENC-2, F-ENC-3, F-ENC-4. Covered by:
`SampleEditTests.EditsOnAWindowsSavedSampleKeepCrlfBomAndFinalNewline`,
`SampleEditTests.ListKeyAndSectionEditsOnAWindowsSavedSampleKeepCrlfBomAndFinalNewline`,
`CollectionEditTests.CollectionEditsKeepCrlfBomAndMissingFinalNewline`,
`SectionEditTests.SectionEditsKeepCrlf`.

### SC-RT-7. I add an alias, or a new value for a field that changes over time

The writer adds `Miss Lucy` to Lucy's `aliases`, or adds another entry to
a temporal field's list, or another point-of-view character to a
chapter's `pov` list.

- The new item goes at the end of the list, in the list's own style: on
  the same line in a flow list (`[Lucy, Miss Lucy]`), or on a line of its
  own at the same indent in a block list. A new temporal entry is written
  like the entry before it (`- { value: ..., from: "dracula:27" }`). The
  other items, their quoting, and any comments are unchanged.

Spec: F-RT-2, F-TMP-7. Covered by:
`SampleEditTests.AnAliasGoesAtTheEndOfTheFlowList`,
`SampleEditTests.ANewTemporalEntryGoesAfterTheLastOneInItsStyle`,
`SampleEditTests.ANestedBlockListGetsANewItemAtItsIndentAndQuoting`,
`SampleEditTests.ANewChapterIsABlockMappingLikeTheOthers`,
`CollectionEditTests.AnItemGoesAtTheEndOfAFlowList`,
`CollectionEditTests.AnItemInABlockListGetsItsOwnLineAfterTheItemBeforeIt`,
`CollectionEditTests.ANewTemporalEntryCopiesItsNeighborsStyle`.

### SC-RT-8. I remove an alias, or one value of a field that changes over time

The writer drops `the Count` from the Count's aliases, or deletes one
entry of Lucy's `role`.

- Only that item goes: in a flow list, the item and the comma next to
  it; in a block list, the item's line, with a comment on that line. The
  other items, and comment lines above the removed one, are unchanged.

Spec: F-RT-2. Covered by:
`SampleEditTests.RemovingAnAliasKeepsTheOthers`,
`SampleEditTests.RemovingATemporalEntryRemovesOnlyItsLine`,
`CollectionEditTests.RemovingAFlowItemTakesOneSeparatorWithIt`,
`CollectionEditTests.RemovingABlockItemRemovesItsLinesAndKeepsTheCommentsAboveIt`.

### SC-RT-9. I change when a fact becomes known, or pin it

Lucy's death is written `died: { value: ..., from: "dracula:12" }`. The
writer moves its `from` to chapter 13, or unpins the Count's `species`.

- Only that value changes, inside the braces. The date, its quotes, the
  other keys, and the comment above the field stay as they were. A new
  key, such as `status`, goes at the end of the braces.

Spec: F-RT-2, F-TMP-8. Covered by:
`SampleEditTests.ChangingWhenAFactIsKnownChangesOnlyItsFrom`,
`SampleEditTests.UnpinningAFactKeepsTheCommentAboveIt`,
`CollectionEditTests.ChangingOneKeyOfAFlowMappingChangesOnlyItsValue`,
`CollectionEditTests.ANewKeyGoesAtTheEndOfAFlowMapping`,
`CollectionEditTests.ChangingOrRemovingAKeyInAMultiLineFlowMappingKeepsItsComments`.

### SC-RT-10. I remove a field

The writer removes the Count's `skills` and `born` fields, or
`spoiler_checks` from `den.yaml`.

- The field's lines go, with any comments inside its value and on its
  last line. Comment lines above it, and every other field, stay.

Spec: F-RT-2. Covered by:
`SampleEditTests.RemovingAFieldRemovesOnlyItsLine`,
`SampleEditTests.RemovingADenYamlKeyKeepsTheRest`,
`CollectionEditTests.RemovingAKeyRemovesItsLinesAndKeepsTheCommentAboveIt`,
`CollectionEditTests.RemovingAKeyWithABlockValueRemovesTheCommentsInsideIt`,
`CollectionEditTests.RemovingAKeyOfAFlowMappingKeepsTheRest`.

### SC-RT-11. I rename a section

Lucy has three `## Appearance` sections. The writer renames the second
one to `Appearance in death`.

- Only that heading's text changes. Its `<!-- wolfden: from=dracula:15 -->`
  comment, its text, and the other two Appearance sections are untouched.
  Closing `#`s, a setext underline, or a trailing HTML comment on the
  heading line stay.
- When the app or an AI assistant makes the rename, links to the old
  heading in other notes (`[[lucy-westenra#Appearance]]`) are rewritten
  in the same change (F-RT-5). That part comes with the write tools (#21).

Spec: F-RT-2, F-RT-5, F-SEC-3, F-SEC-4. Covered by (the one-file edit):
`SampleEditTests.RenamingOneOfThreeSameNamedSectionsLeavesTheOthersAlone`,
`SectionEditTests.RenamingChangesOnlyTheHeadingText`,
`SectionEditTests.RenamingKeepsTheHeadingsMarkup`.

### SC-RT-12. I remove a section

The writer removes Lucy's `### The Un-Dead` subsection, or her last
section, `### Rest`.

- The section goes with its comment, text, and subsections. The spacing
  between the sections around it doesn't change, and the file still ends
  the way it did, with or without a final newline.

Spec: F-RT-2, F-ENC-4. Covered by:
`SampleEditTests.RemovingASubsectionKeepsItsParentAndSiblings`,
`SampleEditTests.RemovingTheLastSectionKeepsTheFileEnding`,
`SectionEditTests.RemovingASectionKeepsTheSpacingAroundIt`,
`SectionEditTests.RemovingASectionRemovesItsSubsections`,
`SectionEditTests.RemovingTheLastSectionRemovesTheBlankLinesBeforeIt`.

### SC-RT-13. I move a section

The writer moves Lucy's `## Relationships` above `## Want`.

- The section moves with its comment, text, and subsections, among its
  siblings. Each section's text is unchanged, the blank lines between
  sections stay where they were, and the file ends the way it did.

Spec: F-RT-2, F-ENC-4. Covered by:
`SampleEditTests.MovingASectionKeepsItsCommentAndTheSpacing`,
`SectionEditTests.MovingASectionKeepsItsTextAndTheSpacingBetweenSections`,
`SectionEditTests.MovingTheLastSectionKeepsTheFilesEnding`,
`SectionEditTests.MovingASectionToTheEndTakesItsSubsections`.

### SC-RT-14. An edit that can't be made cleanly is refused

The writer, or an AI assistant, asks for an edit that Wolfden can't make
without reformatting something else: removing a flow list item whose
comma sits before a comment on the same line, removing an explicit
`? key`, editing a list that puts its commas at the start of lines,
removing or moving a section so that its neighbors (or the lead) would
run together into one heading, removing a section that holds a link
reference definition (`[map]: https://…`) other sections may use, moving
sections that define the same reference label (the first one wins),
moving a flow list item when a comment sits between items rather than
on an item's own line, or treating a list as a mapping.

- The edit is refused with a message that says why, and the file is left
  exactly as it was. Wolfden never rewrites the rest of the file to make
  an edit fit.

Spec: F-RT-1, F-RT-2. Covered by:
`SampleEditTests.TreatingAListAsAMappingIsRefusedAndChangesNothing`,
`CollectionEditTests.RemovingAFlowItemThatWouldTakeACommentWithItIsRefused`,
`CollectionEditTests.RemovingTheFirstKeyOfAListItemIsRefusedWhenItWouldMoveACommentOrEmptyTheItem`,
`CollectionEditTests.RemovingAnExplicitBlockKeyIsRefused`,
`CollectionEditTests.RemovingAnExplicitFlowKeyIsRefused`,
`CollectionEditTests.RemovingTheFlowKeyBeforeAnExplicitKeyIsRefused`,
`CollectionEditTests.AddingToOrRemovingFromALeadingCommaFlowListIsRefused`,
`ListItemMoveTests.AFlowMoveIsRefusedWhenACommentIsntOnAnItemsLine`,
`CollectionEditTests.EmptyingACollectionIsRefusedWhenACommentAboveItMayBeAboutTheParent`,
`SectionEditTests.RemovingASectionThatWouldMergeTheTextAroundItIsRefused`,
`SectionEditTests.MovingASectionThatWouldMergeWithTheTextBeforeItIsRefused`,
`SectionEditTests.RemovingASectionThatWouldMakeAStrayHeadingPartOfTheLeadIsRefused`,
`SectionEditTests.MovingASetextSectionAgainstTheLeadIsRefused`,
`SectionEditTests.RemovingASectionWithALinkReferenceDefinitionIsRefused`,
`SectionEditTests.MovingSectionsThatDefineTheSameLinkReferenceIsRefused`.

### SC-RT-15. I remove the last point-of-view character from a chapter, or from the defaults

`book.md` gives chapter 27 four `pov` characters as a block list, and
chapter 1 one as a flow list. The writer removes them one by one until
none are left. Later, the writer removes the only `pov` from a series'
`chapter_defaults`.

- Each chapter ends with `pov: []`, "none here", block and flow alike, so
  it doesn't fall back to the book's `chapter_defaults`. The series'
  `chapter_defaults` also ends with `pov: []`, so its books stop
  inheriting a narrator from the universe. A comment on the `pov:` line
  stays. Every other field that loses its last item is left as `key:`
  with no value. Inheriting again is a separate action, clearing the
  field.

Spec: F-RT-6, F-CHAP-5. Covered by:
`SampleEditTests.EmptyingAChaptersPovListLeavesNoneHere`,
`CollectionEditTests.RemovingTheLastItemOfABlockChapterListLeavesAnEmptyList`,
`CollectionEditTests.RemovingTheLastItemOfAFlowChapterListLeavesAnEmptyList`,
`CollectionEditTests.AnEmptiedChapterListKeepsCrlf`,
`CollectionEditTests.RemovingTheLastChapterDefaultLeavesAnEmptyList`,
`CollectionEditTests.AnEmptiedChapterDefaultKeepsCrlf`.

### SC-RT-16. I clear the last of a container's chapter defaults

A book's `chapter_defaults` is a block mapping with a comment line,
`# pov for the found-document chapters`, indented above its `pov`. The
writer clears its keys one by one. Later, the writer sets a new default.

- While another key is left, a comment line above the cleared key stays.
  When the last key goes, every comment line inside the mapping goes
  with it, above or below that key, and `chapter_defaults:` is left with
  no value. Clearing the keys in any order ends the same. A comment on
  the `chapter_defaults:` line stays, and so does one after the mapping
  at or left of the `chapter_defaults` key. The same holds for a mapping
  or list at any depth: a chapter's `fields`, an `x-` key's nested
  mapping, or a block list emptied of its last item.
- When an AI assistant makes the edit, the tool's effects say how many
  comment lines went, since one may have been a commented-out value. The
  den's history keeps the old text from the last checkpoint.
- If a comment above the last key is at or left of the parent key, and
  left of the removed key, it might be a note on `chapter_defaults`
  itself, so the edit is refused and the file is unchanged.
- The new default goes on the line after `chapter_defaults:`, after any
  comment lines indented under it.

Spec: F-RT-6, F-RT-2. Covered by:
`CollectionEditTests.ClearingChapterDefaultsKeyByKeyTakesTheCommentsInsideAndASetStartsClean`,
`CollectionEditTests.ClearingTheFirstOfTwoKeysKeepsTheCommentAboveIt`,
`CollectionEditTests.EmptyingANestedMappingAtAnyDepthTakesTheCommentsInsideIt`,
`CollectionEditTests.EmptyingANestedBlockListTakesTheCommentsInsideIt`,
`CollectionEditTests.EmptyingAChapterDefaultListTakesTheCommentsInsideIt`,
`CollectionEditTests.EmptyingACollectionIsRefusedWhenACommentAboveItMayBeAboutTheParent`,
`CollectionEditTests.EmptyingACollectionTakesTheCommentsAfterItThatAreStillInsideIt`,
`CollectionEditTests.ClearingAMappingKeyByKeyInAnyOrderEndsTheSame`,
`CollectionEditTests.EmptyingAListItemByItemInAnyOrderEndsTheSame`,
`CollectionEditTests.OnlyCommentsThatGoWithAnEmptiedCollectionAreCounted`,
`CollectionEditTests.ANewKeyUnderAnEmptyKeyGoesAfterTheCommentsIndentedUnderIt`,
`CollectionEditTests.EmptyingANestedMappingInAYamlFileKeepsItsLineEndings`,
`WriteToolTests.EmptyingAMappingOrListSaysWhichCommentLinesWentWithIt`,
`RenameEntityToolTests.DroppingTheLastAliasSaysWhichCommentLinesWentWithIt`.

### SC-RT-17. I reorder a list, or change one item, and its comments stay with it

Chapter 27's `pov` is a block list: `# the journal chapters` heading
it, `- "[[mina-harker]]"  # main`, then `# letters` above Jonathan, and
Seward. The writer moves Mina to the end, then Jonathan to the top, then
changes Jonathan to Lucy. A note's `tags` are a flow list with each item
on its own line and a comment after some of them. Lucy's `role` is a
list of dated entries.

- Mina's lines move: her line with its `# main`, and any comment lines
  indented under her. `# the journal chapters`, above the first item,
  heads the list and stays at the top; `# letters` moves with Jonathan.
  Blank lines, a comment line left of the dashes, and a commented-out
  item after the last one stay where they were. Moving an item back
  gives back the same bytes, unless it went to the top, where its own
  heading joins the list's. A move that changes the first item says, in
  the effects and the web page's notice, which comment lines stayed at
  the top and which now head the list. CRLF lines stay CRLF.
- Dated entries are never reordered (F-TMP-7): a move of one is refused,
  since entries with the same `from` are read in file order (F-TMP-5)
  and a move could unseat pinned canon. The web page has no control to
  move an entry.
- In the flow list the item moves with the comment on its line, and the
  commas stay put, so the last item still has none. A comment line of
  its own between flow items can't be placed for sure, so that move is
  refused and the file is unchanged.
- Changing Jonathan to Lucy changes only the link's characters: the
  comment on the item, and the comment lines above it, stay. An item
  written over several lines is replaced whole, and the result says
  which comments inside it went. A list given as one item is refused.

Spec: format.md F-RT-7, F-TMP-7, F-RT-1, F-RT-2 (#197). Covered by:
`ListItemMoveTests` (Core),
`FieldEditEndpointTests.ListItemsAreAddedRemovedAndReorderedInTheListsOwnStyle`,
`ChapterFieldEndpointTests.MovingOrReplacingAnItemKeepsItsComments` (API),
`ListItemToolTests` (MCP).

## CLI: working with a den from the command line

Background: besides `check`, the CLI has `init`, `index`, `query`, and
`new-id` (design.md §7). Every command finds the den from the current
folder unless `--den` names one, prints one JSON document with `--json`,
and exits 2 when it couldn't do its job.

### SC-CLI-1. I start a new den

The writer runs `wolfden init` in an empty folder, with
`--universe the-long-dark`.

- The folder gets a `den.yaml` and `universes/the-long-dark/universe.md`
  with an id and the name "The Long Dark". `wolfden check` then reports
  no errors or warnings, and `wolfden query` shows an empty view. Running
  `init` again, inside another den, or in a folder that already holds a
  den a few levels down (say `init` in `writing/` when
  `writing/the-long-dark/` is a den), changes nothing, says which den is
  in the way, and exits 2.

Spec: F-DEN-1, F-ID-6, F-LAY-1. Covered by:
`InitCommandTests.ANewDenWithAUniversePassesCheckAndCanBeQueried`,
`InitCommandTests.ANewDenWithoutAUniverseIsJustDenYaml`,
`InitCommandTests.ADenInsideADenIsRefused`,
`InitCommandTests.ADenAroundADenIsRefused`.

### SC-CLI-2. I delete the index, or it gets corrupted

The writer deletes `.wolfden/` (or a sync conflict garbles it) and runs
`wolfden index`.

- The index is rebuilt from the files with the same content, the command
  says how many universes, entities, links, and files it holds, and no
  file outside `.wolfden/` changes.

Spec: F-LAY-2, F-ID-6, design.md §7. Covered by:
`IndexCommandTests.ItRebuildsTheIndexAndSaysWhatItHolds`,
`IndexCommandTests.ADeletedIndexIsRebuiltWithTheSameContent`.

### SC-CLI-3. What does the reader know at Book 2, chapter 14?

Before drafting `a2:15`, the writer runs `wolfden query --at a2:14` to
see canon so far, or adds `--entity "Mara Voss"` to see one character.

- Each field shows the value in effect at `a2:14`, marked with its
  `from`. A character the reader meets later isn't listed, and nor are
  sections that start later. `--pinned` shows only the pinned facts, and
  `--brief` one line per entity.

Spec: F-VIEW-1, F-VIEW-2, F-VIEW-3, F-TMP-5. Covered by:
`QueryCommandTests.AFieldShowsTheValueInEffectAtThePosition`,
`QueryCommandTests.ACharacterIsLeftOutBeforeTheReaderMeetsThem`,
`QueryCommandTests.PinnedShowsOnlyPinnedFacts`.

### SC-CLI-4. Notes I wrote by hand have no ids

The writer made a few notes in a text editor without ids, and `wolfden
check` warns about each one. They run `wolfden check --fix`.

- Each container and entity file without an id gets one as its first
  frontmatter key, after any opening comment lines, and nothing else in
  it changes, line endings included. Each change is listed, then the
  check runs on the fixed files. Files in a final book and files with
  errors are left alone, and the final book's files are listed as not
  fixed.

Spec: F-ID-2, F-ID-6, F-BOOK-1, F-RT-2. Covered by:
`CheckCommandTests.FixAddsMissingIdsAndReportsEachFile`,
`IdTests.TheIdGoesFirstAfterOpeningCommentLines`.

### SC-CLI-5. I run a command from deep inside the den

The writer is in `universes/vael/series/a/books/a1/entities/characters/`
and runs `wolfden check` or `wolfden query`.

- The command uses the den whose `den.yaml` is in the nearest folder
  above. Outside any den, `query` and `index` say there's no den and exit
  2.

Spec: F-LAY-1. Covered by:
`CheckCommandCurrentFolderTests.FromAFolderInsideADenItFindsTheDenAbove`,
`QueryCommandCurrentFolderTests.FromAFolderInsideADenItFindsTheDenAbove`.

### SC-CLI-6. I edit a note, then query again

The writer runs a query, edits a character's field in a text editor, adds
a new location file, and runs the query again.

- The second query shows the edit and the new location. The index is
  rebuilt only when the files have changed since it was built.

Spec: design.md §7. Covered by:
`QueryCommandTests.AQueryAfterAnEditSeesTheEdit`,
`QueryCommandTests.AQueryAfterANewNoteSeesTheNote`,
`FreshnessTests.EnsureUpToDateRebuildsAStaleIndexAndThenItIsCurrent`,
`FingerprintTests.AnEditToAFileTheIndexDoesntReadDoesntMakeItStale`.

### SC-CLI-7. I need an id for a note I'm writing by hand

The writer runs `wolfden new-id` and pastes the result into a new file's
frontmatter.

- It prints one valid id, a ULID in canonical form, or as many as asked
  for, all different.

Spec: F-ID-1, F-ID-5. Covered by:
`NewIdCommandTests.ItPrintsOneValidId`.

### SC-CLI-8. A note Wolfden can't read

The writer keeps a symbolic link to a note that has since moved, so the
link points nowhere. Another time, a sync tool deletes a note just as
`wolfden check` reads the den, or another program holds the note open, or
its permissions don't let the writer read it.

- `check`, `index`, and `query` still work. They read the den as if that
  note weren't there, so links to it are reported as they would be if it
  were deleted. Once the note can be read again, the next query sees that
  the files changed and rebuilds the index with it.

Spec: design.md §7 (#112). Covered by:
`UnreadableFileTests.AFileThatCantBeReadIsCheckedAsIfItWerentThere` (Core),
`UnreadableFileTests.APackFileThatCantBeReadIsCheckedAsIfItWerentThere` (Core),
`FolderLinkTests.ALinkedFolderWhoseTargetWasDeletedIsWarnedAboutAndSkipped`,
`UnreadableFileTests.BrokenAndLoopingFileLinksAreAbsentToCheckAndTheIndex`,
`UnreadableFileTests.ANoteReplacedByABrokenLinkIsCheckedAndIndexedAsIfItWereDeleted`,
`UnreadableFileTests.ANoteThatCantBeReadIsLeftOutAndTheIndexIsStaleOnceItCanBe`,
`UnreadableFileTests.ANoteThatCantBeReadIsLeftOutAndTheCommandsStillWork` (CLI).

### SC-CLI-9. A folder Wolfden can't list

The writer changes the permissions of a book's folder, or of a
`characters/` folder, so their own account may no longer list what's in
it. Another time, a sync tool deletes a series folder just as
`wolfden check` reads the den.

- `check`, `index`, and `query` still work. They read the den as if the
  folder weren't there: a book folder that can't be listed isn't reported
  as a book folder without its `book.md`, and links to the notes in it are
  reported as they would be if it were deleted. Once the folder can be
  listed again, the next query sees that the files changed and rebuilds
  the index with what's in it.

Spec: design.md §7 (#116). Covered by:
`UnlistableFolderTests.AFolderDeletedAfterItsParentWasListedIsCheckedAsIfItWerentThere` (Core),
`UnlistableFolderTests.AFolderThatCantBeListedIsCheckedAndIndexedAsIfItWereDeleted`,
`UnlistableFolderTests.AFolderThatCantBeListedIsntWarnedAboutByName`,
`UnlistableFolderTests.AFolderTheLoadCantListAfterTheTreeWasTakenMakesTheIndexStale`,
`UnlistableFolderTests.AFolderDeletedAfterTheLoadReadItLeavesTheIndexStale`,
`UnlistableFolderTests.AFolderThatCantBeListedIsLeftOutAndTheCommandsStillWork` (CLI).

### SC-CLI-10. I query a den with two universes

The writer's den has two universes, `vael` and `orn`. They run `wolfden
query`, then `wolfden query --at fen:1` (`fen` is a book of `vael`), then
`wolfden query --at common:1`, where both universes have a book
`common`.

- With no universe and no position, the query says the den has two
  universes, lists their keys, and exits 2. `--at fen:1` settles it:
  `fen` is only in `vael`, so it shows `vael`'s canon as of `fen:1`.
  `common:1` doesn't settle it, so the query asks for `--universe` and
  exits 2, as it does for a book in neither universe. `--universe vael`
  (or `vael` as a plain argument) always works. `--universe '*'` is
  refused, since a canon view is of one universe.

Spec: F-POS-5, design.md §7 (maintainer decision, 2026-10-23). Covered by:
`QueryCommandTests.ADenWithSeveralUniversesNeedsOneNamed`,
`QueryCommandTests.APositionSettlesTheUniverseWhenItsBookIsInOnlyOne`.

### SC-CLI-11. I check one universe of my den

The writer's den has two universes, and they're working in `vael`. They
run `wolfden check --universe vael`.

- It reports `vael`'s findings and the den-level ones (`den.yaml`, packs),
  and nothing from `orn`, whose notes it doesn't read. A note copied
  from `vael` into `orn` with its id is still a duplicate (F-ID-3, naming
  the `vael` copy), and links to it break, as in a whole-den check: the
  other universes' ids come from the index. A broken `den.yaml` is still
  reported, with a note that ids weren't compared across universes,
  since there are no notes to compare with; no index is built. Without
  `--universe`, or with `--universe '*'`, it checks the whole den. A
  universe the den doesn't have, or `--fix` with `--universe`, exits 2;
  an unknown universe is reported before the index is brought up to
  date, and leaves `.wolfden/` alone. With `--no-index`, nothing is
  written under `.wolfden/`: ids aren't compared across universes, and
  the same note says so.

Spec: F-DEN-1, F-ID-3, design.md §7 (maintainer decision, 2026-10-23; #130). Covered by:
`CheckUniverseTests.WithAUniverseOnlyItsFindingsAreReported`,
`CheckUniverseTests.AnIdSharedWithAnotherUniverseIsFound`,
`CheckUniverseTests.ABrokenDenYamlIsStillReported`,
`CheckUniverseTests.AnUnknownUniverseOrFixWithAUniverseExitsTwo`,
`CheckUniverseTests.AnUnknownUniverseOrABrokenDenYamlLeavesTheIndexAlone`,
`CheckUniverseTests.NoIndexLeavesTheIndexAloneAndSaysIdsWerentCompared`.

### SC-CLI-12. I want history from the start

The writer runs `wolfden init kestrel-notes --universe the-long-dark
--git`. Another time they run `wolfden init vael-notes --git` inside
`writing/`, which is already a git repository. A third time, git isn't
installed.

- With git, the new folder is also a git repository with a
  `.gitignore` for `.wolfden/`, and its history starts with a first
  checkpoint, "Start of history", of `den.yaml` and the universe; the
  output lists each step, and the den checks clean. With
  `--no-checkpoint`, nothing is committed yet. Inside `writing/`, no new
  repository is made: the output names the repository the den is in,
  and its history is scoped to the den's files there. Without git, the
  den is still made, the output says the repository wasn't and how to
  turn history on later, and `init` exits 0. If git has no name or
  email, the den is still made, but its history isn't turned on, and
  `init` says how to give them (SC-HIST-15) and exits 2. Without
  `--git`, the den is a plain folder.

Spec: F-LAY-1, design.md §6, §7 (maintainer decision, #57, #122, #195). Covered by:
`InitGitTests.WithGitTheNewDenIsARepositoryWithHistory`,
`InitGitTests.WithNoCheckpointTheNewDensRepositoryHasNothingCommitted`,
`InitHistoryTests.ANewDenWithoutANameOrEmailIsMadeButItsHistoryIsntTurnedOn`,
`InitGitTests.InsideARepositoryItSaysSoAndMakesNoNewOne`,
`InitGitTests.WithoutGitInstalledTheDenIsStillMadeAndItExplains`,
`InitGitTests.WithoutTheFlagTheDenIsAPlainFolder`.

### SC-CLI-13. I copy a character to start another like her

The writer runs `wolfden copy "Count Dracula" "Count Varga"`, then
`wolfden copy "Mr. Swales" "Young Swales" --to dracula/aftermath` for a
sequel book.

- The copy is `count-varga.md` beside the original, with a new id, the
  new name, and no aliases, and a whole-den `check` finds no duplicate
  id. The output names the new file and says what the copy means, as
  `copy_entity` does (SC-MCP-28). `--to` takes a universe, or
  `<universe>/<key>` for one of its series or books
  (`<universe>/book:<key>` when a series shares the key). With one
  universe, `--to aftermath` (or `--to book:aftermath`) is enough, but
  `--to dracula` names both the universe and its book, so it's refused,
  suggesting `dracula/` or `dracula/book:dracula`, and with a series
  `dracula` too, `dracula/series:dracula` as well. With two universes, a bare book key isn't a
  universe, and the error says how to name it. A `den.yaml` that
  doesn't load is said as such. `--json` prints the write result.
- A copy into a final book, or one that would take another note's
  links without `--allow-relinks`, exits 2 and writes nothing. Errors
  name the command's options and commands (`--allow-relinks`,
  `--slug`, `wolfden query`), not the MCP tool's parameters, and a copy
  named "Count allow_relinks" is reported by that name, in text and
  JSON. The refusal for a final book says how to unfreeze it, with the
  `--den` the copy was given (#209), as `wolfden new` does (SC-CLI-15).

Spec: format.md F-ID-3, F-ID-4, design.md §7, §8 (#129). Covered by:
`CopyCommandTests.ACopyGoesBesideTheOriginalWithANewIdAndSaysWhatItMeans`,
`CopyCommandTests.ToNamesTheUniverseOrOneOfItsSeriesOrBooks`,
`CopyCommandTests.ARefusedCopyExitsTwoAndWritesNothing`,
`CopyCommandTests.ADenWhoseDenYamlDoesntLoadSaysSo`,
`CopyCommandTests.AKeyThatIsTheUniverseASeriesAndABookOffersEverySpelling`,
`CopyCommandTests.ANameThatLooksLikeAParameterComesThroughAsWritten`,
`CopyCommandTests.JsonPrintsTheWriteResult`.

### SC-CLI-14. I freeze a finished book from the command line

The writer runs `wolfden freeze dracula`, and later `wolfden unfreeze
dracula`.

- Without history, `freeze` writes `status: final`, prints that no
  snapshot was taken, and exits 0; the book's notes can't be copied
  into or edited after. With git, it prints the checkpoint and the tag
  it added. `unfreeze` writes `status: draft`, and checkpoints with
  git; freezing again adds `final-2`. `--universe` names the book's
  universe, and `--json` prints the result.
- If git refuses the checkpoint, `freeze` exits 2 and says on standard
  error that the book is final but nothing was committed, and to freeze
  it again; doing so once the hook is gone exits 0 and tags it. An
  unknown book, or no book, exits 2.

Spec: design.md §5, §7 (#31). Covered by:
`FreezeCommandTests.WithoutHistoryFreezeMarksTheBookFinalAndUnfreezeWritesDraft`,
`FreezeCommandTests.WithGitFreezeCheckpointsAndTagsAndUnfreezeCheckpoints`,
`FreezeCommandTests.AFailedCheckpointExitsTwoAndSaysToFreezeAgain`,
`FreezeCommandTests.AnUnknownBookOrNoBookExitsTwo`.

### SC-CLI-15. I start a new character from the command line

The writer runs `wolfden new character "Sister Agatha"`, then `wolfden
new location "Buda-Pesth hospital" --in dracula/book:dracula --from
dracula:8`.

- The first is `universes/dracula/entities/characters/sister-agatha.md`,
  made from the character template: a new id, `type`, `name`, and
  `status: draft`, and the template's sections. The output names the new
  file and says where it enters canon, as `create_entity` does
  (SC-MCP-15). The same name again gets `-2`, and `check` finds nothing
  wrong with either.
- `--in` takes what `wolfden copy --to` takes (SC-CLI-13), from the same
  code, and its messages name `--in`: `--in dracula` names the universe
  and its book, so it's refused with both spellings. With two universes,
  leaving out `--in` is refused, saying to name one `with --in`.
  `--from` sets where the reader first meets it, and `--slug` the file
  name. `--json` prints the write result.
- `wolfden new character "Jonathan Harker"` would take the universe's
  links to the book's Jonathan (F-LINK-7), so it exits 2, naming them and
  saying to pass `--allow-relinks`; with it, the note is made and the
  output lists the links that moved.
- A note in a final book, a type no enabled pack defines (the error lists
  the types), a slug that's taken, an unknown book, a `--from` that
  doesn't resolve in the universe (`dracul:5`, `dracula:999`: written, it
  would leave the note out of every canon view, F-POS-4), a missing or
  blank name, one with a line break in it (U+2028 and U+2029 included),
  or one over 200 characters, or a `den.yaml` that doesn't load exits 2
  and writes nothing. The refusal for a final book says how to unfreeze
  it: "To open the book for editing again, run: wolfden unfreeze
  aftermath --universe dracula" (#209). Given `--den`, the hint passes
  the same `--den` on, in double quotes (without a trailing backslash)
  if it has anything but letters, digits, and `_ . / \ : -`.

Spec: types.md F-TPL-6, design.md §7 (#29). Covered by:
`NewCommandTests.ANewNoteComesFromItsTemplateInTheUniverseWithANewId`,
`NewCommandTests.InNamesWhereItGoesAndFromWhereTheReaderMeetsIt`,
`NewCommandTests.ARefusedCreateExitsTwoAndWritesNothing`,
`FreezeCommandTests.TheUnfreezeHintNamesTheDenItWasGiven`,
`NewCommandTests.ANoteThatWouldTakeLinksNeedsAllowRelinks`,
`NewCommandTests.ADenWhoseDenYamlDoesntLoadSaysSo`,
`NewCommandTests.JsonPrintsTheWriteResult`.

### SC-CLI-16. I add my series and books from the command line

In the Dracula sample, the writer runs `wolfden new book "Dracula's
Guest" --in dracula --chapters 2`, then `wolfden new series "The Harker
Papers" --after dracula`, then `wolfden new book "Mina's Journal" --in
the-harker-papers`.

- Each does what the web UI's New book and New series do (SC-WEB-69):
  the folder and its `book.md` or `series.md`, with a new id and the
  name, the book's chapters, and the key in the universe's `order` or
  the series' `books`, at the end, `--after` the work named, or `--first`
  at the start. The
  output names the new file and what was done; `--json` prints the
  result. `check` finds nothing wrong with any of them.
- `--in dracula` is the universe, though it has a book 'dracula' too,
  since a book holds no books. A series' bare key will do in a den with
  one universe, and `<universe>/<series>` in any. With several universes,
  a bare key that's only a book (`--in ember`) is refused, and the hint
  names a universe or a series, never a book.
- A name that's missing, blank, or taken as a key (`Dracula`), an
  `--after` that isn't there, `--chapters` that isn't 1 to 1000 or is
  given for a series, `--from`, `--slug`, or `--allow-relinks` (for notes
  only), `--after`, `--first`, or `--chapters` for a note, `--first` with
  `--after`, a name in another script, a book named by `--in`, or
  a series that isn't there exits 2 and writes nothing.

Spec: design.md §7 (#213). Covered by:
`NewContainerCommandTests.NewSeriesAndBooksGoInStoryOrder`,
`NewContainerCommandTests.ARefusedCreateExitsTwoAndWritesNothing`,
`NewContainerCommandTests.JsonPrintsWhatWasMade`,
`NewContainerCommandTests.FirstPutsItAtTheStart`,
`NewContainerCommandTests.ABareBookKeyInADenWithSeveralUniversesSaysUniverseOrSeries`.

### SC-CLI-17. I add a chapter from the command line

In the Dracula sample, the writer runs `wolfden new chapter "Jonathan's
Letter" --in dracula --after 3`, then `wolfden new chapter --in
dracula`.

- Each does what the web UI's Insert chapter… and Add chapter do
  (SC-WEB-71): `3b`, numbered 4, with chapters 4 to 27 moving up one
  (the output lists the first two, the last, and how many), then `28` at
  the end, after the unnumbered closing `note`, numbered after the last
  numbered chapter. The output names the position and `book.md` and
  what was done; `--json` prints the result. `check` finds nothing
  wrong.
- `--in dracula` is the book, though it's the universe's key too, since
  only a book holds chapters. `book:dracula` and `dracula/dracula` name
  it too.
- No `--in`, a book or universe that isn't there, an `--after` the book
  doesn't list, a title that isn't one line of at most 200 characters,
  `--first`, `--chapters`, `--slug`, `--from`, `--allow-relinks`, or a
  final book exits 2 and writes nothing. For a final book, the message
  ends with the `wolfden unfreeze` command to run, with the den's
  `--den`.

Spec: design.md §7 (#215). Covered by:
`NewChapterCommandTests.ANewChapterGoesAtTheEndOrAfterTheChapterNamed`,
`NewChapterCommandTests.JsonPrintsWhatWasAdded`,
`NewChapterCommandTests.ARefusedAddExitsTwoAndWritesNothing`.

## WATCH: keeping the index up to date while notes change

Background: `wolfden index --watch` (design.md §7, #14) keeps the index up
to date while the writer, or an AI agent, edits notes in other tools. It
reads only the notes that changed where that's safe, and rebuilds the
index where it isn't. Either way the index ends up as a full rebuild of the
same files would make it.

### SC-WATCH-1. I edit notes in my editor while Wolfden watches

The writer runs `wolfden index --watch` in a terminal, then edits Mara
Voss's note in a text editor and saves it a few times, adds a location,
and moves the `characters/` folder of series `a` to `crew/`.

- Within moments each burst of saves shows up in the index as one
  change, and the terminal prints one line for it: the note that changed,
  how long it took, and what the index holds. Only the changed notes are
  read again, so a single save is quick even in a den of thousands of
  notes. Ctrl+C stops watching and exits 0.

Spec: design.md §7. Covered by:
`IncrementalUpdateTests.AnEditedNoteIsUpdatedWithoutARebuild`,
`IncrementalUpdateTests.AMovedFolderOfNotesIsFollowed`,
`IncrementalUpdatePropertyTests.AnyChangeSequenceGivesTheSameIndexAsAFullRebuild`,
`ChangeBatcherTests.ABurstOfSavesIsOneBatchOnceEventsStop`,
`DenWatcherTests.AnEditIsInTheIndexWithinMoments`,
`DenWatcherTests.ANewNoteAndADeletedNoteAreFollowed`,
`IndexWatchTests.ItRebuildsThenPrintsALineForEachChangeUntilStopped`,
`IndexWatchTests.JsonPrintsOneObjectPerLine`,
`PerformanceTests.OneEditOnALargeDenIsUpdatedQuickly`.

### SC-WATCH-2. I rename a character while Wolfden watches

With the watcher running, the writer renames Halden Pike to Halden Marsh.
Other notes still say `[[Halden Pike]]`. Then they add `Halden Pike` to
his aliases.

- After the rename, the links that used the old name are broken in the
  index, as `wolfden check` would report them, though only his note
  changed. After the alias, they resolve again. The index matches a full
  rebuild at every step.

Spec: F-LINK-6, F-LINK-8. Covered by:
`IncrementalUpdateTests.RenamingACharacterBreaksTheOldLinksAndAnAliasMendsThem`,
`IncrementalUpdatePropertyTests.AnyChangeSequenceGivesTheSameIndexAsAFullRebuild`.

### SC-WATCH-3. I change the den's settings or a book's chapters while Wolfden watches

The writer turns on `spoiler_checks` in `den.yaml`, or edits `book.md` of
`a2` to add a chapter.

- The watcher rebuilds the whole index, since such a change can change
  what every note means, and its line says why, such as "den.yaml
  changed". If a typo leaves `den.yaml` unable to load, the line says so,
  and the watcher keeps going until it's fixed.

Spec: F-DEN-1, F-DEN-7, F-BOOK-3, design.md §7. Covered by:
`IncrementalUpdateTests.AStructureChangeRebuilds`,
`IncrementalUpdateTests.AContainerFileChangeRebuilds`,
`IncrementalUpdateTests.AFolderThatCanBeAContainerRebuilds`,
`IndexWatchTests.ItKeepsWatchingWhileDenYamlIsBrokenSoItCanBeFixed`.

### SC-WATCH-4. My tools write files that aren't notes

While the watcher runs, the editor writes swap files, git updates
`.git/`, Obsidian saves its workspace, and the writer drops a map image
into an `entities/` folder, keeps loose notes in `notes/`, edits the
den's `README.md`, and drafts chapters in a book's `drafts/` folder.

- None of it changes what the index holds, and the watcher prints
  nothing for it. `wolfden query` still finds the index up to date, and
  doesn't rebuild it.

Spec: F-LAY-2. Covered by:
`IncrementalUpdateTests.FilesTheIndexDoesntReadChangeNothing`,
`FingerprintTests.FilesTheIndexDoesntReadDontChangeARebuild`,
`ChangeBatcherTests.HiddenPathsAreIgnored`,
`ChangeBatcherTests.AStoresAtomicWriteIsBatchedAsTheNoteAlone`.

### SC-WATCH-5. Something else rebuilds the index while Wolfden watches

With the watcher running, the writer runs `wolfden index` in another
terminal, deletes `.wolfden/`, or starts a second `wolfden index --watch`
on the same den.

- The watcher notices that the index isn't the one it wrote and rebuilds
  it at the next change, rather than patching rows it no longer knows,
  and a change it was writing when the index was replaced isn't lost. A
  second watcher on the same den says another one is already watching and
  exits 2. If the operating system stops reporting changes, say because
  the den is on a drive that went away, the watcher says so, listens
  again, and rebuilds, and keeps saying so while it can't.

Spec: design.md §7. Covered by:
`DenWatcherTests.WhenFileEventsStopItListensAgainRebuildsAndSaysSo`,
`IncrementalUpdateTests.AnIndexRebuiltByAnotherCommandIsRebuiltNotPatched`,
`IncrementalUpdateTests.AnIndexReplacedWhileABatchIsWrittenIsRebuilt`,
`IncrementalUpdateTests.ADeletedIndexIsRebuilt`,
`DenWatcherTests.OnlyOneWatcherRunsPerDen`,
`IndexWatchTests.ASecondWatcherOnTheSameDenExits2`.

### SC-WATCH-6. I query right after a save while Wolfden watches

With the watcher running on a large den, the writer saves Mara Voss's
note and runs `wolfden query --entity "Mara Voss"` at once, before the
watcher has applied the save.

- The query shows the saved note. It waits a moment (up to about a
  second) for the watcher to apply the save, rather than rebuilding the
  whole index itself, which on a large den takes several seconds and
  makes the watcher rebuild too. If the watcher hasn't caught up by
  then, the query rebuilds the index, so the answer still matches the
  files.

Spec: design.md §7. Covered by:
`FreshnessTests.EnsureUpToDateWaitsForARunningWatcherInsteadOfRebuilding`,
`FreshnessTests.EnsureUpToDateRebuildsWhenTheWatcherDoesntCatchUpInTime`,
`QueryCommandTests.AQueryWaitsBrieflyForAWatcherThenRebuildsIfItHasnt`.

### SC-WATCH-7. A note can't be read while Wolfden watches

With the watcher running, the writer replaces a note with a link to a
file that has moved away. Later, another program holds Mara Voss's note
open just after the writer saves it.

- The broken link is left out at once, as if the note were deleted. The
  note that's held open is tried again a few times; if it still can't be
  read, the watcher rebuilds the index without it, as `wolfden index`
  would, and its next save brings it back. Either way the index matches a
  full rebuild of the same files. If the link starts working again
  because its target comes back, often outside the den, the link itself
  gets no file event, so the watcher doesn't see it; the next query sees
  that the files changed and rebuilds the index with the note.

Spec: design.md §7 (#112). Covered by:
`UnreadableFileTests.TheWatcherTreatsABrokenLinkAsAbsentAtOnce`,
`IncrementalUpdateTests.AFileThatStaysUnreadableIsLeftOutByARebuildUntilItCanBeRead`,
`IncrementalUpdatePropertyTests.AnyChangeSequenceGivesTheSameIndexAsAFullRebuild`.

### SC-WATCH-8. A folder can't be listed while Wolfden watches

With the watcher running, the writer takes away their own permission to
list Mara Voss's `characters/` folder, and later that of the book folder
`ember/`.

- Whenever the watcher next gets an event for the folder or for anything
  in it, such as a note saved in place, it leaves the whole folder out,
  as if it were deleted: all the notes in `characters/` drop out of the
  index, and the book folder gets a full rebuild without it. Either way
  the index matches a full rebuild of the same files. A change of
  permissions gets no file event of its own, so until such an event the
  watcher doesn't notice; the next query sees that the files changed and
  rebuilds the index. The same happens when the folder can be listed
  again.

Spec: design.md §7 (#116). Covered by:
`UnlistableFolderTests.TheWatcherLeavesOutAnEntitiesFolderItCantList`,
`UnlistableFolderTests.TheWatcherRebuildsWithoutAContainerFolderItCantList`,
`UnlistableFolderTests.TheWatcherLeavesOutAFolderItCantListWhenAFileInItChanges`.

## MCP: an AI assistant reads the den

Background: `wolfden mcp` (design.md §8, #20) runs an MCP server over
stdio, so an AI assistant can read the den with `search`, `get_entity`,
`list_entities`, `canon_as_of`, `get_chapter`, `get_chapter_defaults`,
`check`, and `get_history`, and write to it with the write tools (#21,
SC-MCP-15 on).
Without a story position, a reading tool gives the author's full view;
with one, it gives canon as the reader knows it there. Pinned canon comes
first. The examples use the Dracula sample, whose book is `dracula`.

### SC-MCP-1. My assistant reads canon as of the chapter I'm drafting

The writer is drafting chapter 4 and asks the assistant to check what's
established so far. It calls `canon_as_of` with `position: "dracula:4"`.

- The result holds every entity the reader has met by chapter 4, at any
  scope, with the field values and sections in effect there: the same
  view `wolfden query --at dracula:4` shows. Lucy and Renfield, who enter
  at chapter 5, and what the Count is, which comes out in chapter 18,
  aren't in it anywhere. Ideas, undecided questions, and retconned or
  bookkeeping material are left out. `type` narrows it, and `brief`
  lists the entities without their content.

Spec: F-VIEW-1, F-VIEW-3. Covered by:
`DraculaToolTests.CanonAsOfAnEarlyChapterLeaksNothingTheReaderMeetsLater`,
`DraculaToolTests.CanonAsOfMatchesTheIndexsViewEntityForEntity`,
`DraculaToolTests.CanonAsOfLeavesOutIdeasAndUndecidedQuestions`,
`DraculaToolTests.CanonAsOfCanBeBriefAndNarrowedToAType`.

### SC-MCP-2. My assistant asks about a character the reader hasn't met yet

Drafting chapter 4, the assistant calls `get_entity` for "Lucy" with
`as_of: "dracula:4"`.

- The result says Lucy isn't in canon as of chapter 4 (`in_view` is
  false) and gives nothing about her: not even her full name. At chapter
  5 it gives her as the reader first meets her, without her death or what
  follows.

Spec: F-VIEW-1, F-ENT-6, format.md §9.5. Covered by:
`DraculaToolTests.GetEntityAsOfAChapterBeforeTheReaderMeetsItReturnsNothingAboutIt`,
`DraculaToolTests.GetEntityAsOfAChapterShowsOnlyWhatsKnownThen`,
`McpServerTests.OverTheWireAnEarlyViewHoldsNoSpoilers`.

### SC-MCP-3. My assistant sees the hard facts first

The writer has pinned the Count's species and the rules of the Un-Dead.
The assistant calls `canon_as_of` at chapter 18, and `get_entity` for the
Count.

- Each result starts with `pinned`: the pinned facts in effect, grouped
  by entity, ahead of the rest. They're listed in place as well, so each
  entity is complete. At chapter 17 the Count's species isn't among them,
  since the reader learns it at chapter 18.

Spec: F-PIN-1, F-VIEW-2, design.md §8. Covered by:
`DraculaToolTests.CanonAsOfServesPinnedCanonFirst`,
`DraculaToolTests.PinnedCanonKeepsToThePositionToo`,
`DraculaToolTests.GetEntityServesPinnedFactsFirst`.

### SC-MCP-4. My assistant reads a whole note, ideas and all

The writer asks the assistant to help decide an open question in the
thread "The Count's end". It calls `get_entity` with no position.

- The result is the author's full view: every field entry and section,
  whatever its status, each with its status, pin, and the positions
  where it starts and ends, so the undecided question and both candidate
  answers are there, marked as such. The entity can be named by its
  name, an alias, its file name, its id, or its path.

Spec: F-STAT-1, F-STAT-4, F-LINK-6. Covered by:
`DraculaToolTests.GetEntityWithoutAPositionIsTheAuthorsFullView`,
`DraculaToolTests.GetEntityTakesANameAnAliasAFileNameAnIdOrAPath`.

### SC-MCP-5. My assistant looks for notes

The assistant searches for "Voivode", and lists the den's characters.

- `search` finds the Count's "Who he was" section, with the match marked
  in a snippet, and says for each hit whether it's canon text;
  `canon_only` leaves out ideas, candidates, and retconned material.
  `list_entities` lists entities of every status as one line each, and
  narrows by type, status, series, or book.

Spec: design.md §7, §8. Covered by:
`DraculaToolTests.SearchFindsNotesAndSaysWhatsCanon`,
`DraculaToolTests.ListEntitiesFiltersByTypeStatusAndScope`.

### SC-MCP-6. My assistant gets a position or a name wrong

The assistant asks for canon as of `dracula:99`, then for "Sherlock
Holmes", then for "Lucy Westenra" in a den where two notes have that
name.

- Each answer is an error result with a code the assistant can act on
  (`bad_position`, `unknown_entity`, `ambiguous_entity`, and so on) and a
  message saying what to do: a bad position lists the universe's books in
  story order, and an ambiguous name lists the matches. With a position,
  a name is matched, tier by tier, among what the reader has met by
  then: a namesake met later can't shadow the entity the reader knows,
  tie with it, or show in the error. `wolfden query --entity --at`
  matches the same way. A den with several universes asks which one
  (SC-MCP-11). An argument of the wrong type, or a missing one, is
  `bad_argument` in the same shape.

Spec: F-POS-2, F-POS-3, F-POS-4, F-LINK-6. Covered by:
`ToolErrorTests`, `OtherDenErrorTests.ANameThatMatchesTwoEntitiesListsThemWithoutSpoilingEarlierViews`,
`OtherDenErrorTests.WithAPositionALaterNamesakeCantShadowAnEntityTheReaderHasMet`,
`OtherDenErrorTests.AnUnknownTypeAtAPositionDoesntListTheTypesInUse`,
`OtherDenErrorTests.ADenWithSeveralUniversesNeedsOneNamed`,
`QueryCommandTests.AtAPositionTheEntityIsMatchedAmongThoseTheReaderHasMet`,
`McpServerTests.ABadCallIsAnErrorResultWithACode`,
`McpServerTests.AnArgumentOfTheWrongTypeIsABadArgument`.

### SC-MCP-7. I edit a note while my assistant is connected

With `wolfden mcp` running, the writer adds a section to Mr Swales's note
in their editor, and the assistant reads him again.

- The next call shows the new section. The server runs the same watcher
  as `wolfden index --watch`, and each call also checks the files first,
  as `wolfden query` does, so an answer never lags behind a save. If
  another Wolfden is already watching the den, the server uses that one.

Spec: design.md §7, §8. Covered by:
`McpServerTests.AnEditWhileConnectedShowsInTheNextCall`.

### SC-MCP-8. My assistant runs the continuity checks

The assistant calls `check` after helping with an edit.

- It gets the same findings as `wolfden check`, each with a path, line,
  severity, rule id, and message, and the den's error and warning
  counts. `severity` and `path` narrow the list. It never changes a
  file.

Spec: F-GEN-1, design.md §7. Covered by:
`DraculaToolTests.CheckReportsTheSamplesFindings`.

### SC-MCP-9. `den.yaml` stops loading while my assistant is connected

The writer makes a typo in `den.yaml`.

- The reading tools answer `den_not_loaded` and say to call `check`,
  which reports what's wrong with `den.yaml`. Once it's fixed, the tools
  work again without restarting the server.

Spec: F-DEN-1. Covered by:
`OtherDenErrorTests.ADenWhoseSettingsDontLoadIsDenNotLoadedButCheckSaysWhy`.

### SC-MCP-10. I connect my AI client to my den

The writer adds `wolfden mcp --den <folder>` to their AI client's MCP
settings.

- The client starts the server, sees its tools, the reading ones
  marked read-only, with
  descriptions that explain dens, scopes, and positions, and can call
  each one, several at once even before the den has an index. The
  server doesn't make the client wait for its first index build
  (#135). Nothing
  but the protocol goes to standard output, and when the client closes
  standard input the server exits 0. Without a den, the server doesn't
  start, says why on standard error, and exits 2; one that fails after
  starting exits 3.

Spec: design.md §7, §8, §9. Covered by:
`McpCommandTests.AnAIClientRunsItOverStdio`,
`McpCommandTests.OverRawPipesStdoutHoldsOnlyJsonRpcAndStdinEofEndsTheServer`,
`McpCommandTests.AServerThatFailsAfterStartingIsExitThree`,
`McpCommandTests.WithoutADenItDoesntStart`,
`ParallelCallTests.ParallelFirstCallsOnADenWithNoIndexAllSucceed`,
`McpServerTests.TheClientSeesTheServerAndItsTools`,
`McpServerTests.EachToolAnswersWithJson`,
`McpHandshakeTests.TheServerAnswersWhileTheWatchersFirstBuildIsHeldUp`.

### SC-MCP-11. My assistant works in a den with two universes

The writer's den holds two universes, `starfall` and `drift`, and each
has a Lantern. The assistant calls `canon_as_of`, then `canon_as_of` at
`ember:1`, then `get_entity` for "Lantern" at `ash:2`.

- With no universe and no position, every tool but `check` answers
  `universe_required`, listing the universe keys and nothing else: no
  entity from either universe. A position settles the universe when its
  book is in only one (`ember` and `ash` are Starfall's), so the third
  call gives Starfall's Lantern as the reader knows it at `ash:2`. A book
  key both universes have doesn't settle it (`universe_required`), and
  one neither has is `bad_position`. `search` and `list_entities` take
  `universe: "*"` to read both universes; the other tools refuse it, and
  so does any tool given a position with it.

Spec: F-POS-5, design.md §8 (maintainer decision, 2026-10-23). Covered by:
`UniverseChoiceTests.WithoutAUniverseOrPositionEveryToolButCheckAsksForOne`,
`UniverseChoiceTests.APositionsBookSettlesTheUniverse`,
`UniverseChoiceTests.ABookKeyInTwoUniversesOrInNoneDoesntSettleIt`,
`UniverseChoiceTests.StarReadsEveryUniverseInSearchAndListOnly`.

### SC-MCP-12. I pin my assistant to one universe

The writer starts the server with `wolfden mcp --universe drift`.

- Every call reads `drift` without naming it, and the server's
  instructions say the session is pinned. A call that names `starfall`,
  or `"*"`, is refused with `bad_argument`, so the assistant can't leave
  its universe; `check` checks `drift` and the den-level files. A
  `--universe` the den doesn't have stops the server from starting (exit
  2).

Spec: design.md §7, §8 (maintainer decision, 2026-10-23). Covered by:
`UniverseChoiceTests.APinnedSessionReadsOnlyItsUniverse`,
`McpCommandTests.AUniverseTheDenDoesntHaveCantBePinned`.

### SC-MCP-13. My assistant checks one universe

The assistant calls `check` with `universe: "drift"`.

- It gets `drift`'s findings and the den-level ones, and none from
  `starfall`, whose notes aren't read. Mara Voss copied from `starfall`
  into `drift`, id and all, is still a duplicate (F-ID-3) and links to
  her still break, as in the whole den; only the hint on a broken link
  that names a match in another universe is missing. A broken pack or
  `den.yaml` is still reported, and `note` says when ids couldn't be
  compared across universes. Without a universe, or with `"*"`, it
  checks the whole den, whose findings are those of each universe plus
  the den-level ones, each once. `path` still narrows the list.

Spec: F-DEN-1, F-ID-3, design.md §7, §8 (maintainer decision, 2026-10-23). Covered by:
`UniverseChoiceTests.CheckOfAUniverseLeavesOutTheOthers`,
`UniverseChoiceTests.TheWholeDenIsTheUnionOfItsUniversesAndTheDenLevel`,
`UniverseChoiceTests.ACheckOfOneUniverseFindsAnIdSharedWithAnotherUniverse`,
`UniverseChoiceTests.WhenTheIndexCantBeBuiltTheScopedCheckSaysTheIdComparisonWasSkipped`,
`UniverseChoiceTests.ABrokenPackOrDenYamlIsReportedWhenAUniverseIsGiven`.

### SC-MCP-14. My assistant looks at a note's history

The writer's den is a git repository. They ask the assistant what changed
in Lucy Westenra's note, and what they've changed since the last
checkpoint.

- `get_history` with `entity: "Lucy Westenra"` lists the checkpoints that
  changed her file, newest first, each with its id, time, author,
  message, and files, and none that only changed other notes. With
  `limit: 1`, the result is `truncated` and gives `next_before`; asking
  again with `before` set to it gives the older checkpoint. `diff: true`
  adds what changed in her file since the latest checkpoint, and
  `diff_from` with a checkpoint's id, what changed since that one. In a
  den without history, the result isn't an error: `available` is false
  and `reason` says the den has no history. A `before` or `diff_from`
  that isn't one of the checkpoints is `unknown_checkpoint`.

Spec: design.md §6, §8 (#19). Covered by:
`HistoryToolTests.TheAssistantSeesAnEntitysCheckpointsAPageAtATimeAndWhatChangedSince`,
`HistoryToolTests.WithoutHistoryTheAnswerSaysSoAndIsntAnError`,
`McpCommandTests.OverStdioHistoryComesFromGit`.

The scenarios below are about the write tools (design.md §8, #21). Each
edit of an existing note takes the hash the assistant read it with
(`expected_hash`, from `get_entity` or `get_chapter`), and every write
goes to the files only, until a checkpoint.

### SC-MCP-15. My assistant adds a character

The writer asks the assistant to add Sister Agatha, the nun who writes to
Mina from Buda-Pesth, to the book, from chapter 8.

- `create_entity` makes `entities/characters/sister-agatha.md` in the
  book from the character template: a new id that no note in the den
  uses, `type`, `name`, the alias and fields given, `status: draft`
  written out, `from`, the template's other fields, and its sections
  with their prompts. The note passes `check`. A second Sister Agatha
  gets `sister-agatha-2.md`; a slug asked for that's taken is refused
  (`already_exists`), and so is a type no pack defines (`unknown_type`).
  A name with nothing to make a slug from uses the type key.
- A `from` that doesn't resolve in the universe (`dracul:5`,
  `dracula:999`) is refused (`bad_position`), since the note would be in
  no canon view (F-POS-4); a bare book key resolves. A book's note whose
  `from` is in another book is made, with the F-ENT-6 warning.
- A name is one line of at most 200 characters: one with a control
  character or a Unicode line or paragraph separator (U+2028, U+2029),
  which would split its `# name` heading, or a longer one, is refused
  (`bad_argument`), by `create_entity`, `add_idea`, `copy_entity`, and
  `rename_entity` alike. A 200-character name gets a 64-character slug.
- A second "Jonathan Harker" at universe scope would be nearer than the
  book's to the universe's notes that link to him (F-LINK-7), so it's
  refused (`bad_argument`), naming each link that would move, and
  nothing is written, as for `copy_entity`. Beside the original, in the
  book, it's `jonathan-harker-2.md` and moves nothing. With
  `allow_relinks: true` it's made, and `effects` list the links that
  moved. `add_idea` does the same for a new idea entity: an idea named
  "Renfield" in the universe would take `seward-asylum.md`'s
  `[[renfield]]`, so it's refused unless its own `allow_relinks`.
- A `from` that's empty or only spaces is no `from`, as on disk
  (F-ENT-6): the note is made without one.

Spec: types.md F-TPL-6, F-TPL-7, F-ID-6, format.md F-POS-4, F-ENT-6. Covered by:
`WriteToolTests.ANewEntityComesFromItsTemplateWithAnIdNoFileUses`,
`WriteToolTests.ASlugIsMadeFromTheNameAsTheTemplateRuleSays`,
`WriteToolTests.ANewEntitysFromMustResolveAndOneInAnotherBookIsAWarning`,
`WriteToolTests.ANewEntityThatWouldTakeLinksFromAnotherIsRefusedUnlessAllowed`,
`WriteToolTests.ANewIdeaThatWouldTakeLinksIsRefusedUnlessAllowed`,
`WriteToolTests.AnEmptyFromIsNoFrom`,
`WriteToolTests.ANameIsOneLineOfAtMost200Characters`.

### SC-MCP-16. My assistant changes a note's fields

The assistant adds an alias to Mr. Swales, gives him an occupation, and
marks him an idea; then it tries to save a status that isn't one, and a
scene position. Later it moves when the reader learns the Count's
pinned species.

- `update_fields` makes each change in place, keeping every other line
  as written, with the new alias in template order, and says that
  Swales is now out of canon views. The old hash is now `stale_file`;
  the result's hash works. An edit that would leave an error
  (`status: maybe`, `from: dracula:7.2`) is refused as `invalid_edit`
  with the rule, and the file is unchanged. An `id` edit is refused
  (F-ID-4); renaming the entity or changing an alias is left to
  `rename_entity` and `change_alias`, so links follow (F-RT-5,
  SC-MCP-26, SC-MCP-27). The change to the pinned species is made,
  and `pinned_canon` says it touched pinned canon, as it does for a
  status change on the Count, whose pinned facts take his status. An
  edit that changes nothing writes nothing, not even a missing id. An
  alias that's another note's name or slug says what links by it now
  reach; an alias with control characters, or a slug Windows can't
  hold (`nul`), is refused, and a name like "Con" gets `con-2`.
- A list item, an alias included, moves in place with a `move` change,
  keeping its comments (SC-RT-17).

Spec: format.md §1.3, F-ID-4, F-RT-2, F-RT-5, F-PIN-1, types.md F-TPL-7
(maintainer decisions, 2026-10-22 and 2026-10-23). Covered by:
`WriteToolTests.FieldEditsKeepTheRestAndSayWhatTheyMean`,
`WriteToolTests.AnEditThatWouldLeaveAnErrorIsRefusedAndNothingIsWritten`,
`WriteToolTests.AnEditOfPinnedCanonIsMadeButFlagged`,
`WriteToolTests.AStatusChangeOnAnEntityWithPinnedFactsIsFlagged`,
`WriteToolTests.APinnedSubsectionUnderAnUnpinnedSectionIsCounted`,
`WriteToolTests.AnEditThatChangesNothingAddsNoId`,
`WriteToolTests.BadNamesAndAliasesAreRefusedAndAnAliasThatCollidesSaysSo`,
`McpServerTests.OverTheWireAWriteTakesItsChangesAsJsonAndAStaleHashIsRefused`,
`WriteEditTests.InTemplateOrderANewTopLevelKeyGoesInItsPlace`,
`WriteEditTests.AWholeTemporalListIsWrittenOneEntryPerLine`,
`ListItemToolTests.UpdateFieldsMovesAnItemWithItsComments`.

### SC-MCP-17. My assistant writes into a note's sections

The assistant adds a line to Swales's Voice, an Appearance he didn't
have, and a subsection under his Arc that the reader meets at chapter 7;
then it fills the Overview of a character it just created.

- `append_section` adds the text after what's in the section. The new
  Appearance goes before Voice, in the character type's section order,
  and the subsection goes at the end of Arc with `from=dracula:7` as its
  section comment. A template's empty section is filled, prompt comment
  kept, never repeated. Text added to a section with subsections goes
  before them. A note saved on Windows keeps its CRLF line endings and
  byte order mark. Status or positions for a section that's already
  there are refused, as is a parent that isn't there.

Spec: format.md §9.2, types.md F-TPL-5, F-TPL-8. Covered by:
`WriteToolTests.TextGoesIntoASectionOrANewOneInItsPlace`,
`WriteToolTests.ATemplateSectionIsFilledNotRepeated`,
`WriteToolTests.TextAddedToASectionGoesBeforeItsSubsections`,
`WriteToolTests.ANoteSavedOnWindowsKeepsItsLineEndingsAndByteOrderMark`,
`WriteEditTests.ASectionGoesBeforeAnotherWithItsComment`.

### SC-MCP-18. My assistant banks an idea

The writer muses that the Count might keep a diary, and that Swales might
have seen the dog leap ashore.

- `add_idea` makes an `idea` entity, "The Count's diary", with `status:
  idea`, `about` linking the Count, and the text in its lead. With an
  entity named, it adds a section marked `status=idea` to Swales
  instead. Canon views leave both out.
- With an entity named, the new entity's parameters (`name`, `type`,
  `series`, `book`, `fields`, and `allow_relinks`) are refused
  (`bad_argument`), since nothing is created (#204).

Spec: format.md §9.3, types.md §9.1. Covered by:
`WriteToolTests.AnIdeaIsRecordedOutOfCanon`,
`WriteToolTests.AnIdeaForAnExistingEntityRefusesTheNewEntitysParameters`,
`WriteEditTests.TextGoesAtTheEndOfTheLead`.

### SC-MCP-19. My assistant renames a section

A note on Whitby dialect links `[[mr-swales#Voice]]` in a field and
`[[Mr. Swales # voice |how he talks]]` in its text. The assistant
renames Swales's Voice to Dialect.

- The heading changes, and both links now name Dialect, with their
  spacing and label kept, in the same change; a link to another of his
  sections is left alone. The result lists both files with their new
  hashes, and `check` finds no heading that doesn't match. A link in a
  block scalar, or in a quoted value with an escape, is spliced in, so
  the value keeps its style. If a note in a final book links to the
  section, nothing is renamed (`frozen`); if a write fails part way,
  what was written is put back. A new name that would move existing
  links (a later section's name, or an earlier one's while links name
  this one) is refused, and a note with errors that links to the
  section is named in the result for the writer to fix by hand.

Spec: format.md F-RT-5, F-LINK-9, F-BOOK-1 (maintainer decision,
2026-10-23). Covered by:
`WriteToolTests.RenamingASectionRewritesTheLinksToIt`,
`WriteToolTests.ALinkInAFrozenBookStopsTheRename`,
`WriteToolTests.ARenameThatFailsPartWayPutsBackWhatItWrote`,
`WriteToolTests.ARenameStoppedByAnyFailureIsPutBackThenRethrown`,
`WriteToolTests.ARollbackLeavesAFileSomeoneElseSavedMeanwhile`,
`WriteToolTests.ALinkInAFrontmatterValueIsSplicedSoTheValueKeepsItsStyle`,
`WriteToolTests.ANameThatWouldMoveExistingLinksIsRefused`,
`WriteToolTests.AFileWithErrorsThatLinksToTheSectionIsNamed`.

### SC-MCP-20. My assistant takes the last POV off a chapter

The book's `chapter_defaults` give every chapter Mina's POV, and chapter
1 has its own, Jonathan's. The assistant removes it.

- `get_chapter` shows chapter 1's `pov` as its own, with Mina's as the
  default from the book. After `update_chapter_fields` removes the item,
  `book.md` says `pov: []`, and the result says chapter 1 now has no
  POV, and that without its own value it would inherit Mina's from the
  book's `chapter_defaults`. Clearing the field goes back to that
  default; clearing a field the chapter doesn't set is refused. An item
  can go in at a given place, and come out by its text, compared
  ignoring case and spacing: `[[Mina]]` doesn't match
  `[[mina-harker]]`. A field named like the chapter's own keys (`key`)
  is refused (types.md F-CHAP-1). An item can also move (`move`) or be
  changed (`replace`) in place, keeping its comments (SC-RT-17). Either
  is refused as `add` and `remove` are (#233) on a field that takes one
  value, isn't written as a list, or has no such item; on a field the
  chapter doesn't write, the refusal says it inherits it and where to
  change it.

Spec: format.md F-RT-6, types.md F-CHAP-5 (maintainer decisions,
2026-10-23). Covered by:
`WriteToolTests.RemovingAChaptersLastPovSaysWhatItNoLongerInherits`,
`WriteToolTests.ChapterListItemsGoInAtAnIndexAndComeOutByValue`,
`ListItemToolTests.ChapterFieldsMoveAndReplaceAnItemKeepingItsComments`.

### SC-MCP-21. My assistant tries to edit a finished book

The writer has frozen the book (`status: final`).

- Adding to Swales, creating a character in the book, changing a
  chapter's fields or the book's `chapter_defaults`, and copying or
  moving a place into the book are each refused as `frozen`, and
  nothing changes. The universe isn't frozen, so a new location there
  is made.
- Each refusal ends by telling the assistant to ask the writer whether
  to unfreeze book 'dracula', since only the writer can (#209). The
  same refusals in the web UI and the CLI say how to unfreeze it
  instead (SC-WEB-44, SC-CLI-15).

Spec: format.md F-BOOK-1 (maintainer decision, 2026-10-22). Covered by:
`WriteToolTests.NothingInAFinalBookIsWritten`,
`ListItemToolTests.AMoveInAFinalBookIsRefused`.

### SC-MCP-22. My assistant checkpoints a session's edits

After adding Whitby Abbey and a line about Lucy, the assistant makes a
checkpoint.

- The edits weren't commits. `checkpoint` records both in one commit
  whose message ends with the assistant's `Co-Authored-By` line; a
  second straight after has nothing to record. In a den without
  history, the result says so and that the edits are still saved in
  the files. A co-author that isn't `Name <email>` on one line is
  refused. With `paths`, only those files are recorded, and the new
  location stays uncommitted; a `paths` list with nothing in it is
  refused rather than read as the whole den.
- Meanwhile the writer fixed a typo in Mina's note in their editor. The
  checkpoint records it too, and its result lists Whitby Abbey and
  Lucy under `written_by_session` and Mina under
  `changed_outside_session`, so the assistant can say "Mina's note
  changed outside this session". After the checkpoint, the session
  starts afresh: a later checkpoint lists only what was written since.
  If the writer adds a line to Lucy after the assistant's edit, Lucy is
  in both lists. If the writer commits the assistant's edit in their
  own git client and then edits Lucy by hand, Lucy is only under
  `changed_outside_session`. A note a failed write put back isn't the
  session's.

Spec: design.md §6, §8 (maintainer decisions, 2026-10-22 and
2026-10-25). Covered by:
`HistoryToolTests.WritesWaitForACheckpointThatNamesTheAgentAsCoAuthor`,
`HistoryToolTests.ACheckpointOfSomePathsRecordsOnlyThem`,
`HistoryToolTests.ACheckpointSaysWhichFilesTheSessionWroteAndWhichChangedOutsideIt`,
`HistoryToolTests.ASessionWriteTheWriterCommittedElsewhereIsntTheSessionsAnyMore`,
`HistoryToolTests.AFileEditedByTheSessionAndTheWriterIsInBothLists`,
`HistoryToolTests.AFileAFailedWritePutBackIsntTheSessions`,
`WriteToolTests.WithoutHistoryACheckpointSaysSoAndTheEditsStay`.

### SC-MCP-23. My pinned assistant creates a note

The server is pinned to universe `drift`, and the new id it draws first
happens to be one a note in `starfall` uses.

- The id is drawn again, so the new note's id is unused anywhere in the
  den, though the session can't read `starfall`. It can't create a
  note in `starfall` either (`bad_argument`).

Spec: F-ID-3 (maintainer decision, 2026-10-23). Covered by:
`WriteToolTests.APinnedSessionChecksNewIdsAcrossTheWholeDen`.

### SC-MCP-24. My assistant makes Mara the default POV for a book

In the Starfall den, `ember`'s prologue says `pov: []` and its chapter 3
has Tomas Reyes's POV; its other chapters set none. The writer asks the
assistant to make Mara Voss the book's default POV.

- `get_chapter_defaults` for the book gives `book.md`'s hash (the same
  as `get_chapter`'s), its defaults, and the books they reach. After
  `update_chapter_defaults` adds `[[mara-voss]]` to `pov`, the result
  says "Book 'ember': chapters 1–2b now have pov [[mara-voss]];
  chapters prologue, 3 keep their own". Removing the item leaves
  `pov: []` and says those chapters now have no POV; clearing the field
  goes back to inheriting, here from nothing. Clearing a field that
  isn't set, and an old hash, are refused, and asking for what the file
  already says writes nothing.
- A series' defaults reach both its books: the result names each book's
  chapters, and a book whose own defaults set the field keeps that
  value, named with where it comes from. A universe's defaults reach the
  standalone book too, and chapters of the series' books keep the
  series' value. Clearing the series' value hands its books the
  universe's.
- A series with no books yet can have defaults ("no chapter's tense
  changes now"), and a series file without an id gets one (F-ID-6).
- A value that doesn't fit the field (`tense: sideways`) is saved, as
  in `update_chapter_fields`, with the F-CHAP-3 warning, and the result
  says it's ignored, so chapters inherit as if it weren't there.
  `get_chapter_defaults` lists it as `ignored`, with the value as
  written. A value the same as the one above is said to be so.

Spec: types.md F-CHAP-5, format.md F-CTR-5, F-RT-6 (maintainer
decisions, 2026-10-23, 2026-10-25). Covered by:
`ChapterDefaultsToolTests.ABooksDefaultPovReachesTheChaptersThatDontSetTheirOwn`,
`ChapterDefaultsToolTests.ASeriesDefaultSaysWhichBooksAndChaptersItReaches`,
`ChapterDefaultsToolTests.ASeriesWithNoBooksYetCanHaveDefaultsAndGetsAnId`,
`ChapterDefaultsToolTests.AValueThatDoesntFitTheFieldIsSavedWithAWarningAndSaysItsIgnored`,
`ListItemToolTests.ChapterDefaultsMoveAndReplaceAnItemKeepingItsComments`.

### SC-MCP-25. My assistant changes a default a finished book inherits

`ash` is final, and its own `chapter_defaults` set `tense: past`.

- Adding a default POV to the series `long-dark` would change what
  `ash`'s chapters have, so it's refused as `frozen`, naming the book
  and chapters, and nothing changes. So is any edit of `ash`'s own
  defaults, and setting the universe's `narration`, which `ash` would
  inherit.
- Setting the universe's `tense` is made: `ash`'s own default shadows
  it, and the result says `ash`'s chapters keep `past`.
- A second book named `ash`, in another series, doesn't hide the final
  one: books are matched by folder, and the result names each `ash` by
  its folder.
- A universe-scope Mara Voss means `[[mara-voss]]` in the universe's
  defaults isn't the series' Mara Voss, so setting the same text in the
  series' defaults changes `ash`'s POV, and is refused. Links are
  compared by their text and by the entity they reach, so a different
  spelling of the same link counts as a change too.
- If `ash` is frozen while the edit is being made, the write is refused
  as `stale_file`, saying to call `get_chapter_defaults` again. If
  `ash`'s `book.md` has errors, so the den leaves it out, but it says
  `status: final`, the edit is refused too; a draft book with errors
  doesn't stop it, and the result says its chapters will inherit the
  edit once it's fixed.

Spec: format.md F-BOOK-1, F-KEY-5, types.md F-CHAP-4, F-CHAP-5 (#151).
Covered by:
`ChapterDefaultsToolTests.ADefaultThatWouldChangeAFinalBooksChaptersIsRefused`,
`ChapterDefaultsToolTests.AFinalBookIsFoundByItsPathWhenAnotherBookSharesItsKey`,
`ChapterDefaultsToolTests.ALinkInADefaultIsComparedByTheEntityItReaches`,
`ChapterDefaultsToolTests.ABookFrozenWhileTheEditIsMadeStopsIt`,
`ChapterDefaultsToolTests.ABookLeftOutForErrorsThatSaysFinalStopsTheEdit`.

### SC-MCP-26. My assistant renames a character

A note on Whitby dialect links `[[Mr. Swales]]`, `[[mr. swales # voice
|how he talks]]`, and `[[mr-swales]]`, and a field links
`[[Mr. Swales#Voice]]`; a chapter's `pov` in `book.md` and a row of the
outline (`[[Mr. Swales\|Swales]]`) link him by name too. The assistant
renames him Old Swales.

- `rename_entity` changes his `name`, and his title heading, which read
  the old name, and rewrites every link that reached him by the old
  name to `[[Old Swales]]`, keeping each link's spacing, heading, and
  label, in the same change. The link by file name is left alone, and
  so is the file's name. The result lists every file written with its
  new hash, and `check` finds no broken or ambiguous link.
- With `keep_old_name`, "Mr. Swales" becomes an alias instead and no
  link changes, which suits a name that changes in the story. If
  another character in the book goes by "Mr. Swales" too, the result
  says that links by it are now ambiguous (F-LINK-7), naming the link
  in the note. A new name that only changes case or spacing changes no
  link.
- If a note in a final book uses the old name, nothing is renamed
  (`frozen`), and the error suggests `keep_old_name`, which leaves the
  frozen book alone. Swales in a final book isn't renamed at all. A new
  name that would send the rewritten links elsewhere ("Renfield", R. M.
  Renfield's file name) is refused, and so is one a link can't hold
  (`#`). A file with errors that uses the old name is named in the
  result for the writer to fix by hand, and a write that fails part way
  puts back what it wrote, as does one where `book.md` changed after
  the rename was planned. The id still never changes.
- Links in code, HTML comments, embeds, and escaped links are left
  alone. A link in a block or folded scalar, or a quoted value, is
  spliced in place; a plain value the new name ("Swales: the elder")
  would make read differently is written anew, quoted. Renamed in the
  universe, a character whose new name a character in the book has
  ("Mr. Swales") would lose the book's links to that one, so the
  rename is refused, naming them. Renaming the Count to his own alias
  "Dracula" takes it out of his aliases.
- Renamed "the Count", Swales would take a note's `[[the Count]]` from
  Count Dracula, since a name matches before an alias, even with
  `keep_old_name`. That's refused, listing the link with its old and
  new target (and saying if it's in a final book); with
  `allow_relinks`, the rename is made and the result lists the link. A
  new name another note already has (Renfield's file name) is said in
  the result even when the old name is kept. If a sailor in the
  universe is also named Mr. Swales, keeping "Mr. Swales" only as an
  alias would send a book note's `[[Mr. Swales]]` to the sailor, whose
  name now wins: refused the same way, unless `allow_relinks`.

Spec: format.md F-RT-5, F-LINK-1, F-LINK-4, F-LINK-6, F-LINK-7, F-SEC-2,
F-BOOK-1 (maintainer decisions, 2026-10-25 and 2026-10-26). Covered by:
`RenameEntityToolTests.RenamingAnEntityRewritesTheLinksThatUsedTheOldName`,
`RenameEntityToolTests.KeepingTheOldNameAsAnAliasLeavesTheLinksAlone`,
`RenameEntityToolTests.ALinkInAFrozenBookStopsTheRewriteButNotKeepingTheOldName`,
`RenameEntityToolTests.AKeptAliasThatAnotherEntityHasIsSaidToBeAmbiguous`,
`RenameEntityToolTests.ANewNameThatWouldSendARewrittenLinkElsewhereIsRefused`,
`RenameEntityToolTests.AFileWithErrorsThatUsesTheOldNameIsNamed`,
`RenameEntityToolTests.ARenameThatFailsPartWayPutsBackWhatItWrote`,
`RenameEntityToolTests.RenamingToOneOfItsOwnAliasesTakesThatAliasOut`,
`RenameEntityToolTests.ANewNameThatCollidesIsSaidEvenWhenTheOldNameIsKept`,
`RenameEntityToolTests.ABookEntityWithTheNewNameShadowsItForLinksInTheBook`,
`RenameEntityToolTests.LinksInCodeCommentsAndEmbedsAreLeftAlone`,
`RenameEntityToolTests.LinksInFrontmatterValuesKeepTheirStyleOrAreQuotedAnew`,
`RenameEntityToolTests.AContainerFileChangedWhileTheRenameIsMadeStopsIt`,
`RenameEntityToolTests.ANameThatTakesAnotherEntitysLinksIsRefusedUnlessAllowed`,
`RenameEntityToolTests.ARelinkInAFinalBookIsListedAndAllowedOnlyWithTheFlag`,
`RenameEntityToolTests.AKeptNameThatLosesItsLinksToAnotherEntitysNameIsRefusedUnlessAllowed`,
`EntityRenameEditTests.ALinksTargetIsReplacedKeepingItsSpacingHeadingAndLabel`,
`EntityRenameEditTests.TheTitleChangesAndNothingElse`,
`EntityRenameEditTests.ASetextTitleOrOneWithACommentKeepsItsForm`.

### SC-MCP-27. My assistant changes one of the Count's aliases

A note links `[[the Count]]`, `[[The  count|him]]`, `[[Count de
Ville]]`, and `[[Dracula]]`. The assistant changes the alias "the
Count" to "the Voivode", then removes "Count de Ville".

- `change_alias` changes the alias in place and rewrites the two links
  that used it to `[[the Voivode]]`, label kept. Removing "Count de
  Ville" takes it out of `aliases` and rewrites its link to the
  Count's name. With `keep_old_alias`, the new alias goes in beside the
  old one and no link changes. An alias he doesn't have, a new alias
  that's already his name or another of his aliases, and
  `keep_old_alias` without a new alias are refused.
- If a note in a final book uses the alias, it isn't removed or
  changed (`frozen`); a change suggests `keep_old_alias`. If a legend
  note in the universe is also named Count Dracula, a universe note's
  `[[the Count]]` rewritten to his name would reach the legend, so
  removing the alias is refused, suggesting keeping it or linking by id
  or file name first.
- A new alias "Lucy" would make a note's `[[Lucy]]`, which reaches Lucy
  Westenra, ambiguous: refused, even with `keep_old_alias`, unless
  `allow_relinks`, and then the result lists the link. A link that was
  ambiguous before (`[[the sailor]]`, which Swales and Young Swales
  both go by) and reaches Young Swales once Swales's alias changes is
  only reported.

Spec: format.md F-RT-5, F-RT-6, F-ENT-4, F-LINK-7, F-BOOK-1 (maintainer
decisions, 2026-10-25 and 2026-10-26). Covered by:
`RenameEntityToolTests.ChangingOrRemovingAnAliasRewritesItsLinks`,
`RenameEntityToolTests.RemovingAnAliasALinkInAFrozenBookUsesIsRefused`,
`RenameEntityToolTests.RemovingAnAliasWhoseLinksWouldReachAnotherEntityIsRefusedWithItsOwnHint`,
`RenameEntityToolTests.AnAliasThatTakesAnotherEntitysLinksIsRefusedUnlessAllowed`,
`RenameEntityToolTests.AnAmbiguousLinkThatNowResolvesIsReportedNotRefused`.

### SC-MCP-28. My assistant copies a character to start a new one

The writer asks for another nobleman like the Count, Count Varga, and
for Mr. Swales's son in a sequel book, Aftermath. Later they start a
legend universe and want a Swales there too.

- `copy_entity` writes `count-varga.md` beside the Count: a new id that
  no note in the den uses (one that clashes is tried again), the name
  Count Varga, his title heading, and no aliases, which stay the
  Count's ("Dracula", "the Count", "Count de Ville"), and the result
  says so. Everything else is the Count's, byte for byte, line endings
  and byte order mark included, and the Count's own file is unchanged.
  A second Count Varga gets `count-varga-2.md`, as does one whose file
  name another file has in a different case; a slug asked for that's
  taken is refused. "Ærin Søvik" gets `aerin-sovik.md`, and a name with
  nothing to make a slug from uses the type key. A note without an id
  gets one in its copy. Comment lines inside a block `aliases` list go
  with it, and the result counts them. Only entities can be copied: `book.md`, an
  outline, `universe.md`, or `den.yaml` is no entity. Links in the copy stay as they
  are: one to Swales's own `#Arc` still reaches Swales, and the result
  counts it.
- Copied into Aftermath, Swales's son lands in
  `aftermath/entities/characters/`, and copied into a series, Count
  Varga lands in its `entities/characters/`. His `from`, `dracula:6`, is in
  another book, which the result says and `check` reports (F-ENT-6). A
  note without `from` copied there enters at the start of Aftermath,
  not of the story, and the result says so. Nothing is copied into a
  final book, but a note can be copied out of one.
- Copied into the legend universe, the copy's `[[whitby]]` is broken
  there (links don't cross universes), and its positions name books
  the legend doesn't have: the result lists the broken link, with what
  it reached in the original, and the positions, and the copy is made.
  A session pinned to one universe can't copy into another.
- Named "the Count", a copy of Swales would take a note's `[[the
  Count]]` from Count Dracula, since a name matches before an alias:
  refused, naming the link, unless `allow_relinks`, and then the result
  lists it. Named "Mr. Swales" beside the original, the copy would
  leave the note's `[[Mr. Swales]]` ambiguous (F-LINK-7), reaching
  neither: refused the same way. So is a universe note's copy filed as
  `notes/whitby.md`, which would tie with the location `whitby.md` for
  every `[[whitby]]`, and a copy of Swales filed in the book as
  `whitby.md`, whose nearer scope would take the book's `[[whitby]]`
  from the location. A broken `[[Old Gale]]` that a copy named Old Gale
  now mends is only reported. A stale `expected_hash` is refused.

Spec: format.md F-ID-3, F-ID-4, F-ID-6, F-LINK-5, F-LINK-6, F-LINK-7,
F-LINK-8, F-ENT-6, F-POS-4, F-BOOK-1, types.md F-TPL-6 (maintainer
decisions, 2026-10-23 and 2026-10-26, #129). Covered by:
`CopyEntityToolTests.ACopyGetsANewIdNameAndFileAndKeepsTheRest`,
`CopyEntityToolTests.LinksInTheCopyAreLeftAsTheyAreAndThoseToTheOriginalAreSaid`,
`CopyEntityToolTests.ACopyIntoAnotherScopeKeepsItsTypeFolderAndSaysWhereItEntersCanon`,
`CopyEntityToolTests.NothingIsCopiedIntoAFinalBookButACopyOutOfOneIsFine`,
`CopyEntityToolTests.ACopyIntoAnotherUniverseListsTheLinksThatBreak`,
`CopyEntityToolTests.ACopyThatWouldTakeAnotherEntitysLinksIsRefusedUnlessAllowed`,
`CopyEntityToolTests.ANameThatMakesALinkAmbiguousIsRefusedUnlessAllowedButMendingABrokenOneIsReported`,
`CopyEntityToolTests.AFileNameThatTiesInTheSameScopeIsRefusedUnlessAllowed`,
`CopyEntityToolTests.AFileNameInANearerScopeThatShadowsAnotherIsRefusedUnlessAllowed`,
`CopyEntityToolTests.ACopyKeepsLineEndingsAndAByteOrderMark`,
`CopyEntityToolTests.OnlyEntitiesCanBeCopied`,
`CopyEntityToolTests.CommentLinesThatGoWithTheAliasesAreSaid`,
`CopyEntityToolTests.ACopyIntoASeriesGoesInItsTypeFolder`,
`CopyEntityToolTests.SlugsAreMadeAsTheTemplateRuleSaysAndCompareIgnoringCase`,
`CopyEntityToolTests.ANoteWithoutAnIdGetsOneAndAStaleOrBadCopyIsRefused`,
`CopyEntityToolTests.APinnedSessionCantCopyIntoAnotherUniverse`,
`CopyEntityToolTests.ANewIdThatClashesIsTriedAgain`.

### SC-MCP-29. My assistant moves a note to another scope

In the invented Starfall den, The Long Dark is a series of two books,
Ember then Ash, followed by the standalone A Quiet Moon and The Drift
War, a series with no books yet. The assistant moves notes between them.

- Ilse Brandt, filed in Ash with no `from`, moves up to the series:
  `move_entity` moves the file to the series' `entities/characters/`
  and writes `from: ash`, the point where she entered canon, so she's
  still out of Ember's views and in from the start of Ash. The result
  gives the new path and hash, and marks the file with its old path.
  Moved from the series into Ember, its first book, a character with no
  `from` gets none: the series starts where Ember does.
- Mara Voss, a series character from `ember:1`, moved into Ash is
  refused (`entry_outside_scope`): the reader has already met her when
  Ash starts, and the error names both ways forward. With `keep_entry`
  she moves and keeps `from: "ember:1"`, and `check` warns that it's
  outside her book (F-ENT-6). With `enter_at_new_scope` her `from` is
  taken out, so she enters at the start of Ash, and since she holds
  pinned canon the result flags it. A series character with no `from`
  kept this way gets `from: ember`. A character of The Drift War with no
  `from` can't keep its entry point, since no position names it, so only
  `enter_at_new_scope` moves it. Sash Orel, met at `ash:2`, moved into
  Ember is refused too, and the error says that `enter_at_new_scope`
  would bring her in earlier than the reader meets her now; with it, she
  is in Ember's views.
- With a second Tessaly in A Quiet Moon, the Ember outline's
  `[[Tessaly]]` and Mara's `[[tessaly]]` reach the series' Tessaly only
  because it's nearer. Moving it into Ash rewrites both to its id with
  the same text (`[[01JA...\|Tessaly]]` in the table row), in the same
  change; a link from Ash, which still reaches it, is left alone. If
  Ember is final, that outline can't be rewritten, so nothing moves.
  Nothing moves into or out of a final book either.
- The Lantern ship moved into Ash would tie with the Lantern item there,
  so Halden Pike's `[[Lantern]]`, which reaches the item, would become
  ambiguous: refused, naming the link, unless `allow_relinks`, and then
  the result lists it. Pike himself moved to the universe would lose his
  own links to the item, since they'd tie there: each is rewritten to the
  item's id, keeping its `#Overview` anchor or its "the lamp" label, and
  listed in the result, so his canon views read as before. A note in Ash
  whose `[[Lantern]]` would reach the ship once moved into A Quiet Moon
  keeps the item the same way, and its broken `[[Nobody we know]]`
  stays as written. With `allow_relinks` its links are left to follow
  the new scope, and the result lists the one that now reaches the ship.
  A move with such rewrites that fails puts the note back byte for byte.
  The ship moved to the
  universe makes the Reach's ambiguous `[[Lantern]]` reach it, which is
  only reported.
- A file already at the new path stops the move (`already_exists`). The
  file is rewritten in place, then moved whole, so the den never holds
  two copies of it, or none. A move that fails part way, at a link
  rewrite, the move itself, or the last write at the new path, puts
  everything back and leaves no new file. A note without an id gets one.
  A checkpoint after the move counts the old path as the session's,
  even when git sees a delete and an add, and even when the session
  edited the note before moving it.
  An event whose `row` ids would now resolve among other outlines
  (moved from the series to the universe) is said in the result.

Spec: format.md F-RT-5, F-ENT-6, F-POS-7, F-ORD-3, F-LINK-7, F-BOOK-1
(maintainer decisions, 2026-10-24 and 2026-10-26). Covered by:
`MoveEntityToolTests.ABookEntityWithNoFromMovedUpGetsTheEntryPointItHadAsItsFrom`,
`MoveEntityToolTests.AMoveThatStartsAtTheSamePointWritesNoFrom`,
`MoveEntityToolTests.ANarrowingMoveOutsideTheEntryPointIsRefusedNamingBothWaysForward`,
`MoveEntityToolTests.KeepEntryKeepsTheFromAndItsWarning`,
`MoveEntityToolTests.EnterAtNewScopeTakesTheFromOutAndFlagsPinnedCanon`,
`MoveEntityToolTests.ASeriesEntityWithNoFromMovedIntoItsSecondBookWritesTheSeriesStartWithKeepEntry`,
`MoveEntityToolTests.AnEntryPointNoPositionCanNameNeedsEnterAtNewScope`,
`MoveEntityToolTests.LinksThatWouldStopReachingItAreRewrittenToItsId`,
`MoveEntityToolTests.ALinkToRewriteInAFinalBookStopsTheMove`,
`MoveEntityToolTests.ALinkThatWouldReachAnotherEntityIsRefusedUnlessAllowed`,
`MoveEntityToolTests.TheMovedFilesOwnLinksThatWouldBeAmbiguousAreRewrittenToTheirTargetsIds`,
`MoveEntityToolTests.TheMovedFilesOwnLinkThatWouldReachAnotherEntityKeepsItsTarget`,
`MoveEntityToolTests.WithAllowRelinksTheMovedFilesOwnLinksFollowTheNewScope`,
`MoveEntityToolTests.AMoveWhoseOwnLinksWereRewrittenRollsBackWhole`,
`MoveEntityToolTests.AnAmbiguousLinkThatNowResolvesIsReported`,
`MoveEntityToolTests.AFileAlreadyThereStopsTheMove`,
`MoveEntityToolTests.AMoveThatFailsPartWayPutsBackWhatItWrote`,
`MoveEntityToolTests.AnEntityWithoutAnIdGetsOneAndOtherArgumentsAreChecked`,
`MoveEntityToolTests.RowIdsThatNowResolveElsewhereAreSaid`,
`MoveEntityToolTests.TheDenNeverHoldsTwoCopiesOrNoneWhileItMoves`,
`MoveEntityToolTests.EnterAtNewScopeForAnEntryAfterItSaysItEntersEarlier`,
`HistoryToolTests.AMovedFilesOldPathIsTheSessionsWhenGitSeesADeleteAndAnAdd`,
`HistoryToolTests.AFileTheSessionEditedThenMovedIsntChangedOutsideIt`,
`HistoryToolTests.AMoveTheWriterCommittedElsewhereIsForgottenAtTheNextCheckpoint`,
`EntityMoveEditTests.ALinkRewrittenToAnIdShowsTheSameText`,
`EntityMoveEditTests.ATableRowIsFoundWithOrWithoutItsOuterPipes`,
`EntityMoveEditTests.AnOverlayCanLeaveAFileOut`.

### SC-MCP-30. My assistant searches field values

The assistant searches the Starfall den for "Lieutenant", then for
"saltmarsh" in the `home` field, then for "the reach" in a `berth`
field that links Kessan Reach by its id.

- `search` finds Mara's rank entry, says it's in field `rank` (with the
  entry's line), and brackets the match in the snippet. A candidate
  entry is found but not as canon, and `canon_only` leaves it out.
- With `field`, only that field's values are searched, every word
  matching, ignoring case and accents: `home: Saltmarsh Docks` matches
  "saltmarsh" and "DOCKS saltm*", not "saltmarsh harbour"; "Voss" in
  `home` finds nothing, though her name holds it.
- A link matches its note's name and aliases as well as the text as
  written, and the snippet adds the one that matched after `→`:
  `[[<id>]] → [the] [Reach]`. After a rename, the new name matches.
- A field no note gives a value for is `bad_argument`. The tool's JSON
  gains `field` on a field hit; snippets keep their brackets, and the
  web API's match ranges aren't in it.
- A quoted query is a phrase (#185): `habits: [chess, "night watches"]`
  matches `"night watches"` as one match, `chess, [night watches]`, but
  not `"watches night"`, and the berth matches `"kessan reach"` by the
  linked note's name, not `"reach kessan"`.

Spec: design.md §8, §9 (#179; the semantics are the maintainer's
decision of 2026-10-26). Covered by:
`FieldSearchToolTests.OrdinarySearchFindsWordsInFieldValuesAndSaysWhichField`,
`FieldSearchToolTests.OneFieldSearchMatchesTheValuesWordsIgnoringCase`,
`FieldSearchToolTests.ALinkMatchesItsNotesNameAndAliasesAndTheTextAsWritten`,
`FieldSearchToolTests.AQuotedQueryMatchesAFieldValueOrALinkedNameAsAPhrase`,
`FieldSearchToolTests.AFieldNoNoteHasIsABadArgument`,
`FieldSearchToolTests.TheToolsJsonNamesTheFieldAndKeepsBracketedSnippets`,
`SearchTests.SearchWordsMatchFieldTextAsTheSearchTableDoes`,
`SearchTests.PhrasesMatchFieldTextAsTheSearchTableDoes`.

### SC-MCP-31. My assistant and I finish a notes pass with one checkpoint

After drafting chapter 12, the writer and the assistant go over the
notes it touched: the assistant adds to Lucy's and Mina's notes through
the write tools, and the writer fixes the outline by hand.

- When the pass is done, the assistant calls `checkpoint` once, with a
  message that says what the pass was, "Notes pass after drafting ch
  12", not one checkpoint per edit. The server's instructions ask for
  that: checkpoint at natural stopping points, such as a notes pass or a
  planning session, with a descriptive message.
- The checkpoint's message is the assistant's, followed by its
  `Co-Authored-By` line, and its result lists Lucy and Mina under
  `written_by_session`; the outline the writer edited is under
  `changed_outside_session`, so the assistant can say it's in the
  checkpoint too. The history has one new checkpoint for the pass.

Spec: design.md §6, §8 (maintainer decision of 2026-10-27, #30).
Covered by: `HistoryToolTests.AfterANotesPassTheAssistantMakesOneCheckpointNamedForIt`,
`HistoryToolTests.ACheckpointSaysWhichFilesTheSessionWroteAndWhichChangedOutsideIt`.

### SC-MCP-32. My assistant edits a note with a chapter that isn't there

The assistant moves when Mr. Swales's death is learned to `dracula:999`,
a chapter Dracula doesn't have, and adds a section from it; later the
writer, editing by hand, makes the same slip in his Arc's section
comment, and the assistant edits the note again.

- `update_fields` refuses a `from` or `until` that doesn't resolve
  (`bad_position`): the note's `from`, an entry's `from` or `until`,
  and a new entry's, naming the position and where it was going. So
  does `append_section` for a new section's `from` or `until`, and
  `create_entity` for a field entry's. Nothing is written: the fact
  would have dropped out of every canon view (F-POS-4). A position
  that resolves is written; `null` in `update_fields`, or an empty or
  blank `from` or `until` for `append_section`, is none.
- A thread's `paid_off` or a new `planted` of `dracula:999` is refused
  the same way, since the thread couldn't be placed in story order
  (types.md F-FT-10). So is a value that isn't a position at all
  (`chapter nine`, `dracula:9.2`), or a mapping or a list where a
  position goes (`paid_off: {a: 1}`, #222), though in a file each is
  only the F-FT-10 warning; one already there, in the same field and
  with the same content, doesn't stop an edit.
- A refusal for a value that isn't a position gives an example in the
  universe's own keys, the first chapter of its first book that lists
  one ('dracula:1'), skipping a book whose key another book shares, or
  'book:chapter' in a universe whose books list no chapters yet.
- The Arc's `from=dracula:999`, already in the file, doesn't stop an
  edit that leaves it alone: the assistant's field change and its text
  for Voice are saved, and `check` still reports it. Writing
  `dracula:999` again somewhere else, or moving it from the Arc's
  section comment to the note's `from`, is refused. Renaming the Arc's
  heading in the whole file keeps it, as `rename_section` does.
- Appending to Voice with an empty `from` is a plain append: empty is
  none, so it asks for no change to the section's positions.
- A create whose position doesn't resolve is refused for that before
  the links it would take, so the writer isn't asked to create it
  anyway first. The message says to take chapter keys from
  `get_chapter`; on the web page it says to pick a chapter the book
  lists.

Spec: format.md F-POS-4, types.md F-FT-10 (maintainer decisions on
#211 and #217, 2026-10-29).
Covered by:
`WriteToolTests.AFieldEditThatSetsAPositionThatDoesntResolveIsRefused`,
`WriteToolTests.ANewSectionWhosePositionDoesntResolveIsRefused`,
`WriteToolTests.APositionThatDoesntResolveAndIsntTouchedDoesntStopAnEdit`,
`WriteToolTests.ANewEntitysFieldEntryMustResolveToo`,
`WriteToolTests.APositionFieldValueThatDoesntResolveIsRefused`,
`WriteToolTests.APositionThatDoesntResolveMovedToAnotherPlaceIsRefused`,
`WriteToolTests.AnEmptyPositionIsNoneWhenAppendingToASectionThatsThere`,
`WriteToolTests.APositionIsCheckedBeforeTheLinksAnEntityWouldTake`,
`WriteToolTests.AnUntouchedSectionPositionKeepsUnderARenamedHeading`,
`WriteToolTests.APositionFieldValueThatIsntAPositionIsRefusedWhenWritten`,
`WriteToolTests.AMappingOrListInAPositionFieldIsRefusedWhenWritten`,
`WriteToolTests.AMappingInAPositionFieldIsTheSameWithItsKeysReorderedOrItsScalarsRequoted`,
`WriteToolTests.ARefusedPositionsExampleUsesTheUniversesOwnKeys`,
`WriteToolTests.ARefusedPositionsExampleSkipsABookWhoseKeyIsShared`.

### SC-MCP-33. My assistant reads the chapter headings I see

Dracula's `book.md` gets `chapter_heading: [pov, setting, when]`, and
the writer asks the assistant for the book's chapters, then has it take
Bistritz out of chapter 1's setting.

- `get_chapter` gives each chapter's `heading`, as the web UI shows it:
  "Jonathan Harker's Journal · Jonathan Harker — Bistritz & Castle
  Dracula, 3–5 May". Without `chapter_heading` the heading is the
  title.
- `update_chapter_fields`' result gives the chapter's heading as it is
  now: "Jonathan Harker's Journal · Jonathan Harker — Castle Dracula,
  3–5 May".

Spec: format.md F-CTR-6 (#227). Covered by:
`WriteToolTests.EachChapterGivesTheHeadingTheWriterSees` (MCP).

### SC-MCP-34. My assistant finds a chapter value it can't add to

Chapter 7 of Dracula says `setting: Whitby harbour`, written as plain
text before the core pack made `setting` a list of links to locations,
so the den ignores it (types.md F-CHAP-3). The writer asks the assistant
to add Whitby to chapter 7's setting.

- `get_chapter` lists the value under the chapter's `ignored`, as
  written and why: `{"field": "setting", "as_written": "Whitby harbour",
  "reason": "setting is a link to one entity, such as …, not 'Whitby
  harbour'."}`. Its `fields` leave it out, and a chapter with nothing
  ignored has no `ignored`. A list item that doesn't fit (`setting:
  ["[[bistritz]]", Borgo Pass]`) is listed with its number, `item: 1`,
  and the rest of the list holds. A field written with no value
  (`setting:`) isn't a fault, so it isn't listed. `get_chapter_defaults`
  marks an ignored default with `ignored: true`, `written`, and `reason`.
- An `add` to it is refused (`bad_argument`), naming the chapter and its
  file, what's written, and why it's ignored, then what to do: "'setting'
  in chapter dracula:7 (Cutting from "The Dailygraph," 8 August)
  (universes/dracula/books/dracula/book.md) is written as one value,
  'Whitby harbour', not a list, so 'add' has no list to add an item to.
  That value is ignored: … To replace it, send a 'set' change with the
  whole value; to go back to what the chapter inherits from
  chapter_defaults, send a 'clear' change." A `remove` says the same, and
  nothing is written. A `set` with `["[[whitby]]"]` replaces it.
- One value that holds, such as `setting: "[[whitby]]"`, isn't a list
  either: an `add` is refused the same way, without calling it ignored.
- A field that takes one value has no list, whatever is written: an
  `add` to `tense` is refused ("takes one value, not a list … To change
  it, send a 'set' change with the new value"), so it never writes
  `tense: [past]`, which would be ignored.
- In a universe's `chapter_defaults` there's nothing above to go back
  to, so the refusal says `clear` takes the value out, so its books have
  none unless their series or book sets one.
- No refusal of `update_chapter_fields` or `update_chapter_defaults`
  names a YAML path such as `chapters.6.fields.setting`, and each names
  the file. An item that isn't there (by number or by text, which adds
  that `get_chapter` lists the items), a place past the end of a list, a
  `remove` from a field the chapter doesn't write or writes with no
  value, a field the chapter doesn't set, and `fields` that aren't a
  mapping each name the chapter (or "the chapter_defaults of book
  'ember'") and the field. A chapter's title is folded onto one line and
  cut short with "…" past 60 characters.

Spec: types.md F-CHAP-3, F-CHAP-5 (#233). Covered by:
`WriteToolTests.AnIgnoredChapterValueIsShownAndItsRefusalsSayWhatToDo`,
`WriteToolTests.ChapterFieldRefusalsNameTheChapterNotAYamlPath`,
`WriteToolTests.ARefusalNamesAChapterByItsTitleOnOneLine`,
`ChapterDefaultsToolTests.AnAddToADefaultThatIsntAListNamesTheContainerAndSaysWhatToDo` (MCP).

## STORE: Wolfden saves, moves, and deletes notes

Background: the den store (design.md §6, #121) is how Wolfden writes a
den's files: the write API, and the MCP write tools to come (#21), save
through it. A den on disk is written in place, atomically, byte for byte
as given. The store refuses a path that could leave the den, or that
wouldn't be the same file on every OS, before anything is touched. The
examples use the invented Starfall den from the tests.

### SC-STORE-1. Wolfden saves a note while something else is reading it, or the power goes out

Wolfden saves Mara Voss's note while the index's watcher, or another
program, reads it, and once the machine loses power part way through a
save.

- Whatever reads the note sees the old note or the new one, never part
  of either, and after a failed save the old note is still there,
  unchanged. No temporary file is left among the notes, and the watcher
  never indexes one. The saved bytes are exactly the ones given: line
  endings, a byte order mark, and a missing final newline are kept.

Spec: F-RT-1, F-ENC-2, F-LAY-2, design.md §6. Covered by:
`DenStoreTests.AWriteKeepsTheBytesExactlyAsGiven`,
`DenStoreTests.AWriteLeavesNoTemporaryFileBehind`,
`DenStoreTests.AWriteThatFailsPartWayLeavesTheOldFileAndNoTemporaryFile`,
`DenStoreTests.AReaderSeesTheOldFileOrTheNewOneNeverPartOfEither`,
`DenWatcherTests.AWriteThroughTheDenStoreIsFollowedLikeAnyEdit`.

### SC-STORE-2. My assistant asks to write a file outside the den

An AI assistant asks to save `../outside/escaped.md`, an absolute path,
`.git/hooks/pre-commit`, or a note inside a folder that's a link to a
folder outside the den.

- The write is refused, saying why, and nothing outside the den, or in
  `.git/` or `.wolfden/`, is created, changed, or deleted. The same holds
  for deleting and moving.

Spec: F-LAY-2, design.md §6. Covered by:
`DenPathTests.APathThatCouldLeaveTheDenOrIsHiddenIsRefused`,
`DenPathTests.ANameWindowsDoesntKeepAsWrittenIsRefusedOnEveryOs`,
`DenStoreTests.AWriteOutsideTheDenOrIntoAHiddenFolderIsRefusedAndTouchesNothing`,
`DenStoreTests.AnAbsolutePathIsRefused`,
`DenStoreTests.EveryWriteChecksItsPaths`,
`DenStoreTests.AWriteNeverGoesThroughAFolderLink`.

### SC-STORE-3. A new note whose name differs from another only in case

Series `long-dark` has `mara-voss.md`. Wolfden is asked to save
`Mara-Voss.md` beside it, or a note under `universes/Starfall/`.

- The save is refused on every OS, naming the file or folder that's
  already there. On Windows and macOS the two names are one file, so the
  save would overwrite Mara's note; on Linux the den would no longer
  clone onto those systems. A Windows short name such as `CHARAC~1`, and
  a name that differs only in Unicode form (`café` typed two ways), are
  refused the same way. So is a new name that an invisible character
  disguises: a zero-width space, a byte order mark, or a text-direction
  control anywhere in it, an invisible character at its start or end, or
  one that looks empty or hidden without them. Emoji (a red heart with
  its variation selector, a family joined with zero-width joiners) and
  the zero-width non-joiner of Persian and Indic spelling are fine, and a
  file already named with any of these, made outside Wolfden, can still
  be saved, moved, and deleted.

Spec: design.md §6. Covered by:
`DenStoreTests.ANameThatDiffersOnlyInCaseIsRefusedOnEveryOs`,
`DenStoreTests.AWindowsShortNameIsAnotherNameAndIsRefused`,
`DenStoreTests.ANameInAnotherUnicodeFormIsRefused`,
`DenStoreTests.ACaseOnlyRenameOntoAnotherFileWithThatNameIsRefused`,
`DenPathTests.ANewNameThatInvisibleCharactersDisguiseIsRefused`,
`DenPathTests.EmojiAndScriptsThatNeedJoinersAreFine`,
`DenStoreTests.ANewNameDisguisedByAnInvisibleCharacterIsRefused`,
`DenStoreTests.ANoteNamedWithEmojiOrAJoinerCanBeMadeMovedAndDeleted`,
`DenStoreTests.AFileAlreadyNamedWithAnInvisibleCharacterCanStillBeSavedMovedAndDeleted`.

### SC-STORE-4. Wolfden is asked to change a built-in pack

Something asks the store of the built-in packs to change
`core/types/thread.yaml`.

- It's refused as read-only, with a message that says to extend the type
  from a pack in the den's `packs/` folder instead. The built-in pack is
  unchanged.

Spec: F-PACK-4, design.md §6. Covered by:
`DenStoreTests.TheBuiltInPacksRefuseEveryWrite`.

### SC-STORE-5. I rename a note or a book's folder

The writer renames `mara-voss.md` to `Mara-Voss.md`, which changes only
the case, and moves book `ember` out of its series.

- The note keeps its bytes under its new name, and the book's folder
  moves with everything in it. A rename that changes only the case works
  on Windows and macOS too. A move never overwrites a file or folder
  that's there, and a folder can't be moved into itself. A case-only
  rename that fails part way leaves the folder under its old name.

Spec: design.md §3, §6. Covered by:
`DenStoreTests.AFileIsMovedOrRenamedWithItsBytes`,
`DenStoreTests.ARenameThatChangesOnlyTheCaseIsAllowed`,
`DenStoreTests.AFolderIsMovedWithEverythingInIt`,
`DenStoreTests.AMoveDoesntOverwriteOrMoveAFolderIntoItself`,
`DenStoreTests.ACaseOnlyFolderRenameGoesThroughAHiddenNameWhenThePlainMoveIsRefused`,
`DenStoreTests.ACaseOnlyFolderRenameThatFailsHalfWayIsPutBack`.

## HIST: version history

Background: history is a capability that a backend provides (design.md
§6). Git is the v0 backend (#19); a den can also be a plain folder, with
no history at all.

### SC-HIST-1. My den has no history

The writer keeps the den in a synced folder without git, and asks for a
note's history, a diff, a checkpoint, or a freeze tag.

- Each says, in words, that the den has no history, so that isn't
  available, and how to turn it on: `wolfden init --git` in the den's
  folder, or **Turn on history** in the web UI's checkpoint dialog (SC-HIST-14),
  not for a den in a synced folder, with a private remote for backup. None of them is an error, and none points the writer at
  a design document or a command that doesn't exist. A checkpoint's
  message adds that the edits are still saved in the files. The den still loads, checks,
  and indexes, and freezing a book is still `status: final` (F-BOOK-1);
  only the tag is missing.

Spec: F-BOOK-1, design.md §5, §6. Covered by:
`HistoryProviderTests.ADenWithoutABackendHasNoHistoryCapabilities`,
`HistoryProviderTests.EveryOperationSaysHistoryIsntAvailableWithoutThrowing`,
`HistoryProviderTests.TheMessagesSendTheWriterNowhereTheyCantGo`,
`HistoryProviderTests.ADenWithoutHistoryStillLoadsAndChecks`,
`GitHistoryTests.ADenOutsideAnyRepositoryHasNoHistory`,
`GitHistoryTests.AGitAboveACeilingDirectoryDoesntCount`,
`FreezeTagTests.WithoutHistoryTheBookIsStillFrozenButNotTagged`,
`FreezeBookTests.WithoutHistoryFreezingIsStatusFinalAloneAndSaysNoSnapshotWasTaken`.

The scenarios below have a den in a git repository, read through the git
provider (design.md §6), which runs the writer's own `git`.

### SC-HIST-2. I look back through a note's history

The writer has checkpointed Mara Voss's note three times over a month,
once on a branch they later merged, and asks for its history.

- The checkpoints that changed her file are listed newest first, each
  with its message (all of it), author, time, and the files it changed;
  checkpoints that only changed other notes aren't. The den's whole
  history lists every checkpoint. Asking a page at a time, with each
  page starting after the last page's final checkpoint, lists every
  checkpoint once, those that came in through the merge included, in
  the same order as the whole list, even when a page ends on a
  checkpoint from the merged branch. A starting point that isn't one of
  the listed checkpoints is refused. Names with spaces, accents, or
  brackets come back as they are.

Spec: design.md §6 (#19). Covered by:
`GitHistoryTests.ANotesHistoryListsTheCheckpointsThatChangedItNewestFirst`,
`GitHistoryTests.TheLogPagesWithALimitAndABeforeRevisionThroughAMerge`,
`GitHistoryTests.APageThatEndsOnAMergedBranchsCommitLosesNoOlderMainLineCommit`,
`GitHistoryTests.NamesWithSpacesAndAccentsComeBackAsTheyAre`.

### SC-HIST-3. I make a checkpoint at the end of a session

The writer edited Mara's note, deleted Fen Harbour's, and added a new
item, then makes a checkpoint, "End of chapter 14 session".

- One commit records all three changes, and the checkpoint lists them.
  A second checkpoint straight after has nothing to record and makes no
  commit. A checkpoint of only Mara's note records only it, and the new
  note that wasn't named stays as it was. The first checkpoint of a new
  repository works too. A checkpoint needs a message, without control
  characters other than line breaks and tabs. Unicode line and
  paragraph separators (U+2028, U+2029) in a message are kept as
  written, not turned into line breaks (#161). If the writer's
  pre-commit hook refuses the commit, the checkpoint says git refused
  it and why, nothing is recorded, and the new note is untracked again,
  as it was before.

Spec: design.md §6 (#19). Covered by:
`GitHistoryTests.ACheckpointRecordsNewChangedAndDeletedNotes`,
`GitHistoryTests.ACheckpointCanRecordOnlySomeNotes`,
`GitHistoryTests.TheFirstCheckpointOfANewRepositoryWorks`,
`GitHistoryTests.ACheckpointNeedsAMessage`,
`GitHistoryTests.ACheckpointMessageCantHoldControlCharacters`,
`GitHistoryTests.ACheckpointMessageKeepsUnicodeSeparatorsAsWritten`,
`GitHistoryTests.ACheckpointAHookRefusesLeavesTheNewFilesUntracked`,
`GitHistoryTests.ACheckpointCancelledAfterGitCommittedKeepsTheNewFilesTracked`.

### SC-HIST-4. My AI assistant makes a checkpoint

An assistant writing through MCP makes a checkpoint of the notes it
added.

- The commit is the writer's, with their name and signing as git is set
  up, and the message ends with a `Co-Authored-By:` line naming the
  assistant. A co-author that isn't `Name <email>` on one line is
  refused, and nothing is committed.

Spec: design.md §6, §8 (#19, #21). Covered by:
`GitHistoryTests.AnAgentsCheckpointRecordsItAsACoAuthor`,
`GitHistoryTests.ACoAuthorIsANameAndAnEmailOnOneLine`,
`HistoryToolTests.WritesWaitForACheckpointThatNamesTheAgentAsCoAuthor`.

### SC-HIST-5. My den is one folder of a bigger repository

The writer keeps `writing/vael-notes/` as a den inside a repository that
also holds a README and a blog. They've staged a README change by hand,
and edited a blog post without staging it. Their editor keeps settings
in `.obsidian/`, and swap files next to notes.

- The den's history lists only commits that changed the den's files,
  with paths relative to the den. A checkpoint commits only the den's
  files: the staged README change stays staged and out of it, and the
  blog post stays as it was. Hidden files and folders (`.obsidian/`,
  `.wolfden/`, swap files) are never committed, compared, or listed,
  and a commit made by hand that only touched them isn't in the den's
  history. Asking for a hidden path's history is refused.

Spec: F-LAY-2, design.md §6 (maintainer decision, #57). Covered by:
`GitHistoryTests.ADenInALargerRepositoryHasOnlyItsOwnHistory`,
`GitHistoryTests.ACheckpointLeavesWhatTheUserStagedOutsideTheDenAsItWas`,
`GitHistoryTests.HiddenPathsAreNeverRecordedOrReported`.

### SC-HIST-6. What have I changed since my last checkpoint?

The writer edited one note, deleted another, and made a new one, and
hasn't checkpointed yet.

- A diff since the latest checkpoint shows all three, the new note
  included although git doesn't track it yet, each with a unified diff.
  Asking for the diff doesn't stage anything: `git status` is the same
  afterwards. A diff of one file shows only it, and a diff between two
  checkpoints shows a moved note as a rename. One checkpoint's own diff
  compares it with the checkpoint before it, or, for the first, with
  nothing, so its notes show as added; narrowed to a moved note's new
  path, it still shows the rename. Narrowed to a path that can't be a
  den file, such as a hidden one, it shows nothing. A checkpoint that isn't
  in the repository is an error that says so, and a revision is never
  read as a git option.
- A patch shows the note's text as it is. A form feed, a lone carriage
  return, NEL, or a Unicode line or paragraph separator in a note stays
  inside its line, and a note saved with CRLF line endings keeps them in
  the patch as git stores them (with `core.autocrlf` on, git stores LF).
  A note whose name holds one of those characters is listed with its
  own patch beside the others (#161).

Spec: design.md §6 (#19). Covered by:
`GitHistoryTests.ADiffSinceTheLatestCheckpointShowsNewChangedAndDeletedNotesAndLeavesTheIndexAlone`,
`GitHistoryTests.ADiffBetweenTwoCheckpointsShowsARename`,
`GitHistoryTests.ACheckpointsOwnDiffComparesItWithTheOneBeforeOrWithNothingForTheFirst`,
`GitHistoryTests.ARevisionThatIsntOneIsAHistoryException`,
`GitHistoryTests.ACheckpointsOwnDiffOfAPathThatCantBeADenFileIsEmpty`,
`GitHistoryTests.APatchKeepsCharactersThatArentLineFeedsAsTheyAre`,
`GitHistoryTests.ALineInAPatchCantStartAnotherFilesPart`,
`GitHistoryTests.ANoteNamedWithALineSeparatorGetsItsOwnPatch`.

### SC-HIST-7. I freeze a finished book

The writer freezes Ember, book `ember` of universe `vael`, and later
adds more notes. Another den in the same repository also has a book
`ember` in a universe `vael`, and is frozen afterwards.

- Freezing writes `status: final` in Ember's `book.md`, in place, and
  keeps the rest of the file. Then it makes one checkpoint of the whole
  den, "Freeze book 'ember' of universe 'vael'", which holds that change
  and anything else the writer hadn't checkpointed, and tags it
  `book/vael/ember/final`, so the tag names a snapshot where Ember
  already says it's final. The result names the checkpoint and the tag,
  and counts the changed files outside the book that the checkpoint took
  in, without naming them.
  Freezing Ember again does nothing, and says it's already frozen and
  tagged.
- Freezing the other den's Ember adds `book/vael/ember/final-2`, since
  the name is taken and Wolfden can't tell the other den's tag from its
  own; the first tag stays where it is. Wolfden never moves or deletes a
  tag. A freeze tag's number is one more than the highest there
  (`final-08` or `finale` isn't one of them). In a repository where
  nothing in the den has been checkpointed yet, there's nothing to tag,
  and the tag step says so. A tag name git doesn't accept, or one git
  would read as something else (`HEAD`, `@`, `refs/...`), is refused.
  A tag without a message works even when the writer's git signs tags.
- A freeze based on a `book.md` that changed since the writer looked is
  refused (`stale_file`), and nothing is written.

Spec: F-BOOK-1, design.md §5 (maintainer decisions, #31). Covered by:
`FreezeBookTests.FreezingCheckpointsTheWholeDenAndTagsThatCheckpoint`,
`FreezeBookTests.AFreezeBasedOnAStaleBookIsRefusedAndWritesNothing`,
`FreezeTagTests.TheTagIsNamedForTheUniverseAndTheBook`,
`FreezeTagTests.OnlyTheBooksOwnFreezeTagsHaveANumber`,
`FreezeTagTests.EachFreezeAddsTheNextNumberAndNeverMovesATag`,
`FreezeTagTests.AnotherDenInTheRepositoryWithTheSameKeysCountsUpAndDoesntMoveTheTag`,
`FreezeTagTests.BeforeAnyCheckpointThereIsNothingToTag`,
`GitHistoryTests.ATagNamesTheLatestCheckpointAndIsNeverMoved`,
`GitHistoryTests.ATagNameMustBeOneGitAccepts`,
`GitHistoryTests.ALightweightTagWorksWhenGitSignsTags`.

### SC-HIST-8. Git isn't installed

The writer's den is in a repository, but this machine has no `git` on the
PATH.

- Each history operation says that git isn't installed, or can't be
  found, so that isn't available, and how to get history; none throws.
  A checkpoint's message adds that the edits are still saved in the
  files.

Spec: design.md §6 (maintainer decision, 2026-10-22). Covered by:
`GitHistoryTests.WithoutGitEveryOperationSaysSoWithoutThrowing`.

### SC-HIST-9. A merged checkpoint's clock was wrong

The writer checkpointed Mara's note at 9:10, then, on a laptop whose
clock was behind, made a branch from it with a checkpoint dated 9:01,
checkpointed again on the main line at 9:20, and merged the branch.

- The den's history lists the merge, the 9:20 checkpoint, the 9:01
  one, and then the 9:10 one it came from: never a checkpoint before
  one that came from it, though git's own order, by date alone, puts
  9:10 first. Asking a page at a time gives the same order, and a
  diff since the latest checkpoint starts at the merge. A history
  without merges, or with honest clocks, is in date order as before.

Spec: design.md §6 (#181). Covered by:
`GitHistoryTests.TheLogNeverListsACheckpointBeforeOneThatCameFromIt`.

### SC-HIST-10. I compare a note across a rename

The writer renamed `mara.md` to `mara-voss.md` and edited it, and
compares it with a checkpoint from before the rename.

- The compare shows a rename from `mara.md`, with the edit, not a new
  note: between two checkpoints, by the new path or the old one, and
  from a checkpoint up to the note as it is now, before and after the
  rename is checkpointed. The note's history lists the checkpoint that
  renamed it as a rename; it ends there, since it follows the current
  path. A folder's view shows a note moved into or out of it as a
  rename too, even when a whole folder of notes was moved into it at
  once.
- A note moved into a hidden folder (`.trash/`) shows as deleted, in a
  compare, a checkpoint's own diff, and the log, even when a copy of it
  appears in the den under another name: the copy is added, not a
  rename of the note. A note brought out of `.wolfden/` into the den is
  added. Their patches are a plain delete's or add's, whole den or one
  path, before or after the checkpoint: none names `.trash` or
  `.wolfden`, or shows what was written in the hidden copy.

Spec: design.md §6, §9 (#175, #181). Covered by:
`GitHistoryTests.ACompareOfOneNoteAcrossARenameShowsTheRename`,
`GitHistoryTests.ACheckpointThatRenamedAWholeFolderShowsEveryRename`,
`GitHistoryTests.ANoteMovedIntoAHiddenFolderIsDeletedNotRenamedToALookAlike`;
through the API, SC-WEB-9.

### SC-HIST-11. A note with errors still has its history

Mina Harker's note has an error now (an unclosed `[` in her `name:`),
so the den leaves it out (F-GEN-2), and the writer wants to see what
changed since it last worked.

- Named by her name, she isn't found, and she has no page. Named by
  her note's path, `get_history` and `/api/history` give the
  checkpoints that changed it, and the diff since the latest one shows
  the line that broke it. The summary knows only the path: its key and
  path are the path, its name is the file name, its scope is the book
  whose `entities/` folder holds it, and its type and status are empty.
  A path that isn't an entity file, such as `book.md`, or a note that
  isn't there, is still `unknown_entity`.
- The browser's History tab needs the note's page, so it can't show a
  note with errors yet (design.md §9).

Spec: F-GEN-2, design.md §8, §9 (#181). Covered by:
`HistoryEndpointTests.ANoteWithErrorsHasAHistoryByItsPath`.

### SC-HIST-12. I unfreeze a book to fix it, then freeze it again

The writer froze Dracula, then finds a mistake in Lucy's note. They
unfreeze the book, fix the note, and freeze it again.

- Unfreezing writes `status: draft` in `book.md` (a `book.md` that had
  no `status` line gets one, and the line is never removed), so the
  book's notes can be edited again. With history, it makes a
  checkpoint, "Unfreeze book 'dracula' of universe 'dracula'", with no
  tag, so the history shows when the book was reopened; the first
  freeze's tag stays where it is. Freezing again adds
  `book/dracula/dracula/final-2` on the new checkpoint, so each freeze
  keeps its own snapshot. Unfreezing a book that isn't frozen changes
  nothing and says so. Freezing then unfreezing gives back `book.md`
  byte for byte: a byte order mark, CRLF line endings, and a comment
  after `status` are kept.

Spec: F-BOOK-1, design.md §5 (maintainer decision, 2026-10-28, #31).
Covered by:
`FreezeBookTests.UnfreezingCheckpointsWithoutATagAndFreezingAgainAddsTheNextTag`,
`FreezeBookTests.UnfreezingWritesStatusDraftRatherThanRemovingTheLine`,
`FreezeBookTests.FreezingAndUnfreezingKeepTheRestOfBookMdByteForByte`.

### SC-HIST-13. Git refuses my freeze's checkpoint or tag

The writer's pre-commit hook refuses the freeze's checkpoint. Another
time, git can't make the tag.

- When the checkpoint fails, the book stays final in `book.md`, with no
  commit and no tag, and the result says why and to freeze the book
  again once that's fixed. Freezing again (the book is already final)
  makes the checkpoint and adds the tag.
- When the tag fails, the checkpoint stands, and the result says so.
  Freezing again tags the checkpoint that made the book final, even if
  the writer has checkpointed other work since.
- Until a freeze is finished, the book's freeze status says it's
  unfinished, so the web UI can offer to finish it.
- A book the writer marked final by hand, then edited in another
  editor, is tagged at the latest checkpoint that changed anything in
  the book's folder, so the snapshot holds the later edit; an edit in
  the folder that isn't checkpointed yet is checkpointed first, and
  tagged `final-2` if an older tag exists.
- If an unfreeze's checkpoint failed (so the latest checkpoint still
  holds the book final and tagged), freezing again writes
  `status: final` back and says the tagged snapshot already holds the
  book as it is, so no tag is added.

Spec: design.md §5 (maintainer decision, 2026-10-25, #31). Covered by:
`FreezeBookTests.ACheckpointGitRefusesLeavesTheBookFinalAndFreezingAgainResumes`,
`FreezeBookTests.ATagGitRefusesLeavesTheCheckpointAndFreezingAgainTagsIt`,
`FreezeBookTests.ABookMarkedFinalByHandIsTaggedWithItsLatestContent`,
`FreezeBookTests.RefreezingAfterAnUnfreezeWhoseCheckpointFailedSaysTheSnapshotAlreadyHoldsIt`.

The scenarios below start from a den without history and turn it on
(#195).

### SC-HIST-14. I turn on history for a den I've been using

The writer has kept the Dracula notes in a plain folder for months, with
a `.gitignore` of their own that ignores `*.bak`. They run `wolfden init
--git` in the den's folder. Another time, they choose **Turn on
history** in the web UI's checkpoint dialog instead.

- The den's folder becomes a git repository. `.wolfden/` is added at
  the end of the writer's `.gitignore`, and the rest of it is left as it
  was (a `.gitignore` that already names `.wolfden/` isn't touched, and
  a den without one gets one). A first checkpoint, "Start of history",
  records every den file as it is, so nothing is left to checkpoint.
- The output lists each step. With `--no-checkpoint` (or the checkbox
  unticked), there's no first checkpoint, and every note is listed as
  not checkpointed yet.
- `--name` and `--universe` are refused on a den that exists: they're
  for a new one. Without `--git`, `init` still refuses an existing den,
  and says `--git` turns on its history.

Spec: design.md §6, "Turning history on", §7 (maintainer decision,
2026-10-28, #195). Covered by:
`HistorySetupTests.ADenWithoutHistoryGetsARepositoryAGitignoreAndAFirstCheckpoint`,
`HistorySetupTests.AnExistingGitignoreKeepsWhatItHasAndGetsTheLineOnlyIfItsMissing`,
`HistorySetupTests.WithoutTheFirstCheckpointTheRepositoryHasNone`,
`InitHistoryTests.AnExistingDenGetsHistoryWithAFirstCheckpoint`,
`InitHistoryTests.FromInsideTheDenWithNoCheckpointItSaysWhatItDidAsJson`,
`InitHistoryTests.NewDenOptionsAreRefusedForAnExistingDen`,
`InitHistoryTests.WithoutGitAnExistingDenIsStillRefused`,
`HistorySetupEndpointTests.TheDialogTurnsHistoryOnAndTheRunningServerUsesItAtOnce`.

### SC-HIST-15. Git doesn't know my name yet

The writer has never used git on this computer, so git has no name or
email. They turn on history for their den.

- Wolfden asks for a name and an email, and says why: git records who
  made each checkpoint. Any name will do, a pen name included, and the
  email can be a placeholder. Until they're given, nothing is changed:
  no repository, no `.gitignore`.
- Given (`--author-name` and `--author-email`, or the dialog's fields),
  they're saved in the den's own repository only (`git config
  --local`), never in the writer's global settings, and the first
  checkpoint is recorded under that name.
- When git has one but not the other, only the missing one is asked for
  and saved; the one git has is left alone, even if another was given,
  and the output says the one given wasn't used.
- When git gives a name and email only inside a repository (`includeIf
  "gitdir:…"`), nothing is asked: they turn up once the folder is a
  repository, and they're used. The dialog's fields are optional while
  it can't be sure.
- A name or email of more than one line, with a control character, or
  with `<` or `>`, is refused, and so is a name git won't record (only
  characters it strips, such as `;`), or an email git would record as
  empty (the same characters), before anything is saved.

Spec: design.md §6, "Turning history on" (maintainer decision,
2026-10-28, #195). Covered by:
`HistorySetupTests.AMissingNameAndEmailAreAskedForAndSavedForTheDensRepositoryOnly`,
`HistorySetupTests.OnlyTheMissingHalfIsSavedAndAnExistingSettingIsLeftAlone`,
`HistorySetupTests.ANameOrEmailThatGitCantRecordIsRefused`,
`HistorySetupTests.ANameAndEmailGitGivesOnlyInsideTheRepositoryAreUsedAndNothingIsAsked`,
`HistorySetupTests.ANameAndEmailGivenWhenGitHasThemAfterAllAreNotUsedAndItSaysSo`,
`HistorySetupTests.ANameGitWontRecordIsRefusedBeforeAnythingIsSaved`,
`InitHistoryTests.ANameAndEmailGitGivesOnlyInsideTheRepositoryAreUsedAndOnesGivenSaySoIfNotUsed`,
`InitHistoryTests.AMissingNameAndEmailAreAskedForThenSavedForTheDenOnly`,
`HistorySetupEndpointTests.AMissingNameAndEmailAreAskedForAndSavedForTheDenOnly`,
`Checkpoint.test.tsx` (web).

### SC-HIST-16. Turning on history stops part way

The writer's global git settings run a pre-commit hook that refuses the
first checkpoint. Another time, the writer had already run `git init` in
the den's folder by hand, and made no commit.

- When the hook refuses, the output says what was done (the repository,
  the `.gitignore`) and what wasn't (the first checkpoint), with git's
  reason, and `init` exits 2. History is on by then: the web UI's
  checkpoint dialog offers the first checkpoint as usual, and running
  `wolfden init --git` again once the hook is fixed finishes the job,
  with the same repository and `.gitignore`.
- A den whose own repository has no commits is finished the same way,
  rather than refused.

Spec: design.md §6, "Turning history on" (#195). Covered by:
`HistorySetupTests.AFailedFirstCheckpointSaysWhatWasDoneAndTryingAgainFinishes`,
`HistorySetupTests.ADenWhoseOwnRepositoryHasNoCheckpointsIsFinished`,
`InitHistoryTests.ADenWhoseOwnRepositoryHasNoCheckpointsIsFinished`,
`HistorySetupEndpointTests.AFailedFirstCheckpointSaysWhatWasDoneAndTheDialogCanFinishIt`.

### SC-HIST-17. Turning on history is refused

The writer runs `wolfden init --git` in a den that's a folder of their
larger `writing/` repository. Another time, in a den that already has
checkpoints. A third time, on a computer without git.

- Each is refused with nothing changed, and `init` exits 2: the den in
  `writing/` already has history there, scoped to its files, so there's
  nothing to turn on; the den with checkpoints already has history on;
  and without git, it says to install git.
- A den in `writing/` with an empty `.git` folder of its own (half
  synced, or half deleted), which git doesn't take for a repository, is
  still in `writing/`, and refused the same way: nothing is saved in
  `writing/`'s config or committed there.
- A den that's an orphan linked worktree of `writing/` (`git worktree
  add --orphan`) has no commits, but its settings are `writing/`'s, so
  it's refused the same way, and `writing/`'s config is left alone.

Spec: design.md §6, "Turning history on", "A den inside a larger
repository" (maintainer decision, 2026-10-28, #195). Covered by:
`HistorySetupTests.ADenInsideALargerRepositoryIsRefusedWithNothingChanged`,
`HistorySetupTests.ADenWhoseOwnRepositoryHasCheckpointsIsRefused`,
`HistorySetupTests.WithoutGitItSaysSoAndChangesNothing`,
`HistorySetupTests.AnEmptyDotGitInsideALargerRepositoryDoesntMakeTheDenItsOwnRepository`,
`HistorySetupTests.AnOrphanLinkedWorktreeIsRefusedAndTheMainRepositorysConfigIsLeftAlone`,
`InitHistoryTests.ADenInsideALargerRepositoryIsRefused`,
`InitHistoryTests.ADenWithHistoryAlreadyIsRefused`,
`InitHistoryTests.WithoutGitInstalledAnExistingDenIsRefused`,
`HistorySetupEndpointTests.ADenInsideALargerRepositoryIsRefused`.

## WEB: the den in a browser

Background: `wolfden serve` (design.md §7, §9, #23) hosts the web UI and
its API on this computer only (127.0.0.1) and opens the default browser.
v0 has no accounts, so it never listens on the network. The browsing,
search, and history pages come in M3 (#24 to #27); their read API comes
first, and SC-WEB-4 to SC-WEB-10 describe what it answers. SC-WEB-11
to SC-WEB-17 describe the browsing pages, SC-WEB-18 to SC-WEB-21 the
search page, and SC-WEB-22 and SC-WEB-23 the history page. SC-WEB-24
and SC-WEB-25 describe field search (#179). SC-WEB-35 to SC-WEB-39
cover images in notes and what a page may load (#180, #186). SC-WEB-40
to SC-WEB-46 cover editing a note's lead and sections in the browser
(#28, part a). SC-WEB-66 covers freezing a book (#31). SC-WEB-80 and
SC-WEB-81 cover adding images from the editor (#187).

### SC-WEB-1. I open my den in the browser

The writer runs `wolfden serve` in their den.

- It prints the address (`http://127.0.0.1:9653/` by default), opens it
  in the default browser unless `--no-open`, and runs until Ctrl+C,
  then exits 0. The page shows the den's name and its universes, with
  how many series, books, and notes each holds; `--universe` shows only
  that one. An edit made in another editor shows on the next load,
  without a restart. If this copy of Wolfden was built without its web
  UI, the page says how to build it, and the API still answers. Without
  a den, or with a universe the den doesn't have, it doesn't start, says
  why, and exits 2.

Spec: design.md §7, §9. Covered by:
`ServeCommandTests.ItServesTheApiAndOpensABrowserUntilStopped`,
`ServeCommandTests.WithoutADenItDoesntStart`,
`ApiTests.HealthSaysOkWithTheVersion`,
`ApiTests.DenGivesItsNameAndUniversesInCamelCase`,
`ApiTests.DenSeesAnEditWithoutARestart`,
`ApiTests.APinnedServerListsOnlyItsUniverse`,
`ApiTests.WithoutABuiltUiTheRootSaysHowToBuildIt`,
`ApiTests.ABuiltUiIsServedWithIndexHtmlForItsOwnRoutes`,
`ServeCommandTests.TheBuiltUiIsServedFromWwwroot`,
`App.test.tsx` (web).

### SC-WEB-2. Wolfden is already serving on that port

The writer runs `wolfden serve` while another `wolfden serve`, or some
other program, already listens on port 9653.

- It doesn't start: it says the port is in use and how to pick another
  with `--port`, and exits 2. `--port 0` picks a free port. A port the
  system won't let it use (one Windows reserves, or one below 1024 on
  Linux) is said as such, with the same advice, and exits 2 too.

Spec: design.md §7. Covered by:
`ServeCommandTests.APortInUseExitsTwoAndSaysSo`,
`ServeCommandTests.APortTheSystemWontLetItUseExitsTwoAndSaysSo`.

### SC-WEB-3. A web page on another site tries to read my den

While `wolfden serve` runs, the writer visits a site whose script sends
requests to a DNS name that points at their own computer.

Another time, a page on another site sends requests straight to
`http://127.0.0.1:9653/api/den`.

- The server listens on 127.0.0.1 only, whatever the environment or an
  `appsettings.json` says, and answers only requests addressed to
  `localhost` or `127.0.0.1`: any other host name gets 400 and nothing
  from the den. The API answers a request the browser marks as from
  another site (`Sec-Fetch-Site: cross-site`, or an `Origin` other than
  the server itself) with 403 `cross_site` and nothing from the den, and
  never sends CORS headers. Its own pages, and requests from outside a
  browser such as `curl`, are answered.

Spec: design.md §9. Covered by:
`ApiTests.ItListensOnLoopbackOnlyAndRefusesOtherHostNames`,
`ApiGuardTests.ACrossSiteRequestIsRefusedWithoutCorsHeaders`,
`ApiGuardTests.ARequestFromItsOwnPageOrWithoutABrowserIsAnswered`,
`ListenerConfigurationTests.EnvironmentVariablesAndAppSettingsAddNoListener`.

### SC-WEB-4. I browse my universe's series, books, and chapters

The writer opens Starfall, which has The Long Dark (Ember, then Ash), a
standalone book A Quiet Moon, and The Drift War, a series with no books
yet.

- `/api/hierarchy` lists the series in story order with their books in
  series order, then the standalone books, each book with its chapters
  (key, number, title, and position). Each universe, series, and book
  says how many entities are filed there itself, in all and by type.
- As of `ember:1`, the counts hold only what the reader has met: Ash's
  Halden Pike isn't counted yet. A position settles the universe, as for
  the MCP tools.
- A server started with `--universe drift` reads only Drift: naming
  another universe, or `*`, is `bad_argument`, and a position in another
  universe doesn't resolve.

Spec: design.md §9 (#24). Covered by:
`ReadEndpointTests.TheHierarchyListsSeriesBooksAndChaptersInStoryOrderWithCountsPerScope`,
`ReadEndpointTests.TheHierarchyAsOfAPositionCountsOnlyWhatTheReaderHasMet`,
`ReadEndpointTests.APinnedServerReadsOnlyItsUniverse`,
`ReadEndpointTests.APinnedServerDoesntNameAnotherUniversesEntityInABrokenLink`.

Chapters and their titles are listed whatever the position (maintainer
decision, 2026-10-26): the as-of view hides notes, not the table of
contents.

### SC-WEB-5. I pick a point in the story

The writer opens the position picker for Starfall.

- `/api/positions` lists every chapter of every book in story order
  (`ember:prologue` through `a-quiet-moon:2b`), with the book's name, its
  series, and the chapter's number and title. A book with no chapters
  yet is listed by its bare key. Every position listed is one `asOf`
  takes. In a den with several universes, the universe must be named.

Spec: design.md §9 (#26). Covered by:
`ReadEndpointTests.PositionsListEveryChapterInStoryOrderWithTitles`.

### SC-WEB-6. I open a character's page, now or as of a chapter

The writer opens Mara Voss's page, then the same page as of `ember:1`.

- `/api/entity` gives what the MCP `get_entity` gives for the same name
  and position, member for member: the full view without a position,
  the canon-as-of view with one, or `inView: false` for an entity the
  reader hasn't met. It adds the scope's container chain (Starfall, The
  Long Dark), where the entity enters canon (its `from`, or the start of
  the scope it's filed in, inherited), and its links: each field value
  or entry and each section lists the links in it, resolved (the entity
  reached, by id and name) or marked broken or ambiguous.
- Backlinks say which entity, field, section, chapter, or outline row
  links to it. As of a position, only links in what the reader has met
  count: Mara's injury section links Kessan Reach only from `ember:2b`.
  A link to an entity the reader hasn't met yet doesn't say what it
  reaches, even by id. Chapter POV fields and outline rows link from
  their chapter, so they count from there on.
- Nothing on any page or list as of a position names an entity the
  reader hasn't met: not its name, an alias, its key, id, or path. When
  Jonathan and Quincey Harker share the alias "Harker", a `[[Harker]]`
  link as of `dracula:25` is ambiguous, names Jonathan, and says only
  that there are others not met yet. A server pinned to one universe
  doesn't name another universe's entity in a broken link's problem.

Spec: design.md §9 (#24, #26). Covered by:
`ReadEndpointTests.AnEntityPageAsOfAPositionGivesWhatGetEntityGives`,
`ReadEndpointTests.ADraculaEntityPageGivesWhatGetEntityGives`,
`ReadEndpointTests.AnEntityPageGivesItsScopeChainEntryPointAndResolvedLinks`,
`ReadEndpointTests.AnEntityWithoutFromInheritsItsScopesStart`,
`ReadEndpointTests.BacklinksSayWhereTheLinkIsAndAsOfAPositionOnlyCountWhatTheReaderHasMet`,
`ReadEndpointTests.AsOfAPositionALinkToAnEntityTheReaderHasntMetDoesntSayWhatItReaches`,
`ReadEndpointTests.BacklinksAsOfAPositionFromChaptersAndOutlineRowsCountUpToThatChapter`,
`SpoilerTests.AnAmbiguousLinksProblemNamesOnlyTheMatchesTheReaderHasMet`,
`SpoilerTests.NothingAsOfAPositionNamesAnEntityTheReaderHasntMet`.

### SC-WEB-7. I list entities and read canon as of a chapter

- `/api/canon` gives what the MCP `canon_as_of` gives, with the same
  series or book filter and type. `/api/entities` without a position
  gives what `list_entities` gives; with `asOf`, only the entities in
  the canon view there, in its order.

Spec: design.md §9 (#24, #26). Covered by:
`ReadEndpointTests.CanonAsOfGivesWhatCanonAsOfGives`,
`ReadEndpointTests.ListsAndSearchesWithoutAPositionGiveWhatTheToolsGive`,
`ReadEndpointTests.ListsAsOfAPositionHoldOnlyWhatTheReaderHasMet`.

### SC-WEB-8. I search my notes

- `/api/search` gives what the MCP `search` gives, with snippets. It can
  also keep only entities of one status, those a series or book filter
  covers (as in a canon view), or, as of a position, hits in the
  entities, sections, and field values the reader has met: "flechette"
  finds nothing at `ember:2` and Mara's injury at `ember:2b`. Field
  search is SC-WEB-24 and SC-WEB-25.

Spec: design.md §9 (#25). Covered by:
`ReadEndpointTests.ListsAndSearchesWithoutAPositionGiveWhatTheToolsGive`,
`ReadEndpointTests.SearchFiltersByStatusScopeAndPosition`.

### SC-WEB-9. I look at a note's history

The writer's den is a git repository with two checkpoints that changed
Mina Harker, and an edit since.

- `/api/history` gives what `get_history` gives, a page at a time.
  `/api/history/diff` gives one checkpoint's own change (the first one's
  shows the note added), the change between two checkpoints, or the
  change since a checkpoint (by default the latest) up to the note as
  it is now, as a unified diff for the UI to show. A checkpoint that
  renamed the note shows the rename, and is listed as one, and a
  compare across it shows the rename too (#181). A checkpoint that
  isn't one is `unknown_checkpoint`; `to` without `from`, or a revision
  that can't be one (it starts with `-` or holds a space), is
  `bad_argument`, and a very long one is cut short in the message.
- A den without history isn't an error: `available` is false, the
  backend is `none`, and `reason` says why. So it is for a diff in a
  repository with nothing checkpointed yet.

Spec: design.md §9 (#27). Covered by:
`HistoryEndpointTests.HistoryPagesThroughAnEntitysCheckpointsAsGetHistoryDoes`,
`HistoryEndpointTests.ADiffIsOfOneCheckpointBetweenTwoOrSinceOneUpToNow`,
`HistoryEndpointTests.ADenWithoutHistorySaysSoRatherThanFailing`,
`HistoryEndpointTests.ACheckpointThatRenamedTheNoteShowsTheRename`,
`HistoryEndpointTests.ARevisionThatCantBeOneIsABadArgumentAndALongOneIsCutShort`.

### SC-WEB-10. The UI asks for something that isn't there

- A position that doesn't resolve is 400 `bad_position`; an entity,
  universe, or series that doesn't exist is 404 (`unknown_entity`,
  `unknown_universe`, `unknown_scope`); a name two entities share is
  409 `ambiguous_entity`, listing both. A parameter an endpoint doesn't
  take (such as `as_of` for `asOf`), one given twice, or a value that
  doesn't fit is 400 `bad_argument`, never quietly ignored. An empty
  `asOf` is `bad_argument` too, rather than the full view.

Spec: design.md §9. Covered by:
`ReadEndpointTests.BadRequestsGetACodeAndAMessage`,
`ReadEndpointTests.AnEmptyAsOfIsRefusedRatherThanShowingEverything`,
`HistoryEndpointTests.ARevisionThatCantBeOneIsABadArgumentAndALongOneIsCutShort`.

### SC-WEB-11. I move through the story with the position bar

The writer opens Dracula in the browser. Then they open a link
bookmarked at chapter 3.

- With no position, the bar says "Everything". The pages ask the API
  without `asOf`, and there's no reset. At `?at=dracula:3`, the slider,
  the book menu, and the chapter menu show chapter 3, and every page
  asks the API as of it.
- The arrow keys on the slider and the step buttons move one chapter.
  A step past the last chapter, or End, goes back to everything and
  takes `at` out of the URL. The book and chapter menus jump there, and
  "Show everything" resets. The URL reads `?at=dracula:5`.
- The position carries to the next page through any link. A position
  the story doesn't have, or a blank `?at=`, shows the API's message and
  offers to show everything; a blank one is never taken for everything.
- Each position's answers are cached on their own: while a new position
  loads, the page says so rather than showing the last position's
  page, and going back to everything shows its page at once.

Spec: design.md §9 (#26, maintainer decision). Covered by:
`PositionBar.test.tsx`, `Navigation.test.tsx` (web).

### SC-WEB-12. As of a chapter, the tree hides what the reader hasn't met

The writer sets the bar to Dracula chapter 3.

- Each group in the tree lists only the notes in the view there. The
  characters list Jonathan and Mina, but not Lucy or Quincey Morris,
  who enter in chapter 5. "2 more appear later" shows those two greyed,
  each with "enters at Dracula chapter 5". A note left out for its
  status, such as an idea, says "not in canon: idea" instead. With no
  position, every note is listed and there's no peek.
- An open peek closes when the position changes, or when the writer
  moves to another chapter's page.

Spec: design.md §9 (#24, #26, maintainer decision). Covered by:
`Browse.test.tsx`, `Navigation.test.tsx` (web).

### SC-WEB-13. I open a note the reader hasn't met yet

The writer, at Dracula chapter 3, opens Quincey Morris's page from a
bookmark.

- The page shows only a banner: "Readers first meet Quincey P. Morris in
  Dracula chapter 5." Nothing else of the note is shown or fetched. A
  button moves the position to chapter 5, and the page appears there.
- "Show it anyway" fetches the note's full view and shows it below the
  banner, greyed, still at chapter 3. Another position, another page,
  or a reload hides it again.

Spec: design.md §9 (#24, #26, maintainer decision). Covered by:
`Browse.test.tsx` (web).

### SC-WEB-14. The links on a note's page

- A resolved `[[...]]` link in a section or a field value is a link to
  that note's page, showing its label or the note's name. A broken link
  is marked as broken, with why. An ambiguous link is marked, and its
  tooltip lists the notes it matches. As of a position, a link to a
  note the reader hasn't met is plain text that doesn't say what it
  reaches. "Linked from" groups backlinks by the note or book they're
  in, and says where each one is.

Spec: design.md §9 (#24). Covered by: `Browse.test.tsx`,
`Navigation.test.tsx` (web).

### SC-WEB-15. A note holds HTML or a script link

A note's lead and a section contain `<script>`, an `<img onerror>`, and
a `javascript:` link.

- The page renders the note's markdown, but no HTML from it. Nothing is
  run, and the `javascript:` link has no address.

Spec: design.md §9 (#24). Covered by: `Browse.test.tsx` (web).

### SC-WEB-16. I open a chapter

The writer opens Dracula chapter 5 from the book's page.

- The page names the chapter by its title, "Letters—Lucy and Mina", and
  lists the notes that enter the story there (Lucy Westenra, Quincey P.
  Morris), hidden with a peek as of an earlier position. A button reads
  the den as of this chapter. A chapter the book doesn't have says so.

Spec: design.md §9 (#24). Covered by: `Navigation.test.tsx` (web).

### SC-WEB-17. I bookmark a note that has no id yet

A note without an id is named by its path, such as
`universes/dracula/notes/the year #1893.md`.

- Its page's URL holds the path, with characters such as spaces and `#`
  encoded, and opens the note. Reloading it, or opening the bookmark,
  serves the page, though the URL ends in `.md`. Real files and `/api`
  are served as before.

Spec: design.md §9 (#24). Covered by:
`ApiTests.AnEntityPageNamedByItsPathIsServedWithIndexHtml`,
`Navigation.test.tsx` (web).

### SC-WEB-18. I search from the top bar

The writer, at Dracula chapter 3, presses `/` and types "blood".

- `/` focuses the search box from anywhere but a text box, where it's
  just typed. Once typing pauses, the search page opens at
  `/u/dracula/search?q=blood&at=dracula:3`, searching only for the
  words typing paused on; Enter searches at once. Further typing there
  replaces the history entry. Opening that URL shows "blood" in the box,
  and leaving the search page empties it.
- A search still waiting on a pause is dropped by Enter or by going to
  another page, such as opening a hit, so it never pulls the writer back
  to search. Moving the position or changing a filter on the same page
  keeps it, and when it runs it reads them as they are then.

Spec: design.md §9 (#25). Covered by: `Search.test.tsx` (web).

### SC-WEB-19. I filter my search

- The page lists the hits by note, best first, each note with its type
  and the universe, series, or book it's filed in. Each hit names its
  section, linked to that section on the note's page
  (`#section-illness`), or "Name or opening" for a match in the name,
  aliases, or lead, with its snippet and the matched words marked.
- Type, scope (a series or a book), status, and "Canon text only" are
  kept in the URL (`type`, `series` or `book`, `status`, `canon=true`)
  and sent to `/api/search`. A blank or unknown value, such as
  `canon=false` or `type=`, is no filter, and a book wins over a series.
  When the API says there are more hits, "Show more matches" asks again
  for more, up to the API's 200; when it can show no more, focus moves
  to the line that says so.

Spec: design.md §9 (#25). Covered by: `Search.test.tsx` (web).

### SC-WEB-20. I search as of a story position

The writer searches "Renfield" at Dracula chapter 3, then follows
"Search everything".

- The search is sent with `asOf=dracula:3`, so it finds only what the
  reader has met by then, and the page says "Searching as of Dracula
  chapter 3". With no hits it says "No matches as of Dracula chapter
  3." There's no peek at hits met later. "Search everything" drops the
  position and searches again. The Type filter lists only the types
  of notes the reader has met, and keeps one already chosen. Results
  from an earlier search at another position never show while the new
  ones load.
- A blank `at=` (or one of spaces) is sent as it is and fails closed:
  the page shows the API's error and "Show everything", never the full
  view. An error the API answers with, such as an unknown type, is
  shown in its words.

Spec: design.md §9 (#25, #26). Covered by: `Search.test.tsx` (web).

### SC-WEB-21. A snippet holds HTML or brackets of its own

- A snippet is rendered as text: `<script>` or `<img onerror>` in a note
  shows as written and never runs. The page marks the match ranges the
  API gives with the hit's text (#179), so a `[[...]]` link or other
  brackets stay as written, and a word inside one is marked only where
  the API matched it.

Spec: design.md §9 (#25, #179). Covered by: `Search.test.tsx` (web).

### SC-WEB-22. I read what changed in a note

The history page shows a change to Mina Harker's note: her status
moved from draft to canon, a line was added at the end of a file that
had no newline there, and the note was saved with CRLF line endings.

- The change reads as a unified diff: each hunk under its `@@` header,
  each line with its old and new line numbers, removed lines in red and
  added ones in green (and named "Removed" and "Added" for a screen
  reader), and within a changed line the words that changed ("draft",
  "canon") marked more strongly. "No newline at end of file" is shown
  under the line it's about. A carriage return, and a NEL, line
  separator, or paragraph separator, is shown as a visible mark, not
  dropped. Long lines wrap. The frontmatter is shown as any other text.
- A rename shows "Renamed from" the old path, and a rename with no edit
  says there's no change to the text. A note that became a link, or an
  empty file, or one whose permissions changed, says so in words.
- HTML in the note, such as `<script>` or `<img onerror>`, is shown as
  text, and nothing in it runs.

Spec: design.md §9 (#27). Covered by: `History.test.tsx` (web).

### SC-WEB-23. I look at a note's history in the browser

The writer's den is a git repository with five checkpoints that changed
Mina Harker, and an edit since. They open her page and pick History.

- The History tab goes to the same page with `?view=history`, keeping
  the position, and works for a note named by a path. The tabs and the
  breadcrumb are in the same place on both tabs. The list shows the
  checkpoints newest first, twenty at a time, each with when it was
  written ("yesterday", with the exact time in its tooltip), its
  author, its message, and how it changed the note (added, modified,
  renamed, or deleted). "Show older checkpoints" loads the next page.
- "Uncommitted changes" is on top, and its diff is shown first. With no
  edits since the latest checkpoint, the latest checkpoint's change is
  shown instead. Picking a checkpoint shows what it changed. Ticking two
  compares them, the older as `from` and the newer as `to`, whichever
  is ticked first; ticking a checkpoint and the uncommitted changes
  compares it with the note as it is now. The choice is kept in the
  URL, and a short id in it still marks its row after a reload. A tick
  doesn't carry over to another note.
- The page says history ignores the story position, and never asks the
  history endpoints as of one. At a position before the note enters,
  both tabs show the #26 banner and nothing of the note until "Show it
  anyway"; the reveal covers both tabs, greyed alike, and is gone after
  leaving the note and coming Back.
- In a den without history, the page gives the API's reason and says
  how to start one: `git init` in the den's folder, or `wolfden init
  --git` for a new den. It isn't shown as an error.

Spec: design.md §9 (#27, the reveal as decided on #26). Covered by:
`History.test.tsx` (web).

### SC-WEB-24. I search one field

The writer's notes give characters an `occupation:`, and Lucy a
`home: Hillingham`.

- Searching "hillingham" with no field finds Lucy's home among the
  other hits: search finds words in field values too. Picking "home" in
  the Field filter searches only that field's values; it's kept in the
  URL (`field=home`) and sent to `/api/search`. A value matches when it
  holds every word of the query, ignoring case and accents, as the main
  search does: `home: Saltmarsh Docks` matches "saltmarsh". A list's
  items are its words.
- A link in a value matches by the linked note's name and aliases as
  well as by the text as written: a berth written as Kessan Reach's id
  matches "kessan reach" and "the reach", and after the place is
  renamed "Old Kessan", it matches "old kessan" though the linking note
  didn't change.
- The hit says which field matched ("Field: home", linked to the note),
  and its snippet is the value as written, then `→` and the linked
  note's name or alias where that's what matched. The Field filter
  lists the fields notes give values for, from `/api/fields`. A field
  no note gives a value for is `bad_argument`.
- Each hit carries its excerpt as plain `text` and its `matches` as
  ranges, so the page marks matches without guessing: a `[[link]]` or
  other brackets in a note are never taken for one. The index keeps
  control characters out of the search table, so the marks it puts in
  snippets can't be the note's own text.
- A quoted query is a phrase in text and in field values alike (#185):
  `"mara voss"` marks "Mara Voss" as one match range, `"voss mara"`
  finds nothing though `voss mara` does, and `"saltmarsh docks"` in
  `home` is one range, `"docks saltmarsh"` none.

Spec: design.md §9 (#179; the semantics are the maintainer's decision
of 2026-10-26; phrases are #185). Covered by:
`FieldSearchEndpointTests.AFieldSearchGivesWhatTheToolGivesWithTextAndMatchRanges`,
`FieldSearchEndpointTests.AQuotedQueryIsAPhraseWhoseMatchIsOneRange`,
`FieldSearchEndpointTests.MatchRangesTellTheSearchsMarksFromTheNotesOwnBrackets`,
`SearchTests.MatchesAreRangesInTheTextSoTheNotesOwnBracketsAndControlCharactersCantPassForThem`,
`Search.test.tsx` (web).

### SC-WEB-25. I search a field as of a story position

Mara Voss, met in *Ember*, has a `watch_mate:` that links Halden Pike by
his id; the reader meets Pike in *Ash*.

- As of `ember:3`, searching "Halden Pike" finds nothing, with or
  without the field: a link matches by name or alias only when the
  reader has met the linked note, so search never confirms the name of
  a note they haven't. The id as written is the writer's text in a note
  the reader has met, so it still matches. As of `ash:1`, and with no
  position, "pike" finds the watch mate, and the snippet reads
  `[[<id>]] → Halden Pike`.
- Only the values in the canon view count: Mara's posting to Kessan
  Reach, from `ash:4`, isn't found at `ash:1`.
- The Field filter lists only the fields of values the reader has met
  (`/api/fields` with `asOf`), as the Type filter lists only met types,
  and keeps one already chosen. Searching a field only notes the reader
  hasn't met have, such as Pike's `debts` before *Ash*, is refused just
  as a made-up field is ("No note the reader has met by ember:3 gives a
  value for a field 'debts'"), so the answer doesn't tell them apart.
- For every position, a field search for a name only an unmet note has
  finds nothing, not just nothing that names it.

Spec: design.md §9 (#179, #26). Covered by:
`FieldSearchEndpointTests.AsOfAPositionALinkMatchesByNameOnlyWhenTheReaderHasMetItsNote`,
`FieldSearchEndpointTests.TheFieldsAreThoseNotesGiveValuesForAndAsOfAPositionOnlyThoseInView`,
`SpoilerTests.NothingAsOfAPositionNamesAnEntityTheReaderHasntMet`,
`Search.test.tsx` (web).

### SC-WEB-35. A note shows an image from a website

Bistritz's note has `![Castle at dusk](https://example.com/castle.jpg)`
in a section, and the den's `den.yaml` doesn't mention `remote_images`.

- The image doesn't load, so opening the note sends nothing to
  example.com. In its place is a small placeholder, "Image not loaded:
  Castle at dusk", with the address as a link that opens in a new tab
  (`rel="noopener noreferrer"`); its tooltip says how to turn images on.
  The same goes for an `http:` address, a `//host/...` one, one written
  as `https:host/...`, and an image given by reference. Without alt
  text it says "Image not loaded". Inside a note's link the address is
  shown as text, so there's no link inside a link. A raw `<img>` or a
  `data:` image never shows.

Spec: design.md §9 (#180, the maintainer's decision), format.md F-DEN-8.
Covered by: `ContentSecurityPolicyTests.ByDefaultTheDenDoesntAllowRemoteImages`,
`Images.test.tsx` (web).

### SC-WEB-36. The browser is told what a page may load

- Every page `wolfden serve` sends has a Content-Security-Policy that
  lets it load script, style, images, and API answers only from the
  server itself (and a `data:` image, for its empty icon), with no
  inline script or style, no plugins, no `<base>`, and no framing by
  another page. The API's answers, the UI's files, and errors (a
  refused host name, a `cross_site` 403, an `internal_error`) get a
  policy that loads nothing, and `nosniff`. The built page has no inline
  script or style for the policy to block. `npm run dev` doesn't get the
  policy, so hot reload still works.

Spec: design.md §9 (#180). Covered by:
`ContentSecurityPolicyTests.PagesGetAPolicyThatLoadsOnlyFromThisServer`,
`ContentSecurityPolicyTests.EverythingElseGetsAPolicyThatLoadsNothing`,
`ContentSecurityPolicyTests.ErrorsGetThePolicyThatLoadsNothing`,
`ContentSecurityPolicyTests.ThePageThatSaysHowToBuildTheUiHasItsOwnPolicy`,
`ServeCommandTests.TheBuiltUiIsServedFromWwwroot`.

### SC-WEB-37. I let my den show images from websites

The writer adds `remote_images: true` to `den.yaml` and reloads the
page.

- Images from websites load, without sending the page's address as the
  referrer, and the page's policy allows `https:` and `http:` images.
  `/api/den` says `remoteImages: true`. Until the UI knows the setting,
  images stay blocked. Setting it back to `false`, or to something that
  isn't true or false (a warning in `wolfden check`), blocks them again
  on the next load, without a restart. A `den.yaml` that doesn't parse,
  or whose `format` this Wolfden can't read, keeps them blocked.
- While a page is open, it keeps the setting it first got, since its
  policy was fixed when it loaded. Turned off, images are blocked at
  once. Turned on, the placeholder stays and says to reload the page,
  rather than showing a broken image.

Spec: format.md F-DEN-8, design.md §9 (#180). Covered by:
`EntityCatalogTests.RemoteImagesAreABooleanThatDefaultsToOff`,
`EntityCatalogTests.ADenYamlWithoutAFormatItCanReadLeavesRemoteImagesOff`,
`ContentSecurityPolicyTests.RemoteImagesTrueAllowsThemInThePolicyAndTellsTheUi`,
`ContentSecurityPolicyTests.ADenYamlThatDoesntLoadKeepsRemoteImagesBlocked`,
`Images.test.tsx` (web).

### SC-WEB-38. A note shows an image kept in the den

Jonathan's note, `universes/dracula/jonathan-harker.md`, has
`![Map of the castle](images/castle-map.png)`, `![Seal](/seal.png)`,
and `![Coach](../shared/coach%20and%20horses.jpg)`.

- The images show, whatever `remote_images` says. Each path starts from
  the note's folder, or from the den root if it starts with `/`, and
  loads from `serve` at `/den-files/universes/dracula/images/castle-map.png`,
  `/den-files/seal.png`, and
  `/den-files/universes/shared/coach%20and%20horses.jpg`.
- `serve` sends each with its image type, `nosniff`,
  `Cross-Origin-Resource-Policy: same-origin`, and a
  Content-Security-Policy with `sandbox`, so an SVG opened at its own
  address runs no script. It's sent with `Cache-Control: no-cache`
  and an `ETag`: after the writer replaces the image, the next load
  shows the new one, and until then the browser gets a 304.

Spec: format.md F-LAY-9, design.md §9 (#186). Covered by:
`DenFileTests.AnImageInTheDenIsServedWithItsTypeAndAPolicyThatRunsNothing`,
`DenFileTests.EachImageTypeHasItsContentType`,
`DenFileTests.TheBrowserRevalidatesAndGetsA304WhileTheFileIsTheSame`,
`Images.test.tsx` (web).

### SC-WEB-39. A note's image isn't there, or isn't one to show

- An image the den doesn't have shows a placeholder, "Image not found:
  <alt text>" (or "Image not found" without alt text), with the path as
  written, and the page doesn't ask for it again. So does a path that
  climbs above the den root (`../../../../outside.png` or
  `/../outside.png`), which the page doesn't ask for at all.
- `serve` answers 404, the same as for a missing file, for anything under
  `/den-files/` that isn't an image in the den: a file that isn't an
  image (`den.yaml`, a `.md` note), a hidden file or folder at any depth
  (`.wolfden/`, `.git/`, `.obsidian/`), a `..` that would leave the den,
  written plainly or percent-encoded, a backslash, a drive, a stream, or
  a wildcard, a folder link (to inside the den or out of it), a file link
  to a file outside the den or to one that isn't an image, and a request
  a browser marks as cross-site.
- Pinned with `serve --universe starfall`, `serve` answers 404 for an
  image under `universes/drift/`, and still serves those under
  `universes/starfall/` and outside `universes/` (`images/crest.png`).
- A path the page can't turn into a file name (an encoded `/`, as in
  `images/a%2Fb.png`) gets the placeholder too, with a tooltip that says
  the path is the problem rather than the den's folder.

Spec: format.md F-LAY-9, design.md §9 (#186). Covered by:
`DenFileTests.AnythingButAnImageInTheDenIsA404`,
`DenFileTests.AnEscapeSentAsWrittenIsA404`,
`DenFileTests.PathsThatLeaveTheDenOrArentWrittenAsDenPathsAreRefused`,
`DenFileTests.AFolderLinkIsntFollowedEvenToImagesInTheDen`,
`DenFileTests.AFileLinkIsServedOnlyIfItLeadsToAnImageInTheDen`,
`DenFileTests.ARequestFromAnotherSiteIsA404`,
`DenFileTests.APinnedServerServesOnlyItsUniverseAndTheDenLevelImages`,
`DenFileTests.APinnedServerDoesntFollowAFileLinkIntoAnotherUniverse`,
`Images.test.tsx` (web).

### SC-WEB-40. I edit a section, or the opening, in place

Mina's note is open. I click Edit under "Overview".

- The section's text opens in place, as Markdown, with highlighting; the
  rest of the note stays readable. Focus moves into the editor. The text
  is the section as written in the file, HTML comments included. Its
  section comment (`<!-- wolfden: from=dracula:5 -->`) isn't in the
  editor; it's shown above it, kept as it is, with "change it with Edit
  file".
- Typing `[[Whit` offers the notes whose names start so, from the search
  API, and picking one writes `[[Whitby]]`.
- Preview shows the text as the page renders it; links show as written
  until the note is saved.
- Save, or Ctrl+Enter (⌘+Enter), sends the text with the file's hash.
  Only the section's own text changes in the file (format.md §12); a file
  without an id gets one (F-ID-6). The editor closes, the page and the
  lists, searches, and history it shows are fetched again, and a note
  says what the save did: its effects, any pinned canon it touched, the
  file's warnings, and that saving doesn't make a checkpoint.
- Cancel, or Escape, closes the editor without saving, and focus goes
  back to the Edit button. With unsaved changes it asks first, in the
  page: "Keep editing" (focused, and back into the editor) or "Discard
  changes".
- The editor always opens on the file as it is now, fetched when it
  opens, never on an answer cached from an earlier edit.
- "Edit opening" does the same for the text before the first section,
  title heading included.
- Text that would add a heading to a section is refused, with a hint to
  use Add section or Edit file; text that would leave the note with an
  error is refused with the errors (`invalid_edit`), and the editor stays
  open.
- While anything is being edited, the page shows the full note, whatever
  the story position, with an "Editing shows everything" badge. A note
  the reader hasn't met can be edited once shown with "Show it anyway".
  When the editor closes, the position view comes back.

Spec: design.md §9 (maintainer decisions on #28, 2026-10-27). Covered
by: `WriteEndpointTests.TheSourceGivesTheFilesHashAndEachSectionsTextAsWritten`,
`WriteEndpointTests.ReplacingASectionChangesOnlyItsTextAndGivesTheFileAnId`,
`WriteEndpointTests.EditingPinnedCanonSaysSo`,
`WriteEndpointTests.ReplacingTheLeadKeepsTheFrontmatterAndSections`,
`WriteEndpointTests.TextThatWouldAddAHeadingToASectionIsRefused`,
`LeadEditTests`, `Editing.test.tsx` (web).

### SC-WEB-41. I add, rename, and remove sections

- Add section asks for a name (offering the type's standard sections the
  note doesn't have), where it goes (at the end, or before a section, at
  that section's level), and its text. A name typed with the heading's
  marks (`## Arc`) is refused, with the name to give instead (`Arc`); a
  name that only starts with `#`, such as `#1 Fan`, is fine.
- Renaming or adding a section to a name another section already has is
  saved, and the result says that a link to that name reaches the first
  one (F-LINK-9).
- Rename turns the heading into a text box. Saving renames the section
  and rewrites every link to it by heading, in any note, as the MCP
  `rename_section` does (format.md F-RT-5); if one of those links is in a
  final book, nothing is renamed.
- Remove asks in the page, not in a browser dialog: "Remove the section
  'Arc' and its 1 subsection?", with "Keep it" focused. Removing takes the
  section's subsections too. Links to it by heading aren't rewritten; the
  result lists them, and what they reach now.

Spec: design.md §9 (#28). Covered by:
`WriteEndpointTests.AddingASectionPutsItAtTheEndOrBeforeAnother`,
`WriteEndpointTests.RenamingASectionRewritesTheLinksToIt`,
`WriteEndpointTests.RemovingASectionTakesItsSubsectionsAndSaysWhichLinksLoseIt`,
`WriteEndpointTests.ASectionNameWithHeadingMarksIsRefusedAndADuplicateNameIsReported`,
`Editing.test.tsx` (web).

### SC-WEB-42. I edit the whole file

- Edit file opens the note's whole source, frontmatter included, for
  anything the other controls can't do, such as a section comment.
- Saving writes it with the file's own line breaks and byte order mark.
  The note's id can't be changed or removed there (F-ID-4). A note
  without an id can be given one there, but not one another note or
  container already has (`already_exists`, F-ID-3), and not one that
  isn't an id (`invalid_edit`, F-ID-1). A new name or alias is saved, and
  the result says, in a writer's words, that links written with the old
  text weren't rewritten, and that an AI assistant working through
  Wolfden can rename the note and rewrite them.
- Text that would leave an error, such as frontmatter that doesn't parse,
  is refused with the errors, and nothing is written.
- A `from=` or `until=` typed in a section comment that names a chapter
  the universe doesn't have (`dracula:999`) is refused (`bad_position`),
  as the assistant's edits are (SC-MCP-32). One already in the file,
  left as it is, doesn't stop the save.

Spec: design.md §9 (#28). Covered by:
`WriteEndpointTests.EditFileReplacesTheWholeFileButNeverItsId`,
`WriteEndpointTests.EditFileRefusesAPositionThatDoesntResolveButKeepsOneAlreadyThere`,
`WriteEndpointTests.EditFileRefusesAnIdAnotherFileHasOrOneThatIsntAnId`,
`WriteEndpointTests.EditFileSaysInTheWritersWordsThatLinksToTheOldNameWerentRewritten`,
`WriteEndpointTests.AnEditThatWouldLeaveAnErrorIsRefusedWithTheErrors`,
`Editing.test.tsx` (web).

### SC-WEB-43. An agent changed the note while I was editing it

I opened "Overview" in the editor; meanwhile an AI agent, through MCP,
saved Mina's note. I press Save.

- Nothing is saved. The page shows my version and the file as it is now
  side by side, with the lines that differ marked, and three choices:
  - **Keep mine** saves my text over the new file, on its new hash.
  - **Take theirs** drops my edit and shows the note as it is now.
  - **Copy mine and reload** copies my text to the clipboard and opens the
    editor again on the file as it is now, with my text shown below it.
- A section is sent by its place and its name together, checked with the
  hash, so an edit never lands in another section of the same name. Keep
  mine finds the section in the new file by the place it had and its
  name, so it stays in the same section even when the new file has two
  of that name.

Spec: design.md §8 "Stale files", §9 (#28). Covered by:
`WriteEndpointTests.AStaleEditIsRefusedWithTheFileAsItIsNow`,
`WriteEndpointTests.AStaleSectionIndexOrNameIsRefused`,
`Editing.test.tsx` (web).

### SC-WEB-44. Editing is guarded

- With unsaved changes, following a link in the app, the browser's Back,
  or moving the story position asks in the page whether to leave ("Keep
  editing" or "Leave without saving"), and closing or reloading the tab
  makes the browser ask. Leaving closes the editor; a new position shows
  the note as of there.
- On a note in a final book, every Edit control is off, and the page says
  why ("... is in book 'dracula', which is final (frozen) ...") and how
  to change that ("Unfreeze… on the page of book 'dracula' opens it
  for editing again.", #209). The API refuses such an edit anyway
  (`frozen`), in the same words.
- One piece is edited at a time: the other Edit controls are off until
  the open one is saved or cancelled.

Spec: design.md §8 "Frozen and pinned", §9 (#28). Covered by:
`WriteEndpointTests.ANoteInAFinalBookIsFrozen`, `Editing.test.tsx` (web).

### SC-WEB-45. Saving what I didn't change leaves the file as it was

A note with CRLF line breaks, a byte order mark, extra blank lines, and
no final newline.

- Saving the opening, a section, or the whole file without changing it
  writes nothing: the bytes are the same. The same holds in a file that
  mixes CRLF and LF line breaks: text that differs only in its line
  breaks is unchanged, so nothing is written.
- In the page, Save is off until the text changes, and Ctrl+Enter on
  unchanged text closes the editor without sending anything.
- Changing one section changes only that section's text (and adds a
  missing id); every other byte, line breaks included, stays as it was.

Spec: format.md §12, F-RT-1, F-RT-2. Covered by:
`WriteEndpointTests.EditingAndSavingTheSameTextKeepsEveryByte`,
`WriteEndpointTests.SavingUnchangedTextInAFileThatMixesLineBreaksWritesNothing`,
`Editing.test.tsx` (web).

### SC-WEB-46. Only Wolfden's own page can edit the den

- An edit is a `POST` with a JSON body. One sent as a form
  (`application/x-www-form-urlencoded`, `multipart/form-data`) or as
  `text/plain` is refused (415, `unsupported_media_type`), so a page on
  another site can't post a form that edits the den.
- One with another site's `Origin`, or `Sec-Fetch-Site: cross-site`, is
  refused (403, `cross_site`), and no answer has CORS headers.
- An edit endpoint answers only `POST` (405 otherwise), and refuses a
  member it doesn't take, such as `expected_hash`, or a missing
  `expectedHash`.
- With `serve --universe drift`, a note in another universe can't be read
  for editing or edited.

Spec: design.md §9 (#23, #28). Covered by:
`WriteEndpointTests.AnEditThatIsntJsonIsRefusedSoAFormCantPostOne`,
`WriteEndpointTests.ACrossSiteEditIsRefused`,
`WriteEndpointTests.AnEditFromItsOwnPageIsAnswered`,
`WriteEndpointTests.AnEditEndpointAnswersOnlyPostAndItsOwnMembers`,
`WriteEndpointTests.APinnedServerEditsOnlyItsUniverse`.

### SC-WEB-50. The top bar tells me what's changed since my last checkpoint

The writer has `wolfden serve` open on a den in a git repository. After
drafting chapter 12 they do a notes pass: two characters edited, a new
location, partly in the browser and partly in their editor.

- The top bar shows "3 notes changed since the last checkpoint ·
  Checkpoint…" ("3 notes not checkpointed yet" before the first
  checkpoint, when every den file counts), and nothing at all when
  nothing has changed. It asks again every minute, when the window
  gets focus back, and straight after a save in the page's editor
  (SC-WEB-40). If the status can't be read, it shows "History
  unavailable", which opens the reason, rather than hiding.
- It opens a dialog (a modal one, not the browser's) that lists the
  changes for review: den-level files first, then the universe's own
  notes, then each series and book, with paths shortened within the
  group and each marked edited, new, deleted, or renamed. A note saved
  in the page's editor is marked "saved in Wolfden"; the others changed
  another way (an editor, an assistant through MCP, or before `serve`
  started), and one saved on the page and then edited by hand isn't
  marked. Focus starts in the message box, which is empty: the writer
  names the checkpoint. Cancel closes the dialog; Escape or a click
  outside does too, but asks first once a name is typed. Focus goes
  back to the indicator.
- `GET /api/checkpoint` lists the changes with their kinds and
  universes; it takes no parameters, and other methods than GET and
  POST are 405 with `Allow: GET, POST`.

Spec: design.md §6, §9 (maintainer decision of 2026-10-27, #30).
Covered by:
`CheckpointEndpointTests.TheStatusListsWhatChangedAndACheckpointRecordsItWithTheWritersMessage`,
`CheckpointEndpointTests.BeforeTheFirstCheckpointEveryDenFileIsListed`,
`CheckpointEndpointTests.ASaveThroughThisServerIsToldApartFromAChangeMadeElsewhere`,
`CheckpointEndpointTests.AWebSaveShowsAsSavedInWolfdenAndStartsAfreshAfterTheCheckpoint`,
`CheckpointEndpointTests.OtherMethodsAreRefusedWithTheOnesItTakes`,
`PendingChangesTests.BeforeTheFirstCheckpointEveryDenFileIsPending`,
`PendingChangesTests.AfterACheckpointOnlyWhatChangedSinceIsPending`,
`Checkpoint.test.tsx`, `Editing.test.tsx` (web).

### SC-WEB-51. My den has no history yet

The writer keeps the den in a plain folder and opens `wolfden serve`.

- The top bar shows "No history · Turn on…". Its dialog says the den has
  no history, that edits are still saved in the files, and offers **Turn
  on history** (SC-WEB-55), not for a den in a synced folder. It offers
  no checkpoint. A server that can't turn history on says to run
  `wolfden init --git` in the den's folder.
- `POST /api/checkpoint` is 409 `history_unavailable`, with the same
  advice. Once history is on, checkpoints work without restarting
  `serve`.

Spec: design.md §6, §9 (#30). Covered by:
`CheckpointEndpointTests.WithoutHistoryTheStatusSaysHowToTurnItOnAndACheckpointIsRefused`,
`Checkpoint.test.tsx` (web).

### SC-WEB-52. I name a checkpoint after a notes pass

Following on from SC-WEB-50, the writer reviews the list and names the
checkpoint "Notes pass after drafting ch 12". After a planning session
another day, they make one named "Planning session: harbour subplot".

- An empty or blank name is refused in the dialog, before anything is
  sent, and the API refuses one too (`bad_argument`), as it does a
  message over 5,000 characters, one with control characters, JSON
  that isn't `{"message": "…"}`, and anything that isn't
  `application/json` (415 `unsupported_media_type`). A request a
  browser marks as from another site is 403 `cross_site`. None of these
  makes a commit.
- **Checkpoint** records every den file that changed, with the name as
  the commit's whole message (trimmed) and the writer as its author,
  with no co-author. The dialog says "Recorded “Notes pass after
  drafting ch 12”, with 3 notes", the indicator goes quiet, and each
  note's History tab lists the new checkpoint. A second one straight
  after has nothing to record, and the dialog says so.
- If git refuses (a pre-commit hook that fails, signing, a lock), the
  dialog shows git's reason, keeps the name to try again, gives focus
  back to it, and nothing is recorded. While the checkpoint is being
  made, focus stays on the dialog.
- On a detached HEAD, or in the middle of a rebase, the dialog warns
  that the checkpoint becomes part of it; the checkpoint is still made,
  as git would make it, and its result says so too.

Spec: design.md §6, §9 (maintainer decision of 2026-10-27, #30; #28,
saving never checkpoints). Covered by:
`CheckpointEndpointTests.TheStatusListsWhatChangedAndACheckpointRecordsItWithTheWritersMessage`,
`CheckpointEndpointTests.ACheckpointNeedsAMessage`,
`CheckpointEndpointTests.ATooLongMessageIsRefused`,
`CheckpointEndpointTests.ACheckpointCantBeMadeFromAnotherSiteOrWithoutJson`,
`CheckpointEndpointTests.AHookThatRefusesIsReportedAndNothingIsRecorded`,
`CheckpointEndpointTests.AManualCheckpointOnADetachedHeadIsMadeButWarned`,
`Checkpoint.test.tsx` (web).

### SC-WEB-53. I turn on automatic checkpoints

The writer adds `auto_checkpoint: 30m` to `den.yaml` and leaves
`wolfden serve` running while they write.

- Once the den's files have gone 30 minutes without a change, `serve`
  makes one checkpoint, named for what changed: "Auto-checkpoint: edited
  1 note", or "Auto-checkpoint: changed 4 notes (2 edited, 1 added, 1
  renamed)", then one line per file, at most 20 and then "and N more".
  It makes no more until the files change again; an edit before the 30
  minutes are up starts the wait again.
- It never starts while a git write may be running (a checkpoint the
  writer is making, a git Wolfden stopped waiting for, or another
  program's `index.lock`), and tries again on the next poll; if git
  stays busy for five minutes, it says so in the log, once. Nor does it
  start while git is in the middle of a rebase, merge, cherry-pick,
  revert, or bisect, or on a detached HEAD: it says so once and makes
  the checkpoint when git's done. If there's nothing to record (the
  writer, or another Wolfden, checkpointed first), it does nothing. If
  git refuses, it logs why once (until the files change) and tries
  again after another 30 quiet minutes.
- A save in the browser while an automatic checkpoint waits on git (a
  signing passphrase prompt, say) isn't held up, and the file saved
  meanwhile stays marked "saved in Wolfden".
- `30m`, `2h`, `1h30m`, `5m`, and `24h` are accepted. An unquoted null
  or `false` leaves it off. `4m`, `25h`, `999999999h`, `1h60m`, `30`,
  `30s`, `1d`, `30M`, a quoted `"false"`, a block scalar, `true`, a
  list, or a mapping is an F-DEN-9 warning in `wolfden check` and in
  `serve`'s log (once), and it stays off. The setting is read on each
  poll, so turning it on or off needs no restart. In a den without
  history, `serve` warns once that the setting does nothing.
- The dialog says when automatic checkpoints are on, and
  `/api/checkpoint`'s `autoCheckpoint` gives the setting as written.
  `wolfden mcp` and `wolfden index --watch` never make them.

Spec: format.md F-DEN-9, design.md §6 (opt-in is the maintainer's
decision of 2026-10-27, #30). Covered by:
`AutoCheckpointTests.AutoCheckpointIsATimeFromFiveMinutesToADay`,
`AutoCheckpointTests.AFileThatDoesntLoadLeavesItOff`,
`AutoCheckpointTests.TheMessageSaysHowManyNotesChangedAndLists`,
`AutoCheckpointTests.AQuietSpellMakesOneCheckpoint`,
`AutoCheckpointTests.ANewChangeStartsTheWaitAgain`,
`AutoCheckpointTests.NothingToRecordDoesNothing`,
`AutoCheckpointTests.AGitWriteUnderWayIsWaitedFor`,
`AutoCheckpointTests.ARefusedCheckpointIsTriedAgainAfterAnotherQuietSpell`,
`AutoCheckpointTests.WithoutTheSettingOrWithABadOneNothingIsMade`,
`AutoCheckpointTests.WithoutHistoryTheSettingOnlyWarns`,
`AutoCheckpointTests.NoAutomaticCheckpointWhileGitIsInTheMiddleOfSomething`,
`AutoCheckpointTests.GitBusyForLongIsSaidOnce`,
`AutoCheckpointTests.ARefusalIsSaidOnceUntilTheFilesChange`,
`AutoCheckpointTests.ASaveDuringACheckpointDoesntWaitAndStaysTheHosts`,
`PendingChangesTests.AnIndexLockMeansAWriteMayBeUnderWay`,
`PendingChangesTests.GitInTheMiddleOfAnOperationIsSaidAndAutoCheckpointsWait`,
`CheckpointEndpointTests.ASaveDoesntWaitForACheckpointThatsWaitingOnGit`,
`CheckpointEndpointTests.TheStatusSaysWhatAutoCheckpointIsSetTo`,
`Checkpoint.test.tsx` (web).

### SC-WEB-54. A server pinned to one universe checkpoints the whole den

The writer serves the Starfall den with `serve --universe starfall`, and
has also edited a note in the `drift` universe and `den.yaml`.

- The indicator counts all three changes. The dialog lists `den.yaml`
  and the Starfall note, and says one more file in another universe is
  included, without naming it or its universe, as the rest of a pinned
  server doesn't.
- The checkpoint records the whole den, the `drift` note included, as
  design.md §6 says a checkpoint does, and as the MCP tool does in a
  pinned session. Its result counts the other universe's file the same
  way.
- The status gives the latest checkpoint by its id and time only, so a
  message such as "Drift: the tide subplot" never shows.

Spec: design.md §6, §9 (#30). Covered by:
`CheckpointEndpointTests.APinnedServerCheckpointsTheWholeDenButNamesOnlyItsUniverse`,
`CheckpointEndpointTests.APinnedServersLatestCheckpointDoesntNameAnotherUniverse`,
`Checkpoint.test.tsx` (web).

### SC-WEB-55. I turn on history from the checkpoint dialog

The writer serves a den without history, chooses **No history · Turn
on…**, then **Turn on history**.

- The dialog says what a checkpoint is, and not to turn history on for
  a den in a synced folder. A checkbox, ticked, makes a first
  checkpoint, "Start of history". When git has no name or email, the
  dialog asks for them (SC-HIST-15); otherwise it doesn't.
- Once it's on, the dialog lists what was done, and the top bar goes
  quiet: the server uses the new history at once, without a restart, for
  the indicator, the History tab, the next checkpoint, and freezing.
- A refusal shows the server's message; if git turns out to need a name
  or email after all, the fields for just what's missing appear, and
  stay while it tries again. When it can't be turned on
  here (no git, say), the dialog says why and offers no button.
- A server pinned to one universe may turn it on: history covers the
  whole den, and the result names no files.
- An automatic checkpoint doesn't run while history is being turned on.
- The request is refused from another site or without JSON, as a
  checkpoint is.

Spec: design.md §6, "Turning history on", §9 (maintainer decision,
2026-10-28, #195). Covered by:
`HistorySetupEndpointTests.TheDialogTurnsHistoryOnAndTheRunningServerUsesItAtOnce`,
`HistorySetupEndpointTests.WithoutTheFirstCheckpointEveryNoteIsLeftToCheckpoint`,
`HistorySetupEndpointTests.ABadRequestIsRefusedWithNothingChanged`,
`HistorySetupEndpointTests.HistoryCantBeTurnedOnFromAnotherSiteOrWithoutJson`,
`HistorySetupEndpointTests.AServerThatCantTurnItOnSaysSo`,
`HistorySetupEndpointTests.APinnedServerTurnsItOnForTheWholeDenAndNamesNoFiles`,
`AutoCheckpointTests.APollSkipsWhileHistoryIsBeingTurnedOn`,
`Checkpoint.test.tsx` (web).

### SC-WEB-60. I edit a field with a control that fits it

The note's type is a den-local `specimen` with a field of every type.

- Each field the type defines has its own Edit. A `string` is a text
  box, `text` a text area, `number` a text box that has to hold a number
  (`heavy` is refused in the page, with the message by it and focus on
  it), `bool` a yes/no dropdown, and `enum` a dropdown of its values. A
  value written `Dried` shows as `dried`, and isn't rewritten unless I
  change it.
- A `date` has its text and an optional sort key: `1893-08-08`, or
  `1893-08-08 06:40` with the time of day, sorts by itself; "early
  August" with sort key `1893, 8` is written
  `{ text: early August, sort: [1893, 8] }`.
- A `ref` looks notes up as I type a few letters, offers the notes of its
  types whose name, alias, or slug starts so (not notes that only
  mention it; SC-WEB-78),
  and the arrow keys and Enter (or a click) write `[[jonathan-harker]]`.
  If another note of the universe has the same file name, the link is
  by id, `[[01J...]]`, so it reaches only the note I picked (file names
  compare ignoring case). A note with no id that no link is sure to
  reach isn't written; the picker says to add an id with Edit file. Escape
  closes the list, not the editor.
- Numbers keep their digits: `2.50` stays `2.50`, and a 20-digit number
  isn't rounded, read or written; `1e400` is refused (`invalid_edit`).
  `0x1F`, `0o17`, `1_000`, `.inf`, and `True` keep their form, unquoted,
  when the field is written whole or an item is moved.
- Saving changes only that field's value, with its comment kept; a value
  that doesn't fit its type is saved with the warning.

Spec: design.md §9 "Editing fields", types.md §5. Covered by:
`FieldEditEndpointTests.TheSourceGivesTheTypesFieldsAndEachValueAsWritten`,
`FieldEditEndpointTests.EachFieldTypeIsSetAndOnlyItsValueChanges`,
`FieldEditEndpointTests.AValueThatDoesntFitItsTypeIsSavedWithTheWarning`,
`FieldEditEndpointTests.NumbersKeepEveryDigitBothWays`,
`FieldEditEndpointTests.HexOctalAndOtherPlainValuesKeepTheirFormWhenWrittenAgain`,
`CheckpointEndpointTests.AWebFieldSaveShowsAsSavedInWolfden`,
`FieldEditing.test.tsx` (web).

### SC-WEB-61. I add, remove, and reorder a list's items

- A list field has Add item, Remove, and move up and down buttons, which
  keep focus on the item. Saving removes the items taken out, adds new
  ones where they go, and moves an item in place (`move`), with its
  comments (#197, SC-RT-17); the others keep their bytes, in the list's
  own style (block or flow). A changed item is set in place, keeping the
  comment on it. Removing an item with a comment says the comment went
  too, and so does an API client that takes an item out and adds it
  again, which writes it afresh.
- Removing the last item leaves `tags:` for a block list and `seen: []`
  for a flow one (format.md F-RT-6): unset either way, with no warning.

Spec: format.md F-RT-6, types.md F-FT-12. Covered by:
`FieldEditEndpointTests.ListItemsAreAddedRemovedAndReorderedInTheListsOwnStyle`,
`FieldEditEndpointTests.RemovingAnItemWithACommentSaysTheCommentWent`,
`ChapterFieldEndpointTests.MovingOrReplacingAnItemKeepsItsComments`,
`FieldEditing.test.tsx`, `ChapterFields.test.tsx` (web).

### SC-WEB-62. I give a fact dates: from chapter 3 until chapter 9

- A field that changes over the story lists its entries, each with
  From and Until, picked by book and chapter from the den's books, or
  typed with "Type a position…". Each says when it holds: "holds from
  Dracula chapter 3 until Dracula chapter 5".
- Add a dated entry adds one at the end; Remove entry takes one out;
  setting Until ends one. Status and Pinned are there for each entry.
  Only the keys I changed are written (`until: "dracula:5"` added to
  that entry's line).
- Unpinning, pinning, or removing a pinned entry is saved, and the result
  lists the pinned canon it touched.
- A plain value that gets a second, dated value becomes a list of
  entries.

Spec: format.md §7.3, §9.3. Covered by:
`FieldEditEndpointTests.DatedEntriesAreAddedEndedAndRemovedAndPinnedCanonIsReported`,
`FieldEditing.test.tsx` (web).

### SC-WEB-63. I add a field, clear one, or edit one in the file

- Add field offers the type's fields the note doesn't set (or sets to
  nothing), in the type's order, and opens that field's form.
- Clear field asks in the page, with Keep it focused, then removes the
  field's lines.
- A field the type doesn't define, or a value the form can't show (such
  as an unquoted `[[velari]]`, F-FLD-6), shows as written with Edit in
  file and why.

Spec: design.md §9 "Editing fields". Covered by:
`FieldEditEndpointTests.ClearingAFieldRemovesItsLinesAndAddingOneGoesAfterTheOthers`,
`FieldEditing.test.tsx` (web).

### SC-WEB-64. Setting a field back gives back the same bytes

- Setting a field to what it says writes nothing (`changed: false`).
  In the page, a form I didn't touch sends nothing, whatever the value's
  shape: an entry's `value: ~` stays `~`, never `[]`.
- Changing a value, an item, or an entry's From, then changing it back,
  leaves the file byte for byte as it was.

Spec: format.md §12, F-RT-1, F-RT-3. Covered by:
`FieldEditEndpointTests.SettingAFieldBackToWhatItWasGivesBackTheSameBytes`,
`FieldEditing.test.tsx` (web).

### SC-WEB-65. Field edits are guarded

- If only another part of the note changed meanwhile, and my save
  changes parts of the field rather than setting it whole, it's saved on
  the new file without asking. If the field's own lines changed (even
  just a comment on them), or my save sets the field whole, I'm asked. If the field changed, a stale save
  shows my value (or "(not set)" for Clear field) and the file's side by
  side: Keep mine saves (or clears) on the new hash, Take theirs drops
  mine.
- A From that isn't a position is refused with the errors
  (`invalid_edit`), and one that names a chapter the universe doesn't
  have is `bad_position` (SC-MCP-32); either way the form stays open. A note in a final book is
  `frozen`.
- An edit names one field, and its paths stay under it: a field name
  that isn't one (F-FLD-1), a path with a dot, an unknown op or member,
  or no changes is `bad_argument`.
- Escape closes the form, or asks first if there are changes.

Spec: design.md §8 "Stale files", §9. Covered by:
`FieldEditEndpointTests.AStaleFrozenOrInvalidFieldEditIsRefusedAndNothingIsWritten`,
`FieldEditEndpointTests.AFromThatDoesntResolveIsRefusedAsBadPosition`,
`FieldEditing.test.tsx` (web).

### SC-WEB-66. I freeze and unfreeze a book on its page

The writer opens Dracula's page and freezes it, then unfreezes it.

- **Freeze…** asks first, in the page: book.md will say
  `status: final`, the book's notes, chapters, and outline can't be
  edited until it's unfrozen, and, with history, Wolfden makes a
  checkpoint and tags it (naming the tag), or, without, no snapshot is
  taken. Escape or Cancel closes it and nothing is sent. Afterwards the
  page shows what was done, and the book's notes say they're in a
  final book. **Unfreeze…** asks the same way, and says the freeze tags
  stay.
- A freeze whose checkpoint or tag failed shows its message as a
  warning, and the final book then offers **Finish freezing…**, which
  freezes again. A freeze based on a `book.md` that changed since is
  refused (`stale_file`), the prompt shows why, and the page looks at
  the book again. The endpoints take only `book`, `universe`, and
  `expectedHash`, as JSON; `/api/book/unfreeze` answers POST only.
- The prompt counts the other changed files the checkpoint will take
  in, without naming them. A server pinned to one universe won't show
  or freeze another universe's book.

Spec: design.md §5, §9 (#31). Covered by:
`FreezeEndpointTests.FreezingCheckpointsAndTagsAndUnfreezingCheckpointsAndTheBooksNotesFollow`,
`FreezeEndpointTests.WithoutHistoryFreezingSaysNoSnapshotWasTaken`,
`FreezeEndpointTests.AStaleOrMalformedFreezeIsRefusedAndWritesNothing`,
`FreezeEndpointTests.APinnedServerWontShowOrFreezeAnotherUniversesBook`,
`Freeze.test.tsx` (web).

### SC-WEB-67. I make a new character where I am

On Dracula's book page, the writer chooses **New ▾** in the top bar,
then Character, names her "Sister Agatha", and creates her.

- The menu lists the den's enabled types, built-in and from the den's
  own packs, by name (`GET /api/types`). It's a menu button: the arrow
  keys, Home, and End move through it, Escape closes it and returns to
  the button, and Enter on a type opens the form.
- The form asks for the type, the name, and where the note goes, which
  starts as where the writer is: the book on a book or chapter page, the
  note's own place on a note's page, and otherwise the universe. A name
  is required before anything is sent. The file isn't named until it's
  made.
- **Create** sends `POST /api/entity/create` with the type, name, and
  place, which makes the file from the template as `create_entity` does
  (SC-MCP-15): a new id, a slug from the name (`-2` when it's taken),
  and no `from`. The page opens the new note, says which file was made,
  and moves focus there; the tree, the counts, search, and the checkpoint
  indicator ask again. Leaving the page ends the notice.
- Each type's list in the contents tree has **New <type>**, with the type
  and that place filled in; both can still be changed.
- Each universe, series, and book page has **New note ▾** (#203), a menu
  button like the top bar's, named for its place ("New note in The
  Harker Papers"). It lists every enabled type, including types with no
  notes there yet, so a new book with no type lists in the tree can
  still have its first character; choosing one opens the form with the
  type and that place filled in. A final book's page has none, as it has
  no Add chapter; the top bar's **New ▾** still reaches the form, which
  explains why the book takes nothing.
- Viewing at a story position the universe has, the form offers "Enters
  the story at <position>", off by default; ticked, the note gets `from:`
  that position. It's offered only where the chosen place can see the
  position (the universe, the series it's in, or its own book), so
  choosing another book takes it away and clears it; a position the
  universe doesn't have is never offered. Escape or Cancel closes the
  form, sends nothing, and returns focus to what opened it.
- After a create every cached answer is asked again, as after any save,
  and the checkpoint dialog lists the new file as added and "saved in
  Wolfden".

Spec: types.md F-TPL-6, design.md §9 (#29). Covered by:
`CreateEndpointTests.TheTypesAreTheDensEnabledTypesBuiltInAndDenLocal`,
`CreateEndpointTests.ANewNoteIsMadeFromItsTemplateWhereTheWriterSaysAndTheAnswerNamesIt`,
`CheckpointEndpointTests.ANoteCreatedThroughThisServerIsMarkedSavedHere`,
`WriteToolTests.TheTypesANewEntityCanBeMadeFromAreTheEnabledOnesByName` (MCP),
`NewEntity.test.tsx` (web).

### SC-WEB-68. New notes are guarded

- A final book can be chosen as the place, marked "final", but the form
  says nothing can be added to it until it's unfrozen, and Create is off.
  If the server refuses anyway (`frozen`, 409), the form shows why, stays
  open with the name kept, and nothing is written.
- `POST /api/entity/create` takes only `type`, `name`, `universe`,
  `series`, `book`, and `from`, as JSON; a blank name, a series and a
  book together, a type no enabled pack defines (`unknown_type`), a
  book the universe doesn't have (`unknown_scope`), a `from` that doesn't
  resolve in the universe (`bad_position`, F-POS-4), or a name with a
  line break (U+2028 and U+2029 included) or over 200 characters
  (`bad_argument`) is refused and writes nothing. It answers POST only,
  and `/api/types` GET only.
- A name that would take links from another note (a second Jonathan
  Harker in the universe) is refused, 400 `bad_argument` with the links in
  `relinks`, and its message suggests no slug, since the endpoint takes
  none; the form lists them, moves focus there, and offers **Create
  anyway**, which resends with `allowRelinks: true` and opens the new
  note. Changing the name ends the refusal, and Cancel still closes.
- A book's note given a `from` in another book is made, with the F-ENT-6
  warning in the result. A second Create while one is in flight sends
  nothing.
- A server pinned to one universe creates only in it.

Spec: format.md F-BOOK-1, design.md §9 (#29). Covered by:
`CreateEndpointTests.ACreateInAFinalBookOrOfAnUnknownTypeOrMalformedIsRefusedAndWritesNothing`,
`CreateEndpointTests.APinnedServerCreatesOnlyInItsUniverse`,
`CreateEndpointTests.ANoteThatWouldTakeLinksIsRefusedUntilTheWriterCreatesItAnyway`,
`NewEntity.test.tsx` (web).

### SC-WEB-69. I add a book or a series from its universe or series

On Dracula's universe page, the writer chooses **New book**, names it
"Dracula's Guest", gives it 3 chapters, and creates it. Then **New
series**, "The Harker Papers", placed after Dracula, and on its page,
**New book**.

- The form asks for the name, **Place after…** (the works in story order,
  "At the start", then "At the end", the default; not shown when there
  are none yet), and for a
  book an optional **Number of chapters**, a whole number from 1 to 1000.
  A name is required and a bad number is refused before anything is sent.
- **Create** sends `POST /api/book/create` (or `/api/series/create`) with
  the hash `GET /api/works` gave for `universe.md` or `series.md`. It
  makes `universes/dracula/books/dracula-s-guest/book.md`, with a new id,
  the name, and chapters `"1"` to `"3"` numbered 1 to 3, and adds the key
  to the universe's `order`. Dracula's universe had one book and no
  `order`, so the list is written with that book first,
  `[dracula, dracula-s-guest]`, and `check` stays quiet. The rest of the
  file is as it was.
- "The Harker Papers" goes after Dracula: `order: [dracula,
  the-harker-papers, dracula-s-guest]`. A book in a series goes in its
  `books`, at the end or after the book chosen. A list that leaves works
  out gets them first, in the order they were read, and a list that
  already names the new key (a book planned ahead) keeps that place, with
  the works it left out listed after it. **At the start** (`first: true`)
  puts the new work before every other: a prequel.
- The page goes to the new book or series, with a notice naming the file
  and what was done, which takes focus; the tree and the counts ask
  again, and the checkpoint dialog lists both files as "saved in
  Wolfden". Leaving the page ends the notice.

Spec: format.md §6, F-UNI-1..3, F-SER-1..3, design.md §9 (#213). Covered by:
`CreateContainerTests.ANewBookInASeriesGoesAtTheEndOfItsBooksWithItsChapters`,
`CreateContainerTests.PlaceAfterPutsTheNewWorkInItsPlaceAndKeepsTheRestOfTheFile`,
`CreateContainerTests.AnOmittedListIsWrittenWithTheWorkAlreadyThereFirst`,
`CreateContainerTests.WorksAListLeavesOutAreListedFirstAndAPlannedEntryKeepsItsPlace`,
`CreateContainerTests.TheWorksAreInStoryOrderWithTheHashOfTheFileThatListsThem`,
`CreateContainerTests.AtTheStartPutsTheNewWorkBeforeEveryOther`,
`CreateContainerTests.APlannedEntryStillGetsTheLeftOutWorksListed` (MCP project, the write tools),
`ContainerEndpointTests.ANewBookAndSeriesAreMadeWhereTheWriterSaysAndTheAnswerNamesThem`,
`CheckpointEndpointTests.ABookCreatedThroughThisServerIsMarkedSavedHere`,
`NewContainerTests.ABookFileHasItsIdNameAndChaptersAsTheFormatWritesThem`,
`NewContainerTests.TheWriteCheckReadsANewContainerFileWithTheListThatNamesIt` (Core),
`NewContainer.test.tsx` (web).

### SC-WEB-70. New series and New book are guarded

- A name that's empty, more than one line (U+2028 and U+2029 included),
  or over 200 characters is refused (`bad_argument`), and so is one whose
  key wouldn't start with a letter ("1984", F-KEY-2). A name in another
  script ("Дракула", "東京") makes no key, since keys are a–z and 0–9 for
  now (#133): the message asks for a Latin name, which can be changed
  back in the file afterwards. Text the chapters field can't read ("e")
  is refused, not taken as no chapters.
- A key that's taken is refused (`already_exists`, 409), not given `-2`,
  since positions name a book by its key alone (F-KEY-5): a second
  "Ember" anywhere in the universe, a series named like a standalone
  book or the other way round, or a folder of that name in any case.
- An `after` that isn't one of the container's works (a series' book,
  when placing a series in the universe), a series that isn't there
  (`unknown_scope`), `chapters` that aren't a whole number from 1 to
  1000, and a `universe.md` or `series.md` that changed since the form
  read it (`stale_file`, 409) are refused. The form shows why, keeps the
  name, and asks for the works again; Escape closes it and returns focus
  to the button.
- A final book doesn't stop a new book beside it: its scope is its own
  `book.md`, outline, and notes (F-BOOK-1), not `series.md` or
  `universe.md`.
- If the second file can't be written, the first is put back and the new
  folder removed, with a `books/` or `series/` folder above it that the
  create made, so nothing is left behind.
- The endpoints take only their own members, as JSON; `/api/works` answers
  GET only, and the creates POST only. A server pinned to one universe
  creates only in it.

Spec: format.md F-KEY-2, F-KEY-5, F-BOOK-1, design.md §9 (#213). Covered by:
`CreateContainerTests.ARefusedCreateWritesNothing`,
`CreateContainerTests.FinalBooksDontStopANewBookBesideThem`,
`CreateContainerTests.AListThatCantBeWrittenLeavesNoNewFolderBehind`,
`CreateContainerTests.ARolledBackCreateRemovesTheFolderItMadeAboveToo`,
`CreateContainerTests.ANameInAnotherScriptSaysKeysAreLatinForNow` (MCP project, the write tools),
`ContainerEndpointTests.AStaleClashingOrMalformedCreateIsRefusedAndWritesNothing`,
`ContainerEndpointTests.APinnedServerCreatesOnlyInItsUniverse`,
`NewContainer.test.tsx` (web).

### SC-WEB-71. I add and insert chapters on a book page

On Ember's page (Starfall: chapters `prologue`, `1`, `2`, `2b` numbered
3, and `3` numbered 4), the writer chooses **Insert chapter…**, picks
"After 2", titles it "The Quiet Hour", and adds it. Then **Add chapter**
with no title.

- The form has **Place** ("At the end, after …", then "After …" for each
  chapter but the last, by number, title, and key) and an optional
  **Title**. **Add chapter** starts at the end; **Insert chapter…**,
  shown when the book has two or more chapters, starts after the first.
- **Add** sends `POST /api/chapter/create` with the hash `GET
  /api/book/chapters` gave for `book.md`. The inserted chapter's key is
  the key it follows with a letter added, one that sorts between it and
  the next key when the next is the same key with letters added (#229):
  `2a`, between `2` and `2b`. It's numbered 3, and `2b` and `3` move up
  to 4 and 5; no key changes, so `ember:3` names the chapter it did.
  After `2b` it would be `2ba`, and after `prologue`, `prologuea`,
  numbered 1, with every number after it moving up. Inserting after `2`
  again, nothing sorts between `2` and `2a`, so it's the first unused
  letter, `2c`.
- At the end, the key is the next whole number, `4`, numbered after the
  last numbered chapter; no number changes. A book with no chapters gets
  `"1"`, number 1. In a book whose chapters have no numbers (Ash,
  `chapters: [1, 2, 3, 4, epilogue]`), the new one has none either and is
  a bare key, and a title makes it a mapping.
- A hand-set gap absorbs an insert: after `1` in 1, 2, 3, 10, the new
  chapter is 2, the next two move up to 3 and 4, and 10 stays.
- `book.md` keeps everything else: its comments (`# added in revision`),
  flow or block items, the quoting of the item before, and blank lines
  between items and after the last one, with comment lines that group
  them (`# Part Two`) staying above the item they head. `check` finds
  nothing new. A `book.md` without an id gets one.
- The form closes, the chapter list asks again and shows the new one in
  its place, and a notice says what was added and renumbered, and takes
  focus until dismissed.

Spec: format.md §6.4, F-BOOK-2..6, design.md §9 (#215, #229). Covered by:
`AddChapterTests.AnInsertedChapterGetsALetterKeyAndTheNumbersAfterItMoveUp`,
`AddChapterTests.AddingAtTheEndGivesTheNextWholeNumberInTheListsOwnStyle`,
`AddChapterTests.ABookWithNoChaptersGetsChapterOne`,
`AddChapterTests.ABookMdWithNoIdGetsOne`,
`AddChapterTests.BlankLinesAndGroupingCommentsInTheChaptersStay` (MCP project, the write tools),
`BlockListBlankLineTests.BlankLinesBetweenItemsAndAfterTheLastStay`,
`BlockListBlankLineTests.AnInsertedItemGoesRightAfterTheOneBeforeItAndTheGroupingCommentStaysWithTheNext`,
`BlockListBlankLineTests.BlockMappingItemsKeepTheirBlankLinesToo`,
`BlockListBlankLineTests.AScalarAliasListKeepsItsBlankLines` (Core),
`NewChapterTests.AnInsertedChapterGetsTheFirstUnusedLetterSuffix`,
`NewChapterTests.AnInsertedKeySortsBetweenItsNeighboursWhenOneCan`,
`NewChapterTests.InsertingAgainAndAgainKeepsKeysInOrderWhereItCan`,
`NewChapterTests.AtTheEndItsTheNextWholeNumber`,
`NewChapterTests.AnEmptyBookGetsChapterOneNumberedOne`,
`NewChapterTests.InsertingRenumbersTheChaptersAfterItAndAddingAtTheEndRenumbersNone`,
`NewChapterTests.HandSetNumbersAreMovedOnlyAsFarAsNeeded`,
`NewChapterTests.AnItemIsWrittenAsTheFormatWritesOne` (Core),
`ChapterEndpointTests.AChapterIsAddedWhereTheWriterSaysAndTheAnswerSaysWhatChanged`,
`AddChapter.test.tsx` (web).

### SC-WEB-72. Adding a chapter is guarded

- A final book (`status: final`) has neither button, and the API refuses
  an add to it (`frozen`, 409) as it refuses any edit of its `book.md`
  (F-BOOK-1). The refusal ends by saying how to unfreeze it, in the
  caller's words (#209): **Unfreeze…** on the book's page for the web,
  `wolfden unfreeze` for the CLI, and asking the writer for an agent.
- An `after` the book doesn't list (`unknown_chapter`, naming its
  chapters; for a book with no chapters, saying so), a title of more than
  one line (U+2028 and U+2029 included) or over 200 characters
  (`bad_argument`; the form refuses a long one before sending), a missing
  `book` or `expectedHash`, a member the endpoint doesn't take, and a
  `book.md` that changed since the form read it (`stale_file`, 409) are
  refused and write nothing. The form shows why, in its own words for a
  stale file, and asks for the chapters again. If the chapter chosen in
  **Place** isn't a choice any more (it's gone, or it's the last), Place
  goes back to the end and the form says so. Escape closes it and
  returns focus to the button.
- A blank title is no title, and one of exactly 200 characters is taken.
- The endpoints take JSON only; `/api/book/chapters` answers GET only,
  and `/api/chapter/create` POST only. A server pinned to one universe
  adds chapters only in it.

Spec: format.md F-BOOK-1, design.md §9 (#215). Covered by:
`AddChapterTests.ARefusedAddWritesNothing`,
`AddChapterTests.AFinalBookIsRefused` (MCP project, the write tools),
`ChapterEndpointTests.AStaleFrozenOrMalformedAddIsRefusedAndWritesNothing`,
`ChapterEndpointTests.APinnedServerAddsChaptersOnlyInItsUniverse`,
`AddChapter.test.tsx` (web).

### SC-WEB-73. I see each chapter's fields, and which are inherited

The Long Dark (`series.md`) has `chapter_defaults: { tense: past }`, and
Ember (`book.md`) has `chapter_defaults: { pov: ["[[mara-voss]]"] }`. Its
prologue writes `fields: { pov: [], when: "2284" }`, and chapter 3
`fields: { tense: futur }`. The writer opens Ember's page, then chapter 1's.

- Under each chapter's title, the book's list shows the fields that have
  a value: the chapter's own as they are, inherited ones greyed and marked
  where they come from ("book default", "series default"). A `[[link]]`
  is a link to the note it reaches, resolved from the file that holds it
  (F-CHAP-4).
- The chapter's page lists every chapter field the den's packs define,
  in pack order (the core pack's `pov`, `narration`, `tense`, `when`, and
  `setting`, then any the den's own packs add), with what holds: chapter 1 has `pov` Mara Voss
  "inherited from book 'ember' defaults" and `tense` past "inherited from
  series 'long-dark' defaults"; `narration` is "not set". The prologue's
  `pov` is "none (none in this chapter, [])".
- Chapter 3's `futur` isn't a tense, so it's shown as written and
  ignored, with why ("written here as "futur", which is ignored: tense
  is one of past, present, future, not 'futur'. Did you mean
  'future'?"), and the chapter has `past` from the series (F-CHAP-3).
  **Edit** says the same before a save replaces it, in a note the form
  is described by (#233). An item of a list that doesn't fit is shown as
  left out ("item 2, "Borgo Pass", is left out: …"); a field written
  with no value isn't flagged.
- Reading as of `ember:1`, chapters after it show no fields in the
  book's list (and no chapter does until the page knows where `ember:1`
  is), and a link in a shown value to a note the reader hasn't
  met yet is plain text that doesn't name the note (#26).
- The API gives the chapters' fields as written, what holds and where
  from, what each would inherit, and the den's chapter field definitions
  (`GET /api/book/chapter-fields`).

Spec: types.md §8 (F-CHAP-1..5), design.md §9 (#225, #233). Covered by:
`ChapterFieldEndpointTests.EachChaptersFieldsSayWhatHoldsAndWhereItComesFrom` (API),
`ChapterFields.test.tsx` (web).

### SC-WEB-74. I give a flashback its own POV and date, then undo it

On chapter 2's page the writer chooses **Edit** for `pov`, adds Halden
Pike next to Mara Voss, and saves. Later they remove both, save, then
choose **Use the default**.

- The form is a note field's (#196): a link picker for `pov`, a dropdown
  for `narration` and `tense`, a date with an optional sort key for
  `when`. A field the chapter inherits starts from the inherited value,
  and says so; what's saved becomes the chapter's own. Saving with
  nothing changed sends nothing.
- Save sends only what changed (`POST /api/chapter/edit`, the MCP
  `update_chapter_fields`): `{ key: "2", number: 2, fields: { pov:
  ["[[mara-voss]]", "[[halden-pike]]"] } }` in `book.md`, the rest of the
  file as it was. The notice says what the chapter has now and what it
  replaced.
- Removing the last item says first that saving writes `pov: []`, "none
  in this chapter", in place of the default, and saving leaves `pov: []`
  (#132, F-RT-6), not the default.
- **Use the default** is shown only for a field the chapter writes. It
  says first what the chapter will have ("This chapter will have
  [[mara-voss]], from book 'ember' defaults", or "Nothing above sets pov, so this chapter
  will have none"), then takes the field out.
- A changed date with a sort key is written whole: `when: { text: Year
  2290, sort: 2290 }`.

Spec: types.md F-CHAP-5, format.md F-RT-6, design.md §9 (#225). Covered by:
`ChapterFieldEndpointTests.AChaptersFieldIsSetThenEmptiedThenClearedBackToTheDefault` (API),
`ChapterFields.test.tsx` (web).

### SC-WEB-75. I set chapter defaults, and give a new chapter its fields

The writer opens the Starfall universe's page and sets **Chapter
defaults** `narration` to third-limited, then adds a chapter to Ember
with **Add chapter**, giving it a POV and a date.

- The universe's, series', and book's pages each have **Chapter
  defaults**, with every chapter field, what the container sets, and
  what it inherits from above, edited as a chapter's fields are
  (`POST /api/chapter-defaults/edit`, the MCP `update_chapter_defaults`),
  with **Clear** to inherit from above again. The notice says what each
  book's chapters have now.
- **Add chapter** shows each chapter field, empty, with what the chapter
  will inherit if it stays empty ("Left empty: past, from book 'ember'
  defaults"). Only fields given a value are sent, in `fields`, and
  written last in the new item: `{ key: "1b", number: 2, title:
  Flashback, fields: { pov: ["[[halden-pike]]"], when: "2270" } }`. In a
  list of bare keys, fields make the new chapter a mapping.

Spec: types.md F-CHAP-5, design.md §9 (#215, #225). Covered by:
`ChapterFieldEndpointTests.AContainersDefaultsAreReadAndEdited`,
`ChapterFieldEndpointTests.ANewChapterCanStartWithItsOwnFields` (API),
`NewChapterTests.AnItemIsWrittenAsTheFormatWritesOne` (Core),
`ChapterFields.test.tsx` (web).

### SC-WEB-76. Chapter field edits are guarded

- A final book's chapter page and **Chapter defaults** have no Edit and
  say to unfreeze it; the API refuses an edit (`frozen`, 409), saying how
  to unfreeze it in the page's words (**Unfreeze…**). A series' or
  universe's default that would change what a final book's chapters have
  is refused too, naming them.
- A stale `book.md` (or container file) is refused (`stale_file`). The
  page sends the same changes again on the new file only if they add or
  remove items and the chapter's item (or the default's entry) is byte
  for byte as it was (moves and changes of one item count, as `move` and
  `replace`); otherwise it shows both values, with **Keep mine**
  and **Take theirs**. A refetch while the form is open doesn't change
  what it's based on. **Use the default** on a stale file shows what the
  file has now, or says it was already cleared; it never clears a value
  the writer didn't see.
- The page moves and changes a list item in place (`move`, `replace`),
  keeping its comments (#197). Removing an item that has a comment on it
  says, in the effects, that the comment went with it, and so does a
  client's remove and add of an item ("Rewrote item 0 of pov of chapter
  '1'; the comment on it was dropped", with a pointer to `replace`).
- A reserved name (`key`, `number`, …, F-CHAP-1), a change member the
  endpoint doesn't take (`field` inside a change), a missing `field`, an
  unknown chapter, and `fields` that isn't an object are refused and write
  nothing. A chapter written as a bare key (`chapters: [1, 2]`) has no
  place for fields: the API refuses (`unsupported_edit`), and the page
  says to write it as `{ key: 1 }` first.
- An `add` to a field that takes one value (chapter 3's `tense`) is
  refused naming the chapter and its file, in the page's words: "To
  change it, edit the field and save the new value." An `add` to a
  default that isn't a list (`setting: Kessan Reach`) says what's
  written, why it's ignored, and "To replace it, edit the field and save
  the whole value; to go back to what its books inherit from above,
  choose Clear" (**Use the default** for a chapter). A `remove` of an
  item that isn't there says to reload the page. No refusal names a YAML
  path (#233).
- `/api/book/chapter-fields` and `/api/chapter-defaults` answer GET only,
  and the edits POST only, as JSON.

Spec: format.md F-BOOK-1, types.md F-CHAP-1, design.md §9 (#225). Covered by:
`ChapterFieldEndpointTests.ChapterFieldEditsAreGuarded`,
`ChapterFieldEndpointTests.AnItemsCommentThatGoesWithItIsReported` (API),
`ChapterFields.test.tsx` (web).

### SC-WEB-77. My chapters are headed by POV, place, and date

The Long Dark (`series.md`) has `chapter_heading: [pov, setting, when]`
and `chapter_defaults: { setting: "[[kessan-reach]]" }` (#227). In
Ember, the prologue (titled Before the Dark) has `when: "2284"`, chapter
1 is Mara Voss's, chapter 2 is Halden Pike's and `[[mara-voss|the
Commander]]`'s, and chapter 2b, The Long Watch, is Mara Voss's again.
Halden Pike enters at the start of Ash.

- Ember's page lists the chapters as "Before the Dark · Kessan Reach —
  2284", "Mara Voss — Kessan Reach", "Halden Pike & the Commander —
  Kessan Reach", and "The Long Watch · Mara Voss — Kessan Reach". A
  chapter's page uses its heading as its title, and so do its links to
  the chapters before and after it.
- Renaming Mara Voss's note changes every heading that links her, with
  no change to `book.md`.
- Reading as of `ember:2`, chapter 2 is "halden-pike & the Commander —
  Kessan Reach": the reader hasn't met Pike, so his link shows as
  written. The Long Watch, after the position, shows its title alone,
  and an untitled chapter after it is "Chapter 3". As of
  `ember:prologue`, Kessan Reach (met at chapter 1) shows as written.
- `/api/hierarchy` gives each chapter's `heading`, as of `asOf` when
  given; `/api/book/chapters` gives the full one.
- Without `chapter_heading`, or with `chapter_heading: []` nearer the
  book, chapters show their titles as before. The nearest container's
  list wins, whole.

Spec: format.md F-CTR-6, design.md §9 (#227, #26). Covered by:
`ChapterHeadingTests` (Core),
`ChapterHeadingIndexTests.AHeadingNamesANoteByItsNameNow` (index),
`ChapterHeadingEndpointTests`, `SpoilerTests.NothingAsOfAPositionNamesAnEntityTheReaderHasntMet` (API),
`ChapterHeading.test.tsx` (web).

### SC-WEB-78. I link a note by an alias I remember

In the Dracula sample, I edit a field that links a character, or a
section, and remember Mina only as "Madam Mina" (#198).

- Typing `madam` in the field's link picker offers "Mina Harker, as
  *Madam Mina*"; picking it writes `[[mina-harker]]`, as any pick does
  (`[[01J...]]` where the file name could reach another note). `mur wil`
  finds her by "Wilhelmina Murray": each word typed starts a word, in any
  order, ignoring case and accents (`zoe ark` would find a Zoë Arkwright).
- Typing `mina` offers Mina Harker first (an alias, exactly), then
  "Mina and Jonathan marry" (a name that starts so), then "The Count
  visits Mina" (a name that holds it). The picker offers only the
  field's `to` types: for a character field, Mina Harker alone.
- `lucys` finds "Lucy's three proposals" by its file name, and an id
  typed or pasted (ten characters or more) finds its note.
- In a section, `[[madam` offers the same, and picking it writes
  `[[Madam Mina]]`: prose links by the text matched, a name or an alias.
  If Quincey Harker also had the alias "Harker", `[[harker` would write
  `[[Jonathan Harker]]` for Jonathan: an alias that would reach another
  note too gives way to the name, then to `[[jonathan-harker]]`, then to
  the id. A note no link is sure to reach (no id, and a file name and
  name another note could take) is offered with "add an id with Edit
  file", and picking it writes nothing.
- Reading as of chapter 1, an editor still offers Van Helsing, whom the
  reader meets at chapter 9: editing shows everything (#28). The
  lookup's `asOf`, which the editors don't send, offers only the notes
  the reader has met (`professor` finds nobody as of `dracula:1`), and
  never names one they haven't.

Spec: design.md §9 "Looking notes up by name and alias" (#198, #28,
#26). Covered by: `EntityLookupTests` (index),
`EntityLookupEndpointTests`,
`SpoilerTests.NothingAsOfAPositionNamesAnEntityTheReaderHasntMet` (API),
`FieldEditing.test.tsx`, `LinkLookup.test.tsx` (web).

### SC-WEB-79. The lookup is guarded

- `/api/entities/lookup` answers GET only, refuses a cross-site request
  like every endpoint, and under `serve --universe` looks in the pinned
  universe and refuses another (or `*`).
- A query with no letters or digits, a parameter it doesn't take (`q`),
  a `limit` outside 1 to 100, `type` naming no known type, and a blank
  `asOf` are refused (`bad_argument`, `unknown_type`). A `type` list
  with one type no pack enables (`character,vampire`, as a field's
  `to` may have) skips it and looks among the rest. If the lookup
  fails, the field's picker says so, and the link can still be typed.

Spec: design.md §9 (#198, #23). Covered by:
`EntityLookupEndpointTests.ALookupIsGuarded`,
`EntityLookupEndpointTests.TypesNarrowALookupToAFieldsToTypes`,
`EntityLookupEndpointTests.AsOfAPositionOnlyNotesTheReaderHasMetAreOffered`.

### SC-WEB-80. I add a picture to a note while editing it

The writer edits the opening of Castle Dracula's note,
`universes/dracula/entities/locations/castle-dracula.md`, and clicks
**Add image…**, picking `Castle Map (1832).PNG` from their computer.

- The image is saved as
  `universes/dracula/entities/locations/images/castle-map-1832.png`,
  byte for byte, and `![Castle Map (1832)](images/castle-map-1832.png)`
  goes in at the cursor. The note's file isn't changed until the writer
  saves; Save is on, since the text now differs. The page says the image
  is saved in the den and stays there even if they cancel.
- `serve` shows the image at that path at once, and the checkpoint
  dialog counts it as saved in Wolfden. Adding it makes no checkpoint.
- Dropping `ship.webp` on the text, or pasting a screenshot
  (`image.png`), does the same, at the drop point or the cursor. Pasted
  or dropped text, or a file that isn't an image, is left to the
  editor. Text copied from Word comes with a picture of it, but it's
  the text that's pasted, and nothing is uploaded. Dropping `map.png`
  with `chapter-notes.txt` adds the map and says the notes file wasn't
  added, since it isn't an image.
- If the note's folder already has `Images/`, the image goes there and
  the link says `Images/...`.
- If the folder already has `castle-map.png`, or `Castle-Map-2.png` in
  another case, a new `castle_map.png` is saved as `castle-map-3.png`
  and the page says why. No file is ever replaced. Several images picked
  at once go in one per line.

Spec: design.md §9 ("Adding images", #187), format.md F-LAY-9. Covered by:
`ImageEndpointTests.AnImageIsSavedBesideTheNoteUnderASlugAndTheNoteIsUnchanged`,
`ImageEndpointTests.AFileAlreadyThereIsNeverReplacedTheNewOneGetsANumber`,
`ImageEndpointTests.AnImagesFolderInAnotherCaseIsUsedAsItsSpelled`,
`EditorImages.test.tsx` (web).

### SC-WEB-81. Adding an image is guarded

- The server picks the name, from the file's as a slug, so a name such
  as `../../../../../escape.png`, `..\..\escape.png`, `/etc/escape.png`,
  or `%2e%2e%2fmap.png` is saved in the note's `images/` folder and
  nowhere else; `.hidden.png` becomes `hidden.png`, `CON.png`
  `con-image.png`, and a name with no letters or digits `image.png`.
- Only `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, `.avif`, and `.svg` are
  taken, and the contents must be that type: a JPEG named `map.png` is
  refused, saying to rename it `.jpg`; text, HTML named `.svg`, and an
  empty file are refused (`bad_argument`). The page refuses another
  type, and a file over 10 MB, without sending it; the server answers a
  body over 10 MiB with 413 `too_large`.
- The body must be the file, sent as `application/octet-stream` or an
  image type: JSON, text, a form, or no type is 415. The endpoint answers
  POST only, takes only `entity`, `universe`, and `name`, each once, and
  refuses a cross-site request.
- A note in a final book refuses the image (`frozen`), saying to use
  **Unfreeze…** on the book's page; the page shows the refusal and
  changes nothing. A server pinned to another universe refuses it.
  While another Wolfden holds the den's write lock past the wait, it's
  `den_busy` and nothing is written. A file named `images` beside the
  note is a conflict (409) that says to rename or move it.
- While an image is being added, the button is off, and an image dropped
  or pasted meanwhile is refused with a note to add it again after.
- An SVG that declares entities in its DOCTYPE (as Illustrator's SVG
  1.1 Save As writes) is refused, saying to export it without them or
  add a PNG. If another program saves a file of the chosen name just
  before Wolfden writes, that file is kept and the image takes the next
  number.

Spec: design.md §9 ("Adding images", #187). Covered by:
`ImageEndpointTests.TheServerPicksTheNameSoItStaysInTheNotesImagesFolder`,
`ImageEndpointTests.TheContentsMustBeTheImageTypeTheNameSays`,
`ImageEndpointTests.OnlyTheFileItselfIsTakenNotAFormOrText`,
`ImageEndpointTests.AnImageOverTenMegabytesIsTooLarge`,
`ImageEndpointTests.ItTakesOnlyTheNoteTheUniverseAndTheName`,
`ImageEndpointTests.ItAnswersPostOnlyAndRefusesAnotherSite`,
`ImageEndpointTests.ANoteInAFinalBookRefusesTheImageWithTheUnfreezeHint`,
`ImageEndpointTests.APinnedServerAddsImagesOnlyInItsUniverse`,
`ImageEndpointTests.AnImageWhileAnotherWolfdenKeepsWritingIsDenBusyAndWritesNothing`,
`ImageEndpointTests.AFileNamedImagesBesideTheNoteIsAConflictThatSaysWhatToRename`,
`AddImageTests.AFileMadeMeanwhileByAnotherProgramIsKeptAndTheImageTakesTheNextName` (MCP),
`ImageFileTests` (core), `EditorImages.test.tsx` (web).
