# Codex Repo Instructions

## Branch Safety

- Before making any file edits, run `git branch --show-current` and `git status --short --branch`.
- Do not edit files on `main` or `master`.
- If the current branch is `main` or `master`, create or switch to a feature branch before editing.
- Prefer issue-linked feature branches: `feat/<issue-number>-<short-slug>`.
- Do not rely only on git hooks for this. Hooks may block commits, but they do not reliably block every edit path in all agent environments.
- Codex edit-time hooks are configured under `.codex/`, but they only work in Codex sessions that have hook support enabled before startup. Always do the explicit branch check above even when hooks exist.

## Feature Requests

- When the user asks for a new feature or separate unit of product work and no suitable issue exists, ask whether they want a new GitHub issue raised for that request.
- If the user confirms, create the issue with `gh issue create`, then create a dedicated issue-linked branch before making edits.
- Use the branch format `<type>/<issue-number>-<short-slug>`, for example `feat/42-add-ai-reinforcement-cap`.
- Do not bundle multiple feature requests into an existing branch unless the user explicitly asks to combine them.
- If already on a branch for one issue and the user asks for an unrelated feature, pause before editing and ask whether to create a new issue and branch for the new work.

## Commit Safety

- Do not commit directly on `main` or `master`.
- The repo expects `git config core.hooksPath .githooks`.
- Let the local hooks run when committing unless the user explicitly asks to bypass them.

## Verification

- For gameplay logic changes, run:

```powershell
dotnet test tests\FactionWars.Tests\FactionWars.Tests.csproj
```

- Run `git diff --check` before committing.
