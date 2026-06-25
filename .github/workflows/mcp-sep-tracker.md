---
name: "MCP: SEP Tracker"
description: >-
  Weekly tracker for Specification Enhancement Proposals (SEPs) in the MCP
  specification repository. Identifies recently merged SEPs, summarizes
  active SEPs and their discussion threads, and assesses each SEP's impact
  on the C# SDK. Runs weekly on Monday and creates a new issue for the
  current week, automatically closing prior weeks' issues.

permissions:
  contents: read
  issues: read
  pull-requests: read

safe-outputs:
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  mentions: false
  allowed-github-references: []
  max-bot-mentions: 1
  create-issue:
    title-prefix: "[MCP: SEP Tracker] "
    labels: [automation, area-mcp]
    close-older-issues: true   # Closes all prior [MCP: SEP Tracker] issues
    close-older-key: mcp-sep-tracker
    max: 1
  update-issue:
    title-prefix: "[MCP: SEP Tracker] "
    target: "*"
    max: 1
  add-labels:
    allowed: [NEEDS-ACTION]
    target: "*"
    max: 1
  remove-labels:
    allowed: [NEEDS-ACTION]
    target: "*"
    max: 1

network:
  allowed:
    - defaults
    - github

tools:
  github:
    toolsets: [default]
    min-integrity: none

timeout-minutes: 60

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

concurrency:
  group: mcp-sep-tracker
  cancel-in-progress: true

post-steps:
  - name: Write summary to step summary
    if: ${{ github.event_name == 'workflow_dispatch' && github.event.inputs.create_issue == 'false' }}
    shell: bash
    run: |
      if [ -f mcp-sep-tracker-report.md ]; then
        # Extract the Summary section from the report
        awk '/^## Summary/,/^## [^S]/' mcp-sep-tracker-report.md | sed '$ d' >> "$GITHUB_STEP_SUMMARY"
      else
        echo "⚠️ No SEP tracker report file was generated." >> "$GITHUB_STEP_SUMMARY"
      fi

  - name: Upload SEP tracker report artifact
    if: always()
    uses: actions/upload-artifact@v7.0.1
    with:
      name: mcp-sep-tracker-report
      path: mcp-sep-tracker-report.md
      if-no-files-found: ignore

on:
  permissions: {}
  schedule: "weekly on monday around 4:00am utc-5"
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

# MCP: SEP Tracker

Track Specification Enhancement Proposals (SEPs) in the MCP specification repository and assess their impact on the C# SDK. The published report belongs in **this** workflow's repository, not in `modelcontextprotocol/modelcontextprotocol` or `modelcontextprotocol/csharp-sdk`.

## Skill source

Read and follow `.github/skills/mcp-sep-tracker/SKILL.md` and any files it references. Treat those local files as the source of truth for the SEP tracking procedure and report structure.

The report must include a conformance-test check against `modelcontextprotocol/conformance` for every SEP listed in the report tables, and the tables must show this status clearly.

> ⚠️ **Workflow file overrides skill content.** The publishing contract, file paths, and title format defined in this workflow take precedence over any conflicting guidance in the skill or its references.

## Error Handling

If individual data sources fail, **note the failure and continue** — do not abort the
report. Use this consistent format inline within the affected section:

> ⚠️ **[Source]: Data unavailable** — [brief reason]

Apply this when a GitHub API call returns a rate-limit response (HTTP 403/429) or
repeated 5xx errors, or any other tool/API call fails after a reasonable retry.

The Summary section should briefly acknowledge any sources that were unavailable so
readers know the report is partial. Do **not** let a single failed source prevent the
rest of the report from being produced.

## Output Mode

Determine the output mode based on how this workflow was triggered:

- **Scheduled run** (`${{ github.event_name }}` is `schedule`): **Always create a GitHub issue** with the full report.
- **Manual run** (`${{ github.event_name }}` is `workflow_dispatch`):
  - If `${{ github.event.inputs.create_issue }}` is `true`: Create a GitHub issue with the full report.
  - If `${{ github.event.inputs.create_issue }}` is `false`: Write the full report to a file named **`mcp-sep-tracker-report.md`** in the repository workspace root using the `edit` or file-writing tool — **do not use shell heredocs** (`cat << EOF`), as the sandbox blocks heredocs containing shell expansion patterns. Do **not** create an issue. A post-step will handle uploading the artifact and writing the step summary.

**Issue title format:** After the `[MCP: SEP Tracker] ` prefix, use `Week of yyyy-MM-dd`
where the date is the **Monday of the current week** in UTC. Compute this using `bash`:
`date -u -d "today - $(( ($(date -u +%u) - 1) )) days" +%Y-%m-%d`
(this subtracts 0 days on Monday, 1 on Tuesday, … 6 on Sunday — always yielding the
current week's Monday). Example: `[MCP: SEP Tracker] Week of 2026-04-13`.

**Weekly lifecycle:** This workflow is scheduled weekly on Monday. Each Monday's run
creates a new issue with the current week's `Week of <Monday>` title, and
`close-older-issues` automatically closes prior weeks' `[MCP: SEP Tracker]` issues.
A manual `workflow_dispatch` rerun later in the same week resolves to the same
`Week of <Monday>` title; the safe-outputs system updates the existing issue rather
than creating a duplicate. Always compute the title from the current week's Monday —
never fall back to an earlier week's date to "catch up" on a missed Monday.

**`noop` is only for two cases:** (1) the user set `create_issue` to `false`, or
(2) you genuinely could not produce **any** report content. If
`mcp-sep-tracker-report.md` exists with usable content, **publish it** via
`create-issue` — do **not** fall back to `noop` because the GitHub MCP server or
another tool encountered a recoverable error during report generation. A partial
report with `⚠️ Data unavailable` notes is still a publishable report.

## `NEEDS-ACTION` Label

When the published report contains one or more entries in the **🚨 Action Items**
section (i.e., the section is not just "_No action items this week._"), the
issue must carry the `NEEDS-ACTION` label. When the section is empty, the label
must not be present.

How to apply, depending on which publishing path you take:

- **Creating a new issue** (`create_issue` — the normal weekly path): include
  `NEEDS-ACTION` in the `labels` array of the `create_issue` call when the
  condition above is met. The configured base labels (`automation`, `area-mcp`)
  are **merged** with this list automatically — do **not** repeat them. When
  the condition is not met, omit the `labels` field (or pass an empty array).

  ```json
  {
    "type": "create_issue",
    "title": "[MCP: SEP Tracker] Week of YYYY-MM-DD",
    "body": "...",
    "labels": ["NEEDS-ACTION"]
  }
  ```

  This is the only label-application path needed for the standard weekly run,
  because the agent does not receive the new issue number back from
  `create_issue` (the safe-outputs job assigns it post-agent).

- **Add / remove on a known existing issue** (`add_labels` / `remove_labels`):
  these tools are available for scenarios where the agent already has a
  specific issue number (`item_number`) — for example, a future workflow
  variant that updates an in-flight issue. They are restricted to the single
  `NEEDS-ACTION` label by the safe-outputs config. **Do not** use these tools
  to discover or modify pre-existing SEP-tracker issues — that would conflict
  with the constraints below (no `search_issues` / `list_issues` /
  `update-issue` against existing issues in the standard weekly flow).

## Constraints

- Never modify any repository (no comments, labels, issue edits, or PRs in the SDK repos).
- The SEP tracker issue always belongs in this workflow's repository, never in the spec or SDK repos.
- When `${{ github.event.inputs.create_issue }}` (`create_issue`) is `false`, never create or update any issue.
- Never call `update-issue`, `search_issues`, `list_issues`, or other discovery / mutation tools against existing issues — the only allowed publication action is a single `create-issue` call when an issue is being created.
