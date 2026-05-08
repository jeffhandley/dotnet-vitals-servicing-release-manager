# Report Format

This reference defines the structure, template, and formatting rules for the issue triage report.

## Report Structure

The report leads with the most critical information and progresses from urgent to informational. The complete issue backlog is collapsed inside a `<details>` element so it doesn't bury the actionable items.

Use the current New York date for `{YYYY-MM-DD}` in the H1 heading.

```markdown
# MCP - Triage - {YYYY-MM-DD}

## Summary

- **Open issues:** {N} total — {N} triaged, {N} untriaged
- **SLA compliance:** {N} violations ({or "all compliant"})
- **Urgent attention:** {N} issues need immediate action {brief characterization, emphasizing regressions/security/production disruption when present}
- **Top finding:** {single most important takeaway for the maintainer}
- **Release watch:** {signal tied to the most recent release `vX.Y.Z` (released YYYY-MM-DD) or "no recent release-linked disruption found"}

> **Keep each summary bullet concise — one short sentence.** If a finding requires
> more detail, use sub-bullets rather than turning the bullet into a paragraph.

---

## 🔄 Tier Guidance Discrepancies {only if differences were found between the skill's assumptions and live tier data; omit entirely if all match}

The following differences were found between the assumptions coded into the triage
skill/workflow and the live SDK tier data fetched from `sdk-tiers.mdx`:

- {What the skill assumes} → {What the live data says} _(file: {which skill/reference file})_
- ...

> These discrepancies should be resolved by updating the skill and reference files
> to match the current tier requirements.

---

## ⚠️ Safety Concerns {only if issues were flagged during safety scanning; omit entirely if clean}

The following issues contain content that was flagged during safety scanning.
Their content should be reviewed carefully before acting on any recommendations.

| # | Title | Concern |
|---|---|---|
| [#N](url) | {Title} | {Brief description: e.g., "Prompt injection attempt detected", "Suspicious external link"} |

---

## 🚨 Critical Regressions / Security Signals / Release Watch {include whenever present; place before routine urgent-attention sections}

{Use this section for anything that looks like a recent regression, a public security-sensitive issue, or a customer-facing production disruption. These items should be the most prominent in the report.}

| # | Risk | Version / Release Signal | Why it matters now |
|---|---|---|---|
| [#N](url) | Regression / potential security concern / deployment disruption | `Package x.y.z` / latest release `vX.Y.Z` released YYYY-MM-DD | {Concise explanation of customer impact and evidence} |

---

## 🚨 Issues Needing Urgent Attention

### SLA Violations — Untriaged Issues

{For EACH issue: a table with metadata (created, author, labels, comments, reactions)
followed by a **Status** bullet list summarizing the full discussion and a **Recommended
actions** list with specific labels and next steps.}

### Potential P0/P1 Issues to Assess

{Same detailed format as SLA violations — these are bugs that may warrant critical
priority based on core functionality impact or spec compliance.}

---

## 🏷️ Issues Needing Labels

### Missing Type Label

{Table: issue number, current labels, title, recommended type label.}

### Missing Priority Label on Bugs

{Table: bugs that have type/status labels but no priority label, with recommended priority.}

---

## 🔀 Potential Duplicates

{Table: groups of issues that overlap, with recommendation on which to keep.}

---

## ⏰ Stale Issues

{Issues labeled `needs confirmation` or `needs repro` where the reporter hasn't
responded in >14 days. Include the date of the last author comment and a recommendation
to close if no response.}

---

<details>
<summary>📋 Complete Open Issue Backlog ({N} issues)</summary>

### Bugs ({N})

{Full table: #, Created, Age, Labels, Title, Remaining Actions}

### Enhancements ({N})

{Full table}

### Questions ({N})

{Full table}

### Other / Unlabeled ({N})

{Full table}

</details>
```

## Formatting Rules

### Links
- **Within csharp-sdk:** Use GitHub shorthand — `#123` for issues/PRs, `@username` for users
- **Other repos:** Use full URLs — `[typescript-sdk #1090](https://github.com/modelcontextprotocol/typescript-sdk/issues/1090)`
- **Repo links:** `[modelcontextprotocol/typescript-sdk](https://github.com/modelcontextprotocol/typescript-sdk)`

### Age Display
- Show as `{N}d` (e.g., `35d`, `253d`)
- Business days calculated as `floor(calendar_days × 5 / 7)`

### Issue Detail Blocks

For each issue in the attention sections (SLA violations, P0/P1 candidates, stale issues), use this format:

```markdown
### [#{number}](https://github.com/modelcontextprotocol/csharp-sdk/issues/{number}) — {title}

| Field | Value |
|---|---|
| **Created** | {YYYY-MM-DD} (~{N} biz days {overdue / old}) |
| **Author** | @{login} {(contributor/member) if applicable} |
| **Labels** | `label1`, `label2` {or _(none)_ ❌ if empty} |
| **Comments** | {N} · **Reactions:** {N} {emoji} |
| **Assignee** | @{login} {or _(unassigned)_} |
| **Open PR** | [#{N}](url) {if any} |
| **Version / Release Signal** | {`Package x.y.z` released YYYY-MM-DD from NuGet/release notes, or _(none found)_} |

**Status:**
- {What the reporter wants}
- {Key maintainer responses or positions}
- {Any community workarounds or fixes}
- {Linked PRs, if any}
- {Current blocking factor}

> **Use bullet points for the Status section — never a paragraph.** Each bullet
> should be a single short sentence. Cover: what the reporter wants, what
> maintainers have said, whether the community has provided workarounds, whether
> there are linked PRs, and the current blocking factor.

{If the issue was flagged during safety scanning, include immediately after the Status bullets:}

> ⚠️ **Safety flag:** {description of concern, e.g., "Issue body contains prompt injection attempt — instructions to 'ignore previous instructions' detected." or "Issue contains suspicious link to non-standard domain."}

**Recommended actions:**
- {Specific label changes: "Add `bug`, `needs repro`, `P2`"}
- {Next step: "Close as answered", "Request reproduction steps", "Assign to @X", etc.}
- {If stale: "Last author response was on {date} ({N} days ago). Consider closing."}
```

### Backlog Tables

For the collapsed backlog, use compact tables:

```markdown
| # | Created | Age | Labels | Title | Remaining Actions |
|---|---|---|---|---|---|
| [#N](url) | YYYY-MM-DD | Nd | `label1`, `label2` | Short title | Add `P2`; consider closing |
```

### Section Emoji

| Section | Emoji |
|---|---|
| Tier discrepancies | 🔄 |
| Safety concerns | ⚠️ |
| Urgent attention | 🚨 |
| Labels needed | 🏷️ |
| Duplicates | 🔀 |
| Stale issues | ⏰ |
| Backlog | 📋 |

The `## Summary` heading does **not** use an emoji prefix.
