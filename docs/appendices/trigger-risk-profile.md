---
title: "Trigger-by-Trigger Risk Profile"
---

| [Table of Contents](../../README.md) |

# Appendix: Trigger-by-Trigger Risk Profile

Quick-reference for every trigger that has a standalone page, plus the triggers that don't. Each entry summarizes the guidance, key dimensions, and links to the full page. See [Triggers](../chapters/triggers.md) for the guidance key.

This appendix also covers triggers **without** a standalone page (e.g., `repository_dispatch`).

---

## [`workflow_dispatch`](../triggers/workflow-dispatch.md) — ✅ Recommended
- **Authz:** Write+ required. Auto-paired with most gh-aw triggers.
- **Approval gate:** Not subject.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do (canonical way for one workflow to invoke another).
- **Concurrency:** Standard.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}` — `workflow_dispatch` is the explicit escape hatch.
- **Note:** Branch selection is user-controlled — a write-role contributor can dispatch against a branch with different workflow YAML.
- **Integrity filtering:** `approved` (default) for outputs that require triage+ permissions. `unapproved`/`none` when the dispatched workflow reads community content (e.g., ad-hoc triage run), paired with tight `safe-outputs`.

## [`schedule`](../triggers/schedule.md) — ✅ Recommended
- **Authz:** None — no actor. Runs as default branch workflow file.
- **Approval gate:** Not subject.
- **Copilot events:** N/A (time-driven).
- **Concurrency:** `${{ github.workflow }}`. Fewest compounding pitfalls of any trigger — concurrency/idempotency challenges exist but are not compounded by privilege-escalation or trust-boundary risks.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}` (defense-in-depth; schedules disabled on forks by default).
- **Idempotency:** Required. Pair with `workflow_dispatch` for manual off-schedule runs.
- **Cost:** Watch for unbounded growth — a workflow designed for 20 issues running against 2,000.
- **Integrity filtering:** `approved` (default) for outputs that require triage+ permissions. `unapproved` for housekeeping pollers that scan community content (e.g., stale issue sweeps, triage of community-filed issues), paired with tight `safe-outputs`.

## [`labeled` / `label_command:`](../triggers/labeled-and-label-command.md) — ✅ Recommended
- **Authz:** Triage+ (label application requires triage role). `triage` is the natural fit for `on.roles:`.
- **Approval gate:** Not subject.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.issue.number }}` with `cancel-in-progress: false`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Idempotency:** Required. `label_command:` auto-removes the label (one-shot), but must be safe to re-run.
- **Integrity filtering:** `none` — the label application is the human-in-the-loop gate.

## [`issues`](../triggers/issue.md) — ✅ Recommended
- **Authz:** Read (anyone can open/edit/close own issues). `on.roles: all` is acceptable.
- **Approval gate:** Not subject.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.issue.number }}`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Sanitize:** Yes — `steps.sanitized.outputs.text`; unsanitized OK within sandboxed agent job.
- **Integrity filtering:** `approved` (default) for outputs that require triage+ permissions (e.g., auto-labeling workflow). `unapproved`/`none` for workflows that scan community issues (e.g., duplicate detection with `add-comment` only).

## [`release`](../triggers/release.md) — ✅ Recommended
- **Authz:** Write+ (creating/publishing releases requires write access).
- **Approval gate:** Not subject.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.release.tag_name }}`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Idempotency:** Required — releases can be deleted and re-created.
- **Integrity filtering:** `approved` (default) for outputs that require triage+ permissions. `unapproved`/`none` when scanning community issues (e.g., closing issues resolved by the release), paired with tight `safe-outputs`.

## [`milestone`](../triggers/milestone.md) — ✅ Recommended
- **Authz:** Write+ (milestone operations require write access).
- **Approval gate:** Not subject.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.milestone.number }}`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Cascade:** Deleting a milestone fires `issues.demilestoned` / `pull_request.demilestoned` for every assigned item. `on: milestone` ≠ `on: issues: types: [milestoned]`.
- **Integrity filtering:** `approved` (default) for outputs that require triage+ permissions. `unapproved`/`none` when scanning community issues (e.g., generating release notes from community-filed issues in the milestone), paired with tight `safe-outputs`.

## [`push`](../triggers/push.md) — ⚠️ Use with caution
- **Authz:** Write+ (push access required).
- **Approval gate:** Not subject.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Runs:** On every matching push; always use explicit `branches:` and optionally `paths:`/`tags:` filters.
- **Concurrency:** `${{ github.workflow }}-${{ github.ref }}`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Integrity filtering:** `approved` (default) for outputs that require triage+ permissions. `unapproved` for post-merge scans that find community issues resolved by the push (e.g., commenting or closing resolved issues), paired with tight `safe-outputs`.

## [`issue_comment`](../triggers/comment-and-slash-command.md) / [`slash_command:`](../triggers/comment-and-slash-command.md) — ⚠️ Use with caution (public repos) · ✅ Recommended (private repos)
- **Authz:** Read (anyone can comment). `on.roles: all` acceptable for community-facing commands.
- **Approval gate:** **Not subject** — even on fork PRs. Key advantage over `pull_request`.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.issue.number }}` with `cancel-in-progress: false`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Sanitize:** Yes — `steps.sanitized.outputs.text`; unsanitized OK within sandboxed agent job.
- **Idempotency:** Required. `lock-for-agent: true` available but use with extreme caution.
- **`slash_command:` specifics:** Always narrow `events:`. Broad subscription + shared concurrency group = [non-matching-cancels-matching race](../chapters/concurrency-and-races.md).
- **Integrity filtering:** `approved` for privileged commands (e.g., `/backport` that applies labels or dispatches workflows). `unapproved` for community-facing bots (e.g., `/help` that only posts replies), paired with tight `safe-outputs`.

## [`pull_request_review`](../triggers/pull-request-review.md) — ⚠️ Use with caution
- **Authz:** Read (anyone can submit a review). `on.roles: all` acceptable — same considerations as `issues`.
- **Approval gate:** Not directly subject (unless workflow also subscribes to `pull_request`).
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.pull_request.number }}` with `cancel-in-progress: false`. Prefer separate workflows per trigger to avoid group-key collisions.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Note:** "Review submitted" ≠ "approved." Fires for `COMMENT`-type reviews from any user.
- **Integrity filtering:** `approved` for post-approval automation (e.g., applying ready-to-land label). `unapproved`/`none` when intentionally reading community review content (e.g., reply-only review bot), paired with tight `safe-outputs`.

## [`pull_request_review_comment`](../triggers/pull-request-review-comment.md) — ⚠️ Use with caution
- **Authz:** Read (anyone can post inline comments). `on.roles: all` acceptable.
- **Approval gate:** Not directly subject (unless workflow also subscribes to `pull_request`).
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.pull_request.number }}` with `cancel-in-progress: false`. Prefer separate workflow from `pull_request` / `pull_request_review`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Note:** The trigger nobody remembers exists. Inline comments are high-leverage surface for positioned content.
- **Integrity filtering:** `approved` (e.g., agent-generated code explanations). `unapproved`/`none` when intentionally reading community inline comments (e.g., reply-only bot). Highest-leverage injection vector.

## [`discussion`](../triggers/discussion.md) / [`discussion_comment`](../triggers/discussion-comment.md) — ⚠️ Use with caution
- **Authz:** Read. `on.roles: all` acceptable — same considerations as `issues`.
- **Approval gate:** Not subject.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.discussion.number }}` with `cancel-in-progress: false`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Sanitize:** Yes — `steps.sanitized.outputs.text`; unsanitized OK within sandboxed agent job.
- **Note:** Most-open untrusted-input surface; no approval gate; lower visibility/monitoring than issues or PRs.
- **Integrity filtering:** `approved` for privileged operations (e.g., labeling or routing discussions). `unapproved` for community Q&A bots (e.g., auto-reply with `add-comment` only), paired with tight `safe-outputs`.

## [`workflow_call`](../triggers/workflow-call.md) — ☢️ Use with extreme caution
- **Authz:** Inherits caller's. Callee cannot enforce its own role check.
- **Approval gate:** Inherits caller's.
- **Copilot events:** Inherits caller's.
- **Concurrency:** Layers on top of caller's.
- **Note:** `secrets: inherit` hands every secret to the callee. Pin by SHA, not branch. Undeclared trust boundary.
- **Integrity filtering:** `approved` as defense-in-depth; caller's trigger determines the real threat model.

## [`workflow_run`](../triggers/workflow-run.md) — ☢️ Use with extreme caution
- **Authz:** Indirect (whoever triggered the upstream workflow).
- **Approval gate:** **Not subject** — this is what makes it dangerous. No human gate between fork PR's CI and privileged downstream.
- **Copilot events:** All workflow completions trigger, including `GITHUB_TOKEN`-triggered ones.
- **Runs:** With **full secrets and write token** on the **default branch**.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.workflow_run.id }}`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`. Treat all downloaded artifacts as untrusted.
- **Alternatives:** [`schedule`](../triggers/schedule.md), [`issue_comment`/`slash_command:`](../triggers/comment-and-slash-command.md).
- **Integrity filtering:** `approved`; `merged` for production-content-only workflows. `unapproved`/`none` carries full-secrets warning. Does not filter downloaded artifacts.

## [`pull_request`](../triggers/pull-request.md) — ⛔ Avoid (public repos) · ☢️ Use with extreme caution (private repos)
- **Authz:** Anyone who can open a PR (incl. fork contributors). `on.roles: all` is acceptable.
- **Approval gate:** **Subject.** The existence of *any* `pull_request` workflow causes the button, and clicking approves *all* gated workflows. Button appears even when the contributor doesn't match `on.roles:`.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Runs:** Read-only `GITHUB_TOKEN`, no secrets for fork PRs[^token-permissions]. The safer of the two PR triggers.
- **Concurrency:** `${{ github.workflow }}-${{ github.event.pull_request.number }}`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}`
- **Alternatives:** [`schedule`](../triggers/schedule.md), [`issue_comment`/`slash_command:`](../triggers/comment-and-slash-command.md), [`labeled`/`label_command:`](../triggers/labeled-and-label-command.md).
- **Integrity filtering:** `approved`. Fork contributors are typically `CONTRIBUTOR` or lower, so their PR content is filtered out before the agent sees it.

## [`pull_request_target`](../triggers/pull-request-target.md) — ⛔ Avoid (public repos) · ☢️ Use with extreme caution (private repos)
- **Authz:** Same actors as `pull_request`. `on.roles: all` only with ☢️ extreme caution.
- **Approval gate:** **Subject** — and this is where it's most dangerous. Clicking approves *all* gated workflows with full secrets. Assume the button will always be clicked — by someone trying to approve a different workflow.
- **Copilot events:** `GITHUB_TOKEN` does not trigger; GitHub App tokens/PATs do.
- **Runs:** On the **base ref** with **full `GITHUB_TOKEN` and all secrets**. Most-exploited vulnerability class[^pwn-requests].
- **Concurrency:** `${{ github.workflow }}-${{ github.event.pull_request.number }}`.
- **Fork guard:** `if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}` + gate agent on `github.event.pull_request.head.repo.fork == false`. **Never check out PR head SHA.**
- **Sanitize:** Yes critically — `steps.sanitized.outputs.text`; unsanitized OK within sandboxed agent job coupled with proper `safe-outputs`.
- **Alternatives:** [`schedule`](../triggers/schedule.md), [`issue_comment`/`slash_command:`](../triggers/comment-and-slash-command.md), [`labeled`/`label_command:`](../triggers/labeled-and-label-command.md).
- **Integrity filtering:** `approved`. Fork contributors are typically `CONTRIBUTOR` or lower, so their PR content is filtered out before the agent sees it. Does not protect against checking out PR head SHA.

---

## Triggers without standalone pages

These either have low headline risk or are irrelevant to agentic workflows in this repo's context.

## `repository_dispatch`
- **Authz:** PAT with `repo` scope.
- **Approval gate:** Not subject.
- **Copilot events:** API-triggered only.
- **Runs:** With base secrets.
- **Unintended:** Anyone with the PAT can fire arbitrary `event_type` strings.

## Other standard events
`branch_protection_rule`, `check_run`, `check_suite`, `create`/`delete`, `deployment`/`deployment_status`, `fork`, `gollum`, `label`, `member`, `merge_group`, `page_build`, `project*`, `public`, `registry_package`, `status`, `watch` — see the [GitHub Events documentation](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows) for activity types and default-branch-only flags. These are generally low-risk for agentic workflows: they either require admin-level actions, fire rarely, or have no untrusted-input surface.

[^token-permissions]: GitHub Docs, [Automatic token authentication](https://docs.github.com/en/actions/security-guides/automatic-token-authentication)
[^pwn-requests]: GitHub Security Lab, [Keeping your GitHub Actions and workflows secure: Preventing pwn requests](https://securitylab.github.com/research/github-actions-preventing-pwn-requests/)

---

| [Table of Contents](../../README.md) |
