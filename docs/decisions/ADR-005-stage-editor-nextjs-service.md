# ADR-005: Stage Editor — Standalone Next.js Service
Date: 2026-06-04
Status: accepted

## Context
A stage editor is required before content production (§11, §14 risk: "Editor is P0"). Options considered:
- Unity Editor Window: tightly coupled to Unity version, no web access, harder to share with non-engineers.
- Standalone TypeScript web app: independent, shareable, file-system CSV access via API routes.

## Decision
Stage Editor is a standalone **Next.js** app (App Router) located at `stage-editor/` in the project root. API routes handle CSV file I/O directly on the local file system (`shared/datas/stage/stage.csv`, `shared/datas/common/color_palette.csv`). Frontend is a React-based board editor UI served by Next.js.

Development only — not deployed to production.

## Consequences
- Independent of Unity version; runs alongside Unity development.
- Single process (Next.js serves both UI and API routes).
- CSV path resolution is relative to `project-flood/` root; requires running from that directory or config.
- TypeScript game-rule logic (`lib/game-rules.ts`) mirrors C# core for in-browser playtest — must be kept in sync with client rule changes.
- `stage-editor/` added to `project-flood/AGENTS.md` Nav.
