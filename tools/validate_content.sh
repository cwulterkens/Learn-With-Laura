#!/usr/bin/env bash

set -euo pipefail

repo_dir="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_dir"

for file in config/*.json content/*.json; do
    jq -e . "$file" >/dev/null
done

jq -e '
    (.Grades | keys | sort_by(tonumber) == ["1", "2", "3", "4", "5", "6", "7", "8"])
    and all(.Grades[];
        (.Operations | keys | sort) == ["addition", "division", "multiplication", "subtraction"]
        and all(.Operations[];
            (.Enabled | type == "boolean")
            and (.MaxValuesByTier | type == "array" and length == 4)
            and all(.MaxValuesByTier[]; type == "number" and . > 0)))
' config/math_difficulty.json >/dev/null

jq -e '
    .ContentVersion >= 2
    and (.Questions | keys | sort_by(tonumber) == ["1", "2", "3", "4", "5", "6", "7", "8"])
    and (all(.Questions[]; (keys | sort) == ["addition", "division", "multiplication", "subtraction"]))
    and (all(.Questions[][]; length == 8))
    and ([.Questions[][][]] as $questions
        | ($questions | length) == 256
        and ($questions | map(.Prompt) | unique | length) == 256
        and all($questions[];
            (.Prompt | type == "string" and length > 0 and length <= 240)
            and (.Answer | type == "number" and . >= 0 and . == floor)))
' content/math_questions.json >/dev/null

jq -e '
    .ContentVersion >= 2
    and (.Subjects | keys | sort == ["english", "geography", "history", "science", "social_studies"])
    and (all(.Subjects[]; (keys | sort) == ["1", "3", "6"]))
    and (all(.Subjects[][]; length == 12))
    and ([.Subjects[][][]] as $questions
        | ($questions | length) == 180
        and ($questions | map(.Id) | unique | length) == 180
        and all($questions[];
            (.Id | type == "string" and length > 0)
            and (.GradeMin | type == "number" and . >= 1)
            and (.GradeMax | type == "number" and . <= 8)
            and .GradeMin <= .GradeMax
            and (.SkillId | type == "string" and length > 0)
            and (.Canon | IN("general", "actual_life", "book", "tv_1974", "inspired"))
            and (.Prompt | type == "string" and length > 0)
            and (.Options | type == "array" and length == 4)
            and (.CorrectIndex | type == "number" and . >= 0 and . < 4)
            and (.Hint | type == "string" and length > 0)
            and (.SuccessText | type == "string" and length > 0)
            and (.SourceTitle | type == "string" and length > 0)
            and (.SourceUrl | type == "string" and startswith("https://"))
        ))
' content/subject_questions.json >/dev/null

jq -e '
    .ContentVersion >= 2
    and (.Areas | length == 5)
    and (all(.Areas[];
        (.Id | type == "string" and length > 0)
        and (.Name | type == "string" and length > 0)
        and (.Description | type == "string" and length > 0)
        and (.BackgroundPath | type == "string" and startswith("res://assets/"))
        and (.Questions | length == 8)))
    and ([.Areas[].Questions[]] as $questions
        | ($questions | length) == 40
        and ($questions | map(.Id) | unique | length) == 40
        and all($questions[];
            (.Id | type == "string" and length > 0)
            and (.Canon | IN("general", "actual_life", "book", "tv_1974", "inspired"))
            and (.Prompt | type == "string" and length > 0)
            and (.Options | type == "array" and length == 4)
            and (.CorrectIndex | type == "number" and . >= 0 and . < 4)
            and (.Hint | type == "string" and length > 0)
            and (.SuccessText | type == "string" and length > 0)
            and (.SourceTitle | type == "string" and length > 0)
            and (.SourceUrl | type == "string" and startswith("https://"))
        ))
' content/laura_minigame.json >/dev/null

jq -e '
    .ContentVersion >= 2
    and (.Badges | length >= 8)
    and (.Badges | map(.Id) | unique | length == length)
    and all(.Badges[];
        (.Id | type == "string" and length > 0)
        and (.Name | type == "string" and length > 0)
        and (.Description | type == "string" and length > 0)
        and (.Metric | type == "string" and length > 0)
        and (.Threshold | type == "number" and . > 0))
' content/badges.json >/dev/null

while IFS= read -r background_path; do
    asset_path="${background_path#res://}"
    if [[ ! -f "$asset_path" ]]; then
        printf 'Missing Laura adventure background: %s\n' "$asset_path" >&2
        exit 1
    fi
done < <(jq -r '.Areas[].BackgroundPath' content/laura_minigame.json)

duplicate_manifest_paths="$(tail -n +2 assets/ASSET_MANIFEST.csv | cut -d, -f1 | tr -d '"' | sort | uniq -d)"
if [[ -n "$duplicate_manifest_paths" ]]; then
    printf 'Duplicate asset manifest paths:\n%s\n' "$duplicate_manifest_paths" >&2
    exit 1
fi

manifest_paths="$(mktemp)"
source_asset_paths="$(mktemp)"
trap 'rm -f "$manifest_paths" "$source_asset_paths"' EXIT

tail -n +2 assets/ASSET_MANIFEST.csv | cut -d, -f1 | tr -d '"' | sort > "$manifest_paths"
{
    find assets -type f ! -name '*.import' ! -name '.DS_Store' ! -name 'ASSET_MANIFEST.csv'
    printf '%s\n' icon.svg
} | sort > "$source_asset_paths"

missing_manifest_rows="$(comm -23 "$source_asset_paths" "$manifest_paths")"
missing_asset_files="$(comm -13 "$source_asset_paths" "$manifest_paths")"
if [[ -n "$missing_manifest_rows" ]]; then
    printf 'Source assets missing manifest rows:\n%s\n' "$missing_manifest_rows" >&2
    exit 1
fi
if [[ -n "$missing_asset_files" ]]; then
    printf 'Manifest rows point to missing source assets:\n%s\n' "$missing_asset_files" >&2
    exit 1
fi

asset_count="$(wc -l < "$manifest_paths" | tr -d ' ')"
printf 'Validated JSON, 256 math questions, 180 subject questions, 40 Laura questions, badges, referenced backgrounds, and all %s inventoried source assets.\n' "$asset_count"
