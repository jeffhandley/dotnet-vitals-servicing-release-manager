# Runtime plugin (dotnet/runtime servicing policy)

This is the **`runtime`** plugin for the servicing-release system: everything specific to validating
`dotnet/runtime` servicing fixes. The repo-agnostic procedures live in [`../SKILL.md`](../SKILL.md);
this file supplies the parameters they ask for.

## Target repository

- **Production:** `dotnet/runtime`
- **Prototype (current):** `jeffhandley/dotnet-runtime-servicing-release-manager` -- the runtime
  *simulation* repo. Use this as the product repo while prototyping in
  `jeffhandley/dotnet-vitals-servicing-release-manager`; switch to `dotnet/runtime` for production.

All "read the product repo" steps below target whichever of these is in effect.

## Servicing branches

- Base branch pattern: `release/*`, including `release/*-staging`.
- The target **major.minor** is parsed from the base branch: `release/MAJOR.MINOR` or
  `release/MAJOR.MINOR-staging` (e.g. `release/8.0-staging` -> `8.0`).
- **Branch awareness.** .NET 8 / .NET 9 fixes land on `release/X.0-staging` first and later
  auto-merge into `release/X.0`; monitor both. .NET 10 has no staging branch and flows through the VMR
  ("Source code updates from dotnet/dotnet").

## PR classification rule (rule-in vs rule-out)

A `release/*` PR earns a repro only if it is a **product bug fix**. Most release-branch PRs are
code-flow, infrastructure, branding, or test-only churn and must be **ignored**. Apply the rule using
the PR's labels, author, title, changed files, and body (read user content through the integrity-gated
GitHub tool; skip `[Filtered]` results). Reading labels is fine -- this system never *adds* labels to
the product repo.

**Gate:** base branch matches `release/*` (incl. `release/*-staging`). **Ignore** unless the PR carries
`Servicing-approved` **or** `Servicing-consider` (treat `Servicing-consider` the same as approved -- a
real fix awaiting formal approval). The defunct `blocking-servicing` label carries no signal.

**EXCLUDE (ignore) if any match:**

- Label `area-codeflow` (~50% of release PRs: Maestro dependency updates, VMR source updates, branding
  bumps, automated branch merges).
- Author `dotnet-maestro[bot]` or `dotnet-maestro-bot`.
- Title matches any of: `^\[automated\] Merge branch .* => .*`, `^Update branding to \d+\.\d+\.\d+`,
  `Source code updates from dotnet/dotnet`, `^\[release/[^\]]+\] Update dependencies from`,
  `^Revert ".*(Update dependencies|Source code updates).*"`.
- Infrastructure: any `area-Infrastructure*` or `area-Build-mono` label, **or** every changed file is
  under `eng/`, ends in `.yml`/`.yaml`, or is `global.json`/`NuGet.config`.
- Test-only: body says "testcode only" / "test-only change", **or** every changed file is under
  `src/tests/**`, `src/**/tests/**`, or `src/libraries/**/{tests,test,ref}/**`.
- "Merging internal commits for release/X.0" (bulk branch-flow with an empty body): not a single
  mappable fix -- skip in the automated path and report it as needing manual review.

**INCLUDE (build a repro) when, after the exclusions above:**

- The PR carries a **product area** label -- e.g. `area-GC-coreclr`, `area-CodeGen-coreclr`,
  `area-VM-coreclr`, `area-Interop-coreclr`, `area-Diagnostics-coreclr`, `area-NativeAOT-coreclr`,
  `area-AssemblyLoader-coreclr`, `area-PAL-coreclr`, `area-ExceptionHandling-coreclr`,
  `area-ILTools-coreclr`, mono runtime areas (`area-Codegen-*-mono`, `area-GC-mono`, `area-Interop-mono`,
  `area-Debugger-mono`), any `area-System.*`, `area-Tools-ILLink`, `area-Host`, `area-ReadyToRun`,
  `area-DependencyModel`; **and**
- Changed files include **product source**: `src/libraries/*/src/**`, `src/coreclr/**` (excluding
  tests/tools), `src/mono/**` (excluding tests), or `src/native/**`; **and**
- (Strongest signal) the body has a servicing **Customer Impact** section, `Fixes #NNNN` /
  `fixes https://github.com/.../issues/NNNN`, or a servicing milestone like `9.0.x`/`8.0.18`.

**Default:** if a PR survives the exclusions but the product-fix signal is weak/opaque, treat it as a
**low-confidence** candidate; it is acceptable to stop with a clear "no concrete repro could be
derived" result.

## SDK feature bands (for `--channel ... --quality daily`)

| Major | Daily band |
|-------|------------|
| 8 | `8.0.4xx` |
| 9 | `9.0.3xx` |
| 10 | `10.0.1xx` |

- **Baseline GA** SDK for major M: `https://builds.dotnet.microsoft.com/dotnet/Sdk/${M}.${MINOR}/latest.version`
  (the latest GA bundles the latest GA runtime, which still exhibits a not-yet-released fix).

## Fix-flow detection specifics

The generic algorithm is in `../SKILL.md` (*Reference: fix-flow detection*). Runtime specifics:

- The fix commit `C` is the **release-branch PR's merge commit** in the product repo.
- The daily SDK's `productCommit-*.txt` exposes `runtime_commit`. For **.NET 8/9** that is already a
  `dotnet/runtime` commit. For **.NET 10** it is a **`dotnet/dotnet` (VMR)** commit -- resolve the real
  `dotnet/runtime` commit from `dotnet/dotnet@<sha>:src/source-manifest.json`,
  `repositories[path=="runtime"].commitSha` (read via the `github` MCP `get_file_contents`).
- Ancestry: `compare/<runtime_commit>...<C>` -> `behind`/`identical` means **C has flowed**; `ahead`
  means not yet. A merge commit unknown to the product repo (HTTP 422) can never have flowed.
- **Timing:** public flow takes ~6-12h after merge; .NET 8/9 fixes often stay on the internal branch
  until Patch Tuesday, so "not yet flowed" is normal -- defer and retry on a later run.

## Reporting identity

When this plugin's fixes are recorded as tracking issues in the monitoring repo, use the key `runtime`
(e.g. issue title prefix `[Servicing: runtime] `) so multiple managed repos never collide.
