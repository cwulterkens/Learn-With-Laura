# Learn With Laura

Learn With Laura is a single-player, offline learning game for grades 1–8. Each local learner has a separate profile, school progress, Prairie Stars, badges, and adventure progress. Correct lesson answers fill an adventure meter that unlocks short Little House–themed knowledge stops and memory mini-games.

## Learning loop

1. Choose a local learner, grade, and subject.
2. Answer grade-band questions in math, science, history, social studies, geography, or English.
3. Use a hint after a first miss, then read the explanation before moving on.
4. Earn Prairie Stars, build a streak, and unlock badges.
5. After five correct answers, play a Laura's Adventure chapter with four knowledge stops and two watch-and-repeat mini-games.
6. Take an optional movement or calming break when the lesson timer ends.

Questions label their context as **Actual Life**, **Little House Books**, **Classic TV Adaptation**, **Imagined Prairie Scenario**, or **School Concept**. Source titles are shown with factual explanations. The game does not fetch those sources or use any network service at runtime.

First-try answers are the mastery signal used to adapt math difficulty. Answers completed with a hint still earn learning credit and adventure progress, while corrected and skipped questions are recorded separately. See [Learning outcome semantics](docs/LEARNING_OUTCOMES.md).

Laura's Adventure uses food, wood, health, morale, days, miles, and region-based camp choices without game-over states or penalties for learning mistakes. See [Journey resource design](docs/JOURNEY_RESOURCES.md).

The Local Progress screen provides offline progress review, timestamped backups, export/import, and confirmed reset controls. Learners use local nicknames rather than account identities. See [Local learner data and privacy](docs/LOCAL_DATA.md).

## Run and build

Requirements: Godot 4.8 Mono (currently 4.8.0-beta.2) and the .NET 10 SDK.

```sh
dotnet restore "Learn With Laura.sln"
dotnet build --no-restore "Learn With Laura.sln"
dotnet test --no-build "Tests/LearnWithLaura.Tests.csproj"
godot --path .
```

Run the content and asset checks before distributing a build:

```sh
./tools/validate_content.sh
```

Godot stores settings, learner profiles, and installed content under its platform-specific `user://` directory. JSON saves use temporary-file validation plus last-known-good backups. No account, server, telemetry, advertising, or multiplayer system is present.

## Content and asset review

- [Content notes](content/CONTENT_NOTES.md) documents grade bands, canon labels, sources, and the editorial review policy.
- [Asset guide](ASSET_GUIDE.md) defines the painterly storybook direction and technical standards.
- [Asset manifest](assets/ASSET_MANIFEST.csv) records runtime status and known provenance for every source asset.
- [Third-party notices](THIRD_PARTY_NOTICES.md) records the bundled font license.

Several inherited images, interface textures, music tracks, and sound effects still have unknown provenance. They are identified in the manifest and require a human rights review before public distribution. Historical and curriculum material should also receive educator review and, for Native history, review by an appropriate Native or Osage cultural authority before release.
