---
title: "Operating Within a Fork"
---

[← Previous: The "Apparent vs. Actual" Trigger Surface](apparent-vs-actual.md) | [Table of Contents](../../README.md) | [Next: Concurrency and Race Conditions →](concurrency-and-races.md)

# Operating Within a Fork

When you fork a repository, a copy of every workflow file (`.github/workflows/**`) comes with the fork — including agentic workflows. **GitHub then treats your fork as its own first-class repository.** Any event you trigger *within your fork* fires the workflow inside *your fork*, with *your fork's* secrets and *your fork's* `GITHUB_TOKEN`. This is structurally separate from the cross-fork PR scenario in [The "Approve and run workflows" Gate](approve-and-run-gate.md) and is frequently a surprise.

## What fires when you operate inside your own fork

| Activity inside your fork | Workflow fires? | Runs as | Token / secrets |
|---|---|---|---|
| You push commits to a branch in your fork | ✅ on `push` | You (fork owner) | Your fork's secrets, full write `GITHUB_TOKEN` to your fork |
| You open a PR `feature → main` **within your fork** (self-review pattern) | ✅ on `pull_request` (this is **not** treated as a cross-fork PR — both refs are in your fork) | You | Your fork's secrets, full write token |
| You open an issue, comment, react in your fork | ✅ on `issues`, `issue_comment`, etc. | You | Your fork's secrets, full write token |
| You apply a slash command in your own fork | ✅ — and **`on.roles:` defaults still apply**, but you're admin of your own fork, so the membership check passes | You | Your fork's secrets, full write token |
| Scheduled (cron) workflows in your fork | ❌ by default — GitHub disables `schedule` triggers on forks until you re-enable them in the Actions tab | n/a | n/a |
| You open a PR from your fork **back to upstream** | ✅ on upstream's `pull_request` (with read-only token, no upstream secrets); ✅ on upstream's `pull_request_target` (with full upstream secrets) | Upstream | Upstream's secrets (with the cross-fork caveats from [`pull_request`](../triggers/pull-request.md) / [`pull_request_target`](../triggers/pull-request-target.md)) |

**Key consequence:** Workflows you forked from upstream — agentic or otherwise — will **start running for you on routine activity in your fork**, often unexpectedly. They run as *you* (or whatever PAT pool *you* configured), not as the upstream owner. This is harmless when secrets are unset (most workflows fail fast), but is an unwanted surprise.

## The `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` guard pattern

The simplest and most reliable defense is a top-level job condition that prevents every workflow from running in any fork. Pick one of two variants depending on whether you want to leave a manual escape hatch. The workflow will still _start_, but it will quietly abort with a successful state.

**With the `workflow_dispatch` escape hatch** (recommended — fork owners can opt in by manually dispatching from the Actions tab):

```yaml
on:
  pull_request:
  push:
  issue_comment:
  workflow_dispatch:

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}
# ...rest of workflow
```

**Without the escape hatch** (strictest — the workflow can never run inside a fork, even on manual dispatch):

```yaml
if: ${{ !github.event.repository.fork }}
# ...rest of workflow
```

_or_

```yaml
if: (!github.event.repository.fork)
# ...rest of workflow
```

**Why both clauses in the recommended pattern?**

- `!github.event.repository.fork` prevents the workflow from running on routine events inside any fork.
- `github.event_name == 'workflow_dispatch'` is the explicit escape hatch: if a forker *intends* to run the workflow themselves to test their changes, they invoke it manually from the Actions tab. They get the consequences of their own action, not a surprise.

> [!NOTE]
> **YAML gotcha — don't start a bare `if:` value with `!`.** When you write an `if:` value *without* the `${{ ... }}` or a `( )` grouping wrapper, YAML parses the value directly, and `!` is reserved as YAML's [tag indicator](https://yaml.org/spec/1.2.2/#691-node-tags). A line like `if: !github.event.repository.fork` is a YAML parse error, not a falsy-fork check. Wrap the expression in parentheses so the value no longer begins with `!`:
> Inside `${{ ... }}` the leading character is `$`, so `!` is fine in that position.

> 💡 **Idiomatic placement.** Apply the guard at the *job* level (or on the activation/pre-activation job for gh-aw), **not** the step level. A job-level `if:` skips the entire job (no runner spun up); a step-level `if:` still consumes a runner.

## What the fork owner *cannot* do to you (the upstream)

Forking does not give the fork owner any new privileges over the upstream. They still have to come back through the cross-fork PR gate ([The "Approve and run workflows" Gate](approve-and-run-gate.md)) to affect upstream. The risks here are entirely **about the fork owner being surprised by their own forked copies of your workflows**, and (transitively) about the *upstream* being judged poorly for shipping workflows that misbehave in forks.

---

[← Previous: The "Apparent vs. Actual" Trigger Surface](apparent-vs-actual.md) | [Table of Contents](../../README.md) | [Next: Concurrency and Race Conditions →](concurrency-and-races.md)
