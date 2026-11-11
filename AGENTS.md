# AGENTS.md

Rules and context for AI agents working in this repository.

## What this is

Xbox Achievement Unlocker (XAU) — a Windows WPF app (net9.0-windows) that
unlocks Xbox achievements by extracting the XAuth token from the Xbox app
(or OAuth login) and calling Xbox REST APIs. Built with .NET 9, WPF-UI
(Fluent, 3.0.0-preview), CommunityToolkit.Mvvm, Newtonsoft.Json, SQLite.

- Solution: `XAU.sln`
- App: `XAU/` (Models, Networking, Services, ViewModels, Views, Util, Theme)
- Tests: `XAU.Tests/` (xunit)
- Events feature docs: `Doc/Events.md` — source of truth for the Events feature

## Build & test

```sh
dotnet build XAU.sln
dotnet test XAU.Tests
```

- Windows-only (WPF). Build and test on a Windows host.
- `bin/` and `obj/` exist locally and are gitignored — never commit build output.

## Code conventions

- .editorconfig at the repo root is authoritative (4-space indent, LF,
  UTF-8, final newline). `dotnet_style_qualification_for_method = true`.
- Nullable is enabled — keep nullability annotations correct.
- Follow the existing MVVM structure: logic in ViewModels/Services, code-behind
  in Views kept minimal; use CommunityToolkit.Mvvm source generators
  (`[ObservableProperty]`, `[RelayCommand]`) as the surrounding code does.
- `Usings.cs` provides global usings — don't add redundant using directives.

## Git workflow

- Commit early and often. Do NOT push unless explicitly asked.
- Keep changes focused — no drive-by refactors, renames, or reformatting.
- Never rewrite history or force-push.

## Safety rules (hard rules)

- Never commit, print, or stage tokens, XAuth strings, OAuth tokens, account
  credentials, or anything captured from Xbox app memory. Auth/cache dumps and
  logs are evidence: leave them on disk, gitignore them, never `git add -f`.
- Do not weaken .gitignore rules for captures, dumps, or auth caches.
- Never trigger update/version-bump flows (`bump_version.py`, Build-Release
  workflow) without being asked; releases ship to real users.

## Testing

- Add/adjust tests in `XAU.Tests` for networking, auth, and scanner changes.
- Prefer testable design: keep Xbox REST interaction behind seams so tests
  can exercise deserialization and logic without hitting live Xbox servers.
- Run `dotnet test XAU.Tests` before claiming work is done.
