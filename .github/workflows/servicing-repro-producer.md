---
name: "Servicing: Repro Producer"
description: >
  Produce and verify a minimum reproduction for any issue or PR (explicit target in any repo, or a
  servicing scan of the managed repos in references/repos.md; currently scoped to dotnet/runtime).
  Walks a backport PR to its main PR and original issue. Runs hourly for the servicing scan, or on
  demand for an explicit target. Installs the baseline SDK, authors a minimal repro, runs it to confirm
  the bug, uploads the repro + output.log as an artifact, and records the result as a per-fix tracking
  issue in this monitoring repository. It never comments on the product repo and adds no labels there.

on:
  schedule: "hourly"
  workflow_dispatch:
    inputs:
      target:
        description: "Optional: an explicit issue/PR as org/repo#number (any repo). Empty = servicing scan."
        required: false
        type: string
      baseline_sdk:
        description: "Optional: baseline SDK version for an explicit target (else auto-discover latest GA of the major)."
        required: false
        type: string
      post_to_target:
        description: "Manual-only: also post the repro as a comment on the target issue/PR in its home repo."
        required: false
        type: boolean
        default: false
      managed_repo:
        description: "Optional: a single managed-repo key to handle (e.g. runtime). Empty = all enabled."
        required: false
        type: string
      pr_number:
        description: "Optional: a single product-repo PR number to build a repro for (requires managed_repo)."
        required: false
        type: string
  permissions: {}

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

permissions:
  contents: read
  issues: read
  pull-requests: read

concurrency:
  group: "servicing-repro-producer"
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
  edit:
  bash: ["dotnet", "git", "curl", "jq", "tee", "sed", "awk", "grep", "head", "tail", "cat", "ls", "find", "mkdir", "rm", "cp", "mv", "chmod", "echo", "date", "env", "test", "bash", "sh", "mktemp", "wc", "cut", "tr", "sort", "uniq", "xargs", "basename", "dirname", "printf", "pwd", "gh"]

checkout: false

network:
  allowed:
    - defaults
    - github
    - dotnet
    - "aka.ms"

safe-outputs:
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  mentions: false
  allowed-github-references: []
  create-issue:
    title-prefix: "[Servicing] "
    labels: [automation, area-servicing]
    max: 3
  add-comment:
    target: "*"
    max: 3
  upload-artifact:
    max-uploads: 3
    retention-days: 30
    allowed-paths:
      - "/tmp/gh-aw/agent/**"
    defaults:
      if-no-files: "ignore"

timeout-minutes: 45
---

# Servicing Repro Producer

Produce and verify a minimum reproduction for **any issue or PR** -- a single explicit target (any
repo), or a servicing scan of the managed repos -- using the **`servicing-release` skill**
(`.github/skills/servicing-release/SKILL.md`). Read that skill and follow **Procedure A**. When the
target is a backport PR, **walk** to its `main` PR and the original issue for the clearest scenario.
You **read** repos cross-repo; you **record** results only here, as tracking issues in
`${{ github.repository }}`. You never comment on a product repo and add no labels there.

## Mode

- **Explicit target (takes precedence)** -- if `target` (`org/repo#number`) is set, handle ONLY it and do NOT scan; build a repro for that single issue/PR
  in any repo (no servicing classification). Walk a backport PR to its `main` PR and the linked issue.
  Baseline SDK = `baseline_sdk` if given, else **auto-discover** the latest GA of the relevant major.
  This needs no registry/plugin. Then go to *For each selected PR*.
- **Servicing scan** -- otherwise, scan the managed repos below.

**Cross-repo posting (manual only).** If `${{ github.event.inputs.post_to_target }}` is `true` **and**
this is a `workflow_dispatch` run, additionally post the repro as a comment on the target issue/PR via
the **add-comment** safe-output (set its repo to the home repo from `target`). Cross-repo posting
requires `GH_AW_GITHUB_MCP_SERVER_TOKEN` to be a PAT with write to the home repo; the agent never
receives that token. On schedules never post cross-repo -- only record here.

## Managed repositories (servicing scan)

Load `.github/skills/servicing-release/references/repos.md`. Handle each **enabled** row,
loading that row's plugin reference (e.g. `references/repo-runtime.md`) for the **target product repo**, its
branch pattern, classification rule, SDK bands, and fix-flow specifics. If `managed_repo` input is set,
handle only that key; if `pr_number` is also set, handle only that one PR in that repo.

## Selecting PRs (scan mode)

For each managed repo, query its **open** pull requests targeting `release/*` branches (include
`release/*-staging`), updated within roughly the last 30 days. Read PR metadata/bodies through the
integrity-gated `github` tool (skip `[Filtered]` items, record the count). Keep a PR only if:

- it is **rule-in** by that repo's plugin classification rule (else skip it); **and**
- it has **no** existing per-fix tracking issue in `${{ github.repository }}` yet. Identify one by
  searching this repo's issues for the product-repo PR reference (e.g. `dotnet/runtime#NNNN`) in a
  `[Servicing]` issue. (gh-aw appends a footer identifying this workflow; the tracking issue body also
  carries the canonical PR URL.)

Across all managed repos, handle up to **3** such PRs this run (oldest-updated first). If none qualify,
call `noop` with a one-line summary and stop.

## For each selected PR

Use a per-PR working directory outside any checkout so each repro uploads separately (replace `<KEY>`
and `<PR>`):

```bash
export WORKDIR="/tmp/gh-aw/agent/servicing-repro/<KEY>-pr-<PR>"
rm -rf "$WORKDIR"; mkdir -p "$WORKDIR"; cd "$WORKDIR"
```

Determine the target `MAJOR.MINOR` from the PR's base branch (per the plugin), then:

1. **Classify** (single-PR mode only -- already done in scan mode). If ruled out, skip; in single-PR
   mode call `noop` with the reason.
2. **Produce + verify** the repro per Procedure A on the **baseline GA SDK** for the target major.
   Capture combined output to `$WORKDIR/output.log` and save `$WORKDIR/step-summary.md`. If the bug
   does **not** reproduce, record that and do **not** create a tracking issue for this PR.
3. **Upload the artifact** for this PR: call `upload_artifact` with name
   `servicing-repro-<KEY>-pr-<PR>` and path `$WORKDIR`.
4. **Record a tracking issue** (if the bug reproduced). Call `create_issue` (the `[Servicing] ` prefix
   is added automatically) with a title like `<KEY> · <product-repo>#<PR> -- <short bug title>` and a
   body containing, in order: (1) a link to the product-repo PR (canonical URL) and the target
   major.minor; (2) a 1-2 sentence issue description; (3) which repro approach was used; (4) the
   isolating code snippet; (5) **Expected Result**; (6) **Actual Result** (quoted from `output.log`);
   (7) a link to the uploaded artifact; and (8) a **Fix verification: pending** line for the tester to
   update later. (gh-aw appends a footer identifying this workflow.)

## Finish

Provide a clear final summary of each PR handled (repro form, reproduced yes/no, issue created). gh-aw
surfaces your final report as the run summary. Also write it to `$GITHUB_STEP_SUMMARY` **best-effort**:
the agentic sandbox often makes that file unwritable, which is expected -- when it is, rely on the
final report and the `step-summary.md` you included in each PR's artifact, and do **not** report it as
a missing tool. **If this run records no issues** (nothing qualified, all ruled out, or none
reproduced), you MUST call `noop` with a one-line summary. All repro work happens under the per-PR
`$WORKDIR`.
