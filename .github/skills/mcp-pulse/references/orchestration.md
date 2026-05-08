# Pulse Orchestration Process

This file describes the orchestration process for the MCP SDK Pulse workflow.
The workflow agent reads this file and follows the steps sequentially.

> 🚨 **This is a REPORT-ONLY process.** Do not post comments, change labels,
> close issues, or modify anything in any repository. Only read operations
> are allowed.

## Step 1: Fetch SDK Tier Data

Fetch the live `sdk-tiers.mdx` from:
```
https://raw.githubusercontent.com/modelcontextprotocol/modelcontextprotocol/refs/heads/main/docs/community/sdk-tiers.mdx
```

Also fetch the live SDK list from:
```
https://raw.githubusercontent.com/modelcontextprotocol/modelcontextprotocol/refs/heads/main/docs/docs/sdk.mdx
```

Cross-check the SDK repos and tiers against the assumptions in
[references/cross-sdk-repos.md](references/cross-sdk-repos.md). If discrepancies
are found, note them in a preamble note in the report and use the **live data**
as authoritative.

## Step 2: Prepare Cross-Reference Themes

Read the canonical theme taxonomy from
[references/cross-sdk-repos.md](references/cross-sdk-repos.md). These themes and
their search keywords will be passed to every sub-agent so theme classification
is consistent across all SDKs.

## Step 3: Launch Per-SDK Sub-Agents

For each SDK repository listed in [references/cross-sdk-repos.md](references/cross-sdk-repos.md),
launch a sub-agent using the `task` tool with:

- The instructions from `SKILL.md` (the sub-agent skill in this same directory)
- The SDK repository, name, and tier
- The full cross-reference theme taxonomy with search keywords

Each sub-agent must return only its fenced `pulse-output` block for that SDK. It must not call `noop`, `create-issue`, or any other safe-output tool, and it must not write any report files.

**Concurrency strategy — two waves** to avoid GitHub API rate limits:

- **Wave 1 — Tier 1 SDKs**: launch all Tier 1 SDKs in parallel.
- **Wave 2 — Remaining SDKs**: launch after Wave 1 completes (or after 5 minutes,
  whichever comes first).

If rate-limit errors occur during Wave 1, wait 30 seconds before launching Wave 2.
If errors persist, proceed with the results already collected and note the skipped
SDKs in the report.

> ⚠️ **One sub-agent per SDK.** Do not batch multiple SDKs into a single sub-agent.

**Timeout handling:** If a sub-agent has not completed after 10 minutes, proceed
with the results you have. Note skipped SDKs in the report.

**Fallback:** If the `task` tool is not available, fall back to sequential analysis
using the same criteria from `SKILL.md`.

## Step 4: Aggregate Results

Collect all sub-agent outputs (the `pulse-output` structured blocks). Parse each to
build the report:

1. **Common Themes matrix** — for each theme, mark ☑️ if the SDK reported `yes`.
   - The table columns are **Tier 1 SDKs only**, with **C# in the leftmost column**
     followed by TypeScript, Python, Go.
   - Below the table, list evidence bullets for each prevalent theme. The C# SDK is
     always the first bullet for each theme and its label is bold; all other SDKs
     follow in tier order (Tier 1 → Tier 2 → Tier 3 → TBD), alphabetical within each
     tier, and are not bolded.

2. **Summary metrics** — derive from the matrix:
   - count of prevalent themes (any SDK with ≥2 matching open issues);
   - count of prevalent themes affecting the C# SDK (and brief list of names);
   - count of themes prevalent across all four Tier 1 SDKs (cross-cutting);
   - the single most notable theme affecting the C# SDK (top concern), with a brief
     reason drawn from the C# evidence bullets;
   - how the C# SDK compares to peer Tier 1 SDKs in theme count.

## Step 5: Generate Report

Produce the pulse report as GitHub-flavored markdown following the template in
[references/report-format.md](references/report-format.md).

Save the completed report as a markdown file in the session. Also save it to
**`mcp-pulse-report.md`** in the repository workspace root for the post-step to
surface as an artifact.

Before the workflow completes, verify that `mcp-pulse-report.md` exists and is
non-empty. Only after that may the top-level workflow agent publish via
`create-issue` (per the workflow's publishing contract) or, when issue creation
is disabled, call `noop`.

The calling workflow handles publishing the report as a GitHub issue. The
title format and the single `create-issue` call are defined in the workflow
file (`.github/workflows/mcp-pulse.md`) and **take precedence** over any
guidance in this skill.
