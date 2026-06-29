# Managed repositories (servicing-release plugin registry)

The servicing-release system is **repo-pluggable**. This registry lists the product repositories whose
`release/*` servicing fixes are validated. Each enabled repo points to a **plugin reference** that
supplies its repo-specific policy: the product repo to watch, its servicing branch pattern, the PR
classification rule, the SDK feature bands, and the fix-flow detection specifics. All repo-agnostic
logic lives in [`../SKILL.md`](../SKILL.md).

The workflows and the agent read this registry and process **each enabled row**, loading that row's
plugin reference for the specifics. Today the system is **scoped to `runtime`** -- one enabled row.

| Key | Product repo | Plugin reference | Enabled |
|-----|--------------|------------------|---------|
| `runtime` | `dotnet/runtime` | [`repo-runtime.md`](repo-runtime.md) | yes |

> **Prototype override.** While prototyping in `jeffhandley/dotnet-vitals-servicing-release-manager`,
> the `runtime` plugin's **target repo is overridden** to the runtime *simulation* repo
> `jeffhandley/dotnet-runtime-servicing-release-manager` (declared in [`repo-runtime.md`](repo-runtime.md) under
> *Target repository*). Switch it back to `dotnet/runtime` when promoting these workflows to
> `dotnet/vitals`.

## How the registry is used

- **Read** the product repo cross-repo with the `github` MCP (PRs, issues, files, commits). The
  workflows run in this monitoring repo, so they can read any repo the token can reach.
- **Write** only into *this* repo. gh-aw safe-outputs are scoped to the workflow's repository, so all
  reporting (per-fix tracking issues + the dashboard) is created here in vitals -- never as comments on
  the product repo. This matches the vitals tenet of generating issues within this repository.

## Adding another repo later

1. Copy [`repo-runtime.md`](repo-runtime.md) to `references/<key>.md` and edit its policy for the new repo.
2. Add a row above with **Enabled: yes**.

No workflow or skill changes are required -- expansion is purely additive configuration.
