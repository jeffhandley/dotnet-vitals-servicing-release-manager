---
title: "Tenets"
---

[← Previous: Table of Contents](../../README.md) | [Table of Contents](../../README.md) | [Next: Triggers →](triggers.md)

# Tenets

The design principles every workflow should strive to must satisfy to keep your repository secure, to keep the workflows working reliably, to minimize noise, and to avoid unnecessary runner time/cost. It is surprisingly difficult to be led into the pit of success. Security holes in workflows lead to compromises in product repositories. This Field Guide exists to empower maintainers to use workflows securely and consistently — automating processes without repeatedly rediscovering unexpected behaviors.

---

1. **Read the docs** — even the `agentic-workflows` Copilot agent provides misleading and incorrect guidance — gh-aw releases multiple times a day, so any pattern learned from an assistant, a memory, or *this Field Guide* must be verified against the current docs at <https://gh.io/gh-aw>.

2. **Avoid privilege-escalation pitfalls, including ones a careful reviewer would miss** — GitHub Actions workflows contain many subtleties that can lead to substantial security holes, unexpected trigger contexts, and other pits of failure. Ensure any action that would require write+ permission through GitHub has a human in the loop with those same permissions.

    In addition to the guidance in this field guide, refer to the following materials to understand the risks and mitigations:

    * [Keeping your GitHub Actions and workflows secure Part 1: Preventing pwn requests \| GitHub Security Lab](https://securitylab.github.com/resources/github-actions-preventing-pwn-requests/)
    * [Keeping your GitHub Actions and workflows secure Part 2: Untrusted input \| GitHub Security Lab](https://securitylab.github.com/resources/github-actions-untrusted-input/)
    * [Keeping your GitHub Actions and workflows secure Part 3: How to trust your building blocks \| GitHub Security Lab](https://securitylab.github.com/resources/github-actions-building-blocks/)
    * [Keeping your GitHub Actions and workflows secure Part 4: New vulnerability patterns and mitigation strategies \| GitHub Security Lab](https://securitylab.github.com/resources/github-actions-new-patterns-and-mitigations/)
    * [Security Architecture \| GitHub Agentic Workflows](https://github.github.com/gh-aw/introduction/architecture/)

3. **Apply least privilege on every dimension** — grant the bare minimum `permissions:`, `safe-outputs:`, `network.allowed:`, secrets, `tools:`, and other capabilities. Use the agent sandbox and safe-outputs controls to operate on untrusted inputs (anything authored outside the trusted maintainer set — PR bodies, comments, fork PR head SHAs, fork file contents, discussion content). **Example:** checking out a fork and executing its code is acceptable *inside* the agent sandbox; the same operation in pre- or post-agent steps executes attacker code on the runner host with full secret access and is not acceptable.

4. **Require human-in-the-loop approval for any consequential actions** — workflows that perform privileged operations must have a clear audit trail with the action attributed to the responsible human team member. This can be represented as the issue/PR event stream or comments.

5. **Eliminate alert fatigue and opaque approvals** — human approval should not be required for routine, inconsequential operations. Every prompt must clearly connote the desired effect. Opaque or routine prompts cause alert fatigue and degrade approvals into theater. (See [The "Approve and run workflows" Gate](approve-and-run-gate.md) for the canonical anti-example.)

6. **Treat security-imposed limitations as the security model, not bugs to work around** — bypasses (e.g., mixing `pull_request_target` with access to fork content, `workflow_run` to escape approval gates, `on.roles: all` to widen the actor pool) erase the protections you appear to still have. When a boundary blocks a legitimate goal, discuss and threat model the scenario before engineering a bypass.

7. **Never present bot actions as actions taken by a human** — the bot remains the visible *actor*; the responsible human is captured separately as the *accountable party* via co-authorship, comment metadata, or audit log.

8. **Use the right tool: deterministic by default** — deterministic Actions and reusable workflows by default; agentic workflows only when the input is unstructured, the decision space is open, or AI unlocks a capability deterministic code cannot provide.

9. **Resist building a bespoke workflow-management platform on top of gh-aw or GitHub Actions** — every layer of meta-orchestration becomes a product you must maintain at high opportunity cost.

10. **Eliminate human single points of failure** — distribute responsibilities and recovery knowledge across the team — e.g., documenting workflow behaviors and configuration, and ensure multiple team members can unblock any common configuration issues.

11. **Converge on canonical patterns** — focus on canonical patterns for achieving goals consistently — especially regarding which trigger to use when. Maintain a cheat sheet that maps each scenario/goal to its trigger + conditions, and keep it current as we learn. The [Triggers](triggers.md) chapter captures the common triggers, their scenarios and pitfalls.

12. **Mind the signal-to-noise ratio of high-volume triggers** — convenience triggers (e.g., `slash_command:`) and activity-type filters (e.g., `pull_request: types: [labeled]`, `issues: types: [opened, edited]`) both compile to broad event subscriptions whose activation step runs on every matching event; the agent step fires on only a fraction of those runs. Weigh the trade-off explicitly: **low-latency invocation requires accepting a high executed-to-filtered ratio**, and that ratio costs runner minutes (and in some configurations, model tokens) on every no-op activation. The activation step must be cheap, and the worst-case invocation rate must be estimated and acceptable.

13. **Limit the agent job to actions best suited for execution within an agent** — keep all filtering and skipping logic in steps or jobs that execute before the agent. Execute deterministic scripts both before and after the agent job. Refactor agent jobs over time to become more deterministic.

14. **Test your workflows in disposable repositories before promoting them to product repositories** — use the [TrialOps](https://github.github.com/gh-aw/patterns/trial-ops/) pattern (`gh aw trial` creates an ephemeral private trial repo). Validate with at least one test GitHub account that has only **read** permission — that's the actor pool most likely to surface privilege-escalation and denial-of-service issues. **Never test in production.**

---

[← Previous: Table of Contents](../../README.md) | [Table of Contents](../../README.md) | [Next: Triggers →](triggers.md)
