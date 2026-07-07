# Project Manager Triage Rules

Review open issues for `darkdhamon/AxlProtocolMusic` and project `darkdhamon` project `#8`.

## Scope and focus
- Review all open issues and related project-card mappings.
- Prioritize Backlog items and open issues that are still untriaged.

## Labeling and sizing
- Ensure each open issue has one of:
  - `priority:critical`
  - `priority:high`
  - `priority:normal`
  - `priority:low`
- Create any missing priority labels when needed.
- Ensure project `Size` is set to one of `XS`, `S`, `M`, `L`, `XL`.
- If an issue is too large, split it into smaller follow-up issues or sub-issues and link them back.

## Priority mapping
- Map issue labels to project `Priority`:
  - `priority:critical` -> `P0`
  - `priority:high` -> `P0`
  - `priority:normal` -> `P1`
  - `priority:low` -> `P2`

## Project status rules
- Keep `Status` as `Backlog` unless the issue is clearly actionable with enough detail and no blockers.
- Set `Status` to `Ready` only when actionable, unblocked, and no active PR review is pending.
- If an issue has an open PR, set `Status` to `In Review`.
- If an open issue has merged changes:
  - `Merged to dev` -> `Done`
  - `Merged to main` -> `Released`
- Do not move to `Ready` when clarification is required.
- If clarification is needed, post a concise comment requesting missing information from `@darkdhamon`.

## Delivery
- Report changes made to labels, size, priority, and project status.
