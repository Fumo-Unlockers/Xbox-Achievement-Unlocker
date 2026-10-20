# Halo MCC achievement research status

This document records evidence boundaries for Halo: The Master Chief Collection research. It is not production mapping data. Experimental candidates must remain outside `Events/Data.json` until independently verified.

## Production baseline

- Xbox catalogue: 700 achievements.
- Production mappings: 172.
- Direct `AchievementEarned` mappings: 169.
- `MissionCompleted` mappings: 3.
- Unmapped achievements: 528.

The three production `MissionCompleted` mappings are Reach Winter Contingency API IDs 900, 915, and 925. They use `HaloTitleId=HaloReach`, `GameCategoryId=18`, and `MapId=179`, with separate time and score ranges. Their existence does not establish MapIds or condition values for other missions.

## Halo 2 coverage

The live Xbox catalogue contains 119 achievements whose description identifies them as Halo 2 (separate from Halo 2 Anniversary multiplayer). Of those, 26 have production mappings and 93 are unmapped. All 26 mapped Halo 2 achievements currently use the direct `AchievementEarned` event; none validates a Halo 2 `MissionCompleted` payload.

The 93 unmapped achievements break down as follows. The categories describe the achievement conditions; they do not imply a specific telemetry event.

| Category | Count | Xbox API IDs |
| --- | ---: | --- |
| Individual mission completion | 13 | 267-279 |
| Par time, including the all-level aggregate | 14 | 280-293 |
| Par score, including the all-level aggregate | 14 | 294-307 |
| Terminals, including the all-terminal aggregate | 14 | 308-321 |
| Skulls, including the all-skull aggregate | 16 | 322-337 |
| Toys and toy collection | 9 | 338-346 |
| Campaign difficulty, completion, and graphics-mode conditions | 5 | 347-349, 606, 738 |
| Playlists and LASO | 3 | 350, 351, 402 |
| Mission-specific gameplay challenges | 2 | 371, 376 |
| Classic multiplayer | 3 | 605, 794, 795 |
| **Total** | **93** | |

This reconciliation uses the Xbox API IDs, descriptions, and requirement records returned for the authenticated profile, the production mappings at the pinned Events submodule revision, and the public Halo 2 catalogue for human-readable mission grouping. The public catalogue is useful for names and categories but does not expose CELL event identifiers.

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

The aggregate achievements moved with the same mission result: API 293 mirrored API 280's time progression and API 307 mirrored API 294's score progression. This demonstrates shared service-side progression, not the event fields or rules that produced it.

The public community `e_map_id` declaration is a sequential engine map enum. In that declaration Cairo Station has ordinal 31 and Winter Contingency has ordinal 179. The existing decoded Reach `MissionCompleted` sample also contains `MapId=179`, so Winter Contingency is one positive control showing that an engine enum ordinal and an observed event value can coincide.

That single control is not a general translation rule. The enum source has no capture provenance, Cairo's value has not appeared in a decoded retail event, and Xbox API progression does not return the originating event property bag. Therefore:

- `31` is a statically corroborated **engine map ID** for Cairo Station.
- `179` is both an engine enum value and an observed Reach `MissionCompleted.MapId`.
- `31` remains an **unverified candidate** for Halo 2 `MissionCompleted.MapId`.

The session exposed no decoded MCC internal event payload. It therefore does not verify `MissionCompleted`, `MapId=31`, or any other proposed Cairo event field. Cairo candidates remain experimental and are not present in production mappings.

### Missing Halo 2 event evidence

A production Cairo mapping would still require genuine Halo 2 evidence for:

- the emitted `HaloTitleId`, `GameCategoryId`, `MapId`, and `DifficultyId` values;
- runtime completion values including `TimePlayedMS`, `MissionScore64`, co-op/player count, skull flags, multipliers, penalties, kills, section statistics, cumulative mission counts, and timestamps;
- which fields are evaluated by the private Xbox service configuration for API 280 and API 294;
- whether `MINIMUM` time and `SUM` score progression consume one `MissionCompleted` event, several events, or title-managed intermediate statistics; and
- any additional filters or conditions not exposed by the achievement API requirement response.

The public Xbox events documentation confirms that titles submit an event name and property bags and that service-side rules configured in Partner Center update statistics and achievements. It does not publish MCC's title-specific event definitions or rules.

### Cairo validation decision

No already available, authorized, non-invasive source can independently validate Cairo's event `MapId`:

- Live Capture can preserve Xbox achievement state and requirement deltas, but it does not receive game-emitted event payloads.
- The retail Bond schema defines field names and types, not Halo 2 values.
- The community map enum supplies an engine identifier, not proof of a retail telemetry value.
- The existing XAU event example is a Reach observation and explicitly treats parts of the rule reconstruction as assumptions.
- The official MCC mod tools expose scenarios, tags, and cache-file identities, not MCC's Partner Center event configuration or retail Xbox event records.
- Previously tested passive ETW, Windows Event Log, DBWIN, local logs, and Windows Diagnostic Data Query did not expose a decoded MCC mission payload.
- Microsoft's documented Fiddler workflow is a title-developer debugging path involving traffic interception; it is outside this investigation's authorization and safety boundaries for a third-party retail title.

**Decision: NO-GO.** There is no legitimate independent validation step left using the evidence already available. `MapId=31` must remain explicitly unverified, and the Cairo investigation is closed unless a genuinely new source meets the reopening criteria below.

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

For Cairo specifically, useful new evidence would be one of:

- an authentic, decoded Halo 2 `MissionCompleted` event from an authorized source showing its runtime property bag;
- publisher-provided or otherwise authoritative MCC event configuration tying Cairo Station to `MapId=31` and documenting the relevant service rules;
- official/generated event-construction metadata that binds the Halo 2 scenario identity to the CELL event value; or
- multiple independently observed Halo 2 mission payloads that reproduce the public engine enum across more than the existing Reach control.

## Sources

- [XAU production MCC mappings at the pinned Events revision](https://github.com/Fumo-Unlockers/XAU-Events/blob/77050d3555af03852cd1d5e5c229263ee99d2639/Data.json)
- [XAU event documentation and the Reach `MissionCompleted` example](https://github.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/blob/Main/Doc/Events.md)
- [Community MCC `e_map_id` declaration](https://gist.github.com/twist84/f44ee956abe0f487a22617aa05a7f90f)
- [Community full map-ID comparison derived from the same research lineage](https://gist.github.com/twist84/cc9777df08dd2b584bf02d8d5862585c)
- [Public Halo 2 achievement catalogue used for names and mission grouping](https://github.com/mickelsonmichael/achievements/blob/master/public/data/halo2/achievements.json)
- [Microsoft GDK event-based statistics documentation](https://learn.microsoft.com/en-us/gaming/gdk/docs/services/player-data/stats-leaderboards/event-based/events/live-game-events)
- [Microsoft MCC tools overview](https://learn.microsoft.com/en-us/halo-master-chief-collection/general/overview-tools)
- [Cairo progression regression evidence preserved in the Live Capture tests](../XAU.LiveCapture.Tests/Program.cs)
