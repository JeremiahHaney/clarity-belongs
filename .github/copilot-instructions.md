# Repository instructions for GitHub Copilot

Read `AGENTS.md` and `AI_CONTEXT.md` before planning changes. For an existing feature, use `AI_INDEX.generated.md` to locate its implementation and `AI_CAPABILITIES.generated.json` for project references, route declarations, source paths/line numbers, tests and changelog files. Inspect actual code and tests before claiming functionality.

Consult the portfolio's canonical `product-atlas.yaml` for product identity and ownership; use this repository's status ledgers, recent changelogs, CI and deployment evidence for maturity. **A source declaration is not proof a feature works.** Classify as Released, Verified, Built/Testing Required, Partial, Planned or Unknown. Prefer improving existing code rather than inventing duplicate platforms.

Do not copy passwords, tokens or environment values into context or generated indexes. Keep security, tenant/brand, local-first and privacy boundaries intact. Determine the latest project changelog version before any version increment.

Generated indexes are automatically refreshed by `.github/workflows/ai-repository-index.yml`. Never manually maintain them.
