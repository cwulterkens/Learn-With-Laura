using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class QuestionEngine
{
    private readonly RandomNumberGenerator _rng = new();
    private readonly Dictionary<string, Queue<int>> _questionDecks = new();
    private readonly Dictionary<string, int> _lastQuestionIndexes = new();

    public QuestionEngine()
    {
        _rng.Randomize();
    }

    public RuntimeQuestion NextQuestion(int grade, string subjectKey)
    {
        return subjectKey == "math"
            ? NextMathQuestion(grade)
            : GetSubjectQuestion(subjectKey, grade);
    }

    public RuntimeQuestion NextLauraQuestion(string areaId, HashSet<int> usedQuestionIndexes)
    {
        LauraAreaDefinition area = AppState.GetLauraArea(areaId);
        if (area == null || area.Questions.Count == 0)
        {
            return new RuntimeQuestion
            {
                SubjectKey = "laura",
                Prompt = "What did Laura love most about the big woods?",
                Options = new List<string> { "Exploring outdoors", "Flying airplanes", "Building robots", "Driving cars" },
                CorrectIndex = 0,
                SuccessText = "Laura loved the woods, the animals, and exploring with her family."
            };
        }

        List<int> availableIndexes = Enumerable.Range(0, area.Questions.Count)
            .Where(index => !usedQuestionIndexes.Contains(index))
            .ToList();

        if (availableIndexes.Count == 0)
        {
            availableIndexes = Enumerable.Range(0, area.Questions.Count).ToList();
        }

        int pickedIndex = TakeIndexFromValues($"laura:{areaId}", availableIndexes);
        usedQuestionIndexes.Add(pickedIndex);
        return ToRuntimeQuestion("laura", area.Questions[pickedIndex]);
    }

    public bool TryApplyAdaptiveProgress(string operation, out string message)
    {
        message = string.Empty;

        if (AppState.ActiveProfile == null)
        {
            return false;
        }

        OperationProgress progress = AppState.GetOperationProgress(operation);
        int windowSize = Math.Max(1, AppState.Settings.QuestionsPerEvaluation);
        OperationConfig config = AppState.GetOperationConfig(AppState.CurrentGrade, operation);
        int maxTier = Math.Max(0, config.MaxValuesByTier.Count - 1);
        if (!AdaptiveMastery.TryEvaluate(
            progress,
            windowSize,
            AppState.Settings.PromoteAccuracy,
            AppState.Settings.DemoteAccuracy,
            maxTier,
            out AdaptiveTierChange change))
        {
            return false;
        }

        message = change switch
        {
            AdaptiveTierChange.Promoted =>
                $"Your first-try mastery in {ToDisplay(operation)} raised the difficulty.",
            AdaptiveTierChange.Demoted =>
                $"Let's practice {ToDisplay(operation)} at a gentler level to build first-try confidence.",
            _ => string.Empty
        };

        return change != AdaptiveTierChange.None;
    }

    private RuntimeQuestion NextMathQuestion(int grade)
    {
        string[] operations = AppState.GetEnabledOperations(grade);
        if (operations.Length == 0)
        {
            operations = new[] { "addition" };
        }

        string operation = operations[_rng.RandiRange(0, operations.Length - 1)];

        bool useAuthored = _rng.Randf() <= AppState.Settings.AuthoredQuestionChance;
        if (useAuthored && TryGetAuthoredMath(grade, operation, out RuntimeQuestion authored))
        {
            authored.SubjectKey = "math";
            authored.OperationKey = operation;
            return authored;
        }

        return GenerateMath(grade, operation);
    }

    private RuntimeQuestion GenerateMath(int grade, string operation)
    {
        OperationConfig config = AppState.GetOperationConfig(grade, operation);
        OperationProgress progress = AppState.GetOperationProgress(operation);

        int tier = Math.Clamp(progress.Tier, 0, Math.Max(0, config.MaxValuesByTier.Count - 1));
        int maxValue = config.MaxValuesByTier.Count > 0 ? Math.Max(1, config.MaxValuesByTier[tier]) : 10;

        int a;
        int b;
        string symbol;
        int answer;

        switch (operation)
        {
            case "subtraction":
                a = _rng.RandiRange(0, maxValue);
                b = _rng.RandiRange(0, maxValue);
                if (b > a)
                {
                    (a, b) = (b, a);
                }
                symbol = "-";
                answer = a - b;
                break;

            case "multiplication":
                a = _rng.RandiRange(0, maxValue);
                b = _rng.RandiRange(0, maxValue);
                symbol = "x";
                answer = a * b;
                break;

            case "division":
                b = _rng.RandiRange(1, Math.Max(2, maxValue));
                int quotient = _rng.RandiRange(0, maxValue);
                a = b * quotient;
                symbol = "/";
                answer = quotient;
                break;

            default:
                a = _rng.RandiRange(0, maxValue);
                b = _rng.RandiRange(0, maxValue);
                symbol = "+";
                answer = a + b;
                operation = "addition";
                break;
        }

        return BuildMathQuestion(
            operation,
            $"What is {a} {symbol} {b}?",
            answer,
            $"The answer is {answer}.");
    }

    private bool TryGetAuthoredMath(int grade, string operation, out RuntimeQuestion question)
    {
        question = new RuntimeQuestion();

        if (!AppState.QuestionContent.Questions.TryGetValue(grade.ToString(), out var gradeQuestions)
            || !gradeQuestions.TryGetValue(operation, out var items)
            || items.Count == 0)
        {
            return false;
        }

        int index = TakeIndex($"math:{grade}:{operation}", items.Count);
        AuthoredQuestion picked = items[index];

        question = BuildMathQuestion(operation, picked.Prompt, picked.Answer, $"The answer is {picked.Answer}.");
        return true;
    }

    private RuntimeQuestion GetSubjectQuestion(string subjectKey, int requestedGrade)
    {
        if (!AppState.SubjectQuestionContent.Subjects.TryGetValue(subjectKey, out var gradeMap)
            || gradeMap.Count == 0)
        {
            return new RuntimeQuestion
            {
                SubjectKey = subjectKey,
                Prompt = $"More {AppState.GetSubjectDisplayName(subjectKey)} questions can be added in the update file later.",
                Options = new List<string> { "OK", "Next", "Learn", "Play" },
                CorrectIndex = 0,
                SuccessText = "New question packs can be dropped into user://content/subject_questions.json."
            };
        }

        string selectedGradeKey = FindBestGradeKey(gradeMap.Keys, requestedGrade);
        List<ChoiceQuestion> questions = gradeMap[selectedGradeKey]
            .Where(question => requestedGrade >= question.GradeMin && requestedGrade <= question.GradeMax)
            .ToList();
        if (questions.Count == 0)
        {
            questions = gradeMap[selectedGradeKey];
        }

        int pickedIndex = TakeIndex($"subject:{subjectKey}:{selectedGradeKey}:{requestedGrade}", questions.Count);
        ChoiceQuestion picked = questions[pickedIndex];
        return ToRuntimeQuestion(subjectKey, picked);
    }

    private RuntimeQuestion BuildMathQuestion(string operation, string prompt, int answer, string successText)
    {
        List<int> distractors = BuildMathDistractors(answer);
        List<string> options = distractors.Select(value => value.ToString()).ToList();
        int correctIndex = options.IndexOf(answer.ToString());

        return new RuntimeQuestion
        {
            SubjectKey = "math",
            OperationKey = operation,
            Prompt = prompt,
            Answer = answer,
            Options = options,
            CorrectIndex = correctIndex,
            Hint = GetMathHint(operation),
            SuccessText = successText
        };
    }

    private static string GetMathHint(string operation)
    {
        return operation switch
        {
            "addition" => "Combine the quantities or expressions carefully, then use an estimate to check the size of your answer.",
            "subtraction" => "Find the difference or missing amount. Inverse addition, a number line, or a diagram can help.",
            "multiplication" => "Look for equal groups, area, rate, percent, probability, or repeated scaling in the problem.",
            "division" => "Look for equal groups, a unit rate, slope, a missing factor, or the size of one share.",
            _ => "Identify the relationship, show one useful step, and estimate before choosing an answer."
        };
    }

    private List<int> BuildMathDistractors(int answer)
    {
        var values = new HashSet<int> { answer };
        int scaleStep = 1;
        if (answer >= 100)
        {
            int digits = (int)Math.Floor(Math.Log10(answer)) + 1;
            scaleStep = (int)Math.Pow(10, Math.Max(1, digits - 2));
        }

        int[] offsets =
        {
            -scaleStep,
            scaleStep,
            -2 * scaleStep,
            2 * scaleStep,
            -1,
            1,
            -2,
            2,
            -3,
            3
        };
        int offsetIndex = 0;

        while (values.Count < 4 && offsetIndex < offsets.Length)
        {
            long candidate = (long)answer + offsets[offsetIndex];
            values.Add((int)Math.Clamp(candidate, 0L, int.MaxValue));
            offsetIndex++;
        }

        while (values.Count < 4)
        {
            values.Add(Math.Max(0, answer + _rng.RandiRange(-5, 5)));
        }

        List<int> shuffled = values.ToList();
        for (int index = shuffled.Count - 1; index > 0; index--)
        {
            int swapIndex = _rng.RandiRange(0, index);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }

        return shuffled;
    }

    private RuntimeQuestion ToRuntimeQuestion(string subjectKey, ChoiceQuestion picked)
    {
        var indexedOptions = picked.Options
            .Select((text, index) => new { Text = text, IsCorrect = index == picked.CorrectIndex })
            .ToList();

        for (int index = indexedOptions.Count - 1; index > 0; index--)
        {
            int swapIndex = _rng.RandiRange(0, index);
            (indexedOptions[index], indexedOptions[swapIndex]) = (indexedOptions[swapIndex], indexedOptions[index]);
        }

        int correctIndex = indexedOptions.FindIndex(option => option.IsCorrect);
        return new RuntimeQuestion
        {
            Id = picked.Id,
            SubjectKey = subjectKey,
            OperationKey = subjectKey,
            SkillId = picked.SkillId,
            Canon = picked.Canon,
            Prompt = picked.Prompt,
            Options = indexedOptions.Select(option => option.Text).ToList(),
            CorrectIndex = Math.Clamp(correctIndex, 0, Math.Max(0, indexedOptions.Count - 1)),
            Hint = picked.Hint,
            SuccessText = picked.SuccessText,
            SourceTitle = picked.SourceTitle,
            SourceUrl = picked.SourceUrl
        };
    }

    private int TakeIndex(string deckKey, int count)
    {
        if (count <= 1)
        {
            return 0;
        }

        if (!_questionDecks.TryGetValue(deckKey, out Queue<int> deck) || deck.Count == 0)
        {
            List<int> shuffled = Enumerable.Range(0, count).ToList();
            Shuffle(shuffled);

            if (_lastQuestionIndexes.TryGetValue(deckKey, out int lastIndex)
                && shuffled.Count > 1
                && shuffled[0] == lastIndex)
            {
                (shuffled[0], shuffled[1]) = (shuffled[1], shuffled[0]);
            }

            deck = new Queue<int>(shuffled);
            _questionDecks[deckKey] = deck;
        }

        int picked = deck.Dequeue();
        _lastQuestionIndexes[deckKey] = picked;
        return picked;
    }

    private int TakeIndexFromValues(string deckKey, List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        if (values.Count == 1)
        {
            _lastQuestionIndexes[deckKey] = values[0];
            return values[0];
        }

        List<int> shuffled = values.ToList();
        Shuffle(shuffled);
        if (_lastQuestionIndexes.TryGetValue(deckKey, out int lastIndex) && shuffled[0] == lastIndex)
        {
            (shuffled[0], shuffled[1]) = (shuffled[1], shuffled[0]);
        }

        int picked = shuffled[0];
        _lastQuestionIndexes[deckKey] = picked;
        return picked;
    }

    private void Shuffle(List<int> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = _rng.RandiRange(0, index);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static string FindBestGradeKey(IEnumerable<string> keys, int requestedGrade)
    {
        List<int> numeric = keys
            .Select(key => int.TryParse(key, out int value) ? value : -1)
            .Where(value => value >= 0)
            .OrderBy(value => value)
            .ToList();

        if (numeric.Count == 0)
        {
            return keys.First();
        }

        int selected = numeric.LastOrDefault(value => value <= requestedGrade);
        if (selected == 0 && !numeric.Contains(0) && requestedGrade < numeric[0])
        {
            selected = numeric[0];
        }

        return selected.ToString();
    }

    private static string ToDisplay(string operation)
    {
        return operation switch
        {
            "addition" => "Addition",
            "subtraction" => "Subtraction",
            "multiplication" => "Multiplication",
            "division" => "Division",
            _ => operation
        };
    }
}
