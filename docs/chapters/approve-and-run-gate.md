---
title: "The 'Approve and run workflows' Gate"
---

[← Previous: pull_request_target](../triggers/pull-request-target.md) | [Table of Contents](../../README.md) | [Next: The "Apparent vs. Actual" Trigger Surface →](apparent-vs-actual.md)

# The "Approve and run workflows" Gate

GitHub gates workflow execution from forks behind a manual approval click ("Approve and run workflows"), governed by repo/org **Actions → General → Fork pull request workflows from outside collaborators** setting[^approval-docs][^approval-policy]:

| Setting | Behavior |
|---|---|
| **Require approval for first-time contributors who are new to GitHub** | Approval needed only for accounts < 2 months old |
| **Require approval for first-time contributors** *(default)* | Approval needed until the contributor has had a merged PR or commit |
| **Require approval for all outside collaborators** | Always require approval for non-org-members |
| **Require approval for all external contributors** *(orgs only)* | Always require approval for anyone without write access |

**The gate applies to `pull_request` and `pull_request_target`.** It does **not** apply to:

- [`issue_comment`](../triggers/comment-and-slash-command.md) (even on PRs from forks!) — a read-only contributor can therefore always *fire* a [`slash_command:`](../triggers/comment-and-slash-command.md) from a comment on their own PR without a click. **The agent step still won't run** unless `on.roles:` includes their permission level (default allow-list excludes `read`).
- [`discussion`](../triggers/discussion.md), [`discussion_comment`](../triggers/discussion-comment.md) — never gated.
- [`issues`](../triggers/issue.md) — never gated; anyone with read access can open one.
- [`workflow_dispatch`](../triggers/workflow-dispatch.md) — never gated, but **requires write access to invoke**.
- `repository_dispatch` — never gated, but **requires a PAT with `repo` scope**.
- [`schedule`](../triggers/schedule.md) — never gated.
- [`workflow_run`](../triggers/workflow-run.md) — never gated (this is what makes it dangerous).

## The approval gate is dangerous, not protective

The "Approve and run workflows" button looks like a security feature. **Treat it as the opposite.** Three structural problems make it actively harmful:

1. **Alert fatigue.** Every first-time fork contributor produces a button click that lists *every* workflow the PR touches. A repo with 15 workflows shows 15 entries. After the maintainer has clicked through dozens of legitimate first-time PRs, the click becomes muscle memory. The hundredth click is no more deliberate than the first.
2. **The UI does not show what is being approved.** There is no per-workflow toggle, no preview of the diff, no preview of the events the workflows are subscribed to, no list of secrets that will be exposed, no indication that some workflows use `pull_request_target` (full secrets, write token) versus plain `pull_request` (read-only, no secrets). The maintainer is approving an opaque blob of YAML files they have likely never read.
3. **A single click runs all of them.** The only way approve only the safe ones is through the Actions UI, but even there it's difficult to see the potential ramifications for approvals. Approving the CI workflow you actually wanted to run *also* approves the auto-merge workflow, the slash-command listener, and any `pull_request_target` workflow in the repo. Worse yet, you could also be approving workflows _included in the pull request's changes_.

**Concrete failure modes when a maintainer clicks the button because they "think they're supposed to":**

- A [`pull_request_target`](../triggers/pull-request-target.md) workflow that does `actions/checkout@v4` with `ref: ${{ github.event.pull_request.head.sha }}` (an extremely common mistake) **executes attacker-controlled code with the upstream's secrets in the environment**. Game over: secrets exfiltrated, packages republished, releases tagged, branches force-pushed[^pwn-requests].
- A [`slash_command:`](../triggers/comment-and-slash-command.md) workflow whose `on.roles:` was relaxed to `all` (perhaps to support a community `/help` bot) sees the attacker's PR body containing `/help` (or whatever command), passes its activation job, and the agent runs with the workflow's full `permissions:` and `safe-outputs:` — see [Authorization, Roles, and Read-Only Contributors](authorization-and-roles.md) for the full capability surface.
- A `slash_command:` workflow that's also subscribed to `pull_request` (the default) gets approved alongside everything else — its activation job runs against the PR body and, if the magic word is there, the agent fires.
- A workflow that posts comments on the PR runs as the upstream `github-actions[bot]`, lending **upstream's apparent authority** to attacker-supplied output (e.g., a fake "✅ All checks passed — safe to merge" comment).
- The maintainer who clicked is not necessarily the same person who reviews the PR diff later; the approval click can effectively pre-authorize a *future* reviewer to merge based on a contaminated CI signal.

**Design rule: assume the approval gate will always be clicked.** The only safe workflows are ones that produce the same outcome whether the actor is a trusted maintainer or an anonymous fork contributor. Concretely:

- Prefer `pull_request` (read-only token, no secrets) over `pull_request_target` for *anything* that touches PR content. Reserve `pull_request_target` for metadata-only operations (labeling, commenting based on path globs, dependency-review on the diff metadata).
- **Never check out the PR head SHA** in a job that has secrets in its environment.
- Keep `on.roles:` at its default — do not set `on.roles: all` to "be friendly to the community"; pair restrictive roles with a separate, narrower workflow if community-facing commands are needed.
- Pin `safe-outputs:` to the minimum required and audit the resulting blast radius via [Authorization, Roles, and Read-Only Contributors](authorization-and-roles.md).
- For any workflow that *must* be powerful, gate the agent step on `github.event.pull_request.head.repo.fork == false` so it refuses to act on cross-fork PRs entirely — and rely on the approval gate **only as defense-in-depth**, never as the primary control.

[^approval-docs]: GitHub Docs, [Approving workflow runs from public forks](https://docs.github.com/en/actions/managing-workflow-runs-and-deployments/managing-workflow-runs/approving-workflow-runs-from-public-forks)
[^approval-policy]: GitHub Docs, [Managing GitHub Actions settings for a repository](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/enabling-features-for-your-repository/managing-github-actions-settings-for-a-repository)
[^pwn-requests]: GitHub Security Lab, [Keeping your GitHub Actions and workflows secure: Preventing pwn requests](https://securitylab.github.com/research/github-actions-preventing-pwn-requests/)

---

[← Previous: pull_request_target](../triggers/pull-request-target.md)| [Table of Contents](../../README.md) | [Next: The "Apparent vs. Actual" Trigger Surface →](apparent-vs-actual.md)
