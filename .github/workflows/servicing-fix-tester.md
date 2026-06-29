---
name: "Servicing: Fix Tester"
description: >
  Verify a repro across any two SDK versions -- two servicing patches, two majors, or a GA vs a preview
  of an upcoming major. Explicit mode takes a baseline and candidate SDK directly; servicing scan
  (managed repos in references/repos.md; currently dotnet/runtime) finds merged fixes that have flowed
  into a daily SDK and runs the unchanged repro on baseline vs that daily build, updating the per-fix
  tracking issue with a verdict. Runs daily for the scan or on demand for explicit verification. It
  never comments on the product repo and adds no labels there.

on:
  schedule: "daily"
  workflow_dispatch:
    inputs:
      target:
        description: "Optional: an explicit issue/PR as org/repo#number to verify (any repo)."
        required: false
        type: string
      baseline_sdk:
        description: "Optional: baseline SDK (expected buggy). Empty = auto-discover the prior GA."
        required: false
        type: string
      candidate_sdk:
        description: "Optional: candidate SDK (expected fixed). Empty = auto-discover the version the fix is in."
        required: false
        type: string
      post_to_target:
        description: "Manual-only: also post the verdict as a comment on the target issue/PR in its home repo."
        required: false
        type: boolean
        default: false
      managed_repo:
        description: "Optional: a single managed-repo key to handle (e.g. runtime). Empty = all enabled."
        required: false
        type: string
  permissions: {}

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

permissions:
  contents: read
  issues: read
  pull-requests: read
  actions: read

concurrency:
  group: "servicing-fix-tester"
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
    toolsets: [pull_requests, repos, issues, actions]
    min-integrity: approved
  edit:
  bash: ["dotnet", "git", "curl", "jq", "tee", "sed", "awk", "grep", "head", "tail", "cat", "ls", "find", "mkdir", "rm", "cp", "mv", "chmod", "echo", "date", "env", "test", "bash", "sh", "mktemp", "wc", "cut", "tr", "sort", "uniq", "xargs", "basename", "dirname", "printf", "pwd", "unzip", "gh"]

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
  update-issue:
    target: "*"
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

timeout-minutes: 60
---

# Servicing Fix Tester

Verify a repro across **any two SDK versions** -- two servicing patches, two majors, or a GA vs a
preview of an upcoming major -- using the **`servicing-release` skill**
(`.github/skills/servicing-release/SKILL.md`). Read that skill and follow **Procedure B**. You **read**
repos cross-repo; you **record** results only here, by updating the per-fix tracking issue in
`${{ github.repository }}`. You never comment on a product repo and add no labels there.

## Mode

- **Explicit verification (takes precedence)** -- if `target` (`org/repo#number`) is set, handle ONLY it and do NOT scan; verify that issue/PR's repro.
  `baseline_sdk` and `candidate_sdk` are used if given; otherwise **auto-discover** -- candidate = the
  version the fix is in (fix-flow detection / next release), baseline = the prior version that lacks it.
  Then skip to *For each fix ready to verify*.
- **Servicing scan** -- otherwise, scan the managed repos below.

**Cross-repo posting (manual only).** If `${{ github.event.inputs.post_to_target }}` is `true` **and**
this is `workflow_dispatch`, also post the verdict as a comment on the target issue/PR via the
**add-comment** safe-output (set its repo to the home repo from `target`). Cross-repo posting requires
`GH_AW_GITHUB_MCP_SERVER_TOKEN` to be a PAT with write to the home repo; the agent never receives it.
On schedules never post cross-repo -- only update the tracking issue here.

## Managed repositories (servicing scan)

Load `.github/skills/servicing-release/references/repos.md`. Handle each **enabled** row,
loading that row's plugin reference (e.g. `references/repo-runtime.md`) for the target product repo, branch
pattern, classification rule, SDK bands, and fix-flow specifics. If `managed_repo` input is set, handle
only that key.

## Selecting fixes to verify

For each managed repo, find the per-fix tracking issues in `${{ github.repository }}` (the `[Servicing]`
issues this project created, excluding the dashboard) whose body shows **Fix verification: pending**.
For each, read the referenced product-repo PR. Keep it only if:

- the PR is **merged**; **and**
- the fix has **flowed into a daily SDK** for the target band (use the skill's *fix-flow detection* with
  the plugin's specifics -- confirm the merge commit exists in the product repo and is an ancestor of
  the daily SDK's runtime commit). If it has **not** flowed (or the merge commit is unknown to the
  product repo), leave the issue as pending and move on -- a later run will pick it up.

Handle up to **3** such fixes this run. If none are ready, call `noop` with a one-line summary and stop.

## For each fix ready to verify

Use a per-fix working directory (replace `<KEY>` and `<PR>`):

```bash
export WORKDIR="/tmp/gh-aw/agent/servicing-fix/<KEY>-pr-<PR>"
rm -rf "$WORKDIR"; mkdir -p "$WORKDIR"; cd "$WORKDIR"
```

1. **Obtain the repro unchanged.** Download the producer's `servicing-repro-<KEY>-pr-<PR>` artifact when
   available; otherwise re-derive it identically per Procedure A steps 1-3. Do **not** change it.
2. **Run Procedure B**: baseline GA SDK (still buggy) then the fixed daily SDK, capturing version-named
   logs and saving `$WORKDIR/step-summary.md`. Render a verdict (Verified fixed / Not fixed /
   Inconclusive).
3. **Upload the artifact** for this fix: `upload_artifact` name `servicing-fix-<KEY>-pr-<PR>`, path
   `$WORKDIR`.
4. **Update the tracking issue.** Call `update_issue` targeting that per-fix issue, replacing the
   **Fix verification: pending** line with a verification section: the verdict; the **Expected** result;
   the **Actual before** (with the baseline SDK version); the **Actual after** (with the fixed SDK
   version); and a link to this run's artifact. Keep the existing repro section intact.

## Finish

Provide a clear final summary of each fix handled (flowed yes/no, verdict). gh-aw surfaces your final
report as the run summary. Also write it to `$GITHUB_STEP_SUMMARY` **best-effort**: the agentic sandbox
often makes that file unwritable, which is expected -- when it is, rely on the final report and the
`step-summary.md` included in the artifact, and do **not** report it as a missing tool. **If this run
updates no issues**, you MUST call `noop` with a one-line summary. All work happens under the per-fix
`$WORKDIR`.
