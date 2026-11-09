# Learning Content Notes

## Purpose and scope

This content supports a single-player learning game for multiple local learner profiles. Correct answers can unlock Little House-themed activities, but the questions themselves must remain instructionally useful: every item has one defensible answer, plausible distractors, a useful hint, and feedback that teaches more than “correct.”

The content distinguishes Laura Ingalls Wilder's documented life, the Little House books, the classic television adaptation, general school knowledge, and newly imagined game adventures. A story detail must never silently move from one category to another.

Both question files use `ContentVersion: 3`:

- `subject_questions.json` contains five subjects with 12 questions in each of three grade bands (180 questions total).
- `laura_minigame.json` contains five themed areas with eight questions each (40 questions total).

`math_questions.json` uses `ContentVersion: 2` and contains eight authored questions in each of four operation buckets for every grade from 1 through 8 (256 questions total). The default mix is 65% authored concept/application questions and 35% procedurally generated fluency practice. Its progression moves from concrete counting and place value through multi-step whole-number work, fractions, measurement, ratios, percent, equations, geometry, and integer-valued expressions. Multiplication and division banks are present for every grade so local difficulty settings can be customized, although the default configuration keeps multiplication and division off in grade 1 and division off in grade 2. The current math schema supports only nonnegative integer answers, so questions involving money, time, fractions, decimals, and rates are worded to produce a whole-number result.

The source URLs are editorial references packaged with the offline data. The released game does not need network access to ask or score any question.

## Grade bands

The grade keys identify bands, not single grades:

| Key | `GradeMin`–`GradeMax` | Intended progression |
| --- | --- | --- |
| `1` | 1–2 | Concrete observations, vocabulary, simple comparisons, and one-step reasoning |
| `3` | 3–5 | Multi-step application, source comparison, data interpretation, and supported explanations |
| `6` | 6–8 | Systems, proportional and causal reasoning, source limitations, argument, and nuanced civic or historical analysis |

Items should be checked against the entire band. A familiar topic does not make an item age-appropriate if its reading load, emotional content, or required background knowledge is too advanced.

## Canon labels

Every item has exactly one `Canon` value:

- `actual_life`: Supported biographical or historical information about Laura, her family, or their documented context.
- `book`: A detail or a question explicitly about the Little House books as literary works. The books are autobiographical fiction, not transcripts.
- `tv_1974`: A detail or media-literacy question explicitly about the classic television adaptation. A TV plot is not evidence that an event happened in Laura's life.
- `general`: Transferable school knowledge that does not depend on the Laura setting.
- `inspired`: An original game scenario. Prompts and feedback explicitly call these scenarios “imagined” so learners do not absorb them as biography.

When two categories matter, the prompt names the comparison and the label identifies the main evidence being assessed. Do not use “true story” as shorthand for a book or television scene.

## Item and source policy

Each item includes `Id`, `GradeMin`, `GradeMax`, `SkillId`, `Canon`, `Prompt`, four `Options`, `CorrectIndex`, `Hint`, `SuccessText`, `SourceTitle`, and `SourceUrl`.

- IDs and prompts must be unique. IDs are stable once released so learner records and analytics remain interpretable.
- Sources should be authoritative and close to the claim: National Park Service, Library of Congress, Smithsonian/National Museum of the American Indian, Minnesota Historical Society, American Printing House for the Blind, published curriculum standards, and established scholarly projects.
- A standards document can support the skill being practiced, but it is not a source for a Laura-specific fact. Historical claims need historical sources.
- Sources support editorial verification; they do not excuse overclaiming. Wording should reflect uncertainty, variation by place, and limits in the evidence.
- Periodic review should check that links still resolve and that a source still supports the exact wording. A link change alone does not justify changing a stable item ID, but a materially changed claim should receive a new ID.
- Do not use stale “current events,” trauma as spectacle, trick questions, stereotypes, or distractors that teach a harmful falsehood through repetition.

## Historical and cultural safeguards

The Kansas material must not frame the prairie as empty or reduce Osage people to background figures in a settler adventure. The Ingalls cabin site was within the Osage Diminished Reserve before legal opening to non-Native settlement. Lessons should identify the Osage as a sovereign Native nation with a continuing present, distinguish homelands from later state borders, and explain that settlement policies produced unequal benefits and harms.

Native nations are distinct political and cultural communities. Do not generalize one nation's history, clothing, language, or experience to all Native peoples. Prefer nation-specific sources and present-day Native-authored materials. Avoid gamifying removal, land seizure, hunger, disability, illness, or death.

Mary Ingalls content should center documented education, skills, agency, relationships, and access—not pity, inspiration clichés, or a deficit-only account of blindness. Accessibility is both a subject-content responsibility and a product requirement.

## Required human review before release

These files are an authored draft, not a claim that consultation has already occurred. Release should remain gated on the following reviews:

1. **Educator review:** Elementary and middle-grade educators should check standards fit, reading level, answer defensibility, distractor quality, hints, explanations, and the actual difficulty progression in every band. Classroom or playtest evidence should be used to revise items that confuse careful learners.
2. **Osage review:** Engage and compensate an Osage Nation-approved cultural or educational reviewer for all Osage, Kansas, reserve, treaty, sovereignty, and settlement material. Follow requested terminology, sourcing, scope, and any decision not to use a particular activity or framing.
3. **Disability and accessibility review:** Engage a disability-history reviewer and blind or low-vision testers, including review of Mary Ingalls material. Verify screen-reader output, keyboard and switch access, contrast, scalable text, non-audio cues, input timing, and alternatives to precision or rapid-response play.
4. **Editorial fact check:** Re-open every cited source, record the check date outside the learner-facing text, verify names/dates/places, and confirm that book and TV details remain labeled as adaptations.
5. **Playtest and bias review:** Test with learners across the stated grade bands and with varied reading, motor, sensory, and cognitive needs. Review answer-position balance, accidental clues, culturally loaded assumptions, and whether feedback invites another try without shame.

Re-run automated validation whenever any question JSON file changes. For choice-question files, check schema, counts, IDs, options, grade ranges, canon, sources, and answer indices; for math, also check bucket counts, prompt uniqueness, and nonnegative integer answers. Increment `ContentVersion` when a schema or interpretation change could affect loaders or saved learner data.
