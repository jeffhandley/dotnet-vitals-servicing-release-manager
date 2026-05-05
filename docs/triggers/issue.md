---
title: "issues"
---

[← Previous: labeled / label_command:](labeled-and-label-command.md) | [Common Triggers](../chapters/triggers.md) | [Next: schedule →](schedule.md)

# `issues`

### ✅ Recommended

Automatically processing issues as they are filed is a primary scenario for workflows and especially agentic workflows. When using agentic workflows, the untrusted inputs from the issue title and description can be safely processed within the agent sandbox, and the workflow's safe-outputs limit the possible operations that result from the issue trigger.

Be aware of subtle event type pitfalls though, such as `edited`, `assigned`, and `labeled`. A read-only contributor can open an issue with a benign body, wait for a privileged approval gate to be applied, then edit the issue body to inject a hostile payload.

---

## Scenarios

- Auto-triage on `opened` — classify, label, assign, link related issues
- Welcome / templated reply for first-time authors
- Project-board syncing on `opened` / `closed` / `reopened`
- **Avoid** `.edited` unless you've designed for re-execution — it re-fires for every old issue edit

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | Acceptable to set `all` — anyone can file an issue and workflows generally should run on all of them. Restricting to `[admin, maintainer, write]` (default) is a special case for workflows that should only react to maintainer-filed issues. `triage` is excluded by default — add it explicitly when triage-role users are the primary operators. |
| Activity types | `[opened]` is safest. Add `edited` only with re-execution design; never default to `[opened, edited, reopened]` without thinking. |
| Concurrency | `${{ github.workflow }}-${{ github.event.issue.number }}`. Not critical with `[opened]` only (each issue opens once). If supporting `edited`/`reopened`, determine the appropriate `cancel-in-progress` behavior — losing in-flight triage to a follow-up edit is usually worse than queuing. |
| Idempotency | **Required.** Deterministic comment markers, upsert labels, check-before-create on follow-up issues. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | See [Bot Filtering](https://github.github.com/gh-aw/reference/frontmatter/#bot-filtering-onbots) and [Skip Bots](https://github.github.com/gh-aw/reference/frontmatter/#skip-bots-onskip-bots). |
| Sanitize payload? | **Yes, always** in pre-agent steps. `issue.body` and `issue.title` are user-controlled; use `steps.sanitized.outputs.text`, never raw `${{ github.event.issue.body }}`. Acceptable to handle unsanitized payload within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | `add-labels`, `add-comment`. Avoid `update-issue` on `.edited` (creates feedback loops). |
| Integrity filtering | `approved` (default) for outputs that require triage+ permissions (`add-labels`, `close`, `update-issue`). Lower to `unapproved` or `none` only when the workflow intentionally reads community content (e.g., duplicate detection) and limits `safe-outputs` to actions any user can perform (`add-comment`, author editing their own description). Must pair with tight `safe-outputs` and `on.roles:`. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: labeled / label_command:](labeled-and-label-command.md) | [Common Triggers](../chapters/triggers.md) | [Next: schedule →](schedule.md)
