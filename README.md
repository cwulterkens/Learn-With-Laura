# Learn With Laura

Learn With Laura is a single-player, offline learning game for grades 1–8. Each local learner has a separate profile, school progress, Prairie Stars, badges, and adventure progress. Correct lesson answers fill an adventure meter that unlocks short Little House–themed knowledge stops and memory mini-games.

Prairie Town is the game's main hub. Learners walk to the one-room schoolhouse to begin lessons, visit the exploration wagon to enter Laura's world, spend learning rewards at the general store, and return to town after lessons and adventures.

The northwest field hosts **Hunting with Pa**, an arcade-style aiming mini-game inspired by classic big-game hunting galleries but adapted for Laura's slingshot. Its textured 2.5D field, rendered animals, depth scaling, shadows, and target motion provide a lightweight 3D-game feel while retaining the existing 2D architecture. Moving adult rabbits and squirrels are the scoring targets; clearly marked young animals must be allowed to pass. The action is non-graphic, misses have no penalty, and completed hunts earn food, Prairie Pennies, and field-journal observations.

## Learning loop

1. Choose a local learner, grade, and subject.
2. Answer grade-band questions in math, science, history, social studies, geography, or English.
3. Use a hint after a first miss, then read the explanation before moving on.
4. Earn Prairie Stars, build a streak, and unlock badges.
5. Earn Prairie Pennies from correct answers, walk from the prairie house into town, explore local places, and buy keepsakes at the general store.
6. After five correct answers, play a Laura's Adventure chapter with four knowledge stops and two watch-and-repeat mini-games.
7. Take an optional movement or calming break when the lesson timer ends.

Questions label their context as **Actual Life**, **Little House Books**, **Classic TV Adaptation**, **Imagined Prairie Scenario**, or **School Concept**. Source titles are shown with factual explanations. The game does not fetch those sources or use any network service at runtime.

First-try answers are the mastery signal used to adapt math difficulty. Answers completed with a hint still earn learning credit and adventure progress, while corrected and skipped questions are recorded separately. See [Learning outcome semantics](docs/LEARNING_OUTCOMES.md).

All classroom subjects use one shared lesson and progress flow. See [Lesson architecture](docs/LESSON_ARCHITECTURE.md).

Math combines authored prompts with adaptive random generation based on grade and demonstrated skill. Science, history, social studies, geography, and English each provide a deterministic catalog of 512 unique grade-specific questions, supplemented by the authored content pack. Every resolved or skipped question presents the answer, why it works, a reusable solving method, and an example or check when appropriate.

Laura's Adventure uses food, wood, health, morale, days, miles, and region-based camp choices without game-over states or penalties for learning mistakes. See [Journey resource design](docs/JOURNEY_RESOURCES.md).

The Local Progress screen provides offline progress review, timestamped backups, export/import, and confirmed reset controls. Learners use local nicknames rather than account identities. See [Local learner data and privacy](docs/LOCAL_DATA.md).

Keyboard/controller focus, screen-reader metadata, larger text, higher contrast, and static reduced-motion memory patterns are built in. See [Accessibility and release verification](docs/ACCESSIBILITY.md).

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
