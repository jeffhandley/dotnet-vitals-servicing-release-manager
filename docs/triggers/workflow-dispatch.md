---
title: "workflow_dispatch"
---

[← Previous: Triggers](../chapters/triggers.md) | [Common Triggers](../chapters/triggers.md) | [Next: labeled / label_command: →](labeled-and-label-command.md)

# `workflow_dispatch`

### ✅ Recommended

**`workflow_dispatch` is the most-trusted trigger we have — users with write access can run any dispatchable workflows.** The `inputs:` defined as free-form strings are considered trusted input because of the write+ permission gate. Operations performed are attributed to the user who dispatched the workflow.

Note that branch selection is also user-controlled: a write-role contributor can run the dispatch against a branch they just pushed (or a long-stale branch) where the workflow YAML is different from `main`'s — meaning the version of the workflow they're invoking may have less-restrictive `permissions:`, weaker `safe-outputs.allowed:`, or an attacker-friendlier prompt. The `main`-branch version is not what runs; _the selected branch's_ version is.

---

## Scenarios

- **Pair with almost every other trigger** so the same workflow can be invoked manually for testing, troubleshooting a stuck run, or off-schedule execution. gh-aw auto-injects `workflow_dispatch` for `slash_command:`, `label_command:`, schedule shorthands, and most trigger shorthands; for any other trigger, add `workflow_dispatch:` explicitly. The only triggers it doesn't pair cleanly with are `workflow_call` (a callee, not an event) and `workflow_run` (which dispatches downstream of another workflow).
- Manual escape hatch for any workflow — debugging, troubleshooting, catch-up runs, ad-hoc invocation
- Administrative operations that should only be invoked deliberately (e.g., release cutting)
- The explicit escape hatch for the fork guard pattern — fork owners can manually dispatch to intentionally test workflows in their fork
- Passing free-form prompt content into agentic workflows via string `inputs:` — the invoker is a write+ user whose input is trusted

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | N/A — only write+ users can invoke `workflow_dispatch`. gh-aw does not inject a role check for `workflow_dispatch`-only workflows. |
| Activity types | Define `inputs:` with constrained types (choice, boolean, number) for structured parameters, and free-form strings for prompt content to pass into agentic workflows. Recommended to pair with almost every other trigger so the workflow is manually invocable for testing, troubleshooting, and off-schedule runs. |
| Concurrency | Generally not a concern for workflows exclusively dispatched manually. For workflows that also have event-driven triggers, match the concurrency group of the primary trigger. |
| Idempotency | **Recommended.** Manual invocations may be retried or fired multiple times by different users. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` — fork owners can dispatch in their own fork against any branch. This is intentional — `workflow_dispatch` is the explicit escape hatch in the fork guard pattern. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | N/A. |
| Sanitize payload? | **No** — `workflow_dispatch` inputs come exclusively from write+ users and are treated as trusted. |
| Safe-outputs | Depends on workflow purpose. No inherent restriction — but audit the blast radius given that any write-role contributor can invoke with any inputs against any branch. |
| Integrity filtering | `approved` (default) for outputs that require triage+ permissions. `unapproved` or `none` when the dispatched workflow intentionally consumes community content — must pair with tight `safe-outputs`. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: Triggers](../chapters/triggers.md) | [Common Triggers](../chapters/triggers.md) | [Next: labeled / label_command: →](labeled-and-label-command.md)
