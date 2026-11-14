using Xunit;

public class SubjectQuestionBankTests
{
    public static IEnumerable<object[]> Subjects()
    {
        yield return new object[] { "science" };
        yield return new object[] { "history" };
        yield return new object[] { "social_studies" };
        yield return new object[] { "geography" };
        yield return new object[] { "english" };
    }

    [Theory]
    [MemberData(nameof(Subjects))]
    public void EveryNonMathSubjectHasAtLeastFiveHundredUniqueQuestions(string subject)
    {
        IReadOnlyList<ChoiceQuestion> questions = SubjectQuestionBank.GetSubjectCatalog(subject);

        Assert.True(questions.Count >= 500);
        Assert.Equal(questions.Count, questions.Select(question => question.Id).Distinct().Count());
        Assert.Equal(questions.Count, questions.Select(question => question.Prompt).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Subjects))]
    public void GeneratedQuestionsHaveCompleteInstructionalSupport(string subject)
    {
        IReadOnlyList<ChoiceQuestion> questions = SubjectQuestionBank.GetSubjectCatalog(subject);

        Assert.All(questions, question =>
        {
            Assert.InRange(question.GradeMin, 1, 8);
            Assert.Equal(question.GradeMin, question.GradeMax);
            Assert.Equal(4, question.Options.Count);
            Assert.Equal(4, question.Options.Distinct().Count());
            Assert.InRange(question.CorrectIndex, 0, 3);
            Assert.False(string.IsNullOrWhiteSpace(question.SuccessText));
            Assert.False(string.IsNullOrWhiteSpace(question.HowTo));
            Assert.False(string.IsNullOrWhiteSpace(question.Example));
            Assert.StartsWith("https://", question.SourceUrl);
        });
    }

    [Fact]
    public void ExplanationBuilderSeparatesAnswerReasonMethodAndExample()
    {
        var question = new RuntimeQuestion
        {
            SubjectKey = "science",
            Options = new List<string> { "Evaporation", "Freezing", "Melting", "Condensation" },
            CorrectIndex = 0,
            SuccessText = "Liquid water becomes water vapor.",
            HowTo = "Track the state before and after the change.",
            Example = "A puddle becomes smaller on a warm day."
        };

        string explanation = QuestionExplanationBuilder.Build(question);

        Assert.Contains("ANSWER\nEvaporation", explanation);
        Assert.Contains("WHY IT WORKS", explanation);
        Assert.Contains("HOW TO SOLVE A QUESTION LIKE THIS", explanation);
        Assert.Contains("EXAMPLE OR CHECK", explanation);
    }
}
