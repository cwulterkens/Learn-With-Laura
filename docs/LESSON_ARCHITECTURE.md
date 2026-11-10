# Lesson architecture

`Scenes/LearningModule.tscn` and `Scripts/LearningModule.cs` are the single classroom lesson flow for math, science, history, social studies, geography, and English.

The earlier standalone math classroom was retired because it was unreachable from navigation and duplicated progress rules. The active lesson already owns all required behavior:

- lesson and recess timing, including accumulated local learning time;
- first-attempt, hint-assisted, corrected, and skipped outcomes;
- adaptive math tiers based on first-attempt mastery;
- Prairie Stars, streaks, badges, and the adventure meter;
- local save/resume points and safe exits to subjects or home;
- scrollable, keyboard-focus-aware presentation for long questions and feedback.

The retired screen's only unique interaction was free-form integer entry. Current content across every subject uses consistent choice questions so learners can focus on the explanation and classroom context. Free-form input can be reconsidered later as an interaction type inside the shared lesson system; it should not be introduced as a separate scoring or persistence flow.

Navigation and saved lesson resume points must target `res://Scenes/LearningModule.tscn`. `LessonArchitectureTests` guards the route, required responsibilities, and absence of references to the retired implementation.
