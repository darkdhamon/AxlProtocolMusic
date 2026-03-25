# AGENTS.md

Repository guidance for Codex-style agents working in `C:\GitHub\AxlProtocolMusic`.

## Operating Notes

- When you discover a non-obvious workaround that materially speeds up future work in this repo, add it to this file before ending the task.
- Prefer documenting concrete commands and outcomes, not vague reminders.
- Keep the notes focused on repo-specific friction, tooling behavior, or repeatable recovery steps.

## Workarounds

### Refreshing Persisted Coverage Results

Problem:
- A normal `dotnet test` run can pass without refreshing the persisted coverage artifacts under `AxlProtocolMusic/TestResults/Coverage`.
- The previously saved `coverage.cobertura.xml` may remain stale even after new tests are added.

Verified workaround:
1. Run the unit test project through a disposable `dotnet-coverage` tool invocation instead of relying on the normal test run to rewrite the saved report.
2. Write the output directly to `AxlProtocolMusic/TestResults/Coverage/coverage.cobertura.xml`.
3. Read the Cobertura XML for refreshed line and branch percentages.

Working commands:

```powershell
dotnet tool install --tool-path "C:\GitHub\AxlProtocolMusic\_codex_tmp\tools" dotnet-coverage
```

```powershell
& "C:\GitHub\AxlProtocolMusic\_codex_tmp\tools\dotnet-coverage.exe" collect "dotnet test C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp.Tests\AxlProtocolMusic.WebApp.Tests.csproj" -f cobertura -o "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\TestResults\Coverage\coverage.cobertura.xml"
```

```powershell
[xml]$coverage = Get-Content "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\TestResults\Coverage\coverage.cobertura.xml"
[math]::Round(([double]$coverage.coverage.'line-rate' * 100), 2)
[math]::Round(([double]$coverage.coverage.'branch-rate' * 100), 2)
```

Notes:
- This is currently the reliable path for forcing a fresh persisted coverage snapshot in this repo.
- Keep the fast `dotnet test` flow for ordinary verification, and use this workaround when the task specifically requires refreshed saved coverage numbers.
