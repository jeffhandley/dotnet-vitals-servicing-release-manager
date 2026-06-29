---
name: servicing-release
description: >
  Produce and verify minimal reproductions for .NET servicing-release fixes (release/* pull requests in
  a managed product repo such as dotnet/runtime). USE FOR: deciding whether a release/* PR needs a
  repro, building a minimum reproduction from a servicing PR/issue, installing specific .NET SDKs
  locally to reproduce a bug, and verifying that a merged fix resolves the issue across a baseline SDK
  (exhibits the bug) and a fixed SDK (contains the fix). Repo-agnostic core; per-repo policy lives in
  references/repos.md and references/<repo>.md. DO NOT USE FOR: commenting on the product repo
  (the system records results as issues in the monitoring repo; this skill never writes to GitHub),
  general code review, or validating non-servicing changes that target main.
---

# Repro & Fix-Validation Skill

This skill turns **any issue or pull request** into a **minimum reproduction** of the bug it
describes, runs that repro to confirm the bug, and verifies a fix by running the **same repro on two
SDK versions** -- a baseline (still buggy) and a candidate (expected fixed). The two SDKs can be any
versions: two servicing patches, two majors, or a GA vs a preview of an upcoming major.

Two general procedures:
- **Procedure A -- produce a repro** for an issue/PR. The target may be a backport PR; **walk** it to
  the original `main` PR (`(#NNNNN)` / "Backport of #NNNNN") and the linked issue for the clearest
  scenario. Build on a baseline SDK that exhibits the bug.
- **Procedure B -- verify a fix** by running the unchanged repro on a baseline vs a candidate SDK and
  comparing to Expected. The two SDK versions are inputs to the procedure.

**Servicing is one configured use** of these procedures, and the system is **repo-pluggable**: the
servicing scan + SDK-selection policy live in **[`references/repos.md`](references/repos.md)** (the
registry) and a per-repo plugin such as **[`references/repo-runtime.md`](references/repo-runtime.md)**
(target repo, branch pattern, classification, SDK bands, fix-flow). Load those only for servicing; for
an explicit issue/PR + two SDKs, neither is required. Currently **scoped to `runtime`**.

It is used in two execution contexts that share all of the core logic below:

- **Interactive** (the `servicing-release` Copilot CLI agent): read PRs/issues for context, produce
  **local artifacts only**, and report findings to the user. **Never** write to GitHub.
- **Agentic workflows** (`servicing-repro-producer`, `servicing-fix-tester`,
  `servicing-validation-tracker`): do the same core work; the **workflow** records results as tracking
  issues in the monitoring repo via safe-outputs. This skill itself never writes to GitHub.

## Operating contract (applies in every context)

1. **Never write to GitHub from this skill.** No comments, issues, PRs, labels, or edits to any
   repository. Reading PR/issue/comment/code content for understanding is fine.
2. **Produce local artifacts only.** Author the repro and capture its output under a fresh, empty
   working directory **outside any git repository**:
   ```bash
   WORKDIR="${WORKDIR:-$(mktemp -d)}"; cd "$WORKDIR"   # caller may pre-set WORKDIR; never author inside a checkout
   ```
   Keep the repro sources, the project, and `output.log` here so the caller can collect them.
3. **Respect a user-supplied SDK.** If the caller has already provisioned an SDK (a path is given, or
   `DOTNET_ROOT`/`PATH` already point at a `dotnet` to use), **do not** discover, download, or install
   any SDK, and **do not** inspect that SDK's bits, version, or embedded commit. Just run the repro
   with it, capture the output, and report. (This supports validating an unreleased build.) Otherwise,
   install SDKs locally as described in *Reference: SDK installation*.
4. **Determinism & isolation.** Install SDKs with `--install-dir` + `--no-path` (no global machine
   changes). Set `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `DOTNET_NOLOGO=1`,
   `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`. Never modify any product repo or global SDK.
5. **The environment is online; the bash harness only gates *ad-hoc* shell.** The runner has full
   network to the firewall-allowed dotnet domains: **`dotnet restore`/`dotnet test` fetch packages from
   nuget.org**, and **`dotnet-install` installs any SDK** (including .NET 10) -- do not assume offline.
   The harness only denies *bare* `curl <url>`, `;`-chained compounds, and long pipelines, so:
   - **Read GitHub-hosted data via the `github` MCP**, not `curl`: commit existence/ancestry
     (`get_commit`, compare) and raw files such as `dotnet/dotnet`'s `src/source-manifest.json`
     (`get_file_contents`). This is the robust primary path for fix-flow detection.
   - **Check pre-provisioned SDKs first** (`dotnet --list-sdks`); install missing ones with
     `bash "$WORKDIR/dotnet-install.sh" --version <ver> --install-dir … --no-path` (a single command).
   - Online package restore is fine -- the **unit test (preferred)** and **file-based** forms work here.
   - Prefer **one simple command per step**; avoid `;`-chaining, inline `$(…)` you can replace by
     writing to a file and reading it back, and long pipelines.
   Interactively (Copilot CLI), these gates do not apply -- use whatever commands you need.

---

## When to build a repro -- target selection

For an **explicit** issue or PR, just build a repro -- no gating. **Servicing scan mode** is selective:
a `release/*` PR earns a repro only if it is a **product bug fix** (most are code-flow,
infrastructure, branding, or test-only churn). That rule is repo-specific -- apply the classification
from the managed repo's plugin (e.g. [`references/repo-runtime.md`](references/repo-runtime.md) *PR
classification rule*). Reading labels is fine; this system never adds labels to the product repo.

---

## Procedure A -- Produce a minimum repro (and confirm the bug)

Inputs: an issue or PR (any repo) and a baseline SDK selector (an explicit version, or "latest GA of
major.minor"). For servicing, the target is a `release/*` PR and the baseline is its major's latest GA.

1. **Classify** (servicing scan only): apply the plugin rule and stop if ruled out. For an explicit
   issue/PR, skip classification and proceed.
2. **Understand the bug.** Read the PR title, body (the *Customer Impact* template: "Customer
   reported", "Regression?", `Fixes #NNNN`, expected vs actual), linked issues, review comments, and
   the fix diff. **Trace the backport:** release PRs are usually backports -- follow the original
   `main` PR (title `(#NNNNN)` or body "Backport of #NNNNN") and its linked issue for the clearest bug
   description, customer scenario, and any shared code snippet. Distill:
   - a 1-2 sentence **issue summary**;
   - the **Expected** result;
   - the **Actual** (buggy) result;
   - the smallest **call site** that triggers it.
3. **Choose the repro form** (most-preferred first; pick the simplest that isolates the bug *and* is
   runnable on the target SDK -- see *Reference: repro forms*):
   1. a **unit test** (`dotnet new xunit`) whose assertion encodes the **Expected** behavior, so it
      **fails** on the buggy baseline and **passes** once fixed;
   2. a **minimal console csproj** (`dotnet new console`, `<UseAppHost>false</UseAppHost>`) that prints
      `Expected` vs `Actual` -- the most portable form because it builds **offline** from the SDK's
      bundled ref/host packs and so runs on **daily/servicing SDKs** as well as GA;
   3. a **standalone file-based C# app** (`dotnet run app.cs`, .NET 10+ only) -- convenient, but **only
      when the repro will run exclusively on a public GA SDK**. Do **not** use it for a fix that will be
      fix-tested (Procedure B): on a daily SDK `dotnet run app.cs` tries to restore ILLink/ILCompiler at
      the unreleased patch version (not on public feeds) and fails.
   Author it under `"$WORKDIR"`. Keep it minimal -- only the APIs/types the fix touches. Because the
   tester reuses the producer's repro **unchanged** on a daily SDK, prefer forms 1 or 2.
4. **Provision the baseline SDK** (the latest public GA of the target major, which still exhibits the
   bug) unless a user-supplied SDK is in effect -- see *Reference: SDK installation*.
5. **Run and capture.** Build/run the repro with the baseline SDK, capturing combined stdout+stderr:
   ```bash
   { <run command>; echo "exit=$?"; } 2>&1 | tee "$WORKDIR/output.log"
   ```
   For a unit-test repro, `dotnet test`; for an app, `dotnet run` (or build + run the dll).
6. **Verify the bug reproduces.** Confirm `Actual` matches the buggy behavior (test fails / app prints
   the wrong result). If it does **not** reproduce, do not fabricate a result -- report that the bug
   could not be reproduced and what you tried, then stop.
7. **Detect regression** (see *Reference: regression detection*). Run the **same** repro on the oldest
   in-support major. If the bug is absent there, it is a regression -- binary-search the in-support
   versions (oldest first) to find the **oldest affected**; if present even on the oldest in-support
   release, it is a long-standing (non-regression) bug. Record the result; do not block on it.
8. **Report.** Produce: which repro **form** was used, the isolating **code snippet**, the **Expected**
   result, the **Actual** result (quoted from `output.log`), the **regression** finding, and the list of
   local artifacts (`$WORKDIR` contents incl. `output.log`). Save this as `"$WORKDIR/step-summary.md"`
   too. The caller records it (a tracking issue, or an interactive report). **Never post anything.**

---

## Procedure B -- Verify a fix (baseline vs candidate SDK)

Inputs: an existing repro (reuse unchanged), a **baseline SDK** (expected buggy) and a **candidate
SDK** (expected fixed). The two may be any versions -- two servicing patches, two majors, or a GA vs a
preview of an upcoming major. For **servicing**, the candidate is selected by fix-flow detection (the
merged PR's fix must have flowed into a daily SDK -- see *Reference: fix-flow detection*); for any
other use, both SDK versions are supplied directly.

1. **Obtain the repro.** Reuse the exact repro the producer authored (from the producer's uploaded
   artifact when available; otherwise re-derive it identically via Procedure A steps 1-3). **Do not
   change the repro** between the two runs.
2. **Confirm/resolve the SDKs.** For servicing, confirm the fix has flowed (skip + retry later if not)
   and resolve `BASELINE_SDK` (buggy) and `CANDIDATE_SDK` (the daily/release that should contain it);
   for non-servicing, use the two supplied versions directly.
3. **Run on the baseline SDK** (expected buggy). Capture to a version-named log:
   ```bash
   { <run command>; echo "exit=$?"; } 2>&1 | tee "$WORKDIR/output-baseline-${BASELINE_SDK}.log"
   ```
4. **Install the candidate SDK and re-run the unchanged repro.** Capture to its own version-named log:
   ```bash
   { <run command>; echo "exit=$?"; } 2>&1 | tee "$WORKDIR/output-candidate-${CANDIDATE_SDK}.log"
   ```
5. **Compare to Expected and render a verdict:** the bug must be present on `BASELINE_SDK` and absent
   on `CANDIDATE_SDK` (the actual now equals Expected). Verdicts: **Verified fixed** (buggy -> correct),
   **Not fixed** (still buggy on the candidate SDK), or **Inconclusive** (e.g. baseline did not exhibit
   the bug -- baseline selection may be off).
6. **Report.** Produce: a reference to the repro used, the **Expected** result, the **Actual before**
   (with the baseline SDK version), the **Actual after** (with the candidate SDK version), and the
   **verdict**, plus the artifact list (both version-named logs + the repro). Save this report as
   `"$WORKDIR/step-summary.md"` too. The caller records it. **Never post anything.**

---

## Reference: SDK installation

Use the official `dotnet-install` script; install side-by-side, never globally. **First check whether a
suitable SDK is already pre-provisioned** -- in a workflow the runner usually already has the latest GA
of each major (that GA is the baseline), so no download is needed:

```bash
dotnet --list-sdks            # is the baseline GA already here?
```

If you must install, fetch the script once and run installs **through `bash`** (a single simple command
-- the agentic harness gates bare `curl <url>`). The baseline GA URL and the daily feature **band** for
the major come from the managed repo's plugin (e.g. `references/repo-runtime.md`):

```bash
curl -fsSL https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh -o "$WORKDIR/dotnet-install.sh"
chmod +x "$WORKDIR/dotnet-install.sh"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Baseline = latest public GA of the target major (still has the bug). If not pre-provisioned:
GA_VERSION="$(curl -fsSL https://builds.dotnet.microsoft.com/dotnet/Sdk/${MAJOR}.${MINOR}/latest.version)"
bash "$WORKDIR/dotnet-install.sh" --version "$GA_VERSION" --install-dir "$WORKDIR/sdk-baseline" --no-path

# Fixed = latest daily build of the servicing feature band for the major (per the plugin):
bash "$WORKDIR/dotnet-install.sh" --channel "$BAND" --quality daily --install-dir "$WORKDIR/sdk-fixed" --no-path

# Use a specific SDK for a run:
export DOTNET_ROOT="$WORKDIR/sdk-baseline"; export PATH="$DOTNET_ROOT:$PATH"
dotnet --version
```

Notes: the install script and SDK tarballs are served from `builds.dotnet.microsoft.com`; daily builds
resolve via `aka.ms` -> `ci.dot.net`. The runner is **linux-x64** in the workflows.

> **Daily-SDK package restore.** A daily/servicing SDK's matching runtime/ref packs are **not** on
> nuget.org yet. Keep repros restore-free (a `UseAppHost=false` console csproj or a unit test builds
> from the SDK's **bundled** packs). Only if a repro genuinely needs extra package refs at the
> unreleased patch version, add the band's daily feed to a local `nuget.config`
> (`https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet<MAJOR>/nuget/v3/index.json`).

## Reference: fix-flow detection (has the fix shipped in a daily SDK?)

Goal: given a fix commit `C` on `release/$MAJOR.0`, decide whether the **latest daily SDK** for the
band already contains it, and identify that SDK version. The band, the VMR-vs-direct runtime-commit
behavior, and other specifics come from the managed repo's plugin.

**Sandbox-robust ordering.** First confirm `C` even exists in the product repo via the `github` MCP
(`get_commit`); a release-branch PR whose merge commit is unknown to the product repo (HTTP 422) can
never have flowed -- report "not flowed" and stop. Resolve any VMR `source-manifest.json` with the
`github` MCP `get_file_contents`, and run the ancestry check with `gh api .../compare` (or the MCP
compare). The only step that needs a non-GitHub fetch is reading the daily build's `productCommit` from
`ci.dot.net`; keep that as a single simple `curl` and, if it is denied, treat the fix as "not yet
verifiable in this environment" rather than guessing.

```bash
# BAND comes from the plugin (e.g. runtime: 8->8.0.4xx, 9->9.0.3xx, 10->10.0.1xx)

# 1) Resolve the latest daily SDK build version via the aka.ms redirect:
REDIRECT="$(curl -fsSIL --max-redirs 5 "https://aka.ms/dotnet/${BAND}/daily/productCommit-linux-x64.txt" \
  | awk 'tolower($1)=="location:"{print $2}' | tail -1 | tr -d '\r')"
FULL_VER="$(printf '%s' "$REDIRECT" | grep -oE 'Sdk/[^/]+' | head -1 | cut -d/ -f2)"   # e.g. 10.0.110-servicing.26326.123
FIXED_SDK="${FULL_VER%%-*}"

# 2) Read the runtime commit baked into that SDK build:
PRODUCT="$(curl -fsSL "https://ci.dot.net/public/Sdk/${FULL_VER}/productCommit-linux-x64.txt")"
RUNTIME_COMMIT="$(printf '%s' "$PRODUCT" | grep -oE 'runtime_commit="[0-9a-f]{40}"' | head -1 | grep -oE '[0-9a-f]{40}')"

# 3) If the plugin says this major builds from the VMR, resolve the real product-repo commit via the
#    github MCP get_file_contents for dotnet/dotnet@${RUNTIME_COMMIT}:src/source-manifest.json
#    (repositories[path=="runtime"].commitSha). curl shown as the fallback:
# RUNTIME_COMMIT="$(curl -fsSL "https://raw.githubusercontent.com/dotnet/dotnet/${RUNTIME_COMMIT}/src/source-manifest.json" \
#   | jq -r '.repositories[] | select(.path=="runtime") | .commitSha')"

# 4) Has fix C flowed in? Prefer the github MCP compare; the gh api form is equivalent:
#    status "behind"/"identical" => C is included (fix HAS flowed); "ahead" => not yet.
STATUS="$(gh api "repos/${OWNER}/${REPO}/compare/${RUNTIME_COMMIT}...${C}" --jq '.status' 2>/dev/null || echo unknown)"
```

After installing, an SDK's runtime commit is the first line of
`shared/Microsoft.NETCore.App/<ver>/.version`.

## Reference: regression detection

A bug is a **regression** if it does not reproduce on an earlier still-supported major. In-support
majors are at https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core (LTS = 3 yrs, STS
= 18 mo from GA). Run the **same** repro on the latest GA of the **oldest** in-support major; if absent,
**binary-search** the in-support majors (oldest first) to the oldest affected. If present on the oldest
in-support major, it is long-standing, not a regression. Install each via `dotnet-install`.

## Reference: repro forms & output conventions

- **Unit test (preferred).** `dotnet new xunit -o repro`; write one `[Fact]`/`[Theory]` that asserts
  **Expected**. It fails on the buggy baseline and passes once fixed. Run with `dotnet test` -- the
  xunit/test-sdk packages restore from nuget.org online (this environment is online) and are version-
  independent, so the same test runs on GA and daily SDKs.
- **Minimal console csproj.** `dotnet new console -o repro` with `<UseAppHost>false</UseAppHost>` (no
  AOT/trim). Builds from the SDK's bundled ref/host packs, so the **same** project runs on a GA baseline
  and a daily/servicing SDK with no extra restore. Print `Expected:`/`Actual:`; optionally exit non-zero
  when buggy. Pin `<TargetFramework>` to the target major. Run: `dotnet build -c Release` then
  `dotnet bin/Release/<tfm>/repro.dll`. Use when a unit test isn't a clean fit.
- **File-based app.** A single `.cs` run via `dotnet run app.cs` (needs a .NET 10+ SDK -- installable
  via `dotnet-install`). Convenient for GA SDKs; for a daily/servicing fixed SDK prefer the unit test or
  console csproj (a file-based app may try to restore ILLink/ILCompiler at the unreleased patch).
- **Output.** Always capture combined stdout+stderr to `output.log` (Procedure A) or
  `output-<role>-<sdkversion>.log` (Procedure B). The report must quote the **Actual** result directly
  from these logs -- never paraphrase a result you did not capture.
- **Trimming/AOT/runtime-config bugs** typically require a csproj (publish with `dotnet publish`), not
  a file-based app.
