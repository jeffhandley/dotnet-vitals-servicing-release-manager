---
title: "discussion"
---

[← Previous: push](push.md) | [Common Triggers](../chapters/triggers.md) | [Next: discussion_comment →](discussion-comment.md)

# `discussion`

### ⚠️ Use with caution

**`discussion` is the most-open untrusted-input surface in the GitHub model and it has no approval gate.** Anyone with repository read access can fire `discussion.created` or `discussion.edited`. The workflow runs with its declared `permissions:` and `safe-outputs:`, which the author thinks of as "what we can do" but is actually "what any read-user can cause us to do." The default `on.roles:` allow-list stops the agent step for read-role actors, but the workflow still queues a runner, and any pre-agent step that does something with the discussion content runs anyway.

---

## Scenarios

- Community Q&A bots — auto-respond to questions in a specific discussion category
- Knowledge-base classification — categorize, label, or route discussions to the right team
- FAQ automation — detect duplicate questions and link to existing answers

**Why ⚠️:** `discussion` is a wide-open untrusted-input surface. Anyone with repository read access can create or edit a discussion — no approval gate, no fork boundary, yet much lower visibility/monitoring than issues or pull requests. The default `on.roles:` allow-list stops the agent step for read-role actors, but the activation job still queues a runner and any pre-agent step that touches discussion content runs anyway.

**Recommended alternatives:**
- **[`schedule`](schedule.md)** — prefer periodic processing if immediate triggering is not needed. Avoids the spamming and untrusted-input surface.
- **[`labeled` / `label_command:`](labeled-and-label-command.md)** — a maintainer applies a label to a discussion to trigger processing. Eliminates the broad event subscription; label application is a triage+ action so the authorization gate is built in.

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | `[admin, maintainer, write]` (default). Acceptable to set `all` for community-facing Q&A bots — same considerations as `issues`. Audit every safe-output if broadening. |
| Activity types | `[created]` is safest. Add `edited` only if you've designed for re-execution against old discussions. `category_changed` is useful for routing workflows. |
| Concurrency | `${{ github.workflow }}-${{ github.event.discussion.number }}`. Use `cancel-in-progress: false` to prevent a benign category change or edit from canceling in-flight work. |
| Idempotency | **Required.** Same discipline as issues — deterministic comment markers, upsert labels, check-before-create. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. Discussions are upstream-only, but forked repos can have Discussions enabled. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | See [Bot Filtering](https://github.github.com/gh-aw/reference/frontmatter/#bot-filtering-onbots) and [Skip Bots](https://github.github.com/gh-aw/reference/frontmatter/#skip-bots-onskip-bots). |
| Sanitize payload? | **Yes, always** in pre-agent steps. Discussion body and title are user-controlled; use `steps.sanitized.outputs.text`, never raw `${{ github.event.discussion.body }}`. Acceptable to handle unsanitized payload within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | `add-comment`, `add-labels`, `create-discussion`. No `update-issue` analog for discussions — discussions have limited structured mutation surface. |
| Integrity filtering | `approved` for privileged operations. `unapproved` for community Q&A bots that intentionally consume untrusted input — must pair with tight `safe-outputs` (e.g., `add-comment` only) and `on.roles:`. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: push](push.md) | [Common Triggers](../chapters/triggers.md) | [Next: discussion_comment →](discussion-comment.md)
