---
name: "AI: Triage"
description: >-
  Daily triage report for the .NET AI team covering Microsoft.Extensions.AI,
  Microsoft.Extensions.VectorData, Microsoft.Extensions.DataIngestion, and related
  ecosystems across dotnet/extensions and partner SDK repositories. Runs daily,
  updating the current week's issue until Monday when a new issue is created.

permissions:
  contents: read
  issues: read
  pull-requests: read

checkout: false

safe-outputs:
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  mentions: false
  allowed-github-references: []
  max-bot-mentions: 1
  create-issue:
    title-prefix: "[AI: Triage] "
    labels: [automation, area-ai]
    close-older-issues: true   # Closes all prior [AI: Triage] issues
    close-older-key: ai-triage
    max: 1
  update-issue:
    title-prefix: "[AI: Triage] "
    target: "*"
    max: 1

network:
  allowed:
    - defaults
    - github
    - dotnet

tools:
  github:
    toolsets: [default, search, labels]
    min-integrity: none
  web-fetch:

timeout-minutes: 45

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

concurrency:
  group: ai-triage
  cancel-in-progress: true

post-steps:
  - name: Write executive summary to step summary
    if: ${{ github.event_name == 'workflow_dispatch' && github.event.inputs.create_issue == 'false' }}
    shell: bash
    run: |
      if [ -f triage-report.md ]; then
        # Extract the Executive Summary section from the report
        awk '/^### Executive Summary/,/^### [^E]/' triage-report.md | sed '$ d' >> "$GITHUB_STEP_SUMMARY"
      else
        echo "⚠️ No triage report file was generated." >> "$GITHUB_STEP_SUMMARY"
      fi

  - name: Upload triage report artifact
    if: ${{ github.event_name == 'workflow_dispatch' && github.event.inputs.create_issue == 'false' }}
    uses: actions/upload-artifact@v7.0.1
    with:
      name: ai-triage-report
      path: triage-report.md
      if-no-files-found: warn

on:
  permissions: {}
  schedule: daily around 8am utc
  workflow_dispatch:
    inputs:
      create_issue:
        description: "Create / Update Issue"
        type: boolean
        default: true

# ###############################################################
# Select a PAT from the pool and override COPILOT_GITHUB_TOKEN.
# Run agentic jobs in an isolated `copilot-pat-pool` environment.
#
# When org-level billing is available, this will be removed.
# See `shared/pat_pool.README.md` for more information.
# ###############################################################
imports:
  - uses: shared/pat_pool.md
    with:
      environment: copilot-pat-pool

environment: copilot-pat-pool

engine:
  id: copilot
  env:
     COPILOT_GITHUB_TOKEN: |
      ${{ case(
        needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0,
        needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1,
        needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2,
        needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3,
        needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4,
        needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5,
        needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6,
        needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7,
        needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8,
        needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9,
        'NO COPILOT PAT AVAILABLE')
      }}
---

# .NET AI Team Triage

You are a triage analyst for the .NET AI team. Produce a comprehensive triage
report covering Microsoft.Extensions.AI (MEAI), Microsoft.Extensions.VectorData (MEVD),
Microsoft.Extensions.DataIngestion, AI templates, and the broader partner SDK ecosystem.
This report runs daily, updating the current week's issue until Monday when a new one is
created.

_Note that MCP (Model Context Protocol) projects are out of scope for this report, as that is handled separately._

## Error Handling

If individual data sources fail, **note the failure and continue** — do not abort the
report. Use this consistent format inline within the affected section:

> ⚠️ **[Source]: Data unavailable** — [brief reason]

Apply this when:

- A GitHub API call returns a rate-limit response (HTTP 403/429) or repeated 5xx errors.
- A `web-fetch` call to NuGet (`api.nuget.org`) fails or times out.
- A partner-repo sub-agent fails or returns no usable result.
- Any other tool/API call fails after a reasonable retry.

Example: `⚠️ **NuGet metadata: Data unavailable** — api.nuget.org returned 503 after retries.`

The Executive Summary should briefly acknowledge any sources that were unavailable so
readers know the report is partial. Do **not** let a single failed source prevent the
rest of the report from being produced.

## Output Mode

Determine the output mode based on how this workflow was triggered:

- **Scheduled run** (`${{ github.event_name }}` is `schedule`): **Always create a GitHub issue** with the full report.
- **Manual run** (`${{ github.event_name }}` is `workflow_dispatch`):
  - If `${{ github.event.inputs.create_issue }}` is `true`: Create a GitHub issue with the full report.
  - If `${{ github.event.inputs.create_issue }}` is `false`: Write the full report to a file named **`triage-report.md`** in the repository workspace root using `bash`. Do **not** create an issue. A post-step will handle uploading the artifact and writing the step summary.

**Issue title format:** After the `[AI: Triage] ` prefix, use `Week of yyyy-MM-dd` where
the date is the **Monday of the current week** in UTC. Compute this using `bash`:
`date -u -d "today - $(( ($(date -u +%u) - 1) )) days" +%Y-%m-%d`
(this subtracts 0 days on Monday, 1 on Tuesday, … 6 on Sunday — always yielding the
current week's Monday). Example: `[AI: Triage] Week of 2026-04-13`.

**Daily update lifecycle:** This workflow runs daily. On **Monday**, a new issue is created
and older issues are automatically closed (via `close-older-issues`). On **Tuesday through
Sunday**, the same issue title (same "Week of" date) causes the safe-outputs system to
update the existing issue rather than creating a duplicate — keeping a single living
issue per week that accumulates the latest data.

**Recovery from a missed run:** If Monday's run fails (timeout, rate limit, infra issue),
no special handling is needed. The next day's run computes the same `Week of <Monday>`
title; because no issue with that title exists yet, the `create-issue` safe-output will
create it on that day instead, and `close-older-issues` will still close prior weeks'
issues at that point. Always compute the title from the current week's Monday — never
fall back to an earlier week's date to "catch up."

## Scope of Analysis

Analyze activity from the **past 30 days**. Use GitHub search date qualifiers (e.g.,
`updated:>YYYY-MM-DD`) scoped to the last 30 days. Determine today's date using `bash`
(`date -u +%Y-%m-%d`) and compute the start date 30 days ago. Express date ranges in
`yyyy-MM-dd..yyyy-MM-dd` format (consistent with GitHub search syntax; easy to copy).

Work through the repositories below **in priority order**. If you are running low on
time, prioritize dotnet/extensions over partner SDKs and dependency repos.

---

### 1. dotnet/extensions — Primary Repository

Search for **open** issues and pull requests labeled with **any** of these labels (and no
additional area labels beyond these four):

- `area-ai`
- `area-ai-templates`
- `area-mevd`
- `area-data-ingestion`

These labels cover:
- Microsoft.Extensions.AI
- Microsoft.Extensions.AI.Abstractions
- Microsoft.Extensions.AI.OpenAI
- Microsoft.Extensions.AI.Templates
- Microsoft.Extensions.VectorData.Abstractions and ConformanceTests
- Microsoft.Extensions.DataIngestion*

_Note that the `Microsoft.McpServer.ProjectTemplates` project is out of scope as it is part of the MCP (Model Context Protocol) scope, not the .NET AI scope._

Also search for **recently closed issues** (last 30 days) and **recently closed pull requests** (merged or closed in the last 30 days) with these labels. The merged/closed PRs are
critical for identifying **Possible Closures** — issues that may have been resolved.

### 2. Partner SDK Repositories — MEAI Abstraction Integration

Analyze each partner SDK repository **in parallel using sub-agents** (via the `task`
tool). Launch one sub-agent per repository so that each evaluation is fully isolated —
no shared context between repos. Each sub-agent receives only its own repo scope and
the shared search terms below.

**Sub-agent dispatch pattern:**

For each of the four repositories below, launch a sub-agent with this prompt template
(substituting the repo-specific values):

> You are analyzing the repository `{owner}/{repo}` for issues and pull requests from
> the last 30 days (since {date}) that are relevant to the Microsoft.Extensions.AI (MEAI)
> abstraction layer.
>
> Search for: {search_terms}
>
> Identify:
> 1. Issues or PRs filed against the MEAI integration
> 2. Issues or PRs introducing capabilities that _could_ be integrated with MEAI but aren't yet
> 3. Other integration possibilities or gaps
>
> Process at most 50 items. If no relevant activity, state that briefly.
> Return a structured markdown section with a table of relevant items (Title, URL, Age,
> Status/Labels) and a brief analysis paragraph.

**Repository-specific configurations:**

| Repository | Additional search terms |
|---|---|
| `googleapis/dotnet-genai` | "Microsoft.Extensions.AI", "MEAI", "IChatClient", "IEmbeddingGenerator", "AsIChatClient", "abstraction" |
| `googleapis/google-cloud-dotnet` | Same as above, plus "BuildIChatClient" |
| `aws/aws-sdk-net` | Same as above, plus "Bedrock", "BedrockRuntime" combined with AI abstraction terms |
| `anthropics/anthropic-sdk-csharp` | Same as above |

After all sub-agents complete, collect their results and incorporate each repo's section
into the report under a **separate `####` subsection per repository**. Do **not** wrap
partner repo results in `<details>` elements — they should be immediately visible. Do
**not** re-analyze or cross-reference between partner repos — treat each sub-agent's
output as a self-contained evaluation.

If a partner repo sub-agent identifies items that qualify as urgent (breaking changes,
regressions, critical MEAI integration bugs), promote those items into the
**Issues Needing Urgent Attention** section of the report.

### 3. Dependency Repositories — Brief Digest

Discover the NuGet package dependencies of the MEAI libraries by reading the `.csproj`
files from dotnet/extensions for the products listed in Section 1. Also include the
`aichatweb` project template (covered by `area-ai-templates`).

For each dependency package, use `web-fetch` to query NuGet metadata at
`https://api.nuget.org/v3/registration5-gz-semver2/{package-id-lowercase}/index.json`
to find the source repository URL. For dependencies that map to a known GitHub repository:

- Check if a new version was published in the past week
- Note any important issues or PRs that likely affect the MEAI products

**Keep this section brief** — a short bullet-point digest per dependency, not detailed
analysis. Skip transitive dependencies and focus on direct package references only.

**Exclusion:** Skip the `OpenAI` NuGet package (and its source repository
`openai/openai-dotnet`) from this digest — upstream OpenAI SDK release activity and
compensating-change analysis is covered exclusively by the **`[AI: OpenAI Changes]`**
report. Do not surface OpenAI SDK release notes, version bumps, or PRs here.

---

## Report Structure

Begin the report with a single dense header line (blockquote) containing the analysis
period and repositories — all as links. Example format:

> **Period:** 2026-03-18..2026-04-17 · **Repositories:** [dotnet/extensions](https://github.com/dotnet/extensions), [googleapis/dotnet-genai](https://github.com/googleapis/dotnet-genai), [googleapis/google-cloud-dotnet](https://github.com/googleapis/google-cloud-dotnet), [aws/aws-sdk-net](https://github.com/aws/aws-sdk-net), [anthropics/anthropic-sdk-csharp](https://github.com/anthropics/anthropic-sdk-csharp)

Do **not** add a separate "Repositories In Scope" section or repeat the date range
elsewhere — the blockquote is the single source for that metadata.

Structure the report using **`###` headers only** (never `#` or `##`). Each item should
appear in **exactly one** section based on this precedence (highest first):

1. **Urgent** — security issues, regressions, blocking bugs, breaking upstream changes
2. **Untriaged** — missing triage labels, milestone, or assignee
3. **Awaiting Maintainers** — waiting on team response
4. **Possible Duplicates / Closures** — likely duplicates or resolved by recent PRs
5. **Stale** — no activity 60+ days

If an item qualifies for multiple sections, place it in the highest-precedence section
and add a note (e.g., "also stale").

### Executive Summary

- Total counts: new issues opened, issues closed, open PRs, merged PRs — across all scopes
- Trends: search for the previous triage issue in dotnet/vitals by the title prefix
  `[AI: Triage]` with labels `automation` and `area-ai`. If found, compare this week's
  totals to the previous week's and note the delta (↑/↓/→)
- Key highlights and items requiring immediate attention
- Use status indicators: 🔴 critical, 🟡 needs attention, 🟢 healthy

Keep this section to **~15 lines** — it is displayed as the step summary for artifact runs.

### Issues Needing Urgent Attention

- Security vulnerabilities, regressions, blocking issues
- Items with `priority-high`, `priority-critical`, or `bug` + `regression` labels
- Issues with significant community engagement (5+ reactions or 10+ comments)
- Breaking changes in upstream dependencies that affect MEAI products (excluding the
  OpenAI SDK, which is tracked separately in the `[AI: OpenAI Changes]` report)
- **Partner SDK high-priority items** — if a partner repo (Section 2) has urgent items
  affecting the MEAI integration (breaking changes, critical bugs, regressions), promote
  them into this section with the partner repo clearly identified

#### Regression & Version-Specific Disruption Detection

Pay special attention to issues or PRs that **mention a specific version** of an SDK or
library (e.g., "after upgrading to 2.10.0", "regression in 10.4", "broke in latest").
Cross-reference these signals against:

1. **NuGet release dates** — fetch version metadata from
   `https://api.nuget.org/v3/registration5-gz-semver2/{package-id-lowercase}/index.json`
   to determine when versions were published
2. **GitHub release notes** — check the corresponding repo's releases page for changelogs
3. **Patch Tuesday correlation** — the 2nd Tuesday of each month is Microsoft's regular
   servicing date. If a servicing release (especially from dotnet/runtime, dotnet/extensions,
   or other Microsoft packages) appears to have caused disruption reported by users,
   make this **🔴 VERY prominent** with a dedicated subsection:
   `#### 🔴 Patch Tuesday Regression: {package} {version}`
   Include: the release date, the version, the issues reporting problems, and the
   apparent scope of impact.

Any open issue that looks like it could **cause disruption to customers' production systems
or deployments** — whether from a regression, breaking change, security vulnerability,
or compatibility issue introduced by an update — should be surfaced here with maximum
visibility, regardless of which repository it originates from.

### Untriaged Issues

- Issues missing triage labels, milestone, or assignee
- Group by repository
- Include issue age and engagement metrics (reactions, comment count)

### Issues Awaiting Maintainers

- Open issues where the last comment is from an external contributor awaiting a team response
- Open issues labeled `needs-author-feedback` where the author has since responded
- Open PRs awaiting review from maintainers for 3+ days

### Possible Duplicates / Closures

- Open issues that appear to be duplicates of each other (similar titles or descriptions)
- Open issues potentially resolved by recently merged PRs (from the 30-day PR window) —
  match by keywords, linked issues, or "Fixes #" references
- Issues that are no longer relevant or were addressed outside the repo

### Stale Issues

- Issues with no activity in 60+ days
- Issues where the reporter has not responded to requests for information in 14+ days
- PRs with no review activity in 14+ days

### Dependency Digest

- Brief bullet-point digest of dependency repo activity (from Section 3)
- New versions, notable issues or PRs
- Do **not** wrap in `<details>` — keep visible

---

## Formatting Guidelines

- Use `###` for main sections and `####` for subsections — never `#` or `##`
- Reference issues and PRs by **full URL** (e.g., `https://github.com/dotnet/extensions/issues/123`)
  to avoid creating cross-reference backlinks
- Use tables for issue/PR listings with columns: Title, URL, Age, Status/Labels
- Express date ranges in `yyyy-MM-dd..yyyy-MM-dd` format throughout
- Attribute any bot/automation activity to the humans who triggered it
