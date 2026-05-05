---
title: "discussion_comment"
---

[← Previous: discussion](discussion.md) | [Common Triggers](../chapters/triggers.md) | [Next: workflow_call →](workflow-call.md)

# `discussion_comment`

### ⚠️ Use with caution

A read-only user can create a discussion, immediately comment on it with any payload, and edit either freely. Any agentic workflow that listens on `discussion_comment` — including `slash_command:` workflows by default — can be invoked at will by a drive-by user. The actor of the workflow run is the read-user but the actor of the workflow outputs is `github-actions[bot]`. If `safe-outputs:` allows `add-comment` and the comment lands on the triggering discussion, the bot output appears to endorse/reply-to the attacker prompt — lending upstream apparent authority to the attacker narrative.

---

## Scenarios

- Community Q&A responses — reply to follow-up questions on discussions
- `/command` handling in discussions via `slash_command:` (which subscribes to `discussion_comment` by default)
- Marking a discussion as answered based on comment content

**Why ⚠️:** A read-only user can create a discussion, immediately comment on it with any payload, and edit either freely. Same low-visibility concern as `discussion` — less monitored than issue/PR comments. The actor of the workflow outputs is `github-actions[bot]`, not the commenter — bot output appears to endorse/reply-to the commenter's content, lending upstream apparent authority to user-supplied narrative.

**Recommended alternatives:**
- **[`labeled` / `label_command:`](labeled-and-label-command.md)** — a maintainer applies a label to a discussion to trigger processing. Eliminates the broad event subscription and drive-by comment risk; label application is a triage+ action so the authorization gate is built in.
- **[`schedule`](schedule.md)** — prefer periodic processing if immediate triggering is not needed. Avoids the spamming and untrusted-input surface.

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | `[admin, maintainer, write]` (default). Acceptable to set `all` for community-facing bots — same considerations as `discussion` and `issues`. |
| Activity types | `[created]` is safest. Add `edited` only if designed for re-execution — same time-bomb as `issue_comment.edited`. |
| Concurrency | `${{ github.workflow }}-${{ github.event.discussion.number }}`. Use `cancel-in-progress: false` — same reasoning as `issue_comment`: any comment on the same discussion shares the group. |
| Idempotency | **Required.** Same discipline as `issue_comment` — check before acting, no-op if done. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | See [Bot Filtering](https://github.github.com/gh-aw/reference/frontmatter/#bot-filtering-onbots) and [Skip Bots](https://github.github.com/gh-aw/reference/frontmatter/#skip-bots-onskip-bots). |
| Sanitize payload? | **Yes, always** in pre-agent steps. Comment body is user-controlled; use `steps.sanitized.outputs.text`, never raw `${{ github.event.comment.body }}`. Acceptable to handle unsanitized payload within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | `add-comment`, `add-labels`. Audit against `on.roles:` — if `all`, every safe-output is reachable by anyone who can comment. |
| Integrity filtering | `approved` for privileged operations. `unapproved` for community-facing bots — same considerations as [`discussion`](discussion.md). See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: discussion](discussion.md) | [Common Triggers](../chapters/triggers.md) | [Next: workflow_call →](workflow-call.md)
