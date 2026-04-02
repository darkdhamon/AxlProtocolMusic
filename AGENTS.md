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

### Refreshing Persisted Coverage Results To Match GitHub Actions

Problem:
- A normal `dotnet test` run can pass without refreshing the persisted coverage artifacts under `AxlProtocolMusic/TestResults/Coverage`.
- The previously saved `coverage.cobertura.xml` may remain stale even after new tests are added.
- A manual `dotnet-coverage collect "dotnet test ..."` run does not match the GitHub Actions warning/fail calculation in this repo.
- The mismatch happens because GitHub reads the Coverlet-generated `coverage.cobertura.xml` from `AxlProtocolMusic.WebApp.Tests.csproj`, including its `ExcludeByFile` filters, while `dotnet-coverage` reports a broader scope and can overstate line coverage.

Verified workaround:
1. Use the same Release build plus `dotnet test --configuration Release --no-build` flow that the GitHub Actions workflow uses.
2. Let Coverlet rewrite `AxlProtocolMusic/TestResults/Coverage/coverage.cobertura.xml` through the test project's existing MSBuild settings.
3. Read the `AxlProtocolMusic.WebApp` package entry from the Cobertura XML, because that is the same package-level line rate GitHub compares against the warning and fail thresholds.
4. Do not use `dotnet-coverage` for threshold comparisons in this repo unless the workflow is changed to use it too.

Working commands:

```powershell
dotnet build "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp.Tests\AxlProtocolMusic.WebApp.Tests.csproj" --configuration Release --no-restore
```

```powershell
dotnet test "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp.Tests\AxlProtocolMusic.WebApp.Tests.csproj" --configuration Release --no-build
```

```powershell
[xml]$coverage = Get-Content "C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\TestResults\Coverage\coverage.cobertura.xml"
$package = $coverage.coverage.packages.package | Where-Object { $_.name -eq 'AxlProtocolMusic.WebApp' } | Select-Object -First 1
[math]::Round(([double]$package.'line-rate' * 100), 2)
[math]::Round(([double]$package.'branch-rate' * 100), 2)
```

Notes:
- This is currently the reliable path for forcing a fresh persisted coverage snapshot that matches GitHub's threshold calculation in this repo.
- Keep the fast `dotnet test` flow for ordinary verification, and use this Release build plus Release test flow when the task specifically requires refreshed saved coverage numbers that match CI.
- If a local `dotnet-coverage` run reports a much higher percentage than GitHub Actions, trust the Coverlet-generated `coverage.cobertura.xml` from the Release test run.

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

### Debugging Live Chatbot Visibility

Problem:
- The admin dashboard's chatbot budget card reflects the shared budget/manual override state, but the site chatbot render path also requires the config flag `Chatbot:Enabled` to be `true`.
- A production deployment can therefore show `Status Enabled` and `Manual Override Off` in the admin UI while the chatbot remains hidden for every visitor if the deployed configuration still has `Chatbot:Enabled=false`.

Verified workaround:
1. Check the deployed production setting for `Chatbot:Enabled` or the environment override `Chatbot__Enabled`.
2. If the admin dashboard says the chatbot is enabled but the launcher is missing site-wide, verify the production host is not inheriting `appsettings.json` with `"Chatbot": { "Enabled": false }`.
3. Keep the admin UI aligned with the render gate by exposing the config flag in the dashboard when investigating live issues.

Working files:

```text
C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp\appsettings.json
```

```text
C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp\Components\Common\SiteChatbot.razor
```

```text
C:\GitHub\AxlProtocolMusic\AxlProtocolMusic\AxlProtocolMusic.WebApp\Components\Pages\AdminDashboard.razor
```

Notes:
- `SiteChatbot.razor` only renders when `ChatbotOptions.Value.Enabled` is true and the manual disable flag is false.
- `appsettings.Development.json` enables the chatbot, but the repo default in `appsettings.json` disables it, so production must override it explicitly.
