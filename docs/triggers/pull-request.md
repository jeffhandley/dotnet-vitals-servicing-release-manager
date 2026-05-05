---
title: "pull_request"
---

[← Previous: workflow_run](workflow-run.md) | [Common Triggers](../chapters/triggers.md) | [Next: pull_request_target →](pull-request-target.md)

# `pull_request`

### ⛔ Avoid (public repos)<br />☢️ Use with extreme caution (private repos)

> **In private repositories, this trigger is ☢️ Use with extreme caution.** There are no outside contributors, so the "Approve and run workflows" button — the primary driver of the ⛔ rating — does not apply. However, the trigger is elevated to ☢️ (rather than fully recommended) for defense-in-depth and to maintain consistent practices across repositories.

**The approve-and-run gate is what makes this catastrophic, not what protects from it.** The approval requirement is on by policy and external contributors must work from forks — so every `pull_request` workflow on every external PR presents the "Approve and run workflows" button, every day. That is alert fatigue by construction: the click-rate trends toward 100%, **the UI does not show what is about to execute or give guidance for what to review before approving**, and the button label conceals what is actually being authorized ("run safely-defined workflows" reads as "rubber-stamp this routine PR," not "grant this stranger's code access to every secret"). **The gate is also one-shot per click — approving *all* gated workflow runs in that batch at once, with no granular visibility or selection of which individual workflows are safe to run for this specific PR.** Click once and every gated workflow on the PR fires. **gh-aw's safe-outputs gate, output sanitization, and `staged: true` exist largely *because* of this trigger's footgun shape.**

**`closed` ≠ `merged`.** Workflows that celebrate a merge, post release notes, file follow-up issues, or kick off downstream automation on `pull_request.closed` fire on PR-closed-without-merge too, unless they explicitly check `github.event.pull_request.merged == true`. The PR author can game this: open a PR, close it, and the "thanks for merging!" workflow runs.

---

## Scenarios

- Deterministic labeling based on changed paths
- CI / build / test on every PR (the canonical CI trigger)
- Code-review assist (agentic review comments on the diff)
- **The safer of the two PR triggers** — read-only `GITHUB_TOKEN`, no secrets for fork PRs. If you must react to PR events directly, this is the one to use over `pull_request_target`.

**Why ⛔:** The existence of *any* `pull_request` workflow causes the "Approve and run workflows" button to appear for outside contributors' PRs (including Copilot), and clicking the button approves *all* gated workflows — including any `pull_request_target` workflows in the repo. The trigger can be safely used, even with fork PRs (the token is already read-only, there are no secrets), but the button's existence creates alert fatigue that undermines the overall security model. **The button is presented even if the user does not match `on.roles:` and the workflow will immediately exit.**

**Recommended alternatives:**
- **[`pull_request: types: [labeled]` / `label_command:`](labeled-and-label-command.md)** — for PR operations triggered by a triage+ user applying a label, where having human-in-the-loop approval is necessary.
- **[`schedule`](schedule.md)** — for polling-based execution of PR workflows where a human-in-the-loop approval is not necessary.

The `labeled`/`label_command` and `schedule` triggers are not subject to event spamming or user-triggered invocations. They are subject to the same idempotency requirements as other alternatives, but those challenges are not compounded by privilege-escalation or trust-boundary risks.

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | Acceptable to set `all` — anyone can create a pull request and workflows generally should run on all of them. Restricting to `[admin, maintainer, write]` (default) is a special case. `triage` is excluded by default — add explicitly when triage users open PRs from branches. |
| Activity types | Default `[opened, synchronize, reopened]` is correct for CI. `synchronize` fires once per push to the PR branch (including force-pushes) — valid for reprocessing when the head changes but note that several events commonly fire in quick succession. Add `ready_for_review` if you skip draft PRs. **Avoid** `edited` unless you need to re-react to title/body changes. |
| Concurrency | `${{ github.workflow }}-${{ github.event.pull_request.number }}` with `cancel-in-progress: true` — the standard CI pattern where only the latest push matters. For agentic workflows that post comments or apply labels, `cancel-in-progress: false` is safer to avoid partial-write races. |
| Idempotency | **Recommended.** Upsert comments (find-and-update rather than post-new), upsert labels, upsert status checks. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. Fork PRs *to upstream* are safe (read-only token, no secrets). |
| Approval gate | **Subject** — outside contributors' PRs trigger the "Approve and run workflows" button, **even when the contributor doesn't match `on.roles:` and the workflow will immediately exit.** The existence of *any* `pull_request` workflow causes this button to appear, and clicking approves *all* gated workflows. |
| Copilot events | Copilot-authored PRs are subject to the "Approve and run workflows" approval gate, which teaches team members to click the button when they see it. |
| Sanitize payload? | **Yes, always** in pre-agent steps. PR title, body, and branch name are user-controlled; use `steps.sanitized.outputs.text`, never raw `${{ github.event.pull_request.body }}`. Acceptable to handle unsanitized payload within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | `add-labels`, `add-comment` for review-assist. `create-issue` if filing follow-ups. No `push-to-pull-request-branch` unless the workflow explicitly intends to commit changes. |
| Integrity filtering | `approved`. Fork contributors are typically `CONTRIBUTOR` or lower, so their PR content is filtered out before the agent sees it. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: workflow_run](workflow-run.md) | [Common Triggers](../chapters/triggers.md) | [Next: pull_request_target →](pull-request-target.md)
