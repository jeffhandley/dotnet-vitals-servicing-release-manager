---
title: "schedule"
---

[← Previous: issues](issue.md) | [Common Triggers](../chapters/triggers.md) | [Next: release →](release.md)

# `schedule`

### ✅ Recommended

**`schedule` _seems_ like the wrong tool for work that should react to events** — and event-driven triggers are more intuitive for that use case. The catch is that event-driven triggers carry their own, often more serious, drawbacks: privilege-escalation risks, trust-boundary gaps, "Approve and run workflows" friction, and exposure to event spam. A scheduled polling workflow still has to **poll** to discover work, track its **own state** to avoid re-processing, and handle **overlapping cron firings** — if any of those are not implemented robustly, the workflow quietly posts duplicate comments, files duplicate issues, or applies duplicate labels every time the cron laps the prior run.

However, those are the same concurrency and idempotency challenges that every event-driven alternative carries: `pull_request`, `pull_request_target`, `issue_comment`, `pull_request_review`, and `pull_request_review_comment` are all equally subject to rapid re-fires, duplicate invocations, and overlapping runs — they just arrive via event payloads rather than polls. Using `schedule` therefore trades invocation latency for the elimination of more serious pitfalls: no triggering actor means no privilege-escalation risk, no trust-boundary ambiguity, and no approval-gate friction. The distributed-systems problems (deduplication, exactly-once processing, locking) remain — they just aren't compounded by the security and trust issues that event-driven triggers introduce.

**`schedule` is the trigger that keeps running quietly in the background without relying on any user interaction at all.** It should not conduct work where human-in-the-loop approval is needed without verifying an approval gate has been cleared—such as the existence of a label (with triage+ is sufficient). The "schedule" is also softer than it reads: GitHub runs the cron **when a runner becomes available**, not on a fixed wall clock — heavy load delays runs by minutes-to-hours, and during peak hours your every-15-minute job may run every hour.

---

## Scenarios

- Polling-based issue and PR operations that avoid the "Approve and run workflows" button — the preferred alternative to `pull_request`, `pull_request_target`, `issue_comment`, `pull_request_review`, `pull_request_review_comment`, and `issues` triggers
- Periodic housekeeping — stale issue/PR sweeps, label audits, project-board syncing
- Recurring reports — CI health, security audit, dependency freshness
- **Fewest compounding pitfalls of any trigger** — no event spamming, no user-triggered invocations, no race with drive-by comments; concurrency and idempotency challenges exist but are not compounded by privilege-escalation or trust-boundary risks

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | N/A — no actor involved. The workflow runs as the default branch's workflow file. |
| Activity types | N/A. Use gh-aw schedule shorthands (`daily`, `weekly on monday`, `every 10 minutes`, `daily around 14:00`, `daily between 9:00 and 17:00`) to spread load. Raw cron is also supported. **Strongly recommended to pair with `workflow_dispatch`** to allow manual triggers off-schedule (for debugging, catch-up runs, or ad-hoc invocation). |
| Concurrency | `${{ github.workflow }}` (no issue/PR number to key on). `cancel-in-progress` depends on whether overlapping cron firings should stack or replace — typically `true` for idempotent pollers. Concurrency and idempotency challenges exist — two overlapping cron firings can race just as event-driven triggers can — but they are not compounded by privilege-escalation or trust-boundary risks. |
| Idempotency | **Required.** Two overlapping cron firings must not double-process. Track state via structured `<!-- comments -->` inside a dedicated issue/PR comment — edit the comment's visible markdown to reflect the current workflow status (e.g. "⏳ in progress", "✅ done"), while appending `<!-- state-machine transition records -->` as an invisible, append-only audit trail — labels, or external state — but take note that cancellation and retries need to be incorporated into the state-tracking design. The workflow starts blind and must poll to discover work — design for exactly-once processing. |
| Fork posture | GitHub disables `schedule` triggers on forks by default until the fork owner re-enables them in the Actions tab. Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` as defense-in-depth. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | N/A — schedule is time-driven, not event-driven. |
| Sanitize payload? | No event payload to sanitize. However, the data the workflow *polls* (issue bodies, PR titles, comments) is still user-controlled — sanitize anything ingested from the GitHub API before passing to the agent. Acceptable to handle unsanitized polled data within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | Depends on workflow purpose. `add-labels`, `add-comment`, `create-issue`, `update-issue`, `create-pull-request`, `push-to-pull-request-branch` are all common for housekeeping pollers. Cost consciousness is critical — a scheduled "triage open issues" workflow written for 20 issues may still run years later against 2,000 issues at 100× the original per-run cost. |
| Integrity filtering | `approved` (default) for outputs that require triage+ permissions. `unapproved` when the workflow intentionally consumes community content (typical for processing issues or PRs) — must pair with tight `safe-outputs`. No triggering actor means no `on.roles:` gate; integrity filtering is the primary content-trust boundary. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: issues](issue.md) | [Common Triggers](../chapters/triggers.md) | [Next: release →](release.md)
