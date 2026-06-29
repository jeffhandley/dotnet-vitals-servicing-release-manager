---
name: "Servicing: Validation Tracker"
description: >
  Maintain a release-level servicing-validation dashboard issue for the product repositories managed by
  the servicing-release system (see references/repos.md; currently dotnet/runtime). Runs daily
  to aggregate the per-fix tracking issues this project created (repro produced, fix verified) into a
  single dashboard issue per managed repo, grouped by release branch. It reads only; it posts no
  product-repo comments and adds no labels there.

on:
  schedule: "daily"
  workflow_dispatch:
  permissions: {}

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

permissions:
  contents: read
  issues: read
  pull-requests: read

concurrency:
  group: "servicing-validation-tracker"
  cancel-in-progress: false

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
  model: claude-opus-4.8
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

tools:
  github:
    toolsets: [pull_requests, repos, issues]
    min-integrity: approved
  bash: ["gh", "jq", "date", "echo", "sed", "awk", "grep", "head", "tail", "cat", "sort", "uniq", "cut", "tr", "wc", "test", "xargs", "printf"]

checkout: false

network:
  allowed:
    - defaults
    - github

safe-outputs:
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  mentions: false
  allowed-github-references: []
  create-issue:
    title-prefix: "[Servicing] "
    labels: [automation, area-servicing]
    max: 1
  update-issue:
    target: "*"
    max: 1

timeout-minutes: 20
---

# Servicing Validation Tracker

Maintain a release-level **Servicing Validation dashboard issue** for the product repositories this
monitoring repo manages. You aggregate the per-fix tracking issues that **`servicing-repro-producer`**
and **`servicing-fix-tester`** already created/updated in `${{ github.repository }}` -- you produce no
repros, install no SDKs, post no product-repo comments, and add no labels there. Use the
**`servicing-release` skill** only for its PR classification framing; ignore its repro/verify procedures.

## Managed repositories

Load `.github/skills/servicing-release/references/repos.md` to know the enabled repos and their
keys (e.g. `runtime`).

## 1. Find the dashboard issue

The dashboard is a single open issue in `${{ github.repository }}` whose title (after the `[Servicing] `
prefix) is exactly `Validation Dashboard`. Search this repo's issues for that title. Remember whether it
exists and its number -- you will **update** it; otherwise you will **create** it.

## 2. Collect the per-fix tracking issues

List the open `[Servicing]` issues in `${{ github.repository }}` **except** the dashboard. Each
represents one servicing fix and carries (from the producer/tester): the managed-repo key, the
product-repo PR reference and link, the target release branch, whether a **repro** was produced, and the
**fix-verification** status (pending, Verified fixed, Not fixed, or Inconclusive). You may read these
issues with `gh` (you are reading this project's own tracking issues). Derive per fix:

- **Fix** -- the managed-repo key + product-repo PR link + short title, and a link to the tracking issue.
- **Release** -- the target `release/MAJOR.MINOR` branch.
- **Repro** -- produced (the tracking issue exists with a repro section).
- **Fix verified** -- ✅ Verified fixed, ❌ Not fixed, ⚠️ Inconclusive, or ⏳ pending.

## 3. Render the dashboard body

Group by managed repo, then by `release/MAJOR.MINOR` (newest major first). Render a compact table per
group:

```
## runtime — release/10.0

| Fix | Repro | Fix verified | Tracking |
|-----|-------|--------------|----------|
| dotnet/runtime#NNNN title | ✅ | ⏳ pending | #<tracking-issue> |
```

Begin with **Last updated: <UTC timestamp>** and a one-line summary (fixes tracked, repros produced,
fixes verified). End with a note that the dashboard is maintained automatically by
`servicing-validation-tracker` and that per-fix detail (repro snippet, Expected/Actual, baseline/fixed
logs) lives in each per-fix tracking issue. Keep the body well under 60 KB.

## 4. Create or update the dashboard

- If it does **not** exist, `create-issue` with title `Validation Dashboard` (the `[Servicing] ` prefix
  is added automatically) and the rendered body.
- If it **exists**, `update-issue` targeting that number, replacing its body with the new dashboard.

If there are **no** per-fix tracking issues and no dashboard exists, call `noop` with a one-line summary
instead of creating an empty dashboard.

## Finish

Provide a clear final summary (fixes tracked, repros, verdicts, dashboard created/updated with its
number). gh-aw surfaces it as the run summary; also write to `$GITHUB_STEP_SUMMARY` best-effort and, if
the sandbox makes it unwritable, do **not** report it as a missing tool. If you took no action, call
`noop` with a one-line reason.
