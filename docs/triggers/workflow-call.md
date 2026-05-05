---
title: "workflow_call"
---

[← Previous: discussion_comment](discussion-comment.md) | [Common Triggers](../chapters/triggers.md) | [Next: workflow_run →](workflow-run.md)

# `workflow_call`

### ☢️ Use with extreme caution

**`workflow_call` is the trigger that *looks* like safe encapsulation but actually creates an undeclared trust boundary — and the called workflow runs with whatever permissions the caller had, which the callee author did not control.** A reusable agentic workflow exposes `inputs:` that downstream callers populate from `${{ github.event.* }}` — the issue body, PR title, comment text, etc. The callee author can sanitize inside the called workflow, but **they cannot enforce that callers don't pipe attacker-controlled prose straight in**. Worse for token surface area: `secrets: inherit` is the convenient knob that hands every secret in the calling repo to the called workflow — including Copilot PATs, deployment tokens, and any cross-repo secret. A maintainer adding `uses: org/shared-workflows/.github/workflows/triage.yml@main` does not see a reviewable diff of what `triage.yml` will do with their secrets; pinning to `@main` (or any moving ref) means the upstream maintainer can change the agent prompt, swap the model, or add new safe-outputs **without the calling repo seeing the change**. Pin by SHA. And note: the calling repo's branch protection / approval gates **do not apply to the called workflow's behavior** — only to the act of merging the caller's YAML.

---

## Scenarios

- Reusable workflow libraries — shared triage, review, or deployment logic across multiple repos
- Encapsulating complex multi-step agentic patterns behind a clean `inputs:` interface
- gh-aw shared components (`.github/workflows/shared/*.md`) compile to `workflow_call`

**Why ☢️:** The called workflow runs with whatever permissions the *caller* had — which the callee author did not control. `secrets: inherit` hands every secret in the calling repo to the callee. Pinning to `@main` (or any moving ref) means the callee author can change behavior without the calling repo seeing the change.

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | N/A — inherits from the caller. The callee cannot enforce its own role check. |
| Activity types | N/A — `workflow_call` has no activity types. Define `inputs:` and `secrets:` on the callee. |
| Concurrency | Inherits the caller's concurrency group. The callee can declare its own `concurrency:` but it layers on top of the caller's, not replaces it. |
| Idempotency | **Recommended.** The callee should be safe to re-invoke — callers may retry or call from multiple triggers. |
| Fork posture | Inherits the caller's fork posture. The callee cannot add its own fork guard — it doesn't have access to `github.event.repository.fork` in a meaningful way. |
| Approval gate | Inherits from the caller. The callee's own branch protection / approval gates do not apply to the caller. |
| Copilot events | Inherits from the caller. |
| Sanitize payload? | **Yes.** The callee cannot enforce that callers don't pipe attacker-controlled prose straight into `inputs:`. Sanitize all inputs inside the called workflow. Acceptable to handle unsanitized inputs within the agent job (sandboxed), coupled with proper `safe-outputs`. **Pin by SHA**, not by branch — `uses: org/shared-workflows/.github/workflows/triage.yml@<sha>`. |
| Safe-outputs | Declared by the callee — but the caller's `permissions:` and secrets are what back them. Audit the callee's safe-outputs against the caller's permission set. |
| Integrity filtering | `approved` as defense-in-depth. The caller's trigger determines the real threat model — the callee inherits the caller's permissions and secrets and cannot know which trigger invoked it. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: discussion_comment](discussion-comment.md) | [Common Triggers](../chapters/triggers.md) | [Next: workflow_run →](workflow-run.md)
