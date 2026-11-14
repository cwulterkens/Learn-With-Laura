using System;
using System.Collections.Generic;

public static class QuestionExplanationBuilder
{
    public static string Build(RuntimeQuestion question)
    {
        string answer = question.Options.Count > 0
            ? question.Options[Math.Clamp(question.CorrectIndex, 0, question.Options.Count - 1)]
            : question.Answer.ToString();
        string why = string.IsNullOrWhiteSpace(question.SuccessText)
            ? $"This choice correctly answers the question: {answer}."
            : question.SuccessText.Trim();
        string howTo = GetHowTo(question);
        string example = GetExample(question);

        var sections = new List<string>
        {
            $"ANSWER\n{answer}",
            $"WHY IT WORKS\n{why}"
        };
        if (!string.IsNullOrWhiteSpace(howTo))
        {
            sections.Add($"HOW TO SOLVE A QUESTION LIKE THIS\n{howTo}");
        }
        if (!string.IsNullOrWhiteSpace(example))
        {
            sections.Add($"EXAMPLE OR CHECK\n{example}");
        }

        return string.Join("\n\n", sections);
    }

    public static string BuildHint(RuntimeQuestion question)
    {
        return $"TRY THIS METHOD\n{GetHowTo(question)}";
    }

    private static string GetHowTo(RuntimeQuestion question)
    {
        if (!string.IsNullOrWhiteSpace(question.HowTo))
        {
            return question.HowTo.Trim();
        }
        if (!string.IsNullOrWhiteSpace(question.Hint))
        {
            return $"{question.Hint.Trim()} Then compare each choice with that clue and eliminate choices that do not fit.";
        }

        return question.SubjectKey switch
        {
            "math" => "Identify the operation or relationship, show one step, and check the result with an estimate or inverse operation.",
            "english" => "Read once for meaning. Read again for the word, sentence, or evidence named in the question, then test each choice.",
            "science" => "Identify the scientific idea and the evidence in the question. Choose the answer that connects both without adding an unsupported claim.",
            "history" => "Place the evidence in time, identify who created it and why, and compare the choices with what the evidence can actually support.",
            "social_studies" => "Identify the institution, rule, resource, or choice involved, then connect it to its effect on people and communities.",
            "geography" => "Identify the place, pattern, scale, or data relationship first. Then use only the information needed by the question.",
            _ => "Restate what the question asks, find the strongest clue, and eliminate choices that conflict with it."
        };
    }

    private static string GetExample(RuntimeQuestion question)
    {
        if (!string.IsNullOrWhiteSpace(question.Example))
        {
            return question.Example.Trim();
        }

        return question.SubjectKey switch
        {
            "english" => "For a similar language question, underline the exact word or sentence feature being tested before choosing an answer.",
            "science" => "For a similar investigation, name what changes, what stays the same, and what observation would support the conclusion.",
            "history" => "For a similar source question, write: creator, date, purpose, useful evidence, and one limitation.",
            "social_studies" => "For a similar civic or economic question, name the choice and trace one likely consequence for the community.",
            "geography" => "For a similar map or data question, identify the units and legend first, then show the comparison or calculation.",
            _ => string.Empty
        };
    }
}
