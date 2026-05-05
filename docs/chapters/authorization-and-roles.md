---
title: "Authorization, Roles, and Read-Only Contributors"
---

[← Previous: Concurrency and Race Conditions](concurrency-and-races.md) | [Table of Contents](../../README.md) | [Next: Trigger-by-Trigger Risk Profile →](../appendices/trigger-risk-profile.md)

# Authorization, Roles, and Read-Only Contributors

The emphasis on **read-only contributors** is critical, and the answer has two layers: **what GitHub itself permits** and **what gh-aw layers on top by default**.

## What each role can do *that fires a workflow* (raw GitHub permissions)

Here is what each role can do *that fires a workflow* — at the GitHub event layer, before any gh-aw filtering:

| Action | Read | Triage | Write | Maintain | Admin |
|---|---|---|---|---|---|
| Open an issue | ✅ | ✅ | ✅ | ✅ | ✅ |
| Edit own issue body/title | ✅ | ✅ | ✅ | ✅ | ✅ |
| Close own issue | ✅ | ✅ | ✅ | ✅ | ✅ |
| Comment on issue/PR/discussion | ✅ | ✅ | ✅ | ✅ | ✅ |
| Edit own comment | ✅ | ✅ | ✅ | ✅ | ✅ |
| React with emoji | ✅ | ✅ | ✅ | ✅ | ✅ |
| Open a PR (from fork or, if collaborator, from branch) | ✅ (via fork) | ✅ | ✅ | ✅ | ✅ |
| Edit own PR body | ✅ | ✅ | ✅ | ✅ | ✅ |
| `/slash-command` in any comment/body they author | ✅ | ✅ | ✅ | ✅ | ✅ |
| Apply a label | ❌ | ✅ | ✅ | ✅ | ✅ |
| Approve/dismiss reviews | ❌ | ❌ | ✅ | ✅ | ✅ |
| Push to repo branches | ❌ | ❌ | ✅ | ✅ | ✅ |
| Invoke `workflow_dispatch` | ❌ | ❌ | ✅ | ✅ | ✅ |
| Click "Approve and run workflows" | ❌ | ❌ | ✅ | ✅ | ✅ |

**Key observation:** A reader can fire any [`slash_command:`](../triggers/comment-and-slash-command.md) and any `issues`/`issue_comment`/`pull_request`/`discussion_comment`/`reaction`-driven workflow, just by typing or clicking. They cannot fire `label_command:` or `workflow_dispatch:` triggered ones.

## gh-aw's `on.roles:` allow-list (the *primary* authz mechanism)

gh-aw automatically injects an **activation-job membership check** into any workflow whose triggers include "unsafe" events (issues, comments, PRs, discussions, slash/label commands). The allow-list is configured by `on.roles:` and **defaults to `[admin, maintainer, write]`**[^role-checks-go]. Behaviors:

- **String form** — `on.roles: write` (single role) or `on.roles: all` (special value disabling the check entirely).
- **Array form** — `on.roles: [admin, maintainer, write]` (the default) or `on.roles: [admin, maintainer, write, triage, read]` to broaden.
- **Inverse field** — `on.skip-roles: [role,...]` blocks specific roles from a wider allow-list (e.g., `on.roles: all` + `on.skip-roles: [read]`).
- **The check still consumes a runner.** Like other activation-job filters, the workflow *runs* and the activation job calls the GitHub repo permission API; the agent job is then skipped if the actor's role is not in the allow-list.
- **`on.roles: all` is required for any chat-style workflow that intentionally serves read-only contributors** — for example, a self-service `/help` command or a community FAQ bot.
- **Does not protect against [`workflow_run`](../triggers/workflow-run.md) chained workflows** (which run as the base repo, not as the original actor).

## GitHub permission roles (vocabulary for this section)

| Role | Default in `on.roles`? | Notes |
|---|---|---|
| `admin` | ✅ | Full control |
| `maintainer` (a.k.a. `maintain`) | ✅ | Can manage repo without billing/admin |
| `write` | ✅ | Can push, label, approve PRs, dispatch workflows |
| `triage` | ❌ | Can manage issues/PRs (label, assign, close) but cannot push |
| `read` | ❌ | Can open issues/PRs/comments/reactions only |
| (anonymous fork contributor) | ❌ | Treated as `read` for membership API purposes |
| `bot` actor | (inherits the bot's permission level) | Use `on.skip-bots:` to block specific bot logins |

**`triage` is invisible in the default allow-list** — that means a workflow like [`label_command:`](../triggers/labeled-and-label-command.md) (which requires triage to apply the label in the first place) will *fire* but its activation job will *deny* a triage user unless `on.roles:` is broadened. This is a frequent footgun.

## Consolidated read-only / fork contributor write surface

The single most important security insight in this guide:

> **What the agent can do is determined by the workflow's `permissions:` and `safe-outputs:` declarations — NOT by the actor who fired it.** When a workflow accepts a read-only or fork contributor as the trigger (i.e., `on.roles: all`, no actor check, or no fork guard), that contributor effectively gets **bot-level write access** to anything the workflow grants the agent.

This is why the gh-aw `on.roles:` default of `[admin, maintainer, write]` exists — it's a *deny-by-default* gate that prevents a read user from inducing the bot to act with the bot's elevated permissions. The moment you set `on.roles: all` (or write a workflow that ignores actor identity), every mutation in the table below becomes reachable by anyone who can fire the trigger.

**Capability matrix.** Assuming the workflow grants the relevant `permissions:` and exposes the relevant safe-output, can a **read-only contributor** (or anonymous fork contributor) cause the listed action by firing each gh-aw trigger?

The triggers are grouped by their security posture — triggers with identical column values are combined.

Legend: ✅ = reachable; ⚠️ = reachable but with caveats; ❌ = not reachable via this trigger; 🔥 = reachable AND the workflow runs with **upstream secrets** (the high-impact pwn vector).

| Action | `issues`, `discussion`, `slash_command` on issue | `issue_comment` on fork PR, `slash_command` on fork PR | `pull_request` (cross-fork) | `pull_request_target` (cross-fork) |
|---|---|---|---|---|
| Post a comment | ✅ | 🔥 | ⚠️ read-only token | 🔥 |
| Add/remove labels | ✅ | 🔥 | ⚠️ | 🔥 |
| Edit issue/PR title or body | ✅ | 🔥 | ⚠️ | 🔥 |
| Close/reopen issue or PR | ✅ | 🔥 | ⚠️ | 🔥 |
| Edit/delete existing comments | ❌ | ❌ | ❌ | ❌ |
| Create a new issue | ✅ | 🔥 | ⚠️ | 🔥 |
| Create a new pull request | ✅ | 🔥 | ⚠️ | 🔥 |
| Push commits to PR branch | ❌ | ⚠️ same-repo only | ❌ | ⚠️ same-repo only |
| Create a discussion | ✅ | | ⚠️ | |
| Trigger downstream `workflow_dispatch` | ✅ | | ⚠️ | |
| Read/exfiltrate secrets | ✅ | | ❌ no secrets | |
| Approve a PR | ❌ | ❌ | ❌ | ❌ |
| Merge a PR | ✅ | | ⚠️ | |
| Admin actions (collaborators, branch protection) | ❌ | ❌ | ❌ | ❌ |

**How to read the column intersections.** A means: a contributor with **only read access** (or none — anonymous fork contributors via PR comments) can, just by typing a `/command` or comment, induce the bot to perform the listed mutation **using the upstream's secrets and write token**. This is the gh-aw-flavored re-statement of the classic "pwn requests" class[^pwn-requests] and is the entire reason `on.roles:` defaults to `[admin, maintainer, write]`.

**Defenses, in priority order:**

1. **Leave `on.roles:` at its default.** Don't set `on.roles: all` unless you've thought through every cell of the matrix above for your workflow's actual `permissions:` and safe-outputs.
2. **Minimize `permissions:`** to the smallest set the agent actually needs.
3. **Minimize `safe-outputs:`** to only the structured mutations the workflow needs to make.
4. **For PR-touching workflows: never check out the PR head SHA in the same job that has secrets.** Use `pull_request` (read-only token) for code analysis, and a separate `pull_request_target` job *without* checkout for the comment/label posting.
5. **Add an explicit fork guard** (`if: github.event.pull_request.head.repo.fork == false`) for any agent step that should refuse to act on cross-fork PRs.
6. **Configure `min-integrity`** to control what content the agent can see during execution — see [Integrity filtering](#integrity-filtering-toolsgithubmin-integrity) below.

## Integrity filtering (`tools.github.min-integrity`)

`on.roles:` gates **who can trigger** the agent; integrity filtering gates **what content the agent can see**. They are layered controls — both matter, and broadening one increases the importance of the other.

The MCP gateway intercepts tool calls to GitHub and filters content by author trust. Items below the configured `min-integrity` level are removed before the agent sees them. The hierarchy, from most to least restrictive: `merged` > `approved` > `unapproved` > `none` > `blocked`.

| Level | Who qualifies |
|---|---|
| `merged` | Merged PRs; commits reachable from the default branch (any author) |
| `approved` | `OWNER`, `MEMBER`, `COLLABORATOR`; non-fork PRs on public repos; all items in private repos; platform bots (dependabot); users in `trusted-users` |
| `unapproved` | `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR` |
| `none` | All content, including `FIRST_TIMER` and users with no association |
| `blocked` | Users in `blocked-users` — always denied, cannot be promoted |

### Standard guidance

**`approved`** — the default. Use when `safe-outputs` include operations that require triage+ permissions: `add-labels`, editing others' issues/PRs, `push-to-pull-request-branch`, `create-pull-request`, `merge`, `close`.

**`unapproved` or `none`** — when the workflow *intentionally* consumes untrusted input to operate on community-filed issues and pull requests. **Must be paired with** tight `safe-outputs` limited to actions any user can perform (`add-comment`, author editing their own description) and `on.roles:` to ensure the trigger's authorization floor prevents privilege escalation. The lower integrity level widens what the agent can read; the `safe-outputs` and `on.roles:` pairing constrains what it can do with that content.

**`none` specifically** — when a human-in-the-loop gate already exists outside the integrity system (e.g., `label_command:` where the label application by a triage+ user *is* the approval signal).

**`merged`** — only for workflows that should exclusively operate on production content (e.g., `workflow_run` acting on merged code, not in-flight PRs).

### Interaction with `on.roles:`

| `on.roles:` | `min-integrity` | Effect |
|---|---|---|
| `[admin, maintainer, write]` (default) | `approved` | **Most restrictive.** Only trusted actors trigger the agent, and the agent only sees trusted content. |
| `[admin, maintainer, write]` (default) | `unapproved` / `none` | Agent only runs for trusted actors, but can read community content during execution. Appropriate for workflows like post-merge scans that need to find community issues resolved by a push. |
| `all` | `approved` | **Two-layer defense.** Any actor can trigger the agent, but the agent only sees content from trusted authors. The integrity filter is the primary content-trust boundary. |
| `all` | `unapproved` / `none` | **Widest exposure.** Any actor triggers the agent, and the agent sees community content. Must pair with minimal `safe-outputs` — the only remaining constraint on blast radius. |

Each trigger page includes an **Integrity filtering** row in its profile table with the specific recommendation. See the [integrity filtering reference](https://github.github.com/gh-aw/reference/integrity/) for full configuration options including `blocked-users`, `trusted-users`, `approval-labels`, and reaction-based endorsement.

[^role-checks-go]: gh-aw source, [`pkg/workflow/role_checks.go`](https://github.com/github/gh-aw/blob/main/pkg/workflow/role_checks.go)
[^pwn-requests]: GitHub Security Lab, [Keeping your GitHub Actions and workflows secure: Preventing pwn requests](https://securitylab.github.com/research/github-actions-preventing-pwn-requests/)

---

[← Previous: Concurrency and Race Conditions](concurrency-and-races.md)| [Table of Contents](../../README.md) | [Next: Trigger-by-Trigger Risk Profile →](../appendices/trigger-risk-profile.md)
