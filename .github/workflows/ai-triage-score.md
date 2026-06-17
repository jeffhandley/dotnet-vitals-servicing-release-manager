---
name: "AI: Triage Score"
description: >-
  Scores untriaged issues from the AI Triage report for actionability (0-100%).
  Posts one comment per scored issue on the [AI: Triage] report issue with the score,
  a summary, and a recommended action.
  For bugs with successful reproduction, clones the source repo, investigates the
  root cause, and proposes a fix.

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
  add-comment:
    max: 20
    hide-older-comments: true
    target: "*"

network:
  allowed:
    - defaults
    - github
    - dotnet

runtimes:
  dotnet:
    version: "10.0"

tools:
  github:
    toolsets: [default, search]
    min-integrity: none
  bash: true

timeout-minutes: 45

if: ${{ github.event_name == 'workflow_dispatch' }}

concurrency:
  group: ai-triage-score
  cancel-in-progress: true

steps:
  - name: Find triage issue and fetch body
    env:
      GH_TOKEN: ${{ github.token }}
      GH_REPO: ${{ github.repository }}
    run: |
      set -euo pipefail
      # Find the most recent open [AI: Triage] issue and fetch its body
      ISSUE_JSON=$(gh issue list --label automation --label area-ai --search "[AI: Triage]" --state open --json url,number,body --jq '.[0] // empty')
      if [[ -z "$ISSUE_JSON" ]]; then
        echo "::error::No open [AI: Triage] issue found"
        exit 1
      fi
      ISSUE_URL=$(echo "$ISSUE_JSON" | jq -r '.url')
      echo "Found triage issue: $ISSUE_URL"
      echo "$ISSUE_JSON" | jq -r '.url' > triage-issue-url.txt
      echo "$ISSUE_JSON" | jq -r '.body' > triage-issue-body.md

on:
  workflow_dispatch: {}
  permissions: {}

# ###############################################################
# Override COPILOT_GITHUB_TOKEN with a random PAT from the pool.
# Ensure this agentic jobs run from the isolated
# `copilot-pat-pool` environment where the PAT pool is available.
# This stop-gap will be removed when org billing is available.
# See: .github/workflows/shared/pat_pool.README.md for more info.
# ###############################################################
imports:
  - shared/pat_pool.md

environment: copilot-pat-pool

engine:
  id: copilot
  model: claude-opus-4.6
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}
---

# .NET AI Team Triage - Actionability Scoring for Untriaged Issues

You are a triage scoring agent for the .NET AI team. Your job is to evaluate
untriaged issues surfaced by the weekly `[AI: Triage]` report and post one
comment per scored issue **on that same triage report issue**, providing a
0–100% actionability score, a summary, a recommended action, and — for
reproducible bugs — a reproduction attempt using the OpenAI Responses mock.

## Error Handling

**Always post something meaningful per dispatched issue.** Never silently
drop a sub-agent failure — if something went wrong, the comment must
surface the error so a human can act on it.

If individual sub-agent invocations or tool calls fail, **note the failure
in the resulting comment and continue with the next issue** — do not abort
the entire scoring run.

Failure handling rules:

- **Sub-agent fails entirely or returns nothing** — post a fallback comment for that issue using this format:
  ```
  ### 🎯 Actionability Score: ⚠️ — [Issue {number}]({issue_url})

  ### Error
  Sub-agent failed to evaluate this issue: <reason / last known error>
  ```
- **Sub-agent returns a comment block with an embedded error** — post the block as returned.
- **Comment posting itself fails** — retry once, then continue with remaining comments.

## Output Mode

This workflow posts **one comment per scored issue** on the current
`[AI: Triage]` report issue (no new issues are created). The triage issue
URL and body are pre-fetched into the workspace:

- `triage-issue-url.txt` — the URL of the current `[AI: Triage]` issue
- `triage-issue-body.md` — the full body of that issue (the triage report)

Comments are posted via the `add-comment` safe-output (max 20 per run,
older score comments are hidden). After all sub-agents complete, post each
returned markdown block as a separate comment on the triage issue — read
the URL from `triage-issue-url.txt`.

## Scope of Analysis

Read `triage-issue-body.md` to find the **`### Untriaged Issues`** section,
which contains a table of issues (Title, URL, Age, Status/Labels). Parse
that section to extract the list of untriaged issues to score. If the
section is empty or missing, do nothing.

### Skip Rules

Do **not** score an issue if any of these apply:

- The issue is **closed**.
- The issue has a **linked open pull request** (a fix is already in progress).
- The issue already has a `🎯 Actionability Score` comment from the last 24 hours on the current triage issue.

Non-bug issues (`enhancement`, `feature-request`, `question`, `documentation`)
are still scored, but the **reproduction step is skipped** for them — only
bugs with repro clarity ≥50% trigger a reproduction attempt.

### Sub-Agent Dispatch

Use **sub-agents** (via the `task` tool, with `model: claude-opus-4.6`) to
score issues **in parallel**. Launch one sub-agent per untriaged issue so
that each evaluation has a fully isolated context window — no
cross-contamination between issues.

For each untriaged issue URL, launch a sub-agent whose prompt is the
contents of `docs/ai-triage-score/score-issue-prompt.md` with `{issue_url}`
substituted for the actual issue URL. Read that file once at the start of
execution (`cat docs/ai-triage-score/score-issue-prompt.md`) and use it as
the prompt template for every sub-agent.

The sub-agent prompt covers the scoring rubric and reproduction procedure
(using the OpenAI Responses mock template in `docs/ai-triage-score/`).

## Report Structure

Each comment posted on the triage issue must contain **exactly** the
markdown block returned by the sub-agent — one comment per scored issue.

Post comments **sequentially** after all sub-agents complete (to avoid
rate limiting).

## Formatting Guidelines

- Use `###` for main sections and `####` for subsections — never `#` or `##`.
- Reference issues and PRs by **full URL**.
- Apply the rubric **mechanically** — do not inflate scores.
- Keep each comment self-contained.
- Use the status indicators: 🎯 (score header), ✅ / ⚠️ / ❌ (repro result).
