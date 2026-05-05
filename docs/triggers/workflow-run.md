---
title: "workflow_run"
---

[← Previous: workflow_call](workflow-call.md) | [Common Triggers](../chapters/triggers.md) | [Next: pull_request →](pull-request.md)

# `workflow_run`

### ☢️ Use with extreme caution

**`workflow_run` is [`pull_request_target`](pull-request-target.md)'s quieter sibling: it launders untrusted output from a fork PR's sandboxed build into a privileged context with full secrets — and nobody reviewing the upstream workflow knows that's about to happen.** The pattern that gets repos pwned: a `pull_request` workflow runs the fork's tests in a sandbox (correct), uploads test results / coverage reports / built artifacts as workflow artifacts (still fine), and then a separate `workflow_run`-triggered workflow downloads those artifacts and acts on them — posting a comment, updating a status check, deploying a preview, or feeding the artifact contents to an agent. **The artifact came from attacker-controlled fork code; the consuming workflow has full write tokens and secrets.** A malicious PR uploads an artifact named `coverage.json` whose contents include shell-injection or prompt-injection payloads and the consuming workflow shells out unsafely, or feeds the JSON to an agent that ingests the prompt-injection. There is no UI signal connecting the upstream PR to the downstream `workflow_run`; reviewers see "tests passed on a fork PR" and miss that a privileged workflow then ran with that PR's data. And `workflow_run` does **not** inherit the upstream's `pull_request` head SHA — the consuming workflow runs against the **default branch's** YAML, so attackers can rely on the production version of the consumer workflow even when the producer workflow on their fork is sandboxed.

---

## Scenarios

- Post-CI actions that need write access — posting coverage comments, updating status checks, deploying previews — after a sandboxed `pull_request` workflow completes
- Cross-workflow orchestration — react to completion of one workflow to trigger downstream processing

**Why ☢️:** `workflow_run` runs on the **default branch** with **full secrets and write token**, even when triggered by a fork PR's CI run. The classic pwn vector: a sandboxed `pull_request` workflow uploads artifacts, then a `workflow_run`-triggered workflow downloads and acts on those artifacts in a privileged context. There is no UI signal connecting the upstream PR to the downstream `workflow_run`, and no approval gate.

**Recommended alternatives:**
- **[`schedule`](schedule.md)** — poll for completed CI runs periodically instead of reacting in real-time. Avoids the artifact-smuggling vector entirely.
- **[`labeled` / `label_command:`](labeled-and-label-command.md)** — a maintainer applies a label after reviewing CI results to trigger post-CI actions.

## Profile

| Dimension | Recommendation |
|---|---|
| `on.roles:` | N/A — no direct actor. The workflow runs as the default branch's workflow file in response to another workflow's completion. |
| Activity types | `[completed]` is the typical choice. `requested` fires when the upstream workflow is queued (before it runs). `in_progress` fires when the upstream starts. **Always require `branches:` filters** — gh-aw enforces this. |
| Concurrency | `${{ github.workflow }}-${{ github.event.workflow_run.id }}` to key on the upstream run. |
| Idempotency | **Required.** The upstream workflow may be retried, producing multiple `workflow_run` events for the same logical CI pass. |
| Fork posture | Apply `if: ${{ github.event_name == 'workflow_dispatch' \|\| !github.event.repository.fork }}` to prevent running within a user's fork. **Critical:** this trigger fires for runs originating from fork PRs — treat all downloaded artifacts as untrusted. gh-aw enforces fork/repository-ID checks. |
| Approval gate | **Not subject** to the "Approve and run workflows" button — this is what makes it dangerous. There is no human gate between the fork PR's CI run and the privileged downstream workflow. |
| Copilot events | Workflow completions via any mechanism trigger `workflow_run` — including `GITHUB_TOKEN`-triggered workflows. |
| Sanitize payload? | **Yes, critically.** All artifacts downloaded from the upstream run must be treated as untrusted — they may contain shell-injection or prompt-injection payloads from attacker-controlled fork code. Acceptable to handle artifacts within the agent job (sandboxed), coupled with proper `safe-outputs`. Never shell out unsafely with artifact contents in pre-agent steps. |
| Safe-outputs | `add-comment`, `add-labels` for post-CI status posting. **Avoid** broad mutations — the workflow has full secrets and any safe-output is backed by a write token. |
| Integrity filtering | `approved` — filters fork contributor content, which is the primary attack vector for this trigger. `merged` when operating only on production content. `unapproved` or `none` carries an explicit additional warning: the full-secrets context makes the `safe-outputs` pairing requirement structurally weaker. Integrity filtering does **not** filter downloaded artifacts — treat all artifacts as untrusted. See [standard guidance](../chapters/authorization-and-roles.md#standard-guidance). |

---

[← Previous: workflow_call](workflow-call.md) | [Common Triggers](../chapters/triggers.md) | [Next: pull_request →](pull-request.md)
