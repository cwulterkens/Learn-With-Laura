# Local learner data and privacy

Learn With Laura is single-player and fully offline. It does not create accounts or send profile, learning, device, or usage data over a network. There is no cloud sync, telemetry, advertising, or multiplayer service.

## Nicknames

Each learner is identified in the interface by a local nickname. Families should use a nickname such as `Bookworm`, `Half-Pint`, or initials rather than a legal name. For compatibility with existing saves, the JSON property that stores this nickname is still named `FirstName`; the game treats it as a display nickname only.

## Storage location

Godot stores the game's settings, profiles, installed content, and backups in the platform-specific `user://` folder. The Local Progress screen shows the exact expanded folder for the current installation. Typical locations are inside the current operating-system account's application-data folder, such as:

- macOS: `~/Library/Application Support/Learn with Laura/`
- Windows: `%APPDATA%/Learn with Laura/`
- Linux: `$XDG_DATA_HOME/Learn with Laura/`, or `~/.local/share/Learn with Laura/` when that variable is not set

Profile files are JSON under `profiles/`. Explicit safety backups are stored under `profile_backups/` with a timestamp. Backup and exported profile files contain the learner's nickname and all locally recorded progress, so families should store exported copies somewhere they trust.

## Backup, export, import, and reset

- **Create Local Safety Backup** writes a timestamped copy inside `user://profile_backups/`.
- **Export Progress Copy** writes a versioned `.json` transfer file to a local destination chosen by the user.
- **Restore / Import Progress** accepts only a supported Learn With Laura transfer file. It creates a safety backup before replacing progress and preserves the current learner nickname.
- **Reset Classroom Progress** clears subject outcomes, adaptive math progress, streaks, sessions, and learning time while preserving adventure progress.
- **Reset Adventure Progress** clears chapter mastery, journey resources, and adventure activity while preserving classroom progress.
- **Reset All Progress** clears both scopes plus rewards and the resume point, while preserving the nickname and selected grade.

Every import and reset requires confirmation. Canceling makes no changes. These operations never contact a server.
