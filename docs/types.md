# Wolfden Types and Packs Specification

**Format version:** 1. **Status:** draft (#6), open to revision until M1
code depends on it.

This document is the normative definition of packs and types: where pack
files live, the shape of a type definition, the field types and how a
value must look for each, standard sections, templates, chapter fields,
how several packs combine, and the core pack itself. It is part of format
1 and extends [`format.md`](format.md), whose conventions (§1), files and
encoding (§2), and YAML rules (§3) apply here unchanged. The reasons
behind packs are in [`design.md`](design.md) §4.

The core pack's files are in [`packs/core/`](../packs/core/) in this
repository.

## Contents

1. [Conventions](#1-conventions)
2. [Packs](#2-packs)
3. [Type definitions](#3-type-definitions)
4. [Extensions and combining packs](#4-extensions-and-combining-packs)
5. [Fields](#5-fields)
6. [Standard sections](#6-standard-sections)
7. [Templates](#7-templates)
8. [Chapter fields](#8-chapter-fields)
9. [The core pack](#9-the-core-pack)
10. [Appendix: a den-local pack](#10-appendix-a-den-local-pack)

## 1. Conventions

Requirement words, rule ids, severities, and notation are as in
format.md §1. Rules in this document use the areas `F-PACK` (packs and
combining), `F-TYPE` (type definitions and sections), `F-FT` (fields and
field types), `F-TPL` (templates), `F-CHAP` (chapter fields), and `F-CORE`
(the core pack).

**Pack files** are the files in a pack folder (§2.1): `pack.yaml`,
`chapter.yaml`, type files, extension files, and templates. They are den
configuration, like `den.yaml`, and are read before any note.

**F-PACK-1.** Pack files follow format.md §2 and §3: UTF-8 (F-ENC-1), the
line-ending rules (F-ENC-3), and the YAML rules (F-YAML-1 to F-YAML-4).
`pack.yaml`, `chapter.yaml`, type files, and extension files are whole
YAML documents, not frontmatter. Templates are markdown files whose
frontmatter is optional (F-TPL-3).

**F-PACK-2.** An [error] in `pack.yaml` excludes the whole pack: none of
its files load. An [error] in any other pack file excludes that file only.
The den still loads either way. An entity whose type is excluded this way
is indexed untyped, with the F-ENT-2 warning. (A `den.yaml` entry that
names no pack at all is still the F-DEN-4 error, and the den can't be
loaded.)

Some problems inside a pack file drop only the item involved, with a
[warning]: one field definition (F-FT-2), one section item (F-TYPE-9), or
a `template` value (F-TPL-1). The rest of the file still loads.

So F-GEN-4 holds: every rule in this document that checks a note against
its type is a [warning]. Pack files only make *their own* content an
error, and a problem that spans pack files (a type defined twice, an
extension of a type that isn't enabled) is a [warning], with F-PACK-11
saying which definition is used.

## 2. Packs

### 2.1 Layout

```text
<pack>/
  pack.yaml                 required: the manifest (§2.2)
  chapter.yaml              optional: chapter fields (§8)
  types/<type>.yaml         type definitions (§3)
  extensions/<type>.yaml    extensions of types from any pack (§4)
  templates/<file>.md       templates (§7)
```

**F-PACK-3.** A **pack** is a folder that holds a `pack.yaml`. The folder
name is the **pack key** (F-KEY-1). Other files and folders in a pack are
ignored, with no diagnostic, and names starting with `.` are ignored as
everywhere (F-LAY-2). The one exception is a `.yml` file where this
document expects a `.yaml` one (`pack.yml`, `chapter.yml`, or a `.yml`
file in `types/` or `extensions/`): it is a [warning] ("did you mean
ship.yaml?"), and it is not read.

**F-PACK-4.** **Built-in** packs ship with Wolfden. In v0 the only
built-in pack is `core` (§9). Its source is the `packs/core/` folder of the
Wolfden repository, and it has the same layout as any other pack. Later
milestones add other built-in packs (space opera and epic fantasy, M5)
the same way.

**F-PACK-5.** A **den-local** pack is a folder directly in the den's
`packs/` folder (format.md §5.1) that holds a `pack.yaml`. A folder there
without a `pack.yaml` isn't a pack, and is a [warning], which names any
`pack.yml` it finds. If `den.yaml` lists its key and no built-in pack has
that key, that entry names no pack, which is the F-DEN-4 error. A folder
whose name isn't a valid key (`packs/My Stuff/`) is a [warning] and is
ignored, rather than the F-KEY-1 error, so a stray folder never stops a
den from loading. Files directly in `packs/` are ignored.

### 2.2 `pack.yaml`

```yaml
# packs/core/pack.yaml
name: Core
description: >-
  The types every den has: character, location, faction, item, event,
  thread, idea, and note.
```

**F-PACK-6.** The top level is a mapping with:

- `name`, REQUIRED: a non-empty display name. [error]
- `description`, optional: a string. [error] for any other shape.
- `version`, optional: a string, the pack's own release version, shown to
  users and otherwise ignored. [error] for any other shape.

Other keys are kept and ignored, with no diagnostic, as in `den.yaml`
(F-DEN-5).

### 2.3 Enabling packs

**F-PACK-7.** The **enabled packs** are `core` and the packs in
`den.yaml`'s `packs` list (F-DEN-4). Listing `core` is allowed and changes
nothing, and a repeated entry is the F-DEN-4 error. The **pack order** is
`core` first, then the listed built-in packs, then the listed den-local
packs, each group in the order `den.yaml` lists them. A den-local pack
that shadows a built-in one (F-PACK-10) is a den-local pack here, except
that one shadowing `core` comes first in `core`'s place.

Pack order is the order in which packs add fields and sections to a type
(F-TYPE-8) and chapter fields (F-CHAP-2), so a den's own additions come
after the built-in ones. It also settles a clash between two packs of the
same kind (F-PACK-11).

**F-PACK-8.** A den-local pack that `den.yaml` doesn't list isn't enabled,
and is a [warning] ("pack 'starfall' is in packs/ but not enabled in
den.yaml"). Its types are unknown types until it is listed. A den-local
pack that shadows `core` is the exception: it is always enabled, as `core`
is.

**F-PACK-9.** The **enabled types** (F-GEN-4, F-ENT-2) are the types
defined by the enabled packs (§3), each combined with its extensions from
the enabled packs (§4).

### 2.4 When packs clash

A den's own definitions win over Wolfden's built-in ones, so an upgrade
never overrides a den's own definitions (design.md §4).

Built-in packs change only by adding: new types, fields, sections, and
chapter fields. Changing an existing built-in field's type, renaming a
section, or adding or changing a `default` is a breaking change (#56).
Template prompts and headings may change in any release, since they only
affect entities created afterward.

An addition can still give meaning to something a den left undefined: a
new core field checks the values a den already wrote under that name, and
a new core type picks up entities that named it. A new standard section
makes repeated custom sections with that name start handing over
(format.md F-SEC-8), so the earlier ones drop out of later views. A
den-local extension that adds a section name does the same. A writer who
relies on a custom field should define it in a den-local pack (§10), so
that its definition is the den's own and wins over any later built-in
one.

**F-PACK-10.** A den-local pack whose key is the key of a built-in pack
**shadows** it. A `den.yaml` entry with that key names the den-local pack,
the built-in pack is ignored, and the den-local `pack.yaml` gets a
[warning] ("packs/core shadows the built-in pack 'core', so Wolfden's
updates to core won't reach this den; rename the folder to make it an
ordinary pack"). Since `core` is always enabled, a den-local
`packs/core/` replaces the built-in core pack whether or not `den.yaml`
lists `core`. A den that shadows a built-in pack no longer gets that
pack's changes when Wolfden is upgraded.

A den-local pack that is excluded by an error in its `pack.yaml`
(F-PACK-2) still shadows the built-in pack: the den then has no pack with
that key until the error is fixed, and the built-in pack doesn't come
back. An error in `pack.yaml` never switches a den to definitions it
chose not to use.

**F-PACK-11.** Two enabled packs **clash** when both define the same type
key (F-TYPE-4), the same `plural` (F-TYPE-5), the same field or section
of a type (F-TYPE-8), an extension template for the same type (F-TYPE-8),
or the same chapter field (F-CHAP-2). One definition is used and the other
is ignored:

1. If exactly one of the two comes from a den-local pack, the den-local
   one is used.
2. Otherwise, the one that comes first is used: for a field or section,
   the one already in the combined definition (F-TYPE-8), and for anything
   else, the one from the pack earlier in pack order (F-PACK-7).

Each clash is a [warning] that names both packs and says which definition
is used. It goes on the file whose definition is ignored, or, when that
file is built in, on the den-local file whose definition is used.

## 3. Type definitions

```yaml
# packs/core/types/thread.yaml, shortened
name: Thread
plural: threads
description: >-
  A plot thread: a setup, a mystery, or a promise to the reader that the
  story has to pay off.
fields:
  planted:  { type: position, list: true }
  paid_off: position
  characters: { type: ref, to: character, list: true }
sections:
  - Overview
  - name: Setup
    description: How and where it's planted.
  - Payoff
  - Open questions
template: thread.md
```

**F-TYPE-1.** A **type file** is a `.yaml` file directly in a pack's
`types/` folder. The **type key** is the file name without `.yaml`, and
MUST be a valid key (F-KEY-1). [error] The key is what entities write in
`type:` (F-ENT-2).

**F-TYPE-2.** The type keys `universe`, `series`, `book`, `chapter`,
`scene`, `outline`, `den`, `author`, and `pack` are reserved, for
containers and for later use (design.md §2, #45, and the scene form of
positions, F-POS-2). A type file with one of them is an [error].

The reserved names in this rule, F-FT-1, and F-CHAP-1 are fixed for this
format version. Reserving another name would make an existing den-local
definition stop working, which an additive change (F-DEN-1) must never do,
so a new reservation needs a new format version.

**F-TYPE-3.** The top level is a mapping with:

- `name`, REQUIRED: the display name, a non-empty string ("Character").
- `plural`, REQUIRED: the type's **folder name**, a key (F-KEY-1), the
  first folder under `entities/` for entities of this type (F-LAY-6). It
  is a key, not a display string, so `characters` and `star-systems`, not
  `Star Systems`.
- `description`, optional: a string, shown in the app and given to AI
  agents through MCP.
- `fields`, optional: a mapping of field definitions (§5).
- `sections`, optional: a list of standard sections (§6).
- `template`, optional: the type's template (§7).

A missing or empty `name`, a missing or invalid `plural`, or a `name`,
`description`, `fields`, or `sections` with the wrong shape is an [error].
A bad `template` value is a [warning] instead (F-TPL-1). Any other key is
a [warning] (usually a typo), and is kept.

**F-TYPE-4.** If more than one enabled pack defines the same type key in
`types/`, F-PACK-11 decides which definition is used. The other is
ignored, along with its template, and the clash is a [warning] ("type
'ship' is defined by both pack 'starfall' and the built-in pack
'space-opera'; using 'starfall'"). Extensions of that key extend the
definition that is used. A den can replace a built-in type this way, but
no pack can remove a type another pack defines.

**F-TYPE-5.** Two enabled types MUST NOT have the same `plural`. If they
do, F-PACK-11 decides which type is used, and the other type is ignored,
with the clash [warning], as in F-TYPE-4. Two types in the same pack are
settled by type key, in ASCII order: the earlier key wins.

## 4. Extensions and combining packs

A den, or another pack, can add to a type it doesn't own: an extra field,
an extra section, or its own template.

```yaml
# packs/starfall/extensions/character.yaml
fields:
  rank: { type: string, temporal: true }
sections:
  - Service record
```

**F-TYPE-6.** An **extension file** is a `.yaml` file directly in a pack's
`extensions/` folder. Its file name without `.yaml` is the key of the type
it extends (F-KEY-1, [error]). The top level is a mapping with:

- `fields`, optional: field definitions (§5) to add.
- `sections`, optional: standard sections (§6) to add.
- `template`, optional: a template (§7) to use instead of the type's own.

`name`, `plural`, or `description` in an extension is an [error]: an
extension can't rename a type or move its folder. A `fields` or
`sections` with the wrong shape is an [error], and a bad `template` value
is a [warning] (F-TPL-1). Any other key is a [warning], and is kept.

**F-TYPE-7.** An extension of a type that isn't enabled is a [warning]
and is ignored. (That's normal for an extension a den keeps for a pack it
has turned off.)

**F-TYPE-8.** Extensions add. A type's **combined definition** starts as
its own definition, the one F-TYPE-4 uses. Then each enabled extension of
it is applied, in pack order (F-PACK-7):

- Each extension's fields are added after the fields already there, in
  file order. A field name that is already defined, by the type or by an
  earlier extension, is a clash (F-PACK-11). If the new definition wins
  (it's den-local and the one already there is built in), the built-in
  definition is dropped from the field order and the new one is added at
  its own place. Otherwise the new definition is ignored. Either way it's
  the clash [warning], and the rest of the extension still applies.
- Each extension's sections are added after the sections already there,
  in file order. A section name already in the list (matched as in
  F-SEC-3) is a clash, settled the same way.
- Of the extensions that set `template`, the one F-PACK-11 picks replaces
  the type's template. Each other one is the clash [warning], and its
  template is ignored. A built-in extension's template never replaces the
  template of a den-local type: that is a clash too, and the den-local
  type's own template is used.

Apart from a den-local definition replacing a built-in one (F-PACK-11),
an extension can't change or remove a field or section that is already
defined. A den that wants a field to behave differently defines a new
field under another name. Fields and sections it doesn't use cost
nothing.

Extension sections come after the type's own, so an extension of a core
type adds its sections after `Open questions`. A den that wants another
order can give the type its own template (§7).

A pack MAY extend a type it defines itself, though there is no reason to.

## 5. Fields

### 5.1 Field definitions

```yaml
fields:
  born: date                                  # shorthand for { type: date }
  factions:
    type: ref
    to: faction
    list: true
    temporal: true
    description: The groups they belong to.
```

**F-FT-1.** `fields` is a mapping from field name to **field definition**.
A field name MUST match F-FLD-1. The names `row` and `placeholder` are
reserved for the universal fields (§5.5), and no pack may define them
(this list is fixed for format 1, F-TYPE-2). Breaking either rule is a
[warning], and that definition is ignored (F-FT-2).

**F-FT-2.** A field definition is either the name of a field type (a
string, shorthand for a mapping with only `type`) or a mapping with:

| Key | Meaning | Default |
|---|---|---|
| `type` | REQUIRED. A field type from §5.2. | |
| `list` | `true` makes the value a list of that type (§5.3). | `false` |
| `temporal` | `true` lets the field have temporal entries (F-FT-13). | `false` |
| `required` | `true` makes a missing value a warning (F-FT-14). | `false` |
| `default` | A value views show when the field has none (F-FT-15). | none |
| `values` | For `enum` only: the allowed values (F-FT-8). | |
| `to` | For `ref` only: the type or types it links to (F-FT-9). | any type |
| `description` | A string, shown in the app and given to AI agents. | none |

A field definition that is broken is a [warning], and that one definition
is ignored, so the field is undefined (F-FLD-2) until it is fixed. The rest
of the file still loads. A definition is broken if its field type is
missing or unknown, if `list`, `temporal`, or `required` isn't a YAML
boolean, if a key has the wrong shape, or if F-FT-1 or F-FT-8 says so. A
null `list`, `temporal`, or `required` (the key with no value) is the
same as leaving it out, so it is `false` (F-FLD-4), not broken.
`values` on a type other than `enum`, `to` on a type other than `ref`,
or a `description` that isn't a string is a [warning], and only that key
is ignored. Any other key is a [warning], and is kept.

**F-FT-3.** The order of a type's fields is the order of its `fields`
mapping, followed by any fields its extensions add (F-TYPE-8). That order
is the **field order** (F-TPL-7).

### 5.2 Field types

| Type | Value | Example |
|---|---|---|
| `string` | One line of text. | `call_sign: Kestrel` |
| `text` | Text, which can run over several lines. | `motto: \|` then lines |
| `number` | A YAML integer or float. | `crew: 140` |
| `bool` | `true` or `false`. | `placeholder: true` |
| `date` | An in-world date: free text, a sortable key, or both. | `born: "2291"` |
| `enum` | One of a fixed list of keys. | `tense: past` |
| `ref` | One link to an entity. | `leader: "[[mara-voss]]"` |
| `position` | A story position (format.md §8). | `paid_off: "ash:20"` |

This section says how a value must look for each type. This is the check
F-FLD-3 refers to. A value that doesn't match its field type is a
[warning], whatever the reason, and the value is kept and indexed as
written. Values are checked as they appear in the file: a plain value
directly, and in a temporal list or single entry (F-TMP-1), each entry's
`value`. A null value (F-FLD-4), including `value: null`, matches every
type.

**F-FT-4. `string`.** Any YAML scalar, read as its text as written
(F-YAML-5), so `call_sign: 7` and `call_sign: "7"` are the same string. A
string containing a line break is a [warning] ("use a text field"). A list
or mapping is a [warning].

**F-FT-5. `text`.** Any YAML scalar, read as its text as written, over
any number of lines. A YAML block scalar (`|` or `>`) is the usual way to
write one. A list or mapping is a [warning].

**F-FT-6. `number`.** A YAML integer or float under the core schema
(F-YAML-1). A quoted number (`"140"`), `.inf`, `.nan`, or anything else is
a [warning].

**F-FT-7. `bool`.** A YAML boolean under the core schema (`true`,
`false`). Anything else, including `yes` and `"true"`, is a [warning].

**F-FT-8. `enum`.** The definition MUST have `values`: a non-empty list of
keys (F-KEY-1) with no repeats. An item that is a key once lowercased
(`Ally`) is a [warning], and is read lowercased. An item that still isn't
a key (`third limited`), or that repeats an earlier item once lowercased,
is a [warning], and that item is dropped. If `values` is missing or no
items are left, the definition is broken: a [warning], and the definition
is ignored (F-FT-2). A value is a scalar whose text, as written (F-YAML-5),
matches one of `values` after Unicode default case folding (as in
F-LINK-4), so `Past` and `PAST` are both `past`. Anything else is a
[warning], which SHOULD suggest a close defined key, such as
`third-limited` for `third_limited`. A writer
that writes an enum value, including one it rewrites in a field it is
editing, writes the defined key (`past`). It MUST NOT change a value in a
field it isn't editing (F-RT-2).

**F-FT-9. `ref`.** A string that holds exactly one link and nothing else,
after trimming (F-LINK-11), such as `"[[mara-voss]]"` or
`"[[Mara Voss|Voss]]"`. It has to be quoted, or YAML reads it as a list
inside a list (F-FLD-6). A string that isn't a single link is a
[warning]. The link resolves and is checked like any other (format.md
§10). `to` is optional, and is a type key or a list of type keys. A link
that resolves to an entity whose type isn't `to`, or isn't one of the
types in `to`, is the F-LINK-11 warning. Without `to`, a ref links to any
type. A `to` naming a type that isn't enabled is a [warning] on the type
file, and the type check skips that name.

**F-FT-10. `position`.** A string matching `position` (F-POS-1), such as
`"ember:14"` or a bare `"ember"` (F-POS-7). A value in the scene form or
the row form is a [warning] with the F-POS-2 or F-POS-6 message, and any
other string that isn't a position is a [warning] with the F-POS-3
message. Unlike a `from` or `until`, a position field's value is data, so
a bad one doesn't make the file invalid. A position that doesn't resolve
is the F-POS-4 warning. The value stays indexed and is shown as written,
but anything that needs its place in story order, such as a thread's
state on the page (§9.1) or F-CORE-2, skips it. A value is never read as
some other position than the one written.

**F-FT-11. `date`.** An in-world date is either a string or a mapping:

```yaml
fields:
  born: "2291"                                # sorts as 2291
  when: "2291-05-14"                          # sorts as 2291, 5, 14
  ends: "2291-05-14 06:40"                    # sorts as 2291, 5, 14, 6, 40
  died: { text: "late spring, the year the Reach fell", sort: [2291, 5] }
  sworn: "the third winter of the siege"      # free text, no sort key
```

```abnf
date-key   = numbers / numbers-3 time
numbers    = ["-"] 1*DIGIT *("-" 1*DIGIT)
numbers-3  = ["-"] 1*DIGIT 2("-" 1*DIGIT)
time       = (SP / "T") 1*2DIGIT ":" 2DIGIT [":" 2DIGIT]
```

- A **string** (any scalar, read as its text as written) is the date's
  text. If the whole text matches `date-key`, it also gives the **sort
  key**: its numbers in order, the first one negative if the text starts
  with `-`. So `2291-05` sorts as 2291, 5, and `-450` as -450. A time of
  day can follow a date with three numbers, after a space or a `T`, with
  seconds optional: `2291-05-14 06:40` sorts as 2291, 5, 14, 6, 40, after
  `2291-05-14` alone. Under YAML 1.2's core schema, an unquoted
  `2291-05-14` or `2291-05-14 06:40` is a string, not a timestamp.
- A **mapping** has `text`, a string, and `sort`, an integer or a list of
  integers. Either can be left out, but not both. Use it when the text is
  fuzzy or partial ("late spring", "before the war") and the date still
  needs a place on a timeline. `text` is read as written (`text: 2291` is
  the text "2291") and never gives a sort key: only `sort` does. The key
  `calendar` is reserved for custom
  calendars (#37): until they arrive, it is a [warning] ("custom calendars
  aren't supported yet"), and it is kept and otherwise ignored. Any other
  key is a [warning], and is kept. These two warnings are advisory: the
  date still matches the `date` type, and its `text` and `sort` are read
  as usual, so a chapter field's value (F-CHAP-3) or a `default`
  (F-FT-15) with them isn't ignored.

A value of any other shape, a `sort` that isn't an integer or a non-empty
list of integers, or a mapping with neither `text` nor `sort` is a
[warning].

Sort keys compare number by number. A key that is a prefix of another
sorts first, so `2291` < `2291, 5` < `2291, 5, 14` < `2292`. The numbers
mean whatever the writer's calendar needs: year, month, day, or age, year,
season. Dates without a sort key are listed after dated ones, in the view's
usual order (F-VIEW-3).

### 5.3 Lists

```yaml
fields:
  factions: ["[[tessaly-crew]]", "[[reach-compact]]"]
  planted: "ember:3"                          # one item
```

**F-FT-12.** A field with `list: true` takes a YAML sequence whose items
each match the field type, checked one by one, so one bad item is one
[warning]. A value that isn't a sequence is read as a one-item list, with
no diagnostic. An empty list means the field is unset, like a null
(F-FLD-4). A null item is a [warning]. There are no lists of lists.

A list field can be temporal (F-FT-13). Each entry's `value` is then the
whole list as of that entry's `from`, and the next entry replaces it:

```yaml
fields:
  factions:
    - { value: ["[[tessaly-crew]]"], from: "ember:1" }
    - { value: ["[[tessaly-crew]]", "[[reach-compact]]"], from: "ash:12" }
```

F-TMP-1 tells the shapes apart: a list whose items are all entries is a
temporal list, a list with no entries is a plain list value, and a list
that mixes the two is the F-TMP-1 error.

### 5.4 Temporal, required, and defaults

**F-FT-13.** `temporal: true` marks a field whose value can change over the
story, so it may have a temporal list or an entry with `until` (format.md
§7.3). On a field without it, those are the F-TMP-8 warning. A single
entry without `until` is fine on any field (F-TMP-8), so any fact can be
pinned, given its own status, or given a `from` where the reader learns
it.

**F-FT-14.** `required: true` means the field SHOULD have a value. A
required field is **missing** if the entity's `fields` has no value for it
that isn't null: no plain value, and no entry with a non-null `value`.
A missing required field is the F-FLD-3 warning. This depends only on the
file, not on positions or status. A field with both `required` and
`default` is a [warning] on the type file, since a default never makes a
field present.

**F-FT-15.** `default` is a value that matches the field's type and
`list` (§5.2, §5.3). A `ref` or `position` field has no default. A
`default` that breaks either rule is a [warning], and the default is
ignored. An advisory warning, such as a date's `calendar` (F-FT-11), is
still reported on the type file, but doesn't break the rule, so the
default is kept. Where an entity has no value for the field at a position (it's
missing, unset, or between entries), views and the index show the
default, marked as a default. A default isn't a fact: it has no position,
status, or pin of its own, and pinned canon (F-VIEW-2) leaves it out.
Writers MUST NOT write a default into a file as a side effect of another
edit. A template MAY write it (§7).

### 5.5 Universal fields

Two fields are defined on every type, so they never trigger F-FLD-2 and no
pack may define them (F-FT-1). Both are read on untyped entities too
(F-ENT-2).

**F-FT-16. `row`** ties an entity to outline rows. It is defined in
format.md (F-FLD-7) and is not temporal. In the field order (F-TPL-7) it
comes after the type's own fields.

**F-FT-17. `placeholder`** is a `bool`, not temporal, defaulting to
`false`. `placeholder: true` says the entity's `name` is a working name,
still waiting on a name check. It says nothing about `aliases`, `status`,
or canon: a placeholder-named character can be `canon`. A value that isn't
a boolean is a [warning] (F-FT-7).

- Views SHOULD mark a placeholder name wherever they show it, and SHOULD
  offer a list of the entities in a scope whose names are still
  placeholders.
- The MCP server SHOULD tell agents when a name is a placeholder, so they
  don't build on it as final.
- When a writer changes the `name` of an entity with `placeholder: true`
  (F-RT-5), it MAY offer to remove the flag in the same change.

In the field order it comes after `row`.

```markdown
---
id: 01JB2C8Y0HR4M6N3KQ5TVW7XZA
type: character
name: Halden Pike
status: candidate
fields:
  role: dockmaster
  placeholder: true
---
```

## 6. Standard sections

**F-TYPE-9.** `sections` is a list of the type's **standard sections**, in
order. Each item is a section name (a non-empty string) or a mapping with:

- `name`, REQUIRED: the section name.
- `required`, optional: `true` makes a missing section a warning
  (F-TYPE-11). Defaults to `false`.
- `description`, optional: what the section is for, shown in the app and
  given to AI agents.

An item with the wrong shape is a [warning], and that item is ignored. So
is an item whose name matches an earlier item's (F-SEC-3). Any other key
in a mapping is a [warning], and is kept.

**F-TYPE-10.** A type's standard sections are the sections "the type
defines" in F-SEC-3, F-SEC-4, and F-SEC-8. They match an entity's level-2
sections by name, as in F-SEC-3, and a later one with the same name takes
over from an earlier one (F-SEC-8). Every other section is a custom
section and is kept like any other (format.md §12).

**F-TYPE-11.** A **required** section is present if the entity has a
level-2 section with that name, whatever its status, position, or content.
A required section that isn't present is the F-SEC-5 warning. This depends
only on the file.

**F-TYPE-12.** The list order is the **section order** that templates
(§7) and writers (F-TPL-8) follow. It has no other meaning: sections in an
entity can be in any order, with no diagnostic.

## 7. Templates

A template is what a new entity of a type looks like when the app, the
CLI, or an MCP tool creates one. This is `packs/core/templates/thread.md`,
shortened:

```markdown
---
type: thread
status: draft
fields:
  planted: []
  paid_off:
---

# {{name}}

## Overview
<!-- What is the thread, in a sentence or two? -->

## Open questions
<!-- wolfden: status=undecided -->
<!-- What else isn't decided yet? Bank the options under each question. -->
```

### 7.1 Template files

**F-TPL-1.** A type's or extension's `template` is the file name of a `.md`
file directly in the same pack's `templates/` folder. A value that isn't a
string, contains a `/` or `\`, or doesn't end in `.md`, and a file that
doesn't exist, are each a [warning] on the type or extension file, which
is then read as if it had no `template`.

**F-TPL-2.** A type with no `template` uses `templates/<type key>.md` from
its own pack if that file exists, and otherwise the **generated
template**: frontmatter with `status: draft` and nothing else, and a body
of `# {{name}}` followed by a level-2 heading for each standard section in
section order, each followed by a blank line. Extensions have no such
fallback. A `.md` file in `templates/` that no type or extension of its
pack uses, by name or through this fallback, is a [warning] ("template
character.md isn't used; add `template: character.md` to
extensions/character.yaml").

**F-TPL-3.** A template is checked when its pack loads, like an entity
file but with these differences:

- Its frontmatter is optional. Without one, the whole file is the body.
  Frontmatter that opens and never closes is an [error] (F-FM-1).
- It MUST NOT have an `id`. [error] (A template with an id would give
  every new entity the same one.) F-ID-2 and F-ID-6 don't apply to
  templates: no tool adds an id to one.
- It MAY have `type`. A `type` that isn't the key of the type using the
  template is a [warning], and the type's key is used.
- It MAY have `name`. It is ignored, since every new entity gets its own.
- It MAY have `status`, which MUST be a valid status (F-STAT-1). [error]
- It MAY have `aliases`, `from`, `fields`, and other keys, which are
  copied into the new entity. They MUST have the shapes format.md §7.1
  gives them. [error] A null field value (`born:`) is fine (F-FLD-4) and
  leaves a prompt for the writer. A field the type doesn't define is a
  [warning] (F-FLD-2).
- Its body follows format.md §9. Anything there that would be an error in
  an entity's body, such as a bad value in a section comment
  (`status=maybe`, F-SEC-7, F-SEC-10), is an [error] on the template.
  Diagnostics that depend on the den, such as a position that doesn't
  resolve (F-POS-4), aren't reported for templates.

A template with an [error] is excluded (F-PACK-2), and its type uses the
generated template. So an entity created from a template never starts
with an error the template put there.

**F-TPL-4.** A template's body is copied into the new entity as written,
with each `{{name}}` replaced by the new entity's name, as written. No
other text is replaced, and other `{{...}}` text is kept as it is. The new
file is UTF-8 with LF line endings and no byte order mark (F-ENC-2,
F-ENC-3), even if the template has CRLF line endings or a byte order mark.
A template SHOULD list its fields in field order (F-FT-3) and its sections
in section order (F-TYPE-12).

**F-TPL-5.** Templates MAY use HTML comments as prompts under each heading
(`<!-- What do they want most? -->`). They are ordinary comments
(F-SEC-11): kept and ignored, hidden when the markdown is rendered, and
there until the writer deletes them. A section that holds nothing but
whitespace and HTML comments is **empty**. Views MAY hide empty sections,
and an empty section still counts as present (F-TYPE-11). A template MAY
also put a section comment under a heading (F-SEC-6), such as
`status=undecided` under `## Open questions`, so the section starts out of
canon.

### 7.2 Creating an entity

**F-TPL-6.** A writer that creates an entity of type `T`, named `N`, in
scope `S`:

1. Picks the path `entities/<plural>/<slug>.md` in `S`'s container folder,
   where `<plural>` is `T`'s folder name (F-TYPE-3). The slug SHOULD be
   made from `N`: lowercase it, replace the letters that don't decompose
   (`æ` with `ae`, `œ` with `oe`, `ø` with `o`, `ß` with `ss`, `þ` with
   `th`, `ð` and `đ` with `d`, `ł` with `l`), decompose it (NFKD), drop
   combining marks, replace each run of characters outside `a-z` and
   `0-9` with one `-`, trim `-` from both ends, and cut it to 64
   characters (at a word boundary, where there is one), trimming a
   trailing `-`. So "Ærin Søvik"
   becomes `aerin-sovik`. If the result is empty, the writer asks for a
   slug or uses the type key (`character`). The slug MUST be a valid key
   (F-KEY-1), and `<slug>.md` MUST NOT match the name of a file already in
   that folder, compared case-insensitively, since many file systems are
   case-insensitive. If it's taken, the writer asks for a slug, or adds
   `-2`, `-3`, and so on, first cutting the slug back (at a word boundary,
   where there is one, and trimming a trailing `-`) so the whole stays
   within 64 characters.
2. Writes the frontmatter in template order (F-TPL-7): `id` (a new ULID,
   F-ID-6), `type: T`, `name: N`, then `aliases`, `status`, `from`, and
   `fields` where there are any, then the template's other keys in the
   template's order. YAML quoting is the writer's job: `name` MUST read
   back as exactly `N`.
3. Takes each value from the caller if it supplies one (aliases, a status,
   `from`, field values), else from the template. `status` falls back to
   `draft`, and the writer MUST write it explicitly (F-ENT-5). Inside
   `fields`, keys from the template keep the template's order, and keys
   the template lacks are added in template order.
4. Writes the body from the template (F-TPL-4).

The result MUST have no error (format.md §1.3). A writer MUST refuse to
create an entity that would, and MUST NOT create one inside a final book
(F-BOOK-1).

The new thread "The missing manifest", shortened:

```markdown
---
id: 01JB2BX7DK9RF3QZ6W1N8HMV4C
type: thread
name: The missing manifest
status: draft
fields:
  planted: []
  paid_off:
---

# The missing manifest

## Overview
<!-- What is the thread, in a sentence or two? -->
```

### 7.3 Template order

**F-TPL-7.** The **template order** that F-RT-4 refers to is:

- for top-level frontmatter keys: `id`, `type`, `name`, `aliases`,
  `status`, `from`, `fields`;
- inside `fields`: the type's field order (F-FT-3), then `row`, then
  `placeholder`.

A writer that adds a key in the template order SHOULD put it right after
the nearest key before it in that order that the file already has, or, if
there is none, before the first existing key from that order, so a file
written from a template stays in order. Keys that aren't in the template
order (an `x-` key, a field the type doesn't define) go after the existing
keys (F-RT-4). A missing `id` always goes first (F-ID-6). Writers MUST NOT
reorder keys that are already there (F-RT-1).

**F-TPL-8.** A writer that adds a standard section to an entity SHOULD
put it before the first level-2 section that comes later in the section
order (F-TYPE-12), or at the end of the body if there is none. A writer
that adds a custom section SHOULD put it at the end of the body.

## 8. Chapter fields

Chapters (format.md §6.4) can carry fields of their own, such as a POV,
the narrative mode, an in-world date, or where the chapter is set. They
are defined by packs, like entity fields, and written in a `fields`
mapping on the chapter item. Values that hold for many chapters can be set
once, in a container's `chapter_defaults`.

```yaml
# series.md
chapter_defaults: { narration: third-limited, tense: past, when: "2291" }
```

```yaml
# book.md
chapter_defaults: { pov: "[[mara-voss]]", setting: "[[kessan-reach]]" }
chapters:
  - { key: prologue, title: Before the Dark, fields: { when: "2284", tense: present } }
  - { key: "1", number: 1, title: Kessan Reach }
  - { key: "2", number: 2, fields: { pov: ["[[mara-voss]]", "[[halden-pike]]"] } }
```

Every chapter of this book is told in the third person, limited, in the
past tense, from Mara Voss's point of view, and takes place in 2291 at
Kessan Reach, except that the prologue is in the present tense and set in
2284, and chapter 2 has two POV characters.

**F-CHAP-1.** A pack's `chapter.yaml` is a mapping with one key, `fields`,
a mapping of field definitions as in §5 (F-FT-1, F-FT-2). A chapter field
has no `temporal`, `required`, or `default`: each of those keys is a
[warning], and is ignored whatever its value, before F-FT-2 checks the
rest of the definition. The names `key`, `number`, `title`, `id`, `name`,
`status`, `from`, `until`, `pin`, `fields`, `row`, `placeholder`, and
`scene` are reserved, for the chapter item's own keys and for later use.
This list is fixed for format 1 (F-TYPE-2). Defining one is a [warning],
and that definition is ignored. Any
other key at the top level is a [warning], and is kept.

**F-CHAP-2.** The **chapter fields** are the fields from the `chapter.yaml`
of every enabled pack, in pack order (F-PACK-7). A name that two packs
both define is a clash, a [warning], and F-PACK-11 decides which
definition is used.

**F-CHAP-3.** In `book.md`, a chapter item's `fields`, if present, is a
mapping from field name to a plain value. The values are checked as in
§5.2 and §5.3, against the chapter field of that name. A null value is
the same as leaving the field out (F-FLD-4), but an empty list is not:
it means "none here" (F-CHAP-5). Unlike an entity field's value, a
chapter-field value that doesn't match its definition is ignored, with
the [warning]: it counts as not set, so the chapter inherits (F-CHAP-5).
So `tense: futur` or `tense: []` doesn't block a `tense` default. In a
list, only the item that doesn't match is ignored, and a list left with
no items counts as not set. Every problem with a chapter's
`fields`, including a value that doesn't match, a name that isn't a
chapter field, and a `fields` that isn't a mapping, is a [warning], never
an error, since an error in a container file would exclude the whole
container (F-GEN-2). A name that isn't a chapter field keeps its value
as written, and it is inherited like any other (as F-FLD-2 keeps an
entity's undefined field). A chapter field is a fact about one
chapter, so it has no temporal form. Entries (format.md §7.3) don't exist
here: the errors of §7.3 (F-TMP-1, F-TMP-2) and the `status` and `pin`
errors inside an entry (F-STAT-1, F-PIN-1) don't apply. A value that is
an entry, or a list that holds one, is a [warning], and that value is
ignored, as if the field were left out. Other keys on the item are kept
but not indexed (F-BOOK-3).

**F-CHAP-4.** Links in chapter fields are recognized (F-LINK-1) and
resolve from the book's scope, like the rest of `book.md` (F-CTR-3). A
link in a chapter item's `fields` has that chapter (`book:chapter`) as its
position for F-LINK-13, so where spoiler checks are on (F-DEN-7), a `pov`
link to a character who enters canon after that chapter gets the F-LINK-13
warning. A link in
`chapter_defaults` resolves from the scope of the container that holds
it. It is checked against F-LINK-13 for each chapter whose effective value
(F-CHAP-5) it is, with that chapter as its position. Each link in the
default gets one warning, on the file that holds the default, naming the
chapters it runs ahead of: the first five in story order, then a count of
the rest.

**F-CHAP-5.** `universe.md`, `series.md`, and `book.md` MAY have
`chapter_defaults` (F-CTR-5): a mapping from chapter field name to a plain
value, checked as in F-CHAP-3, so every problem with it is a [warning]. A
chapter's **effective value** for a chapter field is:

1. its own value in `fields`, if that isn't null;
2. otherwise, the value in the `chapter_defaults` of the nearest container
   in its book's scope chain (book, then series, then universe) that has a
   non-null value for that field;
3. otherwise, none.

For a list-valued chapter field, an explicit empty list is a value, not
an unset field (unlike F-FT-12): it means "none here" and stops
inheritance for that field. A null, or leaving the field out, still
inherits. A writer that removes the last item of a chapter's list
field, or of a list field in any `chapter_defaults`, flow or block,
leaves `[]`, so it still says "none" and inheritance stops (format.md
F-RT-6); going back to inheriting means clearing the field. Scalar fields have no "none" value, so
a chapter that differs writes the value it wants. This is for a book told by one narrator that
has, say, an omniscient prologue or a chapter that is a found document:

```yaml
# book.md
chapter_defaults: { pov: "[[mara-voss]]", narration: first }
chapters:
  - { key: prologue, fields: { pov: [], narration: third-omniscient } }
  - { key: "1", number: 1 }
  - { key: "9", number: 9, title: The Salvaged Log, fields: { pov: [] } }
```

The prologue and chapter 9 have no POV character, and every other chapter
is Mara Voss's. The same holds in `chapter_defaults`: an empty list there
stops inheritance from containers further up.

Each field is looked up on its own, so a series can set `tense` and one of
its books `narration`. Views and the index use effective values.
`chapter_defaults` holds values the writer states for those chapters, so
they are facts about the chapters, unlike a field definition's `default`
(F-FT-15), which chapter fields don't have.

**Headings.** A container's `chapter_heading` (format.md F-CTR-6, #227)
lists chapter fields to show in each chapter's heading, after its title.
A multi-POV book can head each chapter with its POV, and a space opera
with its POV, place, and date:

```yaml
# series.md
chapter_heading: [pov, setting, when]
chapter_defaults: { setting: "[[vesta-yards]]" }
```

```yaml
# book.md
chapters:
  - { key: "3", number: 3, fields: { pov: ["[[ines-ardent|Ardent]]"], when: "2350-04-12" } }
```

Chapter 3 is headed "Ardent — Vesta Yards, 2350-04-12", and a rename of
the Vesta Yards note shows in every heading at once. It is inherited as
a whole list, nearest container first, and `[]` turns it off below.

## 9. The core pack

**F-CORE-1.** The core pack is built in and always enabled (F-DEN-4,
F-PACK-7), unless a den-local pack shadows it (F-PACK-10). It defines the
eight types below and the chapter fields `pov`, `narration`, `tense`,
`when`, and `setting`. Its files, in [`packs/core/`](../packs/core/),
are normative: where this section and those files differ, the files
win, and the difference is a bug in this section. The core pack has no required fields
and no required sections, so a hand-written note never gets a "missing"
warning from it. Each type has a template in `packs/core/templates/`.

The core pack is genre-neutral. Genre types, such as species, planets,
ships, and magic systems, come in other packs (design.md §4). A genre
pack that tracks species adds a `species` *type*, rather than redefining
the character's `species` field: the field is text, so it can hold a link
to a species entity (`species: "[[velari]]"`). Other genre fields, such as
a rank, come as extensions (§4, §10).

### 9.1 Types

| Type | Folder | Fields | Standard sections |
|---|---|---|---|
| `character` | `characters` | `role`\*, `gender`\*, `pronouns`\*, `occupation`\*, `skills`\*, `heritage`\*, `born`, `died`, `species`, `factions`\* | Overview, Appearance, Backstory, Want, Wound, Voice, Relationships, Arc, Open questions |
| `location` | `locations` | `part_of`, `controlled_by`\* | Overview, Description, History |
| `faction` | `factions` | `leader`\*, `part_of`, `base`\* | Overview, Goals, Methods, Members, History |
| `item` | `items` | `owner`\*, `location`\* | Overview, Description, History |
| `event` | `events` | `when`, `ends`, `location`, `participants` | Overview, Causes, Consequences |
| `thread` | `threads` | `planted`, `paid_off`, `characters` | Overview, Setup, Payoff, Open questions |
| `idea` | `ideas` | `about` | none |
| `note` | `notes` | `about` | none |

\* temporal. Every type also has `row` and `placeholder` (§5.5).

| Field | Type | On |
|---|---|---|
| `role` | `text` | character: their part in the story (protagonist, mentor) |
| `gender`, `pronouns`, `heritage` | `text` | character |
| `occupation`, `skills` | list of `text` | character |
| `born`, `died` | `date` | character |
| `species` | `text` | character: plain text, or a link to a species entity |
| `factions` | list of `ref` to `faction` | character: the groups they belong to |
| `part_of` | `ref` to the same type | location: the larger place; faction: the larger group |
| `controlled_by` | `ref` to `faction` | location |
| `leader` | `ref` to `character` | faction |
| `base` | `ref` to `location` | faction |
| `owner` | `ref` to `character` or `faction` | item |
| `location` | `ref` to `location` | item, event |
| `when`, `ends` | `date` | event: when it happens in-world |
| `participants` | list of `ref` to `character` or `faction` | event |
| `planted` | list of `position` | thread: where it's set up |
| `paid_off` | `position` | thread: where it pays off |
| `characters` | list of `ref` to `character` | thread |
| `about` | list of `ref` to any type | idea, note |

**Character.** A person, or any other being with a part in the story. The
sections follow a common character-sheet method: what happened before the
story, what they want most, the old wound that drives them, their voice,
and where their arc ends. The template has every field except `died` and
`species`, which only some characters need. `species` has no default.
Group membership is the character's `factions` field (see Faction below).
A minor character who is only a possibility is a character with `status:
candidate` (F-ENT-5). In-world rank or office is genre-specific and left
to other packs or a den's extension (§10). `died` and `species` aren't
temporal, but a death or a nature the story reveals partway through can be
written as a single entry with a `from`, where the reader learns it
(F-TMP-8), so earlier views don't give it away.

**Location, faction, item.** Places, groups, and objects. `part_of` nests
places and groups. A later era of a place's history goes in a subsection
of `## History` with its own `from=` (F-SEC-9), not a second `## History`,
which would take over from the first at its `from` (F-SEC-8) and hide the
earlier history from later views.

**Faction.** Membership is written once, on each member: the character's
`factions` field. The faction's page lists its members from those links.
Its `## Members` section holds what that list can't: members who don't
have a page yet, and the roles people hold within the group.

**Event.** Something that happens in the story world. An event has two
independent times (F-ORD-4): `when` is when it happens in-world (§5.2), and
the entity's `from` (F-ENT-6) is where the reader learns of it. A
flashback makes them differ. `row` (F-FLD-7) ties the event to the outline
rows that show it. A timeline view SHOULD list events by the sort key of
`when`.

**Thread.** A plot thread: a setup, a mystery, or a promise to the reader
that the story has to pay off. Two things about a thread are independent,
and each is written in one place.

- **Where it stands on the page** comes from `planted`, the positions where
  the story sets it up, and `paid_off`, the position where it pays off
  (F-FT-10). As of a position `p`, or the end of the story, a thread is
  **paid off** if `paid_off` isn't after `p`, else **planted** if one of
  its `planted` positions isn't after `p`, else **not yet planted**.
  Positions that don't resolve are skipped. Views SHOULD show this state.
  Here and in F-CORE-2 and F-CORE-4, `planted` and `paid_off` are each
  read from a plain value, or from a single entry that is in canon
  (F-STAT-3), using its `value`. Anything else, such as a `candidate`
  entry for a payoff chapter still being decided, or a temporal list,
  counts as unset.
- **Whether its resolution is decided** is ordinary `status` (§9.3 of
  format.md), on the thread or on just the part that's open. Usually the
  thread is in canon and only its `## Payoff` section is `undecided`, with
  each option banked as a `candidate` subsection. A thread that might not
  happen at all is itself `idea` or `candidate`.

```markdown
## Payoff
<!-- wolfden: status=undecided -->

### Pike sold it
<!-- wolfden: status=candidate -->
Halden Pike sold the manifest to the Compact to pay his debts.

### It was never aboard
<!-- wolfden: status=candidate -->
The manifest was forged at Kessan Reach before the *Tessaly* docked.
```

Once the payoff is chosen, the chosen option becomes the section's
content, the section's comment goes, and the options not chosen can stay
as `candidate` subsections or be deleted. Rules a writer keeps across all
threads ("no coincidences in a payoff") are author notes, planned in #45.

**F-CORE-2.** A thread whose `paid_off` comes before its earliest
`planted` position, in story order, is a [warning]. Positions that don't
resolve are skipped. If either one is a bare book position (format.md
F-POS-7), only the books are compared, since the chapter isn't known: the
warning needs `paid_off`'s book to come strictly before the planted
position's book. So `planted: ["ember:3"]` with `paid_off: "ember"` gets
no warning.

**F-CORE-4.** A thread's `## Payoff` section takes the thread's `paid_off`
as its `from` when its section comment has no `from` (format.md F-SEC-8),
so the payoff stays out of views from before it happens, and the links in
it have that position (F-LINK-13). An explicit `from=` wins, and the
section's subsections inherit the position as usual (F-SEC-9).

A `paid_off` that is a well-formed position but doesn't resolve yet, such
as a chapter of a book that has no chapters listed, is still the Payoff's
`from`. The section is then left out of canon views like any fact whose
position doesn't resolve (F-POS-4), so a planned payoff never shows early.
It gets no second F-POS-4 warning, since `paid_off` already has one
(F-FT-10).

The rule is narrow. It covers only a level-2 section named `Payoff`
(matched as in F-SEC-3), only in an entity whose type is the built-in core
pack's `thread` (extended or not), and only while `paid_off` is set (as
read above) and is a well-formed position. Otherwise F-SEC-8's usual rule
applies. A den-local type that replaces `thread` (F-TYPE-4, F-PACK-10)
doesn't get this default, and writes `from=` instead. That includes a den
that shadows core with a copy of it, whose copied `thread.yaml` still
describes the default in its Payoff description, so such a den should
edit that description.

**F-CORE-3.** An event whose `ends` is earlier than its `when` is a
[warning]. The two sort keys are compared only over the numbers both
have, so `when: "2291-05-14 06:40"` with `ends: "2291-05-14"`, or `when:
"2291-05"` with `ends: "2291"`, gets no warning. Dates without sort keys
are skipped. Like `planted` and `paid_off` in F-CORE-2, `when` and `ends`
are each read from a plain value, or from a single entry that is in
canon, and the rule covers only the built-in core pack's `event`
(extended or not), as F-CORE-4 covers only its `thread`.

**Idea.** A raw idea that isn't yet a thing of any one type: a premise, a
scene, a twist, a line of dialogue. That's the difference from `status:
idea` (§9.3 of format.md), which can go on any type: a character who might
exist is a `character` with `status: idea`, filed with the characters and
already shaped like one. An `idea` entity is for material that hasn't
found its type, and its template starts it at `status: idea`. `about`
links it to whatever it's for. When an idea is adopted, change its `type`
and move the file to that type's folder, or write the new entity and
delete the idea (with git, its history is kept). `retconned`
is for material that was canon, which an idea never was.

**Note.** Free-form material that isn't a thing in the story: research,
rules of the setting, a style guide, an essay on a theme. Unlike an
untyped entity, a note gets no F-ENT-2 warning and has a home folder
(`entities/notes/`, inside a container, unrelated to the reserved `notes/`
at the den root). Its `status` works as for any entity: rules of the
setting can be `canon`. Until author and den notes arrive (#45), a style
guide is a universe-scope note.

### 9.2 Chapter fields

| Field | Type | Meaning |
|---|---|---|
| `pov` | list of `ref` to `character` | The chapter's point-of-view character or characters. |
| `narration` | `enum`: `first`, `second`, `third-limited`, `third-omniscient`, `third-objective` | The narrative person and point of view. |
| `tense` | `enum`: `past`, `present`, `future` | The narrative tense. |
| `when` | `date` | When the chapter takes place in-world: the story's present, or a flashback out of step with the chapters around it. |
| `setting` | list of `ref` to `location` | Where the chapter takes place. |

Set them where they matter. A book or series told one way throughout sets
`narration` and `tense` once, in `chapter_defaults` (F-CHAP-5), and a
chapter that differs, such as a flashback whose date and POV are out of
step with the chapters around it, sets its own. `when` works the same way
for the story's present: a space opera can set a far-future date once in
`chapter_defaults` and override it as time moves on, and `setting` can
link each chapter to the planet or star system where it happens. Made-up
calendars such as stardates wait for #37. A multi-POV book may set
`pov` on every chapter. No check ties `pov` to `narration`. These fields
are independent of an outline's `pov` column (F-OUT-9), which is per row.
Time and place for each beat, finer than a chapter's `when` and
`setting`, are outline matters (#55).

## 10. Appendix: a den-local pack

The Starfall den from format.md §13 adds a pack of its own, enabled in
`den.yaml`, to give characters a rank and to add a ship type.

```text
starfall-den/
  den.yaml
  packs/starfall/
    pack.yaml
    extensions/character.yaml
    types/ship.yaml
    templates/ship.md
  universes/starfall/...
```

`den.yaml`:

```yaml
format: 1
authors:
  wren-calder: { name: Wren Calder }
packs: [starfall]
```

`packs/starfall/pack.yaml`:

```yaml
name: Starfall
description: Types and fields for the Starfall universe.
```

`packs/starfall/extensions/character.yaml`:

```yaml
fields:
  rank:
    type: string
    temporal: true
    description: Military rank.
  posting: { type: ref, to: [ship, location], temporal: true }
sections:
  - Service record
```

`packs/starfall/types/ship.yaml`:

```yaml
name: Ship
plural: ships
fields:
  class: string
  captain: { type: ref, to: character, temporal: true }
  condition:
    type: enum
    values: [active, damaged, lost]
    default: active
    temporal: true
sections:
  - Overview
  - Crew
  - Service history
```

`packs/starfall/templates/ship.md`:

```markdown
---
type: ship
status: draft
fields:
  class:
  captain:
---

# {{name}}

## Overview

## Crew

## Service history
```

With this pack enabled, Mara Voss's `rank` from format.md §13 no longer
gets the F-FLD-2 warning, and her `posting` can link to the *Tessaly*,
filed at `entities/ships/tessaly.md`, or to a location such as Kessan
Reach. Character fields come in this
order: core's `role`, `gender`, `pronouns`, `occupation`, `skills`,
`heritage`, `born`, `died`, `species`, and `factions`, then the
extension's `rank` and `posting`, then `row` and `placeholder`. The
character's sections end with core's `Open questions`, then the
extension's `Service record`. A ship with no `condition` shows as
`active`, marked as a default.

If the den later enables a built-in pack that also defines `ship`, the
den-local `ship` is still used, and the built-in one is ignored, with the
F-TYPE-4 warning on `packs/starfall/types/ship.yaml` (F-PACK-11). The den
means what it meant before. To adopt the built-in type instead, the den
drops its own `types/ship.yaml` and moves any fields the built-in type
lacks into `extensions/ship.yaml`.
