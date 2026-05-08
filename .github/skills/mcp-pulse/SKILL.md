---
name: mcp-pulse
description: Produce a high-level pulse assessment for a single MCP SDK repository. Summarizes open issue count, recent activity trend, highlights, lowlights, and commonalities with a reference SDK. Designed to be invoked as a sub-agent task in parallel across multiple SDK repos.
---

# MCP - Pulse (Sub-Agent)

Produce a brief, focused theme assessment for the specified MCP SDK repository. This skill is designed to run as a sub-agent task — keep execution fast and output concise.

> 🚨 **This is a READ-ONLY skill.** Do not post comments, change labels, close issues, or modify anything in the repository.

## Inputs

The caller provides:

- **SDK repository** — the `owner/repo` to assess (e.g., `modelcontextprotocol/typescript-sdk`)
- **SDK name** — human-readable name (e.g., "TypeScript")
- **SDK tier** — the tier classification (e.g., "Tier 1", "Tier 2", "Tier 3", "TBD")
- **Cross-reference themes** — the canonical theme taxonomy with search keywords

## Process

### 1. Theme Classification

For each theme in the canonical taxonomy provided by the caller, search the SDK's open issues using the keyword patterns. Classify whether each theme is **prevalent** (≥2 matching open issues) or not. For prevalent themes, capture up to 3 representative issue links with titles.

### 2. Produce Structured Output

Return output in **exactly** this JSON-like structure (as a fenced code block). The orchestrator will parse this to build the Common Themes matrix and evidence bullets.

~~~markdown
```pulse-output
SDK_NAME: {SDK Name}
SDK_TIER: {Tier}

THEMES:
- OAuth / Authorization: {yes|no} | {issue links with titles if yes}
- SSE / Keep-Alive: {yes|no} | {issue links with titles if yes}
- Streamable HTTP: {yes|no} | {issue links with titles if yes}
- Dynamic Tools: {yes|no} | {issue links with titles if yes}
- JSON Serialization: {yes|no} | {issue links with titles if yes}
- Code Signing: {yes|no} | {issue links with titles if yes}
- Resource Disposal: {yes|no} | {issue links with titles if yes}
- Multiple Endpoints: {yes|no} | {issue links with titles if yes}
- Structured Content / Output: {yes|no} | {issue links with titles if yes}
- Reconnection / Resumption: {yes|no} | {issue links with titles if yes}
- MCP Apps / Tasks: {yes|no} | {issue links with titles if yes}
- SEP Implementations: {yes|no} | {issue links with titles if yes}
```
~~~

Keep it factual and concise. Do not pad with generic observations.

## Constraints

- Return only the SDK theme assessment for the assigned repository.
- Do **not** call `noop`, `create-issue`, or any other safe-output tool.
- Do **not** write files or attempt to publish anything.
- The top-level workflow orchestrator handles report generation and publishing.
