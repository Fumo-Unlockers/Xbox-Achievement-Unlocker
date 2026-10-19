# Halo MCC achievement research status

This document records evidence boundaries for Halo: The Master Chief Collection research. It is not production mapping data. Experimental candidates must remain outside `Events/Data.json` until independently verified.

## Production baseline

- Xbox catalogue: 700 achievements.
- Production mappings: 172.
- Direct `AchievementEarned` mappings: 169.
- `MissionCompleted` mappings: 3.
- Unmapped achievements: 528.

The three production `MissionCompleted` mappings are Reach Winter Contingency API IDs 900, 915, and 925. They use `HaloTitleId=HaloReach`, `GameCategoryId=18`, and `MapId=179`, with separate time and score ranges. Their existence does not establish MapIds or condition values for other missions.

## Known CELL schemas

The installed retail package includes `bond/eCellCategory_services.bond`. Its `AchievementEarned` record has event type `0x04010001` and fields for `UserId`, `PlayerSessionId`, `HaloTitleId`, and an `int32 AchievementId`. The event type is not an achievement ID, and the schema supplies no achievement-ID enum or lookup table.

`MissionCompleted` exposes mission and completion fields used by the three existing Reach mappings. Schema conformance alone does not prove that a particular field value satisfies an Xbox achievement rule.

## Identifier and static-analysis results

- Assembly achievement type IDs do not reproduce the known CELL IDs.
- `careerdb.xml` identifiers do not reproduce the known CELL IDs.
- Xbox Achievement API IDs, requirement GUIDs, Steam IDs, catalogue order, and historical ordering are distinct or unverified namespaces.
- Retail map files contain achievement names and engine identifiers, but no validated translation to CELL `AchievementId` values.
- Readable Halo DLLs expose engine-side achievement bookkeeping, while `cell_release_x64_4_0.dll` exposes generic CELL/Bond serialization. No readable module or configuration asset contains a validated translation table between them.
- The retail host executable that may contain the join point is package-protected. Package protections were not altered or bypassed.

No new direct MCC mapping has been independently verified by these comparisons.

## Cairo Station evidence

A legitimate Halo 2 Cairo Station gameplay session produced Xbox API changes:

- API 280, Don't Hangar Around: requirement progress changed from 1,067,400 ms to 846,383 ms and later changed from `InProgress` to `Achieved`.
- API 294, Bomb Squad: requirement progress changed from 5,344 to 6,371 and remained `InProgress`.

The session exposed no decoded MCC internal event payload. It therefore does not verify `MissionCompleted`, `MapId=31`, or any other proposed Cairo event field. Cairo candidates remain experimental and are not present in production mappings.

## Diagnostic experiments

Passive ETW, Windows Event Log, DBWIN/`OutputDebugString`, local logs, and Windows Diagnostic Data Query were evaluated. The Windows diagnostic experiment inspected 101 records, including 21 Xbox-related diagnostics, but found no recognizable MCC internal event name or property bag. Its experimental probe was removed after the negative result.

These observations do not prove MCC never emits internal telemetry; they establish that the tested supported interfaces did not expose a decoded achievement payload.

## Reopening criteria

Further MCC mapping research is justified only by a genuinely new evidence source, such as:

- a legitimate decoded MCC event containing `AchievementId` or `MissionCompleted` fields;
- official symbols, source, or generated event-construction metadata showing the engine-to-CELL translation;
- an authoritative mapping dataset whose identifiers match multiple existing production mappings across Halo titles; or
- independently reproducible documentation tying a specific Xbox achievement to a specific internal event value.

Numeric gaps, ordering, requirement shape, API progression, or schema-valid guessed payloads are not sufficient evidence for production mappings.
