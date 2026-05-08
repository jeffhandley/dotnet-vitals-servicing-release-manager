# MCP SDK Pulse Report Format

The pulse report follows this exact structure. Use the current New York date for `{YYYY-MM-DD}` in the H1 heading.

```markdown
# MCP - Pulse - {YYYY-MM-DD}

## Summary

- **Prevalent themes:** {N} of 12 themes have ≥2 matching open issues in at least one Tier 1 SDK
- **Themes affecting C#:** {N} prevalent themes affect the C# SDK ({brief list of theme names})
- **Cross-cutting:** {N} themes are prevalent across all four Tier 1 SDKs (C#, TypeScript, Python, Go)
- **Top concern for C#:** {single most notable theme affecting the C# SDK and brief reason}
- **C# SDK position:** {how C# compares to peer Tier 1 SDKs in terms of theme count — e.g., "C# shares 4 themes with TypeScript and Python; 2 themes are unique to C#"}

> **Keep each summary bullet concise — one short sentence.** If a finding requires
> more detail, use sub-bullets rather than turning the bullet into a paragraph.

---

## 🔗 Common Themes

Cross-cutting themes observed across Tier 1 MCP SDKs. A ☑️ indicates the theme is prevalent (≥2 matching open issues) in that SDK's issue tracker.

| Theme | C# | TypeScript | Python | Go |
|---|:---:|:---:|:---:|:---:|
| OAuth / Authorization | ☑️ | | ☑️ | |
| SSE / Keep-Alive | ☑️ | ☑️ | | ☑️ |
| Streamable HTTP | | | | |
| Dynamic Tools | ☑️ | | | |
| JSON Serialization | | | | |
| Code Signing | ☑️ | | | |
| Resource Disposal | | | | |
| Multiple Endpoints | | | | |
| Structured Content / Output | | | | |
| Reconnection / Resumption | | ☑️ | ☑️ | |
| MCP Apps / Tasks | | | | |
| SEP Implementations | ☑️ | ☑️ | ☑️ | ☑️ |

{For each theme that has at least one ☑️, list the evidence. List the C# SDK first
and **bold** its label. Then list other SDKs in tier order (Tier 1 → Tier 2 → Tier 3 →
TBD), alphabetical within each tier, with no bolding.}

**OAuth / Authorization:**
- **C#:** [#78](url) Title, [#90](url) Title
- Python: [#123](url) Title, [#456](url) Title
- ...

**SSE / Keep-Alive:**
- **C#:** [#100](url) Title, [#101](url) Title
- TypeScript: [#200](url) Title, [#201](url) Title
- ...

{Omit themes with no prevalent SDKs.}
```

## Formatting Rules

### Links

- **Within `csharp-sdk`:** Use GitHub shorthand — `#123` for issues/PRs, `@username` for users.
- **Other SDK repos:** Use full URLs — `[typescript-sdk #1090](https://github.com/modelcontextprotocol/typescript-sdk/issues/1090)`.
- **Inline issue links in evidence bullets:** Backtick the number — `` [`#1090`](url) Title `` — to keep numbers visually distinct from titles.
- **Repo links:** `[modelcontextprotocol/typescript-sdk](https://github.com/modelcontextprotocol/typescript-sdk)`.

### Section Emoji

| Section | Emoji |
|---|---|
| Common Themes | 🔗 |

The `## Summary` heading does **not** use an emoji prefix.

## Notes

- The Common Themes table uses **only Tier 1 SDKs** as columns, with **C# always
  in the leftmost column** followed by TypeScript, Python, Go.
- The evidence bullets below the table include issue links from all SDKs where the
  theme is prevalent.
- **C# is always the first bullet for each theme and its label is bold.** All other
  SDKs follow in tier order (Tier 1 → Tier 2 → Tier 3 → TBD), alphabetical within
  each tier, and are **not** bolded.
- Themes with no prevalent SDKs are omitted from both the table and the evidence list.
