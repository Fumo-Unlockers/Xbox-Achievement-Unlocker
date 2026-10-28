# Event catalog maintenance

The `Events` directory is tracked in this repository. It is not a submodule. Event changes and application changes can be reviewed together in a pull request targeting `Fumo-Unlockers/Xbox-Achievement-Unlocker`, branch `Main`. The Events ZIP workflow packages these tracked files into the existing `Events-Data` distribution branch.

`Events/Data.json` contains runtime title configuration and ends with `SupportedTitleIDs`. Each listed title has a numeric standalone request template, `Events/<titleId>.json`.

Large achievement maps may live in `Events/Catalog/<titleId>.json`. Such a file contains the achievement-ID dictionary only; its corresponding `Data.json` title has an empty `Achievements` object. `EventCatalog.Load` fills that object before the application uses the catalog. A title cannot have both inline and external mappings. This keeps files below GitHub's single-file size limit without changing event operations. Older application versions do not load these external maps or support multi-request recipes.

An achievement contains either replacement-operation objects directly or ordered `Request1` through `RequestN` objects. Each request contains only replacement-operation objects. Requests must be contiguous and ordered. Operations within a request run in catalog order. Each request starts from the original standalone template. All requests are rendered before submission starts; submission then proceeds sequentially and stops on an HTTP failure.

Supported replacement types are `Replace`, `RangeInt`, `RangeFloat`, and `StupidFuckingLDAPTimestamp`. `Replace` accepts scalar replacements, including existing numeric criteria. `RangeInt` preserves the original minimum-inclusive, maximum-exclusive behavior. Numeric rendering uses invariant culture. Runtime placeholders are `REPLACETIME`, `REPLACESEQ`, `REPLACEXUID`, and `REPLACESESSIONGUID`; one generated session GUID is shared by the requests for an achievement.

Keep research, evidence, notes, achievement-name maps, package provenance, and diagnostic fields outside the runtime catalog. `MetaDataReplacement`, `ext.metadata`, typed payload keys, and required runtime replacements are event data and must be retained. Title fields are restricted to `TitleId`, `TitleName`, `Devices`, `ServiceConfigId`, `Enabled`, `FullySupported`, and `Achievements`. Never put scalar housekeeping fields inside achievement or request objects.

Validate changes offline from the repository root:

```sh
python scripts/validate_events.py Events
python scripts/validate_template_privacy.py Events
dotnet run --project Tests/EventPayloadRenderer.Tests -- Events
dotnet build XAU/XAU.csproj
```

The schema and privacy guards reject unsupported fields and identifying capture values. The C# check calls the same catalog loader and payload renderer used by the app for every listed achievement; it does not authenticate or submit telemetry. The debug page uses that renderer too. Successful rendering establishes local compatibility, not server-side achievement credit.

Before publishing templates, anonymize captured user/device IDs, session and life IDs, machine and OS details, ticket-key values, experiment/boot/shell identifiers, timezone, local paths, account names, and credentials. Preserve required public game/package identities, SCIDs, metadata, and gameplay fields. The privacy guard checks standalone templates and JSON payloads embedded in replacement operations. Never publish capture logs or authentication material with the event catalog.
