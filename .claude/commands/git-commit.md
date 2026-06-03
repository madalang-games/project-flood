Stage and commit changes grouped by logical work unit.

Arguments: $ARGUMENTS — optional: issue number (e.g. `42`), type override, or scope hint.

## Convention
```
{type}#{issue}: {한글 메시지}
{type}: {한글 메시지}
```
Types: `feat` `fix` `refactor` `docs` `test` `chore` `build`

- Issue number: from `$ARGUMENTS` only — never fetch remotely.
- Message: Korean, concise, no verbose explanation.
- Subject line ≤ 50 chars.

## Rules
- Never stage: `.env*`, files in `.gitignore`, `*/generated/*`, `*/Generated/*`
- Use `git -C {path}` syntax — never `cd && git`
- Multiple distinct work units → separate commits (do not squash)

## Steps
1. `git status` + `git diff --stat` → identify all changed files
2. Group by work nature → define commit units
3. Per unit:
   a. Stage only its files/hunks
   b. Commit with convention format
4. Report: `{hash} {message}` per commit, nothing else
