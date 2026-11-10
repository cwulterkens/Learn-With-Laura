# Profile save schema

Learner profiles are local JSON files under `user://profiles`. They are never sent to a server.

`ProfileData.SchemaVersion` identifies the stored shape and semantics. The current version is defined by `ProfileSaveMigration.CurrentVersion`.

## Supported versions

| Version | Meaning | Migration |
| --- | --- | --- |
| 0 | Historical saves with no `SchemaVersion` property | Initializes outcome collections, normalizes learning counters, and clears ambiguous legacy adaptive windows. |
| 1 | Outcome-aware learning progress | Normalizes adventure mastery, journey resources, local streaks, last subject, and resumable-session data. |
| 2 | Current journey/session-aware profile | Validated and loaded without migration. |

Migrations always run in order on a deep copy. Each step sets exactly the next version and is safe to run against data that already contains newer additive fields. The result is validated before activation.

Before a migrated on-disk profile is replaced, its exact source JSON is copied beside it with a `.premigration-vN` suffix. Existing names receive a numeric suffix, so a prior migration archive is never overwritten. The normal atomic save path also keeps the last valid primary as `.bak`.

If migration, validation, or archival fails, the profile is not made active and the primary file is not overwritten. Saves from a newer application version are likewise rejected without modification.

Historical fixtures live under `Tests/Fixtures/profiles`. Add a fixture and migration test for every new supported schema version.
