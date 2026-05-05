---
title: "pull_request_review_comment"
---

[← Previous: pull_request_review](pull-request-review.md) | [Common Triggers](../chapters/triggers.md) | [Next: push →](push.md)

# `pull_request_review_comment`

### ⚠️ Use with caution

An exceptionally noisy event that likely fires more often than needed. Most workflow authors think there is one "PR comments" trigger. There are actually three distinct events: [`issue_comment`](comment-and-slash-command.md) (top-level PR conversation), [`pull_request_review`](pull-request-review.md) (the review body), and `pull_request_review_comment` (the inline line-level comment).

Workflows that subscribe to `issue_comment` for a `/command` or for prompt content will silently miss inline review comments — but the inverse is more troublesome: a workflow subscribed to `pull_request_review_comment` fires on every line-level scribble by anyone with read access.

Concurrency twist: when this trigger is mixed into a PR-handling workflow alongside `pull_request` and `pull_request_review`, group-key choice is even worse than for `pull_request_review` alone — the inline-comment payload doesn't always have a clean `pull_request.number` at the top level (you have to dig through `event.pull_request` which is sometimes null in deleted-comment payloads).

---

## Scenarios

- Responding to inline code-review comments with agent-generated explanations or fixes
- `/command` handling in inline review threads (note: `slash_command:` can subscribe to this event)
- Auto-resolving review threads when the referenced code changes

**Why ⚠️:** This trigger is very noisy, triggering for all inline comments tied to specific code lines. When mixed into a multi-trigger workflow alongside `pull_request` and `pull_request_review`, concurrency group-key choice becomes very difficult.

**Recommended alternatives:**
- **[`labeled` / `label_command:`](labeled-and-label-command.md)** — a maintainer applies a label to signal the PR is ready for agent-assisted review response.
- **[`schedule`](schedule.md)** — poll for PRs with pending inline comments periodically.

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | `[admin, maintainer, write]` (default). Acceptable to set `all` — same considerations as `issue_comment` and `pull_request_review`. |
| Activity types | `[created]` is safest. **Avoid** `edited` — same time-bomb as `issue_comment.edited`. `deleted` is rarely useful for agentic workflows. |
| Concurrency | `${{ github.workflow }}-${{ github.event.pull_request.number }}`. Use `cancel-in-progress: false`. **Prefer a separate workflow** from `pull_request` and `pull_request_review` to avoid group-key collisions — the inline-comment payload's `pull_request.number` is sometimes null in deleted-comment payloads. |
| Idempotency | **Required.** Multiple inline comments fire individually in rapid succession (e.g., a reviewer submitting a batch review with 10 line comments). |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. |
| Approval gate | Not directly subject to the "Approve and run workflows" button. However, if the workflow also subscribes to `pull_request`, the gate applies to those events. |
| Copilot events | See [Bot Filtering](https://github.github.com/gh-aw/reference/frontmatter/#bot-filtering-onbots) and [Skip Bots](https://github.github.com/gh-aw/reference/frontmatter/#skip-bots-onskip-bots). |
| Sanitize payload? | **Yes, always** in pre-agent steps. Comment body is user-controlled; use `steps.sanitized.outputs.text`, never raw `${{ github.event.comment.body }}`. Acceptable to handle unsanitized payload within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | `add-comment` for reply threads. Avoid broad mutations — inline review comments are high-volume and the workflow should be lightweight. |
| Integrity filtering | `approved`. `unapproved` or `none` when intentionally consuming community review content — must pair with tight `safe-outputs`. Inline comments are the highest-leverage prompt-injection surface, making the integrity level choice especially important. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: pull_request_review](pull-request-review.md) | [Common Triggers](../chapters/triggers.md) | [Next: push →](push.md)
