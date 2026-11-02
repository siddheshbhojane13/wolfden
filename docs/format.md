# Wolfden Format Specification

**Format version:** 1. **Status:** draft (#2), open to revision until M1
code depends on it.

This document is the normative definition of a den on disk: its folders,
`den.yaml`, the container files, entity files, story positions, section
comments, status and pins, wiki-links, and outlines. It's meant to
be precise enough to hand-write a valid den and to test a parser
against. The reasons behind the
format are in [`design.md`](design.md) and aren't repeated here. Type and
pack files are defined in [`types.md`](types.md).

## Contents

1. [Conventions](#1-conventions)
2. [Files and encoding](#2-files-and-encoding)
3. [YAML and frontmatter](#3-yaml-and-frontmatter)
4. [Keys and ids](#4-keys-and-ids)
5. [The den](#5-the-den)
6. [Containers: universe, series, book](#6-containers-universe-series-book)
7. [Entities](#7-entities)
8. [Story positions and order](#8-story-positions-and-order)
9. [Sections](#9-sections)
10. [Links](#10-links)
11. [Outlines](#11-outlines)
12. [Preservation](#12-preservation)
13. [Appendix: a minimal den](#13-appendix-a-minimal-den)

## 1. Conventions

### 1.1 Requirement words

The key words MUST, MUST NOT, REQUIRED, SHOULD, SHOULD NOT, and MAY are to
be interpreted as described in BCP 14 ([RFC 2119], [RFC 8174]) when, and only
when, they appear in all capitals.

[RFC 2119]: https://www.rfc-editor.org/rfc/rfc2119
[RFC 8174]: https://www.rfc-editor.org/rfc/rfc8174

### 1.2 Rule ids

Every rule has an id of the form `F-<AREA>-<n>`, such as `F-POS-2`. Ids are
stable: a rule that is dropped keeps its number retired, and new rules get
new numbers. Tests, diagnostics, and issues cite rules by id.

### 1.3 Errors and warnings

A rule that a validator enforces ends with its severity.

- **[error]**: the file is invalid. Wolfden reports it and doesn't index it
  (F-GEN-2 says how far that reaches). The app and MCP tools MUST refuse to
  write a file that would have an error.
- **[warning]**: the file is valid and indexed. `wolfden check` reports the
  warning.

A rule with no severity binds whoever writes files (a person, the app, or
an agent) and has no diagnostic.

**F-GEN-1.** `wolfden check` exits non-zero when the den has any error.
Warnings alone don't make it fail.

**F-GEN-2.** An error in an entity file or `outline.md` excludes that file.
An error in `universe.md`, `series.md`, or `book.md` excludes that container
and everything under it, since scopes and positions there can't be
interpreted. An error in `den.yaml` means the den can't be loaded.

**F-GEN-3.** Validators MUST report every diagnostic they find, not just the
first. Each diagnostic SHOULD carry the rule id, the file path, and a line
number when there is one.

**F-GEN-4.** An [error] MUST be decidable from the file itself, `den.yaml`,
and the set of enabled types. A problem that depends on another file, such
as chapter order, another book's outline, or a second file with the same
id, is a [warning], and the rule says how views handle it. So editing one
file never makes a different file invalid. (A container folder that lacks
its container file, F-LAY-4, is a problem with the folder itself.)

### 1.4 Notation and terms

Grammars use ABNF ([RFC 5234]), except that quoted strings are
case-sensitive. `lower` is `%x61-7A` (a to z). `DIGIT`, `SP`, `WSP`, and
`DQUOTE` are the RFC 5234 core rules.

[RFC 5234]: https://www.rfc-editor.org/rfc/rfc5234

- **Den root:** the folder that holds `den.yaml`.
- **Container:** a universe, series, or book, each a folder with a
  container file (`universe.md`, `series.md`, `book.md`).
- **Scope:** the container an entity belongs to (design.md §2).
- **Scope chain:** a scope and the scopes that enclose it, nearest first.
  For a book in a series, that's book, series, universe.
- **Writer:** anything that writes den files: the app, the CLI, MCP tools.

## 2. Files and encoding

**F-ENC-1.** `den.yaml` and every `.md` file Wolfden reads MUST be UTF-8.
Invalid UTF-8 is an [error].

**F-ENC-2.** Files SHOULD NOT start with a byte order mark, but one is not
an error and has no diagnostic. Readers MUST accept a leading BOM and keep
it when rewriting. Writers MUST NOT add one to a new file.

**F-ENC-3.** LF is the canonical line ending, and new files use it. Readers
MUST accept CRLF. A writer that rewrites a file MUST keep its line endings.
A file that mixes LF and CRLF is a [warning]. A CR not followed by LF is an
[error].

**F-ENC-4.** Files SHOULD end with a newline. Writers MUST keep a file's
final newline, or its absence.

**F-ENC-5.** Text is compared (names, aliases, headings, link targets) after
Unicode NFC normalization. Writers SHOULD write NFC, and MUST NOT
re-normalize text they aren't changing.

## 3. YAML and frontmatter

**F-YAML-1.** `den.yaml` and frontmatter are a single YAML 1.2 document
using the core schema. A parse failure is an [error].

Inside a flow collection (`[...]` or `{...}`), a plain scalar that ends
with `:` right before `}` or `]` is also an F-YAML-1 [error], such as
`{value: drowned, from:}` or `[Voss, Orrin:]`. YAML 1.2 ends the scalar
before that colon and reads the colon as a key separator, but not every
YAML reader does, and Wolfden's reader keeps the colon in the text. Before
a comma (`{from:, value: drowned}`) every reader already ends the scalar
there, so that's fine. The error is on the colon's line, and it
says to add a space after the colon (`from: }`) for a key with an
empty value, or to quote the text if the colon is part of it. Like
F-YAML-3 and F-YAML-4, it marks a file that other tools read differently
from Wolfden. Wolfden never writes this form, since it quotes such a
string and writes an empty value as `~`.

**F-YAML-2.** The top level MUST be a mapping. [error] Frontmatter or a
YAML file that holds no YAML document, only blank lines and comments, or
only a `---` marker, is an empty mapping, with no diagnostic. Missing keys
are then reported by the rules that require them (F-ENT-2, F-ENT-3,
F-CTR-1, F-DEN-1).

**F-YAML-3.** A mapping MUST NOT repeat a key. [error]

**F-YAML-4.** Anchors (`&a`), aliases (`*a`), merge keys (`<<`), and
explicit tags (`!x`, `!!str`) MUST NOT be used. [error]

**F-YAML-5.** Keys, positions, `name`, `aliases`, chapter `title`, `row`
values (F-FLD-7), and the values of `string`, `text`, `date`, `enum`, and
`position` fields (types.md §5.2) are read as the scalar's text as
written, not after type resolution. So `14`, `"14"`, and `'14'` are the
same chapter key, `014` stays `014`, and `name: 1984` is the name "1984".
A YAML null (`~`, `null`, or nothing) is still null, not the text
"null": `name: ~` is a missing name (F-ENT-3), and a null `aliases` item
is an error (F-ENT-4).

**F-FM-1.** Frontmatter starts on the file's first line (after any byte order
mark) with a line that is exactly `---`, and ends at the next line that is
exactly `---`. Trailing spaces or tabs on either line are allowed. `...`
doesn't close frontmatter. A container or entity file with no frontmatter is
an [error], and so is frontmatter that never closes.

**F-FM-2.** The **body** is everything after the closing `---` line. Where
this spec talks about headings, code, HTML comments, and list items, it
means them as [CommonMark 0.31.2] defines them. Wolfden doesn't restrict
any other markdown. A body the reader can't parse as CommonMark, for
example one that nests block quotes or lists deeper than it supports, is
an [error].

[CommonMark 0.31.2]: https://spec.commonmark.org/0.31.2/

## 4. Keys and ids

### 4.1 Keys

Universes, series, books, chapters, entity slugs, author identities, packs,
and types are named by **keys**.

```abnf
key        = word *("-" word)          ; 1 to 64 characters
word       = 1*(lower / DIGIT)
```

**F-KEY-1.** A key MUST match `key` and be 1 to 64 characters long:
lowercase ASCII letters and digits, with single hyphens between words.
No uppercase, spaces, underscores, `:`, or `.`. A key that breaks this rule
is an [error] where it names a container, chapter, pack, or type. An
invalid author key is a [warning] instead, and that author is skipped
(F-DEN-3). A den-local pack folder with an invalid name is a [warning]
too, and is ignored (types.md F-PACK-5). For entity slugs, see F-KEY-4.

**F-KEY-2.** Universe, series, and book keys MUST start with a letter.
[error] Chapter keys and entity slugs MAY start with a digit, so `14` is a
valid chapter key. A chapter key MUST NOT be `r` followed by a digit
(`r75`), since that form is reserved for outline-row positions (F-POS-6).
[error]

**F-KEY-3.** A universe, series, or book's key is its folder name.

**F-KEY-4.** An entity's **slug** is its file name without `.md`. A file
name that isn't a valid key (`Mara Voss.md`) is a [warning]: the entity is
indexed but has no slug, so links reach it only by id, name, or alias.

**F-KEY-5.** Book keys MUST be unique within a universe, across all of its
series and standalone books, since positions name a book without its
series. A series key MUST NOT equal a standalone book key in the same
universe, since `order` (F-UNI-1) mixes them. Either clash spans files, so
it's a [warning] on every container involved (F-GEN-4). While it lasts,
positions naming a duplicated book key don't resolve (F-POS-4), and an
`order` entry naming a clashing key is ignored (F-UNI-2). Chapter keys MUST
be unique within a book (F-BOOK-4).

| Key | Valid? |
|---|---|
| `kessan-reach`, `long-dark`, `14`, `2b`, `ch-07` | yes |
| `Kessan-Reach`, `kessan_reach`, `kessan reach` | no: case, `_`, space |
| `-ember`, `ember-`, `ember--ash` | no: hyphen placement |
| `ember.1`, `ember:1` | no: `.` and `:` are position separators |
| `7th-fleet` as a book key | no: books start with a letter |
| `r75` as a chapter key | no: reserved for row positions |
| `rescue`, `r-2` as chapter keys | yes |

### 4.2 Ids

```abnf
ulid       = %x30-37 25crockford       ; first character 0 to 7
crockford  = DIGIT / %x41-48 / %x4A-4B / %x4D-4E / %x50-54 / %x56-5A
                                       ; A-H J K M N P-T V-Z
```

**F-ID-1.** An `id` MUST be a [ULID] in canonical form: 26 characters of
uppercase Crockford base32. Lowercase, hyphens, or any other length is an
[error]. Readers MUST NOT give the timestamp part any meaning.

[ULID]: https://github.com/ulid/spec

**F-ID-2.** Every container file and entity file SHOULD have an `id`. A
file without one is a [warning] ("no id; run `wolfden check --fix` to add
one"), not an error, so a note written by hand loads before anyone
generates an id for it. The file is indexed and works like any other,
except that links can't reach it by id. Until it has an id, the index
identifies it by its path. An `id` that is present but malformed is still
an [error] (F-ID-1).

**F-ID-3.** An `id` MUST be unique within the den. A duplicate is a
[warning] on every file that has it (F-GEN-4). While it lasts, the files
are indexed and listed, so they can be found and fixed, but every copy is
left out of views and link resolution, and links that would reach one of
them are broken (F-LINK-8). (This is how a file copied by hand gets
caught.) Files without an `id` never clash as duplicates.

**F-ID-4.** An `id` never changes. Writers MUST NOT change an existing
`id`. Moving or renaming a file keeps its `id`, and a copy needs a new one.

**F-ID-5.** Writers generating ids by hand can use any implementation
listed in the [ULID] spec, uppercasing the result if needed, or run
`wolfden new-id`, which prints new ids.

**F-ID-6.** Reading and indexing MUST NOT write to files, so they never add
a missing `id`. A writer that creates a container or entity file MUST give
it an `id`. When the app or an MCP tool edits a container or entity file
that has no `id`, it SHOULD add one in the same write (and MUST when
moving it, F-RT-5), as the first frontmatter key, after any YAML comment
lines that open the frontmatter. That is the
one exception to F-RT-2 and F-RT-4. A write that changes nothing (F-RT-3)
never adds one.
`wolfden check --fix` adds missing ids to container and entity files
in bulk, the same way, except in a final book (F-BOOK-1) and in files
with errors, which it leaves alone. No writer adds an id to any other file, such as `outline.md`
(F-OUT-1) or a pack's template (types.md F-TPL-3).

Valid: `01JA7Q8M4XK3V9R2D5W8HN6TQB`. Invalid: `01ja7q8m4xk3v9r2d5w8hn6tqb`
(lowercase), `01JA7Q8M4XK3V9` (too short), `01JA7Q8M4XK3V9R2D5W8HN6TQI`
(`I` isn't Crockford), `81JA7Q8M4XK3V9R2D5W8HN6TQB` (starts above 7).

## 5. The den

### 5.1 Layout

```text
<den root>/
  den.yaml                        required (§5.2)
  packs/<pack>/...                den-local packs, optional (types.md)
  universes/<universe>/
    universe.md                   required (§6.2)
    entities/<type-folder>/.../<slug>.md
    series/<series>/
      series.md                   required (§6.3)
      entities/...
      books/<book>/
        book.md                   required (§6.4)
        outline.md                optional (§11)
        entities/...
    books/<book>/                 standalone book, same shape as above
  notes/                          reserved (#45)
  authors/                        reserved (#45)
  .wolfden/                       reserved: index and local state
```

**F-LAY-1.** The den root is the folder that holds `den.yaml`. Dens don't
nest: a `den.yaml` anywhere below the root is a [warning] and is ignored.

**F-LAY-2.** Files and folders whose names start with `.` are ignored at
any depth (`.git`, `.obsidian`, `.vscode`). `.wolfden/` at the root is
reserved for the index and local state, SHOULD be gitignored, and is never
a source of truth (design.md §7). Folder links (symbolic links and
junctions) aren't followed either, so the files in a linked folder are
ignored. A folder link where the den reads files is a [warning], so notes
there aren't dropped silently. That is a link that is `packs/` or inside
it, `universes/`, a container folder (F-LAY-4) or the `series/` or
`books/` folder holding one, or an `entities/` folder or inside one.

**F-LAY-3.** `notes/` and `authors/` at the den root are reserved for
Author and den notes (#45). If either exists, v0 ignores its contents and
reports a [warning].

**F-LAY-4.** These folders are containers: every folder directly in the
den's `universes/`, directly in a universe's `series/`, and directly in a
universe's or a series' `books/`. A `series/` folder counts only directly
in a universe, and a `books/` folder only directly in a universe or a
series. Anywhere else they are ordinary folders (F-LAY-7). A container MUST
hold its container file. A container folder that doesn't is an [error],
and it is skipped.

**F-LAY-5.** Every `.md` file under an `entities/` folder, at any depth, is
an entity file. An `entities/` folder counts only directly inside a
container folder. The entity's scope is that container.

**F-LAY-6.** The first folder under `entities/` SHOULD be the plural name of
the entity's type, as the type defines it (types.md F-TYPE-3): characters
go in `entities/characters/`. The file's `type` decides its type, not the
folder. A file in another type's folder, or directly in `entities/`, is a
[warning]. A first-level folder under `entities/` that is no enabled
type's plural (`entities/crew/`) gets no diagnostic. Folders below the
type folder are free for the writer to use
(`entities/characters/crew/mara-voss.md`) and mean nothing to Wolfden.

**F-LAY-7.** Other files and folders in a container folder (images, a
`drafts/` folder) are ignored, with one exception: a `.md` file directly in
a container folder, other than its container file and a book's
`outline.md`, is a [warning], since it's probably a note that belongs in
`entities/`. Non-`.md` files under `entities/` are ignored.

**F-LAY-8.** Other files and folders at the den root, such as a
`README.md`, a `LICENSE`, or a `drafts/` folder, are ignored, with no
diagnostic. F-LAY-7 doesn't apply there, since the den root isn't a
container. A `den.yaml` inside one of them (`archive/old-den/den.yaml`)
still gets the F-LAY-1 warning.

**F-LAY-9.** An image kept in the den is written in Markdown as
`![alt text](path)`, and the path is a path in the den:

- A relative path starts from the folder of the file it's written in (the
  entity file, or the container or outline file):
  `![Map](images/castle-map.png)` in
  `universes/dracula/entities/locations/castle.md` is
  `universes/dracula/entities/locations/images/castle-map.png`. This is how
  GitHub, VS Code's preview, and Obsidian's "Relative path to note" setting
  read it.
- A path that starts with `/` starts from the den root:
  `![Seal](/images/seal.png)` is `images/seal.png`.
- `../` goes up a folder, but never above the den root. A path that
  climbs out of the den doesn't show.
- Names are written as in a web address: `%20` is a space, and `?` or `#`
  ends the path. A space can be written as is inside `<...>`:
  `![Map](<images/castle map.png>)`.

The web UI (`wolfden serve`) shows images of these types only: `.png`,
`.jpg`, `.jpeg`, `.gif`, `.webp`, `.avif`, and `.svg`, in any case. It
never shows a file whose path has a name starting with `.` (F-LAY-2),
or one reached through a folder link, and it never serves anything
outside the den. Pinned to one universe (`wolfden serve --universe`), it
doesn't show images in another universe's folder (`universes/<other>/`);
images outside `universes/`, such as a den-level `images/` folder, still
show. An image that isn't there, or that it won't show, gets a
placeholder with the alt text. An address with a scheme or a host
(`https://...`, `//host/...`) isn't a den path: see F-DEN-8. Image files
are otherwise ignored, wherever they are (F-LAY-7, F-LAY-8), and an image
that's missing gets no diagnostic in v0. The practical side, where to keep
images and how editors such as Obsidian write their paths, is in
[`using-wolfden.md`](using-wolfden.md#images).

### 5.2 `den.yaml`

```yaml
format: 1
name: Starfall Notes
authors:
  wren-calder:
    name: Wren Calder
packs: [space-opera]
bookkeeping_sections: ["Scratch", "* working notes"]
spoiler_checks: true
remote_images: false
auto_checkpoint: 30m
```

**F-DEN-1.** `format` is REQUIRED and is an integer. This spec is format
`1`. A missing or non-integer `format`, or one below `1`, is an [error]. A `format` higher
than the reader supports is an [error] that says so ("den format 2 is newer
than this version of Wolfden"). Additive changes, such as a new optional
key, don't change the format number.

**F-DEN-2.** `name` is an optional display name. It defaults to the den
root's folder name.

**F-DEN-3.** `authors` is an optional mapping from author key (F-KEY-1) to
an Author Identity. Each identity is a mapping with a REQUIRED non-empty
`name`. Other keys in an identity are kept. Any other shape is a
[warning], and the den still loads: an `authors` that isn't a mapping is
ignored, and an identity with an invalid key, or one that isn't a mapping
with a name, is skipped. A container that names a skipped author gets the
F-CTR-2 warning.

**F-DEN-4.** `packs` is an optional list of pack keys to enable. A null
`packs` or an empty list enables none beyond `core`, the same as leaving it
out. The `core` pack is always enabled, and listing it is allowed. An entry that names
neither a built-in pack nor a den-local pack (a folder `packs/<key>/` that
holds a `pack.yaml`) is an [error]. If the folder has a `pack.yml`
instead, the message says to rename it. A repeated entry is an [error]. Pack
contents and how packs combine, including a den-local pack named like a
built-in one, are defined in [`types.md`](types.md) §2 and §4.

**F-DEN-5.** Other keys are kept and ignored, with no diagnostic.

**F-DEN-6.** `bookkeeping_sections` is an optional list of non-empty
heading patterns that mark a section as bookkeeping, in addition to the
built-in ones (F-SEC-12). A null value or an empty list adds none, the
same as leaving it out. Any other shape is an [error]. A pattern made
only of `*` and spaces, which matches every section, is a [warning].

**F-DEN-7.** `spoiler_checks` is an optional boolean that turns on the
spoiler checks. In format 1 that's one check, F-LINK-13. It defaults to
`false`.
The positions on facts record where the reader learns each one, so a
writer can pace the reveals. The check helps writers who track that, by
flagging a link to something the reader hasn't met yet. A null value is
the same as leaving the key out. Any other value that isn't a YAML boolean
is a [warning], and the checks stay off.

**F-DEN-8.** `remote_images` is an optional boolean that lets the web UI
(`wolfden serve`) load images in notes whose address is on a website:
`![Harbour](https://example.com/harbour.jpg)`, an `http:` address, or one
that starts with `//`. It defaults to `false`: such an image doesn't
load, so opening a note doesn't tell the website that the den was opened,
and the page shows a placeholder with the image's alt text and its address
as a link to open in the browser. An image kept in the den, with a path
in the den (F-LAY-9), isn't affected by this setting: it always shows. A null value is the same as leaving the key
out. Any other value that isn't a YAML boolean is a [warning], and remote
images stay blocked. Apart from that warning, the setting changes only
what the web UI shows.

**F-DEN-9.** `auto_checkpoint` is an optional time that turns on
automatic checkpoints: a running `wolfden serve` makes a checkpoint once
the den's files have gone that long without a change (design.md §6,
#30). It's off by default, so checkpoints are made only when the writer
or an agent asks. The time is hours, minutes, or both, in that order,
each a whole number with its unit straight after and no spaces: `30m`,
`2h`, `1h30m`. Units are lowercase `h` and `m`; there are no seconds or
days, and with hours, the minutes are under 60 (`1h60m` isn't a time;
write `2h`). It must be from `5m` to `24h`, inclusive. An unquoted null
(or no value) or an unquoted `false` is the same as leaving the key out;
a quoted `"false"` or `"null"` is text, not one of those. Any other value
is a [warning], and automatic checkpoints stay off: a time out of range
(`4m`, `25h`, `999999999h`), a bare number (`30`), another unit (`30s`,
`1d`), an uppercase unit, `1h60m`, a block scalar, `true`, a list, or a
mapping. A den without history isn't a format matter: the setting is
valid there, and `wolfden serve` warns that it does nothing. Apart from
that, the setting changes only when checkpoints are made, never what the
den means.

*Note:* an automatic checkpoint is a commit made by the writer's own
git, so if git signs commits (with GPG, say), it needs the passphrase
cached by an agent, or it waits on a prompt. While it waits, saving
isn't blocked: edits go to the files as usual (design.md §6).

## 6. Containers: universe, series, book

### 6.1 Common rules

**F-CTR-1.** A container file's frontmatter SHOULD have an `id` (F-ID-1,
F-ID-2) and MUST have a non-empty string `name`. [error] for a missing or
empty `name`.

**F-CTR-2.** `authors` is an optional list of author keys from `den.yaml`.
A book without `authors` takes its series' list, and a series without one
takes its universe's. An unknown author key is a [warning]. So is an
`authors` that isn't a list (`authors: wren-calder`), or an entry that
isn't an author key, and either is ignored.

**F-CTR-3.** The body is the container's overview prose. Its links resolve
from the container's own scope (§10), and its sections follow §9.

**F-CTR-4.** Other frontmatter keys are kept and ignored, with no
diagnostic (F-ENT-9 applies here too).

**F-CTR-5.** `chapter_defaults` is an optional mapping of chapter field
values for the chapters of the books in the container, inherited down the
scope chain, with the nearest container winning field by field and a
chapter's own `fields` (F-BOOK-3) overriding them. Its rules are in
[`types.md`](types.md) F-CHAP-5, and every problem with it is a [warning].

**F-CTR-6.** `chapter_heading` is an optional list of chapter field names
([`types.md`](types.md) §8) that builds the heading shown for the chapters
of the books in the container, such as `chapter_heading: [pov, setting,
when]` (#227). Like `chapter_defaults`, it's inherited down the scope
chain, but as a whole list: the nearest container in a book's scope chain
(book, series, universe) that sets it wins, and its list replaces any
further up. One name on its own (`chapter_heading: pov`) is short for a
one-item list. A null `chapter_heading` is the same as leaving it out,
and an empty list (`[]`) means "no fields here", which stops inheritance.
Leaving it out everywhere changes nothing: a chapter shows its title.

A chapter's **heading** is its `title`, if it has one, then the effective
values (types.md F-CHAP-5) of the listed fields, in list order. A field
the chapter has no value for, or "none here" (`[]`), is left out. A link
shows its label, or else the current name of the note it reaches, so a
rename follows; a link that doesn't resolve shows its target as written.
A date mapping (types.md F-FT-11) shows its `text`, or, if that's
missing or blank, its `sort` as a date: the first number (the year) as
written, then month and day padded to two digits and joined with `-`,
then any time of day after a space, joined with `:`, so `[2350, 4, 12]`
shows as `2350-04-12`. A year of `0` before other numbers, the usual way
to sort dates of no stated year, is left out: `[0, 5, 3]` shows as
`05-03`. The parts are joined as in "The Long Watch ·
Ardent — Vesta Yards, 2350-04-12": ` · ` after the title, ` — ` after
the first field, `, ` between the others, and ` & ` between the items of
a list, such as two POV characters. White space, line breaks included,
becomes one space. Headings are for display only and never affect keys,
order, or positions. As of a story position, Wolfden never supplies the
name of a note the reader hasn't met (design.md §9): a link to one shows
as written, its label or else its target, so a writer who links such a
note by its key or id gives it a label.

Every problem with `chapter_heading` is a [warning]:

- A value that is neither a name nor a list (a mapping, say) is ignored,
  so the books inherit from further up.
- An entry that isn't a name (a list, a mapping, a null), and an entry
  that repeats an earlier one, are ignored. A list left with no entries
  counts as not set, unlike an explicit `[]`.
- A name that isn't a chapter field is kept, as a chapter's own undefined
  field is (types.md F-CHAP-3): a heading shows it for a chapter whose
  `fields` write it.

### 6.2 `universe.md`

```markdown
---
id: 01JAM9S346Q3D25VT4F5V37E3S
name: Starfall
authors: [wren-calder]
order: [long-dark, a-quiet-moon]
---

The Kessan Reach and the long war over it.
```

**F-UNI-1.** `order` is a list of the universe's series keys and standalone
book keys, in **story order**: the order in which a reader is meant to meet
the works. Positions are ordered by it (§8.2). An `order` that isn't a list,
such as a single key (`order: long-dark`), is an [error]. A null `order` is
the same as leaving it out.

**F-UNI-2.** An `order` entry that names no series or standalone book of
this universe, or a key that clashes (F-KEY-5), is a [warning] and is
ignored. A repeated entry is an [error].

**F-UNI-3.** `order` MAY be omitted when the universe has at most one series
or standalone book. Otherwise each series or standalone book missing from
`order` is a [warning], and the missing ones come after the listed ones, by
key in ASCII order.

### 6.3 `series.md`

```markdown
---
id: 01JA3E28JT97KB6CQ643DZVMXX
name: The Long Dark
books: [ember, ash]
---
```

**F-SER-1.** `books` lists the series' book keys in story order. A `books`
that isn't a list, such as a single key, is an [error]. A null `books` is
the same as leaving it out.

**F-SER-2.** An entry that names no book folder in this series is a
[warning] and is ignored. A repeated entry is an [error].

**F-SER-3.** `books` MAY be omitted when the series has at most one book.
Otherwise each book missing from `books` is a [warning], and the missing
ones come after the listed ones, by key in ASCII order.

### 6.4 `book.md`

```markdown
---
id: 01JAQKFBF5KZNWJ47TAN9ZT24M
name: Ember
status: draft
chapters:
  - { key: prologue, title: Before the Dark }
  - { key: "1", number: 1, title: Kessan Reach }
  - { key: "2", number: 2 }
  - { key: 2b, number: 3, title: The Long Watch }   # added in revision
  - { key: "3", number: 4 }
---
```

Chapter `2b` was inserted after `2`, so the chapters after it were
renumbered. Their keys didn't change, so positions such as `ember:3` still
point at the same chapter.

**F-BOOK-1.** `status` is optional: `draft` (the default) or `final`. Any
other value is an [error]. `final` marks a frozen book (design.md §5).
Writers MUST refuse edits within a final book's scope (its `book.md`,
`outline.md`, and entities) until it's unfrozen. `status: final` is the
whole freeze. The git tag that freezing adds when the den has git history
(design.md §6, #31) isn't part of the format.

**F-BOOK-2.** `chapters` is an optional list. The list order is the chapter
order. A missing or empty list means the book has no chapters yet, and
positions in it don't resolve (F-POS-4), except a bare book position
(F-POS-7). A null `chapters` is the same as a missing one. A `chapters`
that isn't a list, such as a single key or a mapping, is an [error].

**F-BOOK-3.** Each item is a mapping with:

- `key`, REQUIRED: the chapter key (F-KEY-1, F-KEY-2).
- `number`, optional: a non-negative integer used for display only. It
  never affects order or positions. A prologue or interlude can leave it
  out.
- `title`, optional: a string.

A `number` that isn't a non-negative integer (`"3"`, `3.5`, `-1`) is a
[warning], and the chapter is treated as having no number. Chapter-level
fields such as POV, an in-world date, or a setting go in an optional
`fields` mapping on the item, defined by packs
([`types.md`](types.md) §8), and can be set for many chapters at once
with `chapter_defaults` (F-CTR-5), and shown in the chapter's heading
with `chapter_heading` (F-CTR-6). Other keys are kept but not indexed
in v0. An item MAY instead be a plain scalar, short for
`{ key: <scalar> }`, so `chapters: [prologue, 1, 2, 3]` is valid. An
item of any other shape, or one with no `key`, is an [error].

**F-BOOK-4.** A repeated chapter key is an [error].

**F-BOOK-5.** `number` values, where present, SHOULD increase strictly down
the list. A `number` that doesn't is a [warning].

**F-BOOK-6.** Renumbering changes `number` only. Writers MUST NOT change a
chapter key as a side effect of renumbering or reordering.

## 7. Entities

### 7.1 Frontmatter

```markdown
---
id: 01JA7Q8M4XK3V9R2D5W8HN6TQB
type: character
name: Mara Voss
aliases: [Voss, "the Commander"]
status: canon
fields:
  species: "[[velari]]"
  rank:
    - { value: Lieutenant, from: "ember:1" }
    - { value: Commander,  from: "ember:22" }
  born: "2291"
x-color: teal
---
```

**F-ENT-1.** An entity SHOULD have an `id` (F-ID-1, F-ID-2). A missing
`id` is a [warning], and a malformed one is an [error].

**F-ENT-2.** `type` is REQUIRED and names a type enabled in the den: from
`core` or a pack enabled in `den.yaml`, built in or den-local (types.md
F-PACK-9). A missing `type` is an [error].
An unknown type is a [warning] ("unknown type 'charcter'"), and the entity
is indexed **untyped**: its fields are kept and indexed as written, it
takes part in links and views like any other entity, and the checks that
need a type (F-LAY-6, F-FLD-2, F-FLD-3, F-SEC-5) are skipped for it.

**F-ENT-3.** `name` is REQUIRED and is a non-empty string after trimming.
[error]

**F-ENT-4.** `aliases` is an optional list of non-empty strings. A null
`aliases` (the key with no value, or `~`) or an empty list means the
entity has no aliases, the same as leaving the key out, with no
diagnostic. That's what a writer leaves behind after removing the last
alias from a block list (F-RT-6). Any other shape, including a single
string, is an [error].

**F-ENT-5.** `status` is optional and defaults to `draft`, so nothing
counts as settled until the writer says so: an unmarked note still shows in
canon views, marked as draft. Its values are the status vocabulary shared
by entities, sections, field entries, and outline rows (F-STAT-1, §9.3).
Any other value is an [error]. A missing `status` has no diagnostic.
Templates SHOULD write a `status`, and a writer that creates an entity
MUST write its `status` explicitly (types.md F-TPL-6). A `candidate`
entity, such as one of several possible minor characters, is left out of
canon views like any other out-of-canon fact.

A name that is still a placeholder, waiting on a name check, is marked with
an optional `placeholder: true` field, defined on every type (types.md
F-FT-17).

Note: `status` says whether something is canon, not where it stands in the
story. A plot thread that hasn't paid off yet is still canon. Whether it
is planted or paid off comes from the `thread` type's `planted` and
`paid_off` fields (types.md §9.1). A resolution that isn't decided yet is
marked with `status` on just the part that's open, usually the thread's
`## Payoff` section.

**F-ENT-6.** `from` is an optional position (§8) where the entity enters
canon, that is, where the reader first meets it. Without `from`, a
book-scope entity enters at the start of its book, a series-scope entity at
the start of its series, and a universe-scope entity at the start of the
story (F-ORD-3). A canon-as-of view at position `p` leaves
out an entity whose `from` is after `p`. A book-scope entity whose `from`
is outside its own book is a [warning]. A `from` with no value, or with
`~` or another YAML null (F-YAML-5), counts as missing, with no
diagnostic. There is no entity-level `until`:
use `status` for material that leaves canon.

**F-ENT-7.** `fields` is an optional mapping from field name to value
(§7.2). Any other shape is an [error].

**F-ENT-8.** The body holds the entity's sections (§9).

**F-ENT-9.** Other top-level keys are kept and ignored, with no diagnostic.
Keys a writer invents SHOULD start with `x-`. Future format versions won't
define top-level keys with that prefix.

### 7.2 Field values

**F-FLD-1.** Field names MUST match `lower *(lower / DIGIT / "_")`, up to
64 characters (`species`, `see_also`). [error]

**F-FLD-2.** A field the entity's type doesn't define is a [warning]
(usually a typo). Its value is kept and indexed as written.

**F-FLD-3.** How a value must look for its field type (`string`, `ref`,
`enum`, ...) is defined with the field types in [`types.md`](types.md)
§5.2. A value that doesn't match its field type is a [warning], and so is
a missing required field.

**F-FLD-4.** A null value (`born:` with nothing after it) means the field is
unset, the same as leaving it out.

**F-FLD-5.** A value is **plain** (true at every position), a single
**entry**, or a **temporal** list of entries (§7.3). F-TMP-1 decides
which.

**F-FLD-6.** In YAML, `species: [[velari]]` without quotes is a list inside
a list, not a link. A value that is a one-item list whose only item is a
list of strings is a [warning] that suggests quoting it:
`species: "[[velari]]"`.

**F-FLD-7.** A field named `row` ties an entity (usually an `event`) to
outline rows. It is defined on every type, so it never triggers F-FLD-2.
Its value is a row id (§11) or a list of row ids, and views can show the
entity with the rows it names (F-OUT-8). A `row` entry with
`from` or `until` is a [warning]. A row id resolves among the outlines of
the entity's series, or of its standalone book. For a universe-scope entity
it resolves among every outline in the universe, and a row id found in more
than one series is ambiguous. So is a row id that appears twice in the
outlines it resolves among (F-OUT-5). A row id that doesn't resolve, or is
ambiguous, is a [warning]. `row` is not a position: v0 positions stay
`book:chapter` (F-POS-6).

### 7.3 Entries and temporal values

```yaml
fields:
  born: { value: "2291", pin: true }
  species: { value: "[[velari]]", from: "ash:4" }
  rank:
    - { value: Lieutenant, from: "ember:1" }
    - { value: Commander, from: "ember:22", pin: true }
    - { value: Captain, from: "ash:30", status: candidate }
  posting:
    - { value: "[[tessaly]]", from: "ember:1", until: "ash:9" }
    - { value: "[[kessan-reach]]", from: "ash:12" }
```

`born` is a plain fact, pinned. `species` never changes, but the reader
only learns it at `ash:4`, so it's **known from** there (F-TMP-8):
canon-as-of views leave it out before `ash:4`. As of `ember:21`, `rank` is
Lieutenant. From `ember:22` on, it's Commander, a pinned fact. Captain is
a banked option, so canon views leave it out and Commander doesn't end at
`ash:30`. `posting` is unset from `ash:9` up to `ash:12`.

**F-TMP-1.** An **entry** is a mapping with a `value` key. A field's value
is a single entry, a **temporal list** (a non-empty list in which every
item is an entry), or else a plain value. A list in which some items are
entries and others aren't is an [error]. A single entry means the same as a
one-item temporal list (F-TMP-8 aside). The shape decides, not the type, so a parser can
tell these apart without loading packs. As a result, a plain value that is
a mapping can't use a key named `value`. This section covers an entity's
`fields`. Chapter fields and `chapter_defaults` have no entries, and their
problems are warnings (types.md F-CHAP-3).

**F-TMP-2.** An entry has:

- `value`, REQUIRED: any YAML value, including a list. A `value` that is
  itself an entry or a temporal list is an [error]. `value: null` means the
  field is unset from that entry's `from`.
- `from`, optional: a position (§8). Without it, the entry starts at the
  start of the story.
- `until`, optional: a position.
- `status`, optional: the entry's status (F-STAT-1). Without it, the entry
  takes its entity's status.
- `pin`, optional: `true` pins the entry (F-PIN-1).

A `from` or `until` with no value, or with `~` or another YAML null
(F-YAML-5), counts as missing, with no diagnostic.

Other keys in an entry are a [warning], and are kept.

**F-TMP-3.** An entry holds at positions `p` with `from <= p < until`.
`from` is inclusive and `until` is exclusive. An `until` that isn't after
its `from` is a [warning], not an error, since it depends on chapter order
in `book.md` (F-GEN-4). Such an entry is never in effect and is left out of
views.

**F-TMP-4.** Only **live** entries take part in this rule and F-TMP-5: an
entry is live if it's in canon (F-STAT-3), its positions resolve (F-POS-4),
and its explicit range isn't empty (F-TMP-3). A live entry without `until`
holds until the `from` of the next live entry in story order, or to the end
of the story if there is none. So in the example above, Lieutenant and
Commander don't overlap. Any other entry without `until` runs to the end of
the story.

**F-TMP-5.** Two live entries **overlap** if they share a `from` (two
entries without `from` included), or if an explicit `until` reaches past
the next live entry's `from`. Overlap is a [warning]. Where entries
overlap, a canon-as-of view uses the one with the later `from`, and for
equal `from`s, the one later in the file. Entries that aren't live never
overlap, so several `candidate` values can share a `from`.

**F-TMP-6.** An explicit `until` that comes before the next entry's `from`
leaves a gap in which the field is unset. Gaps are allowed.

**F-TMP-7.** The order of entries in the file doesn't change their meaning,
except for the tie in F-TMP-5. Writers SHOULD list entries in story order
and MUST NOT reorder them.

**F-TMP-8.** A temporal list, or an entry with `until`, on a field the
type defines as not temporal is a [warning]. It's still read as temporal.
A single entry without `until` is fine on any field, with no diagnostic,
whatever else it has (`value`, `from`, `status`, `pin`). On a field that
isn't temporal, its `from` means the fact is **known from** that
position: it's where the reader learns the fact, not where the value
changes. It's read like any entry (F-TMP-3, F-TMP-4), so canon-as-of views
leave the value out before its `from` and show it from there on. That's
how a fact such as a death or a hidden identity stays out of views from
before the reveal. The `row` field is the exception: a `row` entry with
`from` is still a [warning] (F-FLD-7).

## 8. Story positions and order

### 8.1 Position syntax

```abnf
position   = book-key [ ":" chapter-key ]
book-key   = key                       ; starts with a letter (F-KEY-2)
chapter-key = key
scene-form = book-key ":" chapter-key "." 1*DIGIT   ; reserved for scenes
row-form   = book-key ":" "r" row-id             ; reserved for rows
```

**F-POS-1.** A position is a string matching `position`, with no
whitespace. Positions appear in `from` and `until` values in frontmatter,
in section comments (§9.2), and as the values of `position` fields
(types.md F-FT-10). In YAML they SHOULD be quoted.

**F-POS-2.** A position in the scene form (`ember:14.2`) is an [error] with
the message "scenes are not supported yet; v0 positions are chapter-level".
It MUST NOT be read as chapter `14` or as a chapter key `14.2`. Any position
with a `.` after the `:` gets this error.

**F-POS-3.** Any other string that doesn't match `position` is an [error]
("invalid story position").

**F-POS-4.** A position resolves to a book of the universe the file belongs
to (any series or standalone) and a chapter in that book's `chapters`,
except a bare book position, which resolves to the start of the book
(F-POS-7). A book or chapter that doesn't exist is a [warning], not an
error, so writers can plan ahead. The fact that uses it (a temporal entry,
a section, or an entity's `from`) stays indexed but is left out of canon
views. It isn't
live (F-TMP-4), and range checks (F-TMP-3) skip it.

**F-POS-5.** Positions don't cross universes (design.md §2). A book key
from another universe doesn't resolve, even if it exists there.

**F-POS-6.** A position's **chapter part** is the text after its first
`:`. A position whose chapter part is `r` followed by a digit (`ember:r75`,
`ember:r7:2`) is in the reserved row form. It is an [error] with the
message "outline-row positions are not supported yet; v0 positions are
chapter-level". This check comes before F-POS-2, so `ember:r75.1` gets this
error too. F-KEY-2 keeps chapter keys out of this form. To tie a fact to a
row in v0, use a `row` field (F-FLD-7).

**F-POS-7.** A **bare book position** (`ember`) names the **start of the
book** (F-ORD-3), which orders the same as its first chapter. It resolves
even in a book with no chapters yet, and it's the same point F-ENT-6 gives a
book-scope entity with no `from`. It's for facts that "became true in Book
2" and for imports where only the book is known. Writers SHOULD use
chapter-level positions wherever they know the chapter, but
`wolfden check` doesn't warn on the bare form. A bare key that names no
book of the universe doesn't resolve (F-POS-4).

| Position | Result |
|---|---|
| `ember:14`, `ember:prologue`, `a-quiet-moon:2b` | valid |
| `ember` | valid, the start of `ember` (F-POS-7) |
| `ember:14.2` | [error] F-POS-2, scenes not supported yet |
| `ember:r75`, `ember:r69a` | [error] F-POS-6, rows not supported yet |
| `ember:`, `:14` | [error] F-POS-3 |
| `Ember:14`, `ember: 14`, `ember:14:2` | [error] F-POS-3 |
| `ember:99`, where `ember` has no chapter `99` | [warning] F-POS-4 |

`from: ember: 14`, with a space after the second colon, isn't a position
at all: it's a YAML syntax error (F-YAML-1).

### 8.2 Order

**F-ORD-1.** The universe's books are put in **story order** by walking its
`order` (F-UNI-1, F-UNI-3) and putting each series' books in place, in the
order of its `books` (F-SER-1, F-SER-3). That gives one total order over
every book in the universe.

**F-ORD-2.** Position `a` is before position `b` if `a`'s book is earlier
in story order, or if both are in the same book and `a`'s chapter is
earlier in that book's `chapters` list.

**F-ORD-3.** Four more points complete the order. The **start of the
story** comes before every position. The **start of a series** is the start
of its first book in story order. A series with no books yet starts at
its place in story order: after every chapter of the works before it.
The **start of a book** comes after every chapter of earlier books and
orders the same as the book's first chapter, or, in a book with no
chapters yet, before every chapter of later books. The **end of the story** comes after
every position. A missing `from` or `until` means one of these points. The
only one a position can name is the start of a book, with a bare book
position (F-POS-7).

**F-ORD-4.** Story order is reading order, not in-world chronology. When
something happens in-world is the `when` field of `event` entities
(types.md §9.1).

With `order: [long-dark, a-quiet-moon]` and `books: [ember, ash]`, every
`ember` position is before every `ash` position, which is before every
`a-quiet-moon` position.

## 9. Sections

### 9.1 Structure

```markdown
# Mara Voss

Executive officer of the *Tessaly*.

## Appearance
Silver-streaked hair.

## Shoulder injury
<!-- wolfden: from=ember:19 -->
Takes a flechette round at [[kessan-reach]].

### Recovery
<!-- wolfden: from=ash:3 until=ash:20 -->
Months of physical therapy aboard the hospital ship.
```

**F-SEC-1.** A **section** is a heading of level 2 to 6 plus everything
after it up to the next heading of the same or a higher level (fewer `#`
signs). Level-2 sections are the entity's sections, and deeper ones are
subsections of the section that holds them. Both ATX (`##`) and setext
headings count. Text inside code blocks and HTML blocks is never a heading.
Only headings at the top level of the body start sections: a heading inside
a block quote or a list item (`> ## Aside`, `- ## Item`) is part of the
section's text.

**F-SEC-2.** Content before the first level-2 heading is the **lead**,
including any headings of level 3 to 6 in it: they don't start sections,
and a wolfden comment under one is misplaced (F-SEC-10). A
level-1 heading in the lead is the display title. It's optional and doesn't
have to match `name`. A level-1 heading after the first level-2 heading is
a [warning], and is treated as a level-2 heading, so the content under it
is a section named by it rather than content with no owner.

**F-SEC-3.** A section's **name** is its heading text, without the `#`
markers or closing `#`s, and without any trailing HTML comments (F-SEC-10),
trimmed, with any inline markup kept as written.
Names match (against a type's standard sections, and in `[[target#Heading]]`
links) under the normalization in F-LINK-4. The `Custom:` prefix in
design.md's example has no special meaning. Any section that the type
doesn't define is a custom section, and it's kept like any other (§12).

**F-SEC-4.** Two sections MAY have the same name, for example one
`## Appearance` per era, each with its own position. For a section the
entity's type defines (a standard section, types.md §6), a new section
replaces the old one: a live level-2 section hands over to the next one
with the same name (F-SEC-8). Two live level-2 standard sections with the
same name **overlap** if they have the same `from` (as F-SEC-8 gives it,
explicit or not), or if an
explicit `until` reaches past the next one's `from`, as for entries
(F-TMP-5). Overlap is a [warning]. Unlike F-TMP-5, there's no
tie-break: where sections overlap, views show each of them that is in
effect, so no prose is hidden. Custom sections never hand over and
never get this warning, so cumulative ones such as `## Additions` or
`## Session notes` can repeat freely, and out-of-canon sections don't
count either.

**F-SEC-5.** A required section (types.md F-TYPE-11) that's missing is a
[warning].

### 9.2 Section comments

```abnf
pos-line   = 0*3SP "<!--" *WSP "wolfden:" *WSP attr *(1*WSP attr) *WSP "-->" *WSP
attr       = attr-name "=" attr-value
attr-name  = lower *(lower / DIGIT / "-" / "_")
attr-value = 1*vchar / DQUOTE *qchar DQUOTE
vchar      = <any character except DQUOTE and Unicode whitespace,
              and never the sequence "-->">
qchar      = <any character except DQUOTE, CR, LF,
              and never the sequence "-->">
```

"Unicode whitespace" is as CommonMark defines it, so it includes tabs and
no-break spaces. A section comment is indented by at most three spaces,
as in CommonMark: a line indented by four or more spaces, or by a tab, is
an indented code block, so it's text, not a comment, and has no
diagnostic. The comment ends at its first `-->`, and no value can
contain the sequence `-->` (single `-` and `>` characters are fine, as in
`from=a-quiet-moon:2b`), so an unquoted value stops there:
`from=ember:19-->` is `from=ember:19`. A line with anything but whitespace
after its first `-->`, such as `from=ember:19--> -->`, doesn't match
`pos-line` (F-SEC-10). Unquoted values can be any characters except
whitespace and `"`, so `pov=Zoë` works. Values with spaces need quotes:
`pov="Mara Voss"`.

**F-SEC-6.** A **section comment** is a line matching `pos-line`, placed on
the line after a heading of level 2 to 6, or after a stray level-1
heading that F-SEC-2 treats as level 2 (after the underline, for a setext
heading). One blank line between them is allowed, where a blank line is
empty or holds only spaces and tabs, as in CommonMark. It applies to that
heading's section.

**F-SEC-7.** Attributes are space-separated `key=value` pairs, in any
order, and a value MAY be double-quoted. A section's comment takes:

- `from` and `until`: positions (§8). An empty value (`from=""`) or `~`
  counts as missing, with no diagnostic.
- `status`: the section's status (F-STAT-1).
- `pin`: `true` or `false` (F-PIN-1).
- `canon`: `false` marks the section as bookkeeping (F-SEC-12). `true` keeps
  a section out of bookkeeping even if its name matches a bookkeeping
  pattern, unless its parent section is bookkeeping, in which case it's a
  [warning] and does nothing. Without it, F-SEC-12 decides. Any other value
  is an [error].

A repeated attribute is an [error]. Any other attribute is a [warning], and
is kept as written.

```markdown
## Shoulder injury
<!-- wolfden: status=candidate from=ember:19 -->
```

**F-SEC-8.** A section with `from` or `until` is in effect at positions
`p` with `from <= p < until`, the same as F-TMP-3. A missing `from` means
from the entity's own `from` (F-ENT-6), except for a thread's `## Payoff`
(types.md F-CORE-4). An `until` that isn't after its `from` is a
[warning] (F-GEN-4), and the section is never in effect.

A missing `until` depends on the section:

- A **standard section** (types.md F-TYPE-10) at level 2 **hands over**
  like a temporal entry (F-TMP-4). If it's live, it ends at the earliest
  `from` after its own among the other live level-2 sections with the same
  name (matched as in F-SEC-3), or at the end of the story if there is
  none. A section is **live** if it's in canon (F-STAT-3), isn't
  bookkeeping (F-SEC-12), its positions resolve (F-POS-4), and its
  explicit range isn't empty. So a bookkeeping section never ends an
  earlier one, since canon views leave it out. Its `from` here
  is the one this rule gives it, explicit or not. A standard section that
  isn't live runs to the end of the story, so views that show
  out-of-canon facts, such as an idea bank, still have a range for it.
- Any other section, including a custom section and a subsection, runs to
  the end of the story, or for a subsection to the end of its parent
  (F-SEC-9). It never ends on its own when a later section starts.

An explicit `until` always wins. A section's **effective `until`** is its
`until`, or else the end that this rule gives it. A level-2 section with
neither `from` nor `until` that doesn't hand over is in effect wherever
its entity is.

```markdown
## Appearance
Silver-streaked hair.

## Appearance
<!-- wolfden: from=ember:19 -->
Silver-streaked hair, and her left arm in a brace.

## Appearance
<!-- wolfden: from=ash:20 -->
The brace is gone, but she still favors the arm.
```

The first `## Appearance` is in effect up to `ember:19`, the second from
`ember:19` up to `ash:20`, and the third from `ash:20` on.

**F-SEC-9.** A subsection is in effect only where both it and every section
that holds it are in effect. So a subsection's **effective `from`** is the
later of its own `from` and its parent's effective `from`, and the same
goes for `until` with the earlier of the two, where a level-2 parent's
`until` is its effective `until` (F-SEC-8), so a subsection ends when its
parent hands over. A subsection whose own `from` or `until` lies outside
its parent's range is a [warning].

**F-SEC-10.** Outside code blocks and code spans, `<!--` followed by
optional whitespace and `wolfden:` starts a **wolfden comment**. Its only
valid place is a line of its own under a section heading of an
entity or container file (F-SEC-6). In a valid place, a line that doesn't
match `pos-line`, such as one that doesn't close on the same line or has
no attributes, is a [warning], and the comment is ignored: the section
loads without it. A comment that doesn't close on its own line starts a
CommonMark HTML block that runs to the next `-->` in the file, or to the
end of the file, and headings
inside it aren't headings (F-SEC-1), so the warning SHOULD name the line
where that block ends. Values are still checked strictly. In a comment that
does match, a bad value (`status=maybe`, `from=ember:14.2`, `pin=yes`) or a
repeated attribute is an [error] (F-SEC-7), since guessing what it meant
would be worse. A wolfden comment anywhere else, whether well-formed or
not, is a [warning] and does nothing. That includes one at the end of a
heading line (`## Shoulder injury <!-- wolfden: from=ember:19 -->`), where
the warning says to move it to the next line, and any in `outline.md`.

**F-SEC-11.** All other HTML comments are kept and ignored, wherever they
are, including directly under a heading. A comment inside a code block or
code span is just text.

**F-SEC-12.** A **bookkeeping** section records work on the notes rather
than the story: a change log or session notes. A section is bookkeeping if
its comment has `canon=false`, or if its name matches a **bookkeeping
pattern** and its comment doesn't have `canon=true` (F-SEC-7). The
subsections of a bookkeeping section are bookkeeping too, whatever their
own comments say.

The built-in patterns are `Change log`, `Changelog`, `Revision history`,
`Session note`, and `Session notes`, each alone or followed by ` *`, and
`* session note` and `* session notes`, each also followed by ` *` or not.
`den.yaml` can add more (F-DEN-6). In a pattern, `*` matches any text,
including none, and everything else matches literally. A pattern is matched
against the section's name as plain text, that is, the CommonMark text
content of the heading with emphasis, code, and link markup removed
(`## **Change log**` matches), and both sides get F-LINK-4 normalization
first, so case doesn't matter. `## v12 session notes (merged)` and
`## Changelog 2026` are bookkeeping. `## Session`, `## Change logistics`,
and `## Possession notes` are not.

Bookkeeping sections are kept, indexed, and searchable, but
canon-as-of views, pinned canon, and the MCP server's canon context leave
them out (§9.4), whatever their status. Pinning one is a [warning]. Links
in a bookkeeping section skip F-LINK-12 and F-LINK-13, since a change log
is where links to retconned or later material belong, but broken and
ambiguous links are still reported.

| Under `## Shoulder injury` | Result |
|---|---|
| `<!-- wolfden: from=ember:19 -->` | valid |
| `<!-- wolfden: from="ember:19" until=ember:30 -->` | valid |
| `<!--wolfden: until=ash:1 -->` | valid |
| `<!-- wolfden: from=ember:19 from=ember:20 -->` | [error] F-SEC-7 |
| `<!-- wolfden: from=ember:14.2 -->` | [error] F-POS-2 |
| `<!-- wolfden: from=ember:19` (no `-->`) | [warning] F-SEC-10, ignored |
| `<!-- wolfden: from=ember:19-->` | valid |
| `<!-- wolfden: pov=Zoë -->` | [warning] F-SEC-7 (unknown attribute, not malformed) |
| `<!-- wolfden: -->` | [warning] F-SEC-10, no attributes, ignored |
| `<!-- wolfden: status=undecided pin=true -->` | valid ([warning] F-PIN-3) |
| `<!-- wolfden: status=maybe -->` | [error] F-STAT-1 |
| `<!-- wolfden: pin=yes -->` | [error] F-PIN-1 |
| `<!-- wolfden: canon=false -->` | valid, bookkeeping (F-SEC-12) |
| `<!-- wolfden: pov=mara -->` | [warning] F-SEC-7 |
| a blank line, then `<!-- wolfden: from=ember:19 -->` | valid (F-SEC-6) |
| two blank lines, then `<!-- wolfden: from=ember:19 -->` | [warning] F-SEC-10 |
| `   <!-- wolfden: from=ember:19 -->` (three spaces) | valid |
| a blank line, then `    <!-- wolfden: from=ember:19 -->` (four spaces) | code, not a comment |
| `## Shoulder injury <!-- wolfden: from=ember:19 -->` | [warning] F-SEC-10 |
| `<!-- todo: check the date -->` | kept, ignored |

### 9.3 Status and pins

Entities, sections, field entries (§7.3), and outline rows (§11) share one
status vocabulary. Sections, entries, and rows are **facts**, and sections
and entries can be pinned.

| `status` | Meaning | In canon |
|---|---|---|
| `idea` | Undecided material (an idea bank) | no |
| `undecided` | An open question, with options banked under it | no |
| `candidate` | One option for an undecided question | no |
| `draft` | Meant as canon, not settled | yes, marked as draft |
| `canon` | Settled | yes |
| `retconned` | Was canon, has been replaced | no |

**F-STAT-1.** A `status` MUST be one of the six values above. [error],
except in an outline table cell, where it's a [warning] (F-OUT-9).

**F-STAT-2.** A fact without a `status` takes the status of what holds it:
a subsection its parent section's, a section or field entry its entity's.
An outline row without one is `draft`, like an entity (F-ENT-5).

**F-STAT-3.** A fact can't be more canon than what holds it. Write
`own(f)` for a fact's written status, or what F-STAT-2 gives it, and
`eff(f)` for its **effective status**. For an entity or a row, `eff` is
`own`. For any other fact, with `h` the fact's holder:

1. If `own(f)` is out of canon, `eff(f)` is `own(f)`.
2. Otherwise, if `eff(h)` is out of canon, `eff(f)` is `eff(h)`.
3. Otherwise, if `eff(h)` is `draft`, `eff(f)` is `draft`.
4. Otherwise, `eff(f)` is `own(f)`.

So a `candidate` section in an `idea` entity stays `candidate`, and a
`canon` section in an `idea` entity is `idea`. A fact whose written status
is `draft` or `canon` inside an out-of-canon holder (rule 2) is a
[warning]. "In canon" always means an effective status of `draft` or
`canon`.

**F-STAT-4.** Out-of-canon facts are kept and indexed, and views that ask
for them (an idea bank, a list of undecided questions) show them. Canon
views leave them out (§9.4), just as they leave out out-of-canon entities.

**F-PIN-1.** A **pinned** fact is hard canon: one of the facts most
important to check before drafting. A section is pinned with `pin=true` in
its comment, and a field entry with `pin: true`, including a plain fact
written as a single entry (`born: { value: "2291", pin: true }`). `pin` is
`true` or `false`, and defaults to `false`. Any other value is an [error].

**F-PIN-2.** Pinning a section pins its subsections.

**F-PIN-3.** A pinned fact whose effective status is out of canon is a
[warning]. A pinned `draft` fact is allowed, and is marked as draft.

### 9.4 Canon views

**F-VIEW-1.** A **canon-as-of** view of universe `U` at position `p`
covers the entities of every scope of `U`: the universe, each series, and
each book, wherever they sit in story order. It includes an entity if its
`status` is `draft` or `canon` and `p` isn't before its `from` (F-ENT-6),
compared in the universe's story order (F-ORD-1). So the folder an entity
is filed in decides only where it enters canon when it has no `from`, not
whether a later view shows it: a view late in the story shows a side
character filed under an earlier book, and a later book's entities
are left out of an earlier view by position alone. For each included
entity it shows the lead, each field's value at `p` from its live entries
(F-TMP-4), and the in-canon sections in effect at `p` (F-SEC-8, F-SEC-9),
except bookkeeping sections (F-SEC-12). Facts whose positions don't
resolve are left out (F-POS-4). `p` MAY be omitted, which means the end of
the story: all canon so far.

A view MAY be **filtered** to a series or book `S` of `U`. It then covers
only the entities filed in `S`'s scope chain (§1.4) and in the scopes
inside `S`: a series filter keeps the universe's entities,
the series', and those of all its books, and leaves out other series and
standalone books, and a book filter keeps the book's entities and those of
its series and universe. `p` doesn't have to be in `S`, since story order
runs across the whole universe. A filter of `U` itself, like no filter,
covers every scope.

**F-VIEW-2.** A **pinned canon** view at `p`, with or without a filter,
is the part of the canon-as-of view that is pinned: every pinned section
and field entry that the canon-as-of view includes, grouped by entity.
The MCP server serves it ahead of the rest of canon (design.md §8).

**F-VIEW-3.** Views list entities in a fixed order, so output can be
compared in tests: by scope (the universe, then series and books in story
order), then by type key, then by normalized `name` (F-LINK-4), then by
file path. A series orders at its start (F-ORD-3), just before its first
book, and a series with no books yet at its own place in story order.
Within an entity, fields come in frontmatter order and sections in file
order.

### 9.5 Keeping reveals in place

This section has no rules. It describes how to write notes whose
canon-as-of views don't give away later reveals, for writers who track
where the reader learns each fact.

Summary prose runs ahead of the story easily. An `## Overview`, `## Arc`,
or `## Relationships` that sums up a character tends to name people met
later and events still to come, and a view from an early chapter would
show all of it. To keep it in place, write the unpositioned part as of
the entity's `from`, and put each later reveal in a subsection with its
own `from=` (F-SEC-9):

```markdown
## Overview
Executive officer of the *Tessaly*.

### In command
<!-- wolfden: from=ember:22 -->
Takes command of the *Tessaly* after the ambush at [[kessan-reach]].
```

A fact in a field works the same way. A single entry with a `from` keeps a
fact that never changes, such as a death, out of views from before the
reader learns it (F-TMP-8).

With `spoiler_checks: true` (F-DEN-7), F-LINK-13 flags a link that runs
ahead of its position. It sees links only. A later fact written as plain
text, or a link to something that entered canon earlier but says
something new about it, gets no warning, so read summary prose for
reveals as well as links.

Aliases (F-ENT-4) aren't positional: they hold for the whole story, so
looking up a hidden identity's alias, as a link or a lookup by name does,
finds the entity at any position and can confirm the reveal.

## 10. Links

### 10.1 Syntax

```abnf
link       = "[[" target [ "#" heading ] [ "|" label ] "]]"
target     = 1*tchar
heading    = 1*tchar
label      = 1*lchar
tchar      = <any character except "[", "]", "|", "#", CR, LF>
lchar      = <any character except "[", "]", CR, LF>
```

`[[mara-voss]]` links to an entity. `[[mara-voss|Voss]]` shows "Voss".
`[[mara-voss#Shoulder injury]]` links to a section. Without a label, the
link shows its text as written. `\|` separates the label too, so a link
escaped for a table cell (`[[mara-voss\|Voss]]`, F-OUT-12) reads the same
anywhere.

**F-LINK-1.** In a body (entity, container, or outline), links are
recognized anywhere except in code spans, code blocks, and HTML comments.
A `[[` preceded by a backslash isn't a link. In frontmatter, links are
recognized in every string under an entity's `fields` (including list
items and temporal `value`s), under a chapter item's `fields` in
`book.md`, and under a container's `chapter_defaults` (types.md §8), and
nowhere else (not in `name`, `aliases`, or `x-` keys).

**F-LINK-2.** `![[...]]` (an Obsidian embed) isn't a link in v0. It's left
as text, with no diagnostic.

**F-LINK-3.** The target, heading, and label are trimmed. A link with an
empty target (`[[]]`, `[[ |Voss]]`) is a [warning] and isn't a link.

### 10.2 Resolution

**F-LINK-4.** Targets match slugs, names, and aliases, and headings match
section names, after this normalization of both sides: NFC (F-ENC-5), trim,
collapse each run of whitespace to one space, then Unicode default case
folding. So matching is case-insensitive. Hyphens and spaces stay distinct:
`[[mara voss]]` matches the name "Mara Voss", not the slug `mara-voss`.

**F-LINK-5.** The candidates are the entities of the linking file's
universe, at every scope and of every status. Container files aren't link
targets in v0.

**F-LINK-6.** Resolution tries these tiers in order and stops at the first
one with any match:

1. **id:** the target is a ULID (F-ID-1) equal to an entity's `id`.
2. **slug:** the target matches an entity's slug.
3. **name:** the target matches an entity's `name`.
4. **alias:** the target matches one of an entity's `aliases`.

**F-LINK-7.** If the winning tier has one match, the link resolves to it.
If it has several, the match in the nearest scope of the linking file's
scope chain wins. If none of the matches is in the scope chain, or two tie
at the nearest scope, the link is **ambiguous**: a [warning] that lists the
matches, and the link doesn't resolve.

**F-LINK-8.** A link with no match is **broken**, a [warning]. Broken links
are allowed, since they often mark notes not yet written. If the target
would match an entity in another universe of the den, the warning says so
("matches only 'Mara Voss' in universe 'drift'; links don't cross
universes", design.md §2). If it matches a book or series key or name, the
warning says containers aren't link targets yet.

**F-LINK-9.** A `#heading` matches the first section of the resolved entity,
at any level, whose name matches (F-LINK-4). No match is a [warning], and
the link still resolves to the entity.

**F-LINK-10.** The index stores each resolved link's target `id`
(design.md §3), or the target's path while it has no `id` (F-ID-2).
Indexing MUST NOT rewrite link text. Renames and moves made in the app
rewrite slug links (F-RT-5).

**F-LINK-11.** A `ref` field (types.md F-FT-9) holds a single link per
value, with nothing else in the string. A resolved entity whose type isn't
the one the field names is a [warning].

### 10.3 Link checks

**F-LINK-12.** A link in canon text to an `idea` or `retconned` entity is
a [warning]. Canon text is an in-canon fact (F-STAT-3): a section, a field
entry, or an outline row. It is also the lead and the plain field values
of an in-canon entity, judged by the entity's status, and a chapter
item's `fields` and a container's `chapter_defaults`, which count as in
canon. Container bodies and the free-form notes of an outline aren't
checked.

**F-LINK-13.** A link's **position** depends on where it is:

- In an entry (F-TMP-1), on any field: the entry's `from`.
- In a section: the section's effective `from` (F-SEC-9).
- Elsewhere in an entity (the lead, or a plain field): the entity's `from`
  (F-ENT-6).
- In an outline row: its `chapter` cell (F-OUT-9), or else the start of
  its book. A `chapter` cell that doesn't resolve also falls back to the
  start of the book, with its F-OUT-9 warning.
- In a chapter item's `fields` in `book.md`: that chapter. In
  `chapter_defaults`: each chapter that inherits the value (types.md
  F-CHAP-4).
- In a container body: none.

A link whose target enters canon after the link's position is a [warning],
since it's usually a spoiler or a mistake (design.md §7). Links with no
position, or whose position is the start of the story, aren't checked.
A link in `chapter_defaults` that runs ahead of several chapters is one
warning that names them (types.md F-CHAP-4).

This check is a spoiler check: it runs only in a den whose `den.yaml` has
`spoiler_checks: true` (F-DEN-7). It sees links only, not prose. A section
that mentions a later fact in plain text gets no warning, so prose needs
the same care (§9.5).

## 11. Outlines

An outline is a set of GFM tables, one row per beat. Rows stay short:
details live in an entity that the row links to.

```markdown
# Ember outline

## Act one

| row | chapter | pov | beat | notes |
|---|---|---|---|---|
| 37 | 1 | [[mara-voss]] | Mara wakes aboard the *Tessaly*. | |
| 38 | 2 | [[mara-voss]] | The team lands at [[kessan-reach]]. | [[landing]] |
| 39a | 2 | | The ambush. | |
| 39b | 2b | [[mara-voss\|Voss]] | Mara is hit. | |

## Act two

| row | beat | status | progress |
|---|---|---|---|
| 40 | Voss takes command. | candidate | outlined |
```

```abnf
row-id     = row-number *lower         ; 37, 69a, 69ab
row-number = %x31-39 *DIGIT            ; no leading zeros
```

**F-OUT-1.** `outline.md` is optional and is only read in a book folder.
Frontmatter is optional, needs no `id`, and its keys are kept. An outline
isn't an entity.

**F-OUT-2.** An **outline table** is a GFM table in `outline.md` whose
header has both a `row` and a `beat` column (F-OUT-11). Each body line of
the table is a **row**. Other tables, headings, lists, and prose in the file
are free-form notes and are ignored, with no diagnostic, except that a
table whose header has exactly one of `row` and `beat` is a [warning] ("did
you mean an outline table?"). Only tables at the top level of the file
count, as for headings (F-SEC-1): a table inside a block quote or a list
item is a free-form note.

**F-OUT-3.** An outline **section** is the part of the file under one
heading, up to the next heading of any level (or the part before the first
heading). Each section SHOULD hold at most one outline table. A second one
in the same section is a [warning], and its rows are still read.

**F-OUT-4.** Rows are ordered by number, then by letter suffix as an ASCII
string, with no suffix first: `69 < 69a < 69aa < 69b < 70`. Rows that
aren't in ascending order in the file are a [warning]. Each row is
compared with the row just before it, so one misplaced row gives one
warning. A row with the same id as the one before it is a repeat
(F-OUT-5), not an F-OUT-4 warning.

**F-OUT-5.** Row ids SHOULD be unique across all outlines of a series, or
within the outline of a standalone book. A repeated row id is a [warning]
on every outline involved (F-GEN-4). The rows stay indexed, and a `row`
field naming that id is ambiguous (F-FLD-7).

**F-OUT-6.** Row ids keep counting across a series: every row of a book
SHOULD come after every row of the book before it in series order. A book
whose lowest row id isn't after the highest row id of the nearest earlier
book in the series that has rows is a [warning]. Books with no rows are
skipped, and gaps between books are allowed.

**F-OUT-7.** To split a row, add letter suffixes (`69` becomes `69a` and
`69b`, or `69` stays and `69a` follows it). Writers MUST NOT renumber
existing rows, and SHOULD NOT reuse the id of a deleted row.

**F-OUT-8.** Links in cells resolve from the book's scope (§10). A
reference such as "row 69a" in prose is plain text in v0. Rows don't have
positions (F-POS-6). An entity with a `row` field naming a row (F-FLD-7) is
associated with it, so views can show the entity alongside the row.

**F-OUT-9.** Columns, in any order:

- `row`, REQUIRED: the row id. A row whose `row` cell is empty or isn't a
  `row-id` is a [warning], and the row is ignored.
- `beat`, REQUIRED: short text saying what happens. It MAY be empty for a
  placeholder.
- `chapter`: a chapter key of this book, mapping the row to that chapter.
  A chapter that doesn't exist is a [warning].
- `pov`: the point-of-view character, usually a link.
- `status`: the row's status (F-STAT-1), matched case-insensitively, so
  `Candidate` works. It defaults to `draft` (F-STAT-2). A value outside the
  vocabulary is a [warning], and the row is read as `draft`.
- `notes`: optional, conventionally a link to an entity that holds the
  row's details.
- Any other column is a **custom field** (`progress`, `tags`), with no
  diagnostic. Its values are strings, and Wolfden doesn't split them, so
  `action, ambush` stays one string in v0.

An empty cell means the field is unset for that row.

**F-OUT-10.** Rows with a `chapter` SHOULD follow chapter order: a row
mapped to an earlier chapter than a row above it is a [warning].

**F-OUT-11.** Header cells are matched case-insensitively after this
normalization: lowercase, replace each run of characters outside `a-z` and
`0-9` with `_`, then trim `_` from both ends. So `POV`, `Pov`, and ` pov `
are all `pov`, `Row #` is `row`, `POV / Narrator` is `pov_narrator`, and
` Draft-Status ` is `draft_status`. A header that normalizes to something
that isn't a valid field name (F-FLD-1), such as an empty string, is a
[warning], and the column is ignored. Two columns that normalize to the
same key are a [warning], and the later one is ignored. The header text is
kept as written.

**F-OUT-12.** A GFM table cell is one line. Long notes, bullets, and other
detail belong in a separate entity, linked from the beat text or from the
`notes` column. Links work in any cell. A `|` inside a cell, including the
one in `[[target|label]]`, MUST be escaped as `\|`, as GFM requires. Links
are recognized in each cell's text after GFM removes that escape, so
`[[mara-voss\|Voss]]` is the link `[[mara-voss|Voss]]`. An unescaped pipe
splits the cell and shifts every later column, so a body row with more
cells than the header is a [warning], and so is a cell with a `[[` that
has no matching `]]`.

## 12. Preservation

**F-RT-1.** Writers MUST keep everything they don't mean to change: unknown
frontmatter keys, YAML comments, key order, quoting style, unknown and
custom sections, HTML comments, whitespace, line endings (F-ENC-3), the
final newline (F-ENC-4), and any byte order mark (F-ENC-2).

**F-RT-2.** An edit to one field, entry, or section changes only the bytes
of that field, entry, or section, except for adding a missing `id`
(F-ID-6).

**F-RT-3.** Reading a file and writing it back unchanged MUST produce the
same bytes (#18 tests this).

**F-RT-4.** A writer that adds a frontmatter key puts it after the existing
keys, unless the type's template gives an order (types.md F-TPL-7). A
missing `id` goes first (F-ID-6).

**F-RT-5.** When the app or an MCP tool renames or moves something, it MUST
rewrite, in the same change, every reference that depends on the old name:

- Renaming a book or chapter key: every position that names it, in
  frontmatter and in wolfden comments, and every `order` or `books` entry.
- Renaming an entity file: every link that matched it by slug.
- Changing an entity's `name` or one of its `aliases`: every link that
  matched the old text. The writer MAY instead offer to keep the old text
  as an alias, so those links keep resolving.
- Renaming a section: every link to it by heading
  (`[[entity#Old name]]`, matched as in F-LINK-4), in any file, so it
  names the new heading.
- Moving an entity to another scope: any link that would no longer resolve
  to it (F-LINK-7) is rewritten to its id or another unique form. An
  entity without an `id` MUST get one in the same change (F-ID-6).
  Positions resolve across the universe (F-POS-4), so a move changes no
  `from` or `until`, but the default of a missing entity `from` depends
  on scope (F-ENT-6). So an entity without `from` gets the entry point it
  had before the move written as an explicit `from`, so every canon view
  stays the same. That's the bare key of the first book at or after that
  point (F-POS-7), which orders the same for every position. Nothing is
  written when the new scope starts at that same point. If the entity
  enters canon outside the new scope (a series-level character the reader
  first meets in Book 1, moved into Book 2), the writer chooses: keep the
  old entry point as `from`, accepting the F-ENT-6 warning, or enter
  canon at the start of the new scope. The app, or a CLI at a terminal,
  warns and asks. A non-interactive writer (an MCP tool, a script)
  refuses, with an error that explains the entity is already in canon
  before the new scope and names both ways forward. The same goes when
  no position can name the old entry point (the start of a series with
  no books, with no book after it), except that only entering at the new
  scope is possible. (Maintainer decision, 2026-10-24, #89.) What stays
  the same is every canon view, not every link: a link that was
  ambiguous and now resolves (the moved entity is nearer to it), and a
  `row` id that now resolves among other outlines (F-FLD-7), are
  reported to the writer, not prevented. The moved entity's own links
  resolve from its new scope too, so one that would reach a different
  entity, or become ambiguous, is rewritten in the same change to the id
  of the entity it reaches now, keeping what it shows. A writer MAY
  offer to leave them as written instead, so they follow the new
  scope. Broken and ambiguous links in it stay as written. (Maintainer
  decision on #164.)

The core library's edits each change one file (`WithSectionName` renames
a heading, for example). The den-wide rewrite belongs to the app's and
the MCP server's write tools (#21), which make all of it one change.

A rename made outside the app, by hand or in another tool, is a refactor
Wolfden can't see. Positions that name the old key become F-POS-4
warnings, and slug links to the old name, or heading links to a renamed
section, become broken links (F-LINK-8). Links by id, name, or alias keep
working. That is the sense in which files can be renamed by hand "without
breaking links" (design.md §3).

**F-RT-6.** A writer that removes the last item from a block list, or the
last key from a block mapping, leaves the key with no value (`aliases:`)
rather than removing the key or writing `[]` or `{}`. That's the smallest
change, and it reads as null: unset, the same as leaving the key out
(F-FLD-4, F-ENT-4). A flow list or mapping keeps its brackets and becomes
`[]` or `{}`.

The exception is a chapter list field, where null and an empty list mean
different things: `[]` is "none here" and null inherits (types.md
F-CHAP-5). That's a list-valued field under a chapter's `fields:` in
`book.md` (F-BOOK-2), or in the `chapter_defaults` of any container
(`universe.md`, `series.md`, or `book.md`). There, removing the last item
always leaves `[]`, whether the list was written flow or block, so it
keeps saying "none" and inheritance stops, rather than falling back to
the default or the container above. Going back to inheriting is a
separate edit: clearing the field. (Maintainer decisions, 2026-10-23.)

Comments go the same way as with any other removal: a comment on the
removed key's or item's last line, and comment lines right after it
indented under it, go with it. In a mapping or list that still has
other keys or items, comment lines above the removed one stay. When the
last one goes, every comment line inside the collection is in one that's
now empty, so it can only have been about what was removed, and it goes
too, with the blank lines among them: the lines between the parent's
line (the key, or a list item's dash) and the removed key or item, and
the comment lines after it that are still indented past the parent's
key. So clearing a mapping key by key ends the same in any order. A
comment on the parent's own line stays, and so does a comment after the
removed key or item at or left of the parent's key, which is about what
comes next. A comment above the removed key or item that's at or left
of the parent's key, and left of the removed one, might be a note on the
parent, so the writer refuses the edit rather than guess (#157). A
comment line there may be a commented-out value (`# pov: "[[wren-calder]]"`),
so the MCP write tools say how many comment lines went. The den's
history keeps the old text from the last checkpoint (a commit) before
the edit. A new key or item written under an empty key later goes
after any comment lines already indented under that key.

**F-RT-7.** A writer that reorders a list moves each item in place,
with the comments that belong to it, rather than removing it and adding
it again (#197). In a block list, an item's own lines are:

- its dash line and the rest of its value, with a comment on any of them
  (`- "[[mina-harker]]"  # main`);
- comment lines right after it indented deeper than its dash, which are
  written under it, as a removal takes them; and
- comment lines right above its dash, with no blank line between, at or
  right of its dash and not indented under the item before: a heading
  for it, as a new item goes after the item before and the comments
  under that one, not between a heading and its item. Comment lines
  above the first item are the exception: they may head the whole list
  (`# in order of appearance`), so they stay at the top.

Each item's lines are a slot, and the items move between the slots, as
sections do: blank lines, and comment lines that aren't an item's (above
the first item, left of the dash, or above a blank line), stay where
they are. So a commented-out item after the last one
(`# - "[[lucy-westenra]]"` at the dash's indent) stays at the end of the
list, and an item moved to the top goes under the list's heading. Its
own heading comes with it and then reads as part of the list's, so it
stays at the top if the item moves down again. A move that changes
which item is first says so: the MCP write tools, and the web page's
notice after saving, name the comment lines that stayed at the top and
those that now head the list.

A list of dated entries (§7.3) is never reordered (F-TMP-7): of two
entries with the same `from`, the later one in the file holds (F-TMP-5),
so a move could change what holds, pinned canon included. A writer
refuses such a move.

In a flow list, the item's text moves, with a comment on its line if it
has a line of its own, and the commas stay in their places, so a list whose last item has no comma
still has none. A comment anywhere else between the brackets, such as a
line of its own between items, can't be placed for sure, so the move is
refused (SC-RT-14).

Changing one item in place keeps the comments on and above it too: only
the value's characters change (F-RT-2), so `- "[[mina-harker]]"  # main`
becomes `- "[[lucy-westenra]]"  # main`. An item written over several
lines (a block mapping) is replaced whole, so comment lines inside it go
with the old value, and the MCP write tools say which. An item is one
value: a list where an item goes is refused. Removing an item and adding
another in its place, by contrast, writes the new one afresh without
the old one's comment; the MCP write tools say when a comment went.

## 13. Appendix: a minimal den

A complete den with one universe, one series, one book, and two entities.
It uses only the core pack and loads with no errors. The core character
type doesn't define `rank` (types.md §9.1), so `rank` gets an F-FLD-2
warning. types.md §10 shows a den-local pack that adds it.

```text
starfall-den/
  den.yaml
  universes/starfall/
    universe.md
    entities/locations/kessan-reach.md
    series/long-dark/
      series.md
      entities/characters/mara-voss.md
      books/ember/
        book.md
        outline.md
```

`den.yaml`:

```yaml
format: 1
authors:
  wren-calder: { name: Wren Calder }
```

`universes/starfall/universe.md`:

```markdown
---
id: 01JAM9S346Q3D25VT4F5V37E3S
name: Starfall
authors: [wren-calder]
---

A frontier war at the edge of settled space.
```

`universes/starfall/series/long-dark/series.md`:

```markdown
---
id: 01JA3E28JT97KB6CQ643DZVMXX
name: The Long Dark
books: [ember]
---
```

`universes/starfall/series/long-dark/books/ember/book.md`:

```markdown
---
id: 01JAQKFBF5KZNWJ47TAN9ZT24M
name: Ember
chapters: [prologue, 1, 2, 3, 4]
---
```

`universes/starfall/entities/locations/kessan-reach.md`:

```markdown
---
id: 01JADJ8FSSZ5AWSH8VHTPRE95B
type: location
name: Kessan Reach
aliases: [the Reach]
status: canon
---

## Overview
A contested belt of mining stations at the edge of settled space.
```

`universes/starfall/series/long-dark/entities/characters/mara-voss.md`:

```markdown
---
id: 01JA7Q8M4XK3V9R2D5W8HN6TQB
type: character
name: Mara Voss
aliases: [Voss]
status: canon
from: "ember:1"
fields:
  rank:
    - { value: Lieutenant, from: "ember:1" }
    - { value: Commander, from: "ember:4" }
---

# Mara Voss

## Overview
Executive officer of the *Tessaly*.

## Shoulder injury
<!-- wolfden: from=ember:3 -->
Takes a flechette round in the ambush at [[kessan-reach]]. The arm never
fully recovers.
```

`universes/starfall/series/long-dark/books/ember/outline.md`:

```markdown
| row | chapter | beat |
|---|---|---|
| 1 | 1 | A distress call from the edge of [[the Reach]]. |
| 2 | 2 | [[Mara Voss]] leads the away team. |
| 3 | 3 | The ambush. |
| 4 | 4 | Voss takes command. |
```

As of `ember:2`, Mara Voss is a lieutenant with an Overview and no Shoulder
injury. As of `ember:4`, she's a commander and the injury section is in
effect. As of `ember:prologue`, she isn't in canon yet (F-ENT-6).
