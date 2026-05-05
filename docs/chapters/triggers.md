---
title: "Triggers"
---

[← Previous: Tenets](tenets.md) | [Table of Contents](../../README.md) | [Next: workflow_dispatch →](../triggers/workflow-dispatch.md)

# Triggers

Each page in this section covers one common trigger: when to use it, cross-cutting concern profiles, and headline notes. There are many other triggers not covered that are not commonly used; they appear in [Appendix A: Trigger-by-Trigger Risk Profile](../appendices/trigger-risk-profile.md).

The guidance below is shown for both **public** and **private** repositories. Private repos have a fundamentally different trust model — all actors have been granted access by the organization — which eliminates several risk vectors (untrusted fork contributors, anonymous spamming, the "Approve and run workflows" button). Where the classification differs, the Private column reflects the reduced threat surface.

---

## Guidance key

| | Guidance | Meaning |
|---|---|---|
| ✅ | **Recommended** | Well-understood, safe defaults, fewest overall pitfalls. |
| ⚠️ | **Use with caution** | Legitimate uses exist but the trigger has sharp edges that require deliberate configuration. |
| ☢️ | **Use with extreme caution** | Runs with full secrets and/or creates undeclared trust boundaries; the failure mode is repo compromise. |
| ⛔ | **Avoid** | Causes the "Approve and run workflows" button or other structural problems; prefer the recommended alternatives listed on the page. |

## ✅ Recommended

| Trigger | Headline |
|---|---|
| [`workflow_dispatch`](../triggers/workflow-dispatch.md) | Paired with most triggers; manual escape hatch for debugging and ad-hoc runs. Any write-role user can fire against any branch. |
| [`labeled` / `label_command:`](../triggers/labeled-and-label-command.md) | Preferred alternative to `pull_request` and `pull_request_target`. Human-in-the-loop gate via triage+ label application; preferred for on-demand operations. Should implement concurrency and idempotency logic. |
| [`issues`](../triggers/issue.md) | Best trigger for immediate, community-facing issue workflows with `safe-outputs` that do not require a human-in-the-loop gate. |
| [`schedule`](../triggers/schedule.md) | Preferred alternative to `pull_request` and `pull_request_target`. Implemented as a polling workflow that does not require a human-in-the-loop gate. Must implement concurrency and idempotency logic. |
| [`release`](../triggers/release.md) | Post-release automation — release notes, follow-up issues, downstream notifications. Trusted trigger (write+). |
| [`milestone`](../triggers/milestone.md) | Release management automation on milestone close. Release notes, follow-up issues, downstream notifications. Trusted trigger (write+). |

## ⚠️ Use with caution (public repos)<br />✅ Recommended (private repos)

| Trigger | Headline |
|---|---|
| [`issue_comment` / `slash_command:`](../triggers/comment-and-slash-command.md) | On-demand operations via `/slash_command` comments. Preferred over `pull_request` or `pull_request_target`, but in public repositories the broad underlying event subscription causes concurrency, idempotency, spamming, and escalation of privilege pitfalls. Recommended alternative in public repositories: `schedule`. |


## ⚠️ Use with caution

| Trigger | Headline |
|---|---|
| [`pull_request_review`](../triggers/pull-request-review.md) | Post-approval automation by filtering to approved event from users with write+ roles. Beware of concurrency/idempotency, broad underlying event subscription, and potential for privilege escalation. Recommended alternative: `labeled`/`label_command:` or `schedule`. |
| [`pull_request_review_comment`](../triggers/pull-request-review-comment.md) | Immediate triggering from inline code-review comments (many triggers per pull request review). Beware of concurrency, idempotency, noisy underlying trigger, and privilege escalation risks. Recommended alternatives: `labeled`/`label_command:` or `schedule`. |
| [`push`](../triggers/push.md) | Post-merge automation on push to specified `branches:` filter. Bare `on: push` (without `branches:`) fires on every branch and is the trigger most likely to cause silent cost blowout. |
| [`discussion`](../triggers/discussion.md) | Community Q&A routing triggered by discussion creation/edits. Most-open untrusted-input surface with no approval gate and lower visibility than issues. Prefer `schedule` for periodic processing. |
| [`discussion_comment`](../triggers/discussion-comment.md) | Community Q&A routing triggered by comments on discussions. Most-open untrusted-input surface with no approval gate and lower visibility than issues. Prefer `schedule` for periodic processing. |

## ☢️ Use with extreme caution

| Trigger | Headline |
|---|---|
| [`workflow_call`](../triggers/workflow-call.md) | Reusable workflow libraries. Undeclared trust boundary — callee inherits caller's permissions and secrets. `secrets: inherit` hands every secret to the callee. Pin by SHA, not branch. |
| [`workflow_run`](../triggers/workflow-run.md) | Post-CI actions with full secrets. `pull_request_target`'s quieter sibling — launders untrusted fork artifacts into a privileged context with no approval gate and no UI signal. Recommended alternatives: `labeled`/`label_command:` or `schedule`. |

## ⛔ Avoid (public repos)<br />☢️ Use with extreme caution (private repos)

| Trigger | Headline |
|---|---|
| [`pull_request`](../triggers/pull-request.md) | Causes the "Approve and run workflows" button for all external contributor PRs (including Copilot). Can be safely used, even with fork PRs, but the button's existence creates alert fatigue that undermines the overall security model. Has the same concurrency/idempotency challenges as the recommended alternatives: `labeled`/`label_command:` or `schedule`. |
| [`pull_request_target`](../triggers/pull-request-target.md) | The trigger most likely to get a repo pwned. Runs on base ref with full secrets and write token, but often used to consume the PR's untrusted HEAD input. Causes the "Approve and run workflows" button for all external contributor PRs (including Copilot). Can be safely used when extreme care is taken, but the button's existence creates alert fatigue that undermines the security model. Has the same concurrency/idempotency challenges as the recommended alternatives: `labeled`/`label_command:` or `schedule`. |

> **No trigger is free of pitfalls.** Each page assumes you've internalized the [Tenets](tenets.md) — the pitfall framing is what they look like in production when a tenet is silently violated.

---

[← Previous: Tenets](tenets.md) | [Table of Contents](../../README.md) | [Next: workflow_dispatch →](../triggers/workflow-dispatch.md)
