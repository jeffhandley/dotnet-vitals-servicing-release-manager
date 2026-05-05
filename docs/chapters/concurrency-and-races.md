---
title: "Concurrency and Race Conditions"
---

[← Previous: Operating Within a Fork](operating-in-a-fork.md) | [Table of Contents](../../README.md) | [Next: Authorization, Roles, and Read-Only Contributors →](authorization-and-roles.md)

# Concurrency and Race Conditions

GitHub's concurrency model[^concurrency]:

```yaml
concurrency:
  group: ${{ github.workflow }}-${{ github.event.issue.number }}
  cancel-in-progress: true
```

- **One run per group.** Newer queued runs cancel older queued runs in the same group.
- With `cancel-in-progress: true`, a newer run also cancels an in-progress older run.
- **Cancellation is asynchronous**: GitHub sends `SIGTERM`, waits up to 7500 ms, then `SIGKILL`. Already-running steps may complete; in-flight API calls may succeed.

## The "non-matching cancels matching" race

Concrete scenario: workflow `triage.md` has `slash_command: triage` and `concurrency.group: triage-${{ github.event.issue.number }}` with `cancel-in-progress: true`.

```
T+0  s: User comments "/triage" on issue #42 → run A starts; pre-activation matches; agent job begins.
T+10 s: Same user adds a clarifying comment "Actually, I meant the 1.x version." (no slash command).
        → issue_comment event fires; run B is queued.
        → Concurrency group "triage-42" already has run A in-progress → A is canceled.
        → Run B's pre-activation runs, sees no slash command, exits skip.
        → Net result: the triage agent for /triage was killed mid-run by an unrelated comment.
```

This is the **non-matching-cancels-matching pathology**. Mitigations:

1. **Narrow `slash_command` `events:`** so the workflow doesn't subscribe to events that can't possibly match — e.g., `events: [issue_comment]` when the slash command will only ever appear in comments.
2. **Make the concurrency group depend on the activation outcome.** This is hard — `concurrency:` is evaluated *before* the job starts and cannot reference job outputs. The practical workaround is to make the group narrower (e.g., include `${{ github.event.comment.id }}`) so unrelated events don't share the group.
3. **Don't set `cancel-in-progress: true`** for slash-command workflows; let them queue.
4. **Make outputs idempotent** (use deterministic comment markers, upsert labels, check for existing PRs) so a re-run produces the same end state.

## The pre-cancellation race

Even without conflicting groups, **steps that have already started will complete** before cancellation lands. This means:

- An agent that has already posted a comment cannot un-post it.
- A `safe-outputs.create-pull-request` that has already run cannot un-create the PR.
- A `bash` step mid-execution may write partial files, push partial commits, etc.

Concurrency is not a substitute for idempotency.

[^concurrency]: GitHub Docs, [Using concurrency](https://docs.github.com/en/actions/using-jobs/using-concurrency)

---

[← Previous: Operating Within a Fork](operating-in-a-fork.md)| [Table of Contents](../../README.md) | [Next: Authorization, Roles, and Read-Only Contributors →](authorization-and-roles.md)
