---
name: "MCP: Triage"
description: >-
  Daily triage report for the C# MCP SDK team covering ModelContextProtocol,
  ModelContextProtocol.Code, ModelContextProtocol.AspNetCore, and other libraries
  introduced as part of the C# MCP SDK. Context from other MCP SDKs is pulled in
  to identify themes and patterns across the platforms. Runs daily, updating the
  current week's issue until Monday when a new issue is created.

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
    title-prefix: "[MCP: Triage] "
    labels: [automation, area-mcp]
    close-older-issues: true   # Closes all prior [MCP: Triage] issues
    close-older-key: mcp-triage
    max: 1
  update-issue:
    title-prefix: "[MCP: Triage] "
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
  group: mcp-triage
  cancel-in-progress: true

post-steps:
  - name: Write executive summary to step summary
    if: ${{ github.event_name == 'workflow_dispatch' && github.event.inputs.create_issue == 'false' }}
    shell: bash
    run: |
      if [ -f mcp-triage-report.md ]; then
        # Extract the Executive Summary section from the report
        awk '/^### Executive Summary/,/^### [^E]/' mcp-triage-report.md | sed '$ d' >> "$GITHUB_STEP_SUMMARY"
      else
        echo "⚠️ No triage report file was generated." >> "$GITHUB_STEP_SUMMARY"
      fi

  - name: Upload triage report artifact
    if: ${{ github.event_name == 'workflow_dispatch' && github.event.inputs.create_issue == 'false' }}
    uses: actions/upload-artifact@v7.0.1
    with:
      name: mcp-triage-report
      path: mcp-triage-report.md
      if-no-files-found: warn

on:
  permissions: {}
  schedule: "daily around 5:00am utc-5"
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

# MCP: Triage

Perform issue triage for the C# SDK. Always use the workflow, skill, and reference files from the checked-out repository workspace. The issue data target is cross-repository: always triage `modelcontextprotocol/csharp-sdk`. Any created or updated report issue belongs in the repository where this workflow is running.

## Skill source

Read and follow these files:

- `.github/skills/mcp-triage/SKILL.md`
- Any files the skill references under `.github/skills/mcp-triage/references/`

Treat those local files as the source of truth for the triage procedure, safety rules, prioritization logic, and report structure. Do not change the target repository. Always triage `modelcontextprotocol/csharp-sdk`.

> ⚠️ **Workflow file overrides skill content.** The publishing contract, file paths, and title format defined in this workflow take precedence over any conflicting guidance in the skill or its references.

## Prioritization override

Put the following at the **very top** of the report whenever present, ahead of routine SLA or labeling work:

1. probable regressions;
2. publicly visible security-sensitive issues;
3. anything likely to disrupt customer production systems, deployments, auth flows, or transport connectivity.

Look for issues or PRs that mention a specific SDK/library/package version (for example, "after upgrading to 0.3.2" or "regressed in 0.4.1"). Cross-reference those version signals against:

- the `modelcontextprotocol/csharp-sdk` releases page;
- release notes / changelogs; and
- package versions on NuGet.org to determine release dates.

Start by identifying the **most recent release** from `https://github.com/modelcontextprotocol/csharp-sdk/releases`. If there is evidence that the most recent release may have caused customer disruption, a regression, or a public security-sensitive concern, make that **VERY prominent** in the report summary and urgent-attention section.

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

**Issue title format:** After the `[MCP: Triage] ` prefix, use `Week of yyyy-MM-dd` where
the date is the **Monday of the current week** in UTC. Compute this using `bash`:
`date -u -d "today - $(( ($(date -u +%u) - 1) )) days" +%Y-%m-%d`
(this subtracts 0 days on Monday, 1 on Tuesday, … 6 on Sunday — always yielding the
current week's Monday). Example: `[MCP: Triage] Week of 2026-04-13`.

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

## Constraints

- Treat the C# SDK as the primary interest but not as the reference example or authority.
- Do not treat a single SDK's pulse assessment as the final result. The deliverable is one combined cross-SDK report.
- Never modify any repository (no comments, labels, issue edits, or PRs in the SDK repos).
- When `${{ github.event.inputs.create_issue }}` (`create_issue`) is `false`, never create or update any issue.
- Never call `update-issue`, `search_issues`, `list_issues`, or other discovery / mutation tools against existing issues — the only allowed publication action is a single `create-issue` call when an issue is being created.
- Use the skill's output as the published content verbatim.
