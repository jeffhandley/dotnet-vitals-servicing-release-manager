---
title: "The 'Apparent vs. Actual' Trigger Surface"
---

[← Previous: The "Approve and run workflows" Gate](approve-and-run-gate.md) | [Table of Contents](../../README.md) | [Next: Operating Within a Fork →](operating-in-a-fork.md)

# The "Apparent vs. Actual" Trigger Surface

Many of the agentic workflow event triggers are simulated conveniences that operate over broader underlying triggers. For example, [`slash_command:`](../triggers/comment-and-slash-command.md) seemingly triggers only when somebody comments `/backport`. **In reality**, it runs on every issue open/edit, every PR open/edit, every comment, every PR review comment, every discussion, and every discussion comment in the repo — and *then* decides to skip if the first word isn't `/backport`.

The official docs acknowledge this in two places:

> *"By default, command triggers listen to all comment-related events, which can create skipped runs in the Actions UI. Use the `events:` field to restrict where commands are active."*[^command-doc]

> *"The command must be the **first word** of the comment or body text to trigger the workflow. This prevents accidental triggers when the command is mentioned elsewhere in the content."*[^command-doc]

The first quote concedes the noise; the second concedes that even narrow matching is *post-event*, not *pre-event*.

Review the [Triggers | GitHub Agentic Workflows](https://github.github.com/gh-aw/reference/triggers/) documentation and experiment with the `gh aw compile` command to ensure the desired trigger doesn't have a broader subscription than expected and accepted for the scenario.

## Why this matters operationally

| Consequence | Mechanism |
|---|---|
| **Runner consumption** | Pre-activation job uses `ubuntu-latest` for ~5–30 s per skipped run; on a busy repo with `slash_command:` on a single small command this can be hundreds of skipped runs/day. |
| **Copilot token consumption** | Zero — the agent step is gated by the activation output and never runs in skipped cases. The COPILOT_GITHUB_TOKEN selection happens *in pre-activation* but no model requests are made. |
| **Concurrency cancellations** | If `concurrency.group` is shared across the workflow, a non-matching activation can cancel an in-progress matching one. See [Concurrency and Race Conditions](concurrency-and-races.md). |
| **Actions UI noise** | Operators learn to ignore "skipped" runs and miss real failures. |
| **Audit surface** | Every skipped run still emits webhook events for `workflow_run` listeners downstream. |
| **Approval prompts (forks)** | A first-time fork contributor opening *any* PR can produce an "Approve and run" entry on a `slash_command` workflow whose intent had nothing to do with PRs — see [The "Approve and run workflows" Gate](approve-and-run-gate.md). |

[^command-doc]: gh-aw [Command Triggers reference](https://github.github.com/gh-aw/reference/command-triggers/)

---

[← Previous: The "Approve and run workflows" Gate](approve-and-run-gate.md) | [Table of Contents](../../README.md) | [Next: Operating Within a Fork →](operating-in-a-fork.md)
