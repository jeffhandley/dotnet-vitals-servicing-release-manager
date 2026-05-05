---
title: "labeled and label_command"
---

[← Previous: workflow_dispatch](workflow-dispatch.md) | [Common Triggers](../chapters/triggers.md) | [Next: issues →](issue.md)

# `pull_request: types: [labeled]` and `label_command:`

### ✅ Recommended

`label_command:` turns a label application into a **structured one-shot agentic operation** — applying a label like `backport`, `run-benchmarks`, or `run-outerloop` fires the workflow, runs the agent, and _optionally_ removes the label automatically to signal completion. Compared to [`slash_command:`](comment-and-slash-command.md), `label_command:` carries a **higher authorization floor**: label application requires triage-or-higher role, not just the ability to post a comment. Setting `remove_label: false` shifts the pattern from a one-shot command to a **persistent state marker**, where the label's presence on the issue or PR drives continuous workflow behavior.

**`label_command:` inherits the same broad-subscription concurrency shape as `slash_command:`.** It subscribes to *every* `labeled` event on the configured event types (`issues`, `pull_request`, `discussion`) — the workflow runs (consuming a runner) on every label application by every triage-or-higher user, and only the name match causes early abort. Like `slash_command:`, concurrency groups cannot safely use `cancel-in-progress: true` (a benign label change on the same target would cancel an in-flight command). Therefore `label_command:` workflows **should be implemented to be idempotent**: check before acting, no-op if the work is already in progress or already done.

The audit trail is preserved but *subtle* — the issue/PR event stream shows `monalisa added the backport label` immediately followed by `github-actions[bot] removed the backport label`.

**The label application is itself the human-in-the-loop approval gate** — there is no separate "Approve and run workflows" prompt. Because it serves as the gate, the workflow must be designed to assume mistakes will happen: a label can be applied to the wrong issue, re-applied after removal, or applied inadvertently during bulk edits.

---

## Scenarios

- **Higher authorization floor than `slash_command:`** — applying a label requires triage+ role, which is a meaningful step up from read-only comment access
- One-shot agentic operations triggered by applying a label — `backport`, `run-benchmarks`, `run-outerloop`
- `label_command:` with `remove_label: false` turns the label into a persistent state marker rather than a one-shot command — `benchmarks-approved`, `outerloop-approved`

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | `[admin, maintainer, write]` is the default but **`triage` is the natural fit here** — any triage-or-higher user can apply a label, so `on.roles:` should match the actual authorization floor. Restricting to `write` or higher means the workflow fires (consuming a runner) on every triage-user label application but the agent step is denied, which compounds the concurrency/idempotency problem and makes the workflow appear buggy to the user who applied the label. Set to `triage` explicitly for most label-driven workflows. |
| Activity types | For raw `issues`/`pull_request`/`discussion`: `types: [labeled]` only. For `label_command:`: configured via the label name; `events:` defaults to `[issues, pull_request, discussion]` — **narrow this to the event types you actually need**. Include `discussion` only if the workflow is intentionally designed to handle discussion label events; leaving it in by default means every label applied to any discussion fires the workflow. |
| Concurrency | `${{ github.workflow }}-${{ github.event.issue.number }}`. **Use `cancel-in-progress: false`** as _any label_ application on the same target shares the group, so a benign label change would cancel an in-flight command. |
| Idempotency | **Strongly recommended.** Check before acting, no-op if already done. Bulk label operations (via GitHub UI or scripts) fire one event per item in rapid succession. `label_command:` auto-removes the label after activation, providing a natural one-shot signal, but the workflow must still be safe to re-run if the label is re-applied. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. Labels are upstream-only, but forked repos have their own label sets where these workflows would fire. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | See [Bot Filtering](https://github.github.com/gh-aw/reference/frontmatter/#bot-filtering-onbots) and [Skip Bots](https://github.github.com/gh-aw/reference/frontmatter/#skip-bots-onskip-bots). |
| Sanitize payload? | The label name is constrained, but the issue/PR body, title, and comments remain attacker-controlled; sanitize in pre-agent steps using `steps.sanitized.outputs.text`, never raw `${{ github.event.issue.body }}`. Acceptable to handle unsanitized payload within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | Depends on command purpose. `add-labels`, `add-comment` for state transitions. Audit against `on.roles:` — triage users can apply labels, so every safe-output is reachable by the triage role if included. |
| Integrity filtering | `none` — the label application by a triage+ user *is* the human-in-the-loop gate, replacing the need for integrity filtering to gate content trust. Must pair with tight `safe-outputs` and `on.roles:` including `triage`. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: workflow_dispatch](workflow-dispatch.md) | [Common Triggers](../chapters/triggers.md) | [Next: issues →](issue.md)
