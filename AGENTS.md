# AGENTS.md

Repository guidance for Codex-style agents working in `C:\GitHub\AxlProtocolMusic`.

## Operating Notes

- When you discover a non-obvious workaround that materially speeds up future work in this repo, add it to this file before ending the task.
- Prefer documenting concrete commands and outcomes, not vague reminders.
- Keep the notes focused on repo-specific friction, tooling behavior, or repeatable recovery steps.

## Workarounds

### Capturing Unit Test Failure Details In GitHub Actions

Problem:
- `AxlProtocolMusic.WebApp.Tests` uses the Microsoft Testing Platform (`EnableNUnitRunner` + `TestingPlatformDotnetTestSupport`).
- In this repo, adding a classic `--logger "trx;LogFileName=..."` argument to `dotnet test` does not reliably produce a `.trx` file under the expected results directory.
- Relying on `.trx` output in the workflow can therefore leave GitHub Actions with only a generic `Process completed with exit code 1`.

Verified workaround:
1. Run `dotnet test` from PowerShell and pipe the combined output through `Tee-Object`.
2. Save that output to `AxlProtocolMusic/TestResults/UnitTests/dotnet-test.log`.
3. Parse the saved console output for the `Failed: X, Passed: Y, Skipped: Z, Total: T` summary line and emit it through `::error::` plus `GITHUB_STEP_SUMMARY`.
4. Extract the Microsoft Testing Platform `.log` path from the `Tests failed: '...log'` line, copy that log into `AxlProtocolMusic/TestResults/UnitTests/testing-platform.log`, and parse its `failed ...` blocks.
5. Write the failed test names and the matching exception/stack sections into `GITHUB_STEP_SUMMARY`, then upload both logs as workflow artifacts instead of depending on a `.trx` file.

### Refreshing Persisted Coverage Results

Problem:
- A normal `dotnet test` run can pass without refreshing the persisted coverage artifacts under `AxlProtocolMusic/TestResults/Coverage`.
- The previously saved `coverage.cobertura.xml` may remain stale even after new tests are added.

Verified workaround:
1. Run the unit test project through a disposable `dotnet-coverage` tool invocation instead of relying on the normal test run to rewrite the saved report.
2. Write the output directly to `AxlProtocolMusic/TestResults/Coverage/coverage.cobertura.xml`.
3. Read the `AxlProtocolMusic.WebApp` package entry from the Cobertura XML, not the root coverage total, because the root total can include the test assembly and overstate app coverage.

Working commands:

```powershell
dotnet tool install --tool-path "C:\GitHub\AxlProtocolMusic\_codex_tmp\tools" dotnet-coverage
```

```powershell
& "C:\GitHub\AxlProtocolMusic\_codex_tmp\tools\dotnet-coverage.exe" collect "dotnet test C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp.Tests\AxlProtocolMusic.WebApp.Tests.csproj" -f cobertura -o "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\TestResults\Coverage\coverage.cobertura.xml"
```

```powershell
[xml]$coverage = Get-Content "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\TestResults\Coverage\coverage.cobertura.xml"
$package = $coverage.coverage.packages.package | Where-Object { $_.name -eq 'AxlProtocolMusic.WebApp' } | Select-Object -First 1
[math]::Round(([double]$package.'line-rate' * 100), 2)
[math]::Round(([double]$package.'branch-rate' * 100), 2)
```

Notes:
- This is currently the reliable path for forcing a fresh persisted coverage snapshot in this repo.
- Keep the fast `dotnet test` flow for ordinary verification, and use this workaround when the task specifically requires refreshed saved coverage numbers.
- When comparing against ReSharper/dotCover, treat the app package entry as the comparable scope. dotCover usually shows app-only statement coverage, while Cobertura reports line coverage.

### Avoiding Static Web Asset Compression File Locks

Problem:
- Running `dotnet build` for `AxlProtocolMusic.WebApp` in parallel with `dotnet test` for `AxlProtocolMusic.WebApp.Tests` can fail in `Microsoft.NET.Sdk.StaticWebAssets.Compression.targets`.
- The observed error is `The process cannot access the file ... obj\Debug\net10.0\compressed\*.gz because it is being used by another process.`

Verified workaround:
1. Do not run the web app build and test commands in parallel in this repo.
2. Run `dotnet test` first and wait for it to exit before starting `dotnet build`.
3. If the lock already happened, rerun the exact same `dotnet build` command after the test process finishes.

Working commands:

```powershell
dotnet test "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp.Tests\AxlProtocolMusic.WebApp.Tests.csproj"
```

```powershell
dotnet build "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp\AxlProtocolMusic.WebApp.csproj"
```

Notes:
- Serial execution cleared the lock immediately in this repo without deleting `bin` or `obj`.
- The contention shows up under `AxlProtocolMusic.WebApp\obj\Debug\net10.0\compressed`.
