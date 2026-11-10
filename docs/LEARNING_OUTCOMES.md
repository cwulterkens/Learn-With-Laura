# Learning outcome semantics

Learn With Laura treats practice and mastery as related but different classroom signals. Every completed lesson question records exactly one outcome locally:

| Outcome | Meaning | Learning credit | Adaptive mastery credit |
| --- | --- | --- | --- |
| First-try correct | The learner chose the correct answer before receiving a hint. | Correct answer, two Prairie Stars, streak progress, and adventure-meter progress. | Yes |
| Hint-assisted correct | The learner chose an incorrect answer, received the question's hint, and then chose the correct answer. | Correct answer, one Prairie Star, and adventure-meter progress. | No |
| Corrected | The learner made two incorrect choices, then read the correct answer and explanation. | Practice and explanation shown; no correct-answer or adventure-meter credit. | No |
| Skipped | The learner selected **Skip & Explain**, before or after seeing a hint. | Practice and explanation shown; no correct-answer or adventure-meter credit. | No |

The math difficulty window uses **first-try rate**: first-try correct outcomes divided by completed math questions in that operation's evaluation window. At the configured upper threshold, the operation can advance one tier. At the lower threshold, it can move down one tier to provide gentler confidence-building practice. Hint-assisted work remains visible as successful learning but cannot by itself increase difficulty.

Profiles save outcome totals at the learner, subject, and math-operation levels so a future fully local progress screen can report first-try and assisted learning separately. No outcome information leaves the installed machine.

Older profiles retain their difficulty tier and lifetime correct/attempted totals. Any partially filled adaptive window saved before these outcome categories existed is reset because its old “eventually correct” count cannot be safely interpreted as first-try mastery.
