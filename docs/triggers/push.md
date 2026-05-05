---
title: "push"
---

[← Previous: pull_request_review_comment](pull-request-review-comment.md) | [Common Triggers](../chapters/triggers.md) | [Next: discussion →](discussion.md)

# `push`

### ⚠️ Use with caution

**`push` is one of the highest-volume triggers in the repo and the easiest to subscribe to overbroadly.** A workflow declared as just `on: push` (no `branches:` filter) fires on every push to every branch by anyone with write access — including bot branches, dependency-update branches, codeflow branches, and short-lived feature branches that no one expected the workflow to care about. For an agentic workflow that consumes Copilot tokens per run, the cost blowout is silent until the bill arrives — this is the trigger most likely to turn "it's just a PoC" into a line-item on next month's invoice. The fix is always-explicit `branches:` filters and a narrow subscription that matches the actual workflow intent.

*Concurrency twist:* rapid pushes to a feature branch (rebasing, force-pushing) stack up runs unless `concurrency.cancel-in-progress: true` is set — which in turn re-creates the race condition (see [Concurrency and Race Conditions](../chapters/concurrency-and-races.md)) where the canceled run was halfway through a write that the next push won't redo.

---

## Scenarios

- CI / build / test on every push to protected branches (with explicit `branches:` filter)
- Post-merge automation (release tagging, deployment, changelog generation) on `push` to `main`
- Monorepo selective builds (with `paths:` filters)
- **Avoid** bare `on: push` without `branches:` — fires on every push to every branch

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | N/A — only write+ users can push. |
| Activity types | N/A — `push` has no activity types. Always use explicit `branches:` and optionally `paths:`/`tags:` filters. |
| Concurrency | `${{ github.workflow }}-${{ github.ref }}` with `cancel-in-progress: true` — the standard CI pattern where only the latest push to a branch matters. `cancel-in-progress` depends on whether output from prior runs would be discarded/overwritten. |
| Idempotency | **Recommended.** Force-pushes rewrite history and can invalidate prior-run artifacts. |
| Fork posture | Forks push to their own fork; does not propagate to upstream. Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | Pushes via `GITHUB_TOKEN` **do not** trigger `push`. Pushes by GitHub Apps with installation tokens or PATs **do**. |
| Sanitize payload? | Commit messages are user-controlled but generally trusted (write access required to push). |
| Safe-outputs | Depends on workflow purpose — `add-comment` on linked issues, `create-issue` for release tracking. |
| Integrity filtering | `approved` (default) for outputs that require triage+ permissions. `unapproved` when the workflow intentionally consumes community content (e.g., scanning community issues resolved by the push) — must pair with tight `safe-outputs`. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

## `pull_request.synchronize` reference

`synchronize` is an activity type on `pull_request` / `pull_request_target`, not a standalone trigger. Its profile lives on those pages. This section documents the behavioral details.

`synchronize` fires once per push to the PR branch (including force-pushes). It does **not** fire per commit — a single push with 50 commits fires one event.

### Things that *do not* fire `synchronize` (despite reading like they should)

- **Draft ↔ ready-for-review conversions.** Marking a PR as ready fires `pull_request.ready_for_review`; converting back to draft fires `pull_request.converted_to_draft`. Neither fires `synchronize`. Workflows whose `on:` block uses default `types: [opened, synchronize, reopened]` will **not** run when a long-running draft is finally marked ready, even though "the PR just changed" intuitively feels like a sync.
- **Base-ref edits.** Changing the PR's base branch fires `pull_request.edited` (with `changes.base` populated), not `synchronize`. Workflows that rely on `synchronize` to re-run CI against the new base will silently skip.
- **Reviews, comments, label changes.** All have their own activity types on `pull_request` or fire entirely separate events (`pull_request_review`, `issue_comment`).
- **Pushes to the *base* branch** (e.g., someone merges to `main` while your PR targets `main`). This does not fire `synchronize` on your open PRs — it fires `push` on `main`. Your PR's CI won't re-run against the new base unless you push to your own branch (or close-and-reopen).

### Branch-protection interaction (often surprising)

Branch protection's "Dismiss stale pull request approvals when new commits are pushed" setting fires on the same head-SHA-changed event that drives `synchronize`. This is independent of your workflow — but it means a `synchronize` from any push (including a force-push that didn't actually change file contents, e.g., a rebase onto current `main`) silently invalidates every prior approval. A workflow that gates downstream behavior on "PR is approved" can therefore see the approval state flip back to "0 approvals" mid-run.

### Head-SHA invalidation

The PR head SHA changes on every push. Any artifact the workflow created (status check, comment, posted analysis) referencing the *previous* head SHA is now stale — and a follow-up `pull_request_target` workflow that checks out `head.sha` is now checking out **different code** than the prior run analyzed. There is no built-in mechanism to "carry forward" prior-run state across a `synchronize`; idempotency must be explicit.

---

[← Previous: pull_request_review_comment](pull-request-review-comment.md) | [Common Triggers](../chapters/triggers.md) | [Next: discussion →](discussion.md)
