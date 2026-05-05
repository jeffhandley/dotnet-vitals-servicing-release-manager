---
title: "release"
---

[← Previous: schedule](schedule.md) | [Common Triggers](../chapters/triggers.md) | [Next: milestone →](milestone.md)

# `release`

### ✅ Recommended

---

## Scenarios

- Post-release automation — generate release notes, update changelogs, post announcements to discussions
- Artifact publishing — trigger downstream package builds, container image pushes, documentation site deploys
- Follow-up issue creation — file tracking issues for the next milestone after a release is published
- Pre-release gating — react to `prereleased` to run validation before promoting to `released`

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | N/A — creating/publishing releases requires write+ access. |
| Activity types | `[published]` is the most common and safest — fires when a release is published (including from draft). `released` fires when a release is moved out of pre-release. Add `prereleased` for pre-release validation workflows. **Avoid** `edited` unless you've designed for re-execution — editing an old release's notes fires today with today's secrets. |
| Concurrency | `${{ github.workflow }}-${{ github.event.release.tag_name }}`. `cancel-in-progress` depends on whether you want multiple rapid edits to a release to stack or replace. |
| Idempotency | **Required.** Releases can be deleted and re-created, or edited after publication — downstream automation must be safe to repeat. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. Releases are upstream-only but forked repos can create their own. |
| Approval gate | Not subject to the "Approve and run workflows" button. |
| Copilot events | Releases created via `GITHUB_TOKEN` **do not** trigger. Releases via GitHub App tokens or PATs **do**. |
| Sanitize payload? | Release name and body are maintainer-controlled and generally trusted (write access required). Acceptable to handle unsanitized payload within the agent job (sandboxed), coupled with proper `safe-outputs`. |
| Safe-outputs | `create-issue`, `add-comment`, `create-discussion`, `create-pull-request` for post-release automation. `workflow_dispatch` via `gh workflow run` if triggering downstream workflows. |
| Integrity filtering | `approved` (default) for outputs that require triage+ permissions. `unapproved` or `none` when scanning community issues to close resolved items — must pair with tight `safe-outputs`. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: schedule](schedule.md) | [Common Triggers](../chapters/triggers.md) | [Next: milestone →](milestone.md)
