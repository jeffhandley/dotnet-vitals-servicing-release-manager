---
name: "MCP: Pulse"
description: >-
  Weekly cross-SDK pulse assessment for the MCP ecosystem covering all official
  MCP SDK repositories equally. The orchestrator launches per-SDK sub-agents,
  aggregates their assessments into a single cross-SDK report, and publishes
  one issue per week. Runs weekly on Monday and creates a new issue for the
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
    title-prefix: "[MCP: Pulse] "
    labels: [automation, area-mcp]
    close-older-issues: true   # Closes all prior [MCP: Pulse] issues
    close-older-key: mcp-pulse
    max: 1
  update-issue:
    title-prefix: "[MCP: Pulse] "
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

timeout-minutes: 90

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

concurrency:
  group: mcp-pulse
  cancel-in-progress: true

post-steps:
  - name: Write summary to step summary
    if: ${{ github.event_name == 'workflow_dispatch' && github.event.inputs.create_issue == 'false' }}
    shell: bash
    run: |
      if [ -f mcp-pulse-report.md ]; then
        # Extract the Summary section from the report
        awk '/^## Summary/,/^## [^S]/' mcp-pulse-report.md | sed '$ d' >> "$GITHUB_STEP_SUMMARY"
      else
        echo "⚠️ No pulse report file was generated." >> "$GITHUB_STEP_SUMMARY"
      fi

  - name: Upload pulse report artifact
    if: always()
    uses: actions/upload-artifact@v7.0.1
    with:
      name: mcp-pulse-report
      path: mcp-pulse-report.md
      if-no-files-found: ignore

on:
  permissions: {}
  schedule: "weekly on monday around 3:00am utc-5"
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

# MCP: Pulse

Produce a weekly cross-SDK pulse assessment for the MCP ecosystem. Treat all official MCP SDK repositories equally — no SDK receives special treatment. The published report belongs in **this** workflow's repository, not in any `modelcontextprotocol/*` repository.

## Skill source

Read and follow these files:

- `.github/skills/mcp-pulse/references/orchestration.md` — the step-by-step orchestration process for the top-level workflow agent
- `.github/skills/mcp-pulse/SKILL.md` — the per-SDK sub-agent instructions (used as the prompt template for sub-agents, **not** as the orchestrator's own instructions)
- Any additional files referenced under `.github/skills/mcp-pulse/references/`

Treat those local files as the source of truth for the pulse procedure and report structure.

> ⚠️ **Workflow file overrides skill content.** The publishing contract, file paths, and title format defined in this workflow take precedence over any conflicting guidance in the skill or its references.

This workflow agent is the **orchestrator**. Coordinate all SDK assessments, assemble **one** cross-SDK pulse report, write it to `mcp-pulse-report.md` in the repository workspace root, then publish it. Do not stop after a single SDK assessment.

Per-SDK sub-agents must **never** call safe-output tools (no `noop`, `create-issue`, etc.); they only return their `pulse-output` block to the orchestrator.

## Error Handling

If individual data sources fail, **note the failure and continue** — do not abort the
report. Use this consistent format inline within the affected section:

> ⚠️ **[Source]: Data unavailable** — [brief reason]

Apply this when:

- A GitHub API call returns a rate-limit response (HTTP 403/429) or repeated 5xx errors.
- A per-SDK sub-agent fails or returns no usable result.
- Any other tool/API call fails after a reasonable retry.

The Summary section should briefly acknowledge any sources that were unavailable so
readers know the report is partial. Partial results are acceptable — include whatever
SDKs succeeded and note which were skipped. Do **not** let a single failed source
prevent the rest of the report from being produced.

## Output Mode

Determine the output mode based on how this workflow was triggered:

- **Scheduled run** (`${{ github.event_name }}` is `schedule`): **Always create a GitHub issue** with the full report.
- **Manual run** (`${{ github.event_name }}` is `workflow_dispatch`):
  - If `${{ github.event.inputs.create_issue }}` is `true`: Create a GitHub issue with the full report.
  - If `${{ github.event.inputs.create_issue }}` is `false`: Write the full report to a file named **`mcp-pulse-report.md`** in the repository workspace root using the `edit` or file-writing tool — **do not use shell heredocs** (`cat << EOF`), as the sandbox blocks heredocs containing shell expansion patterns. Do **not** create an issue. A post-step will handle uploading the artifact and writing the step summary.

**Issue title format:** After the `[MCP: Pulse] ` prefix, use `Week of yyyy-MM-dd` where
the date is the **Monday of the current week** in UTC. Compute this using `bash`:
`date -u -d "today - $(( ($(date -u +%u) - 1) )) days" +%Y-%m-%d`
(this subtracts 0 days on Monday, 1 on Tuesday, … 6 on Sunday — always yielding the
current week's Monday). Example: `[MCP: Pulse] Week of 2026-04-13`.

**Weekly lifecycle:** This workflow is scheduled weekly on Monday. Each Monday's run
creates a new issue with the current week's `Week of <Monday>` title, and
`close-older-issues` automatically closes prior weeks' `[MCP: Pulse]` issues. A manual
`workflow_dispatch` rerun later in the same week resolves to the same `Week of <Monday>`
title; the safe-outputs system updates the existing issue rather than creating a
duplicate. Always compute the title from the current week's Monday — never fall back
to an earlier week's date to "catch up" on a missed Monday.

**`noop` is only for two cases:** (1) the user set `create_issue` to `false`, or
(2) you genuinely could not produce **any** report content. If `mcp-pulse-report.md`
exists with usable content, **publish it** via `create-issue` — do **not** fall back
to `noop` because the GitHub MCP server or another tool encountered a recoverable
error during report generation. A partial report with `⚠️ Data unavailable` notes
is still a publishable report.

## Constraints

- Treat the C# SDK as the primary interest but not as the reference example or authority.
- Do not treat a single SDK's pulse assessment as the final result. The deliverable is one combined cross-SDK report.
- Never modify any repository (no comments, labels, issue edits, or PRs in the SDK repos).
- The pulse issue always belongs in this workflow's repository, never in any `modelcontextprotocol/*` repository.
- When `${{ github.event.inputs.create_issue }}` (`create_issue`) is `false`, never create or update any issue.
- Never call `update-issue`, `search_issues`, `list_issues`, or other discovery / mutation tools against existing issues — the only allowed publication action is a single `create-issue` call when an issue is being created.
- Per-SDK sub-agents must never call safe-output tools — only the top-level orchestrator may.
