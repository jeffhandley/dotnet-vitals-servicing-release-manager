# AI Triage Score

This directory contains prompt templates and supporting documents for the
`AI: Triage Score` agentic workflow.

## Files

- `score-issue-prompt.md` — Sub-agent prompt template (substitutes `{issue_url}`)
- `MockOpenAIResponses.cs` — HTTP-mock reproduction template for OpenAI Responses API
- `global.json` — .NET SDK version pinning for repro projects

## Overview

The workflow scores untriaged issues from the weekly `[AI: Triage]` report
for actionability (0–100%) and posts one comment per scored issue on the
triage report issue. For bugs with sufficient repro clarity, it attempts
reproduction using the mock templates.
