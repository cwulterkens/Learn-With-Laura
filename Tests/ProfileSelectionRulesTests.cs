using Xunit;

public class ProfileSelectionRulesTests
{
    [Fact]
    public void SwitchingProfilesImmediatelyUsesEachLearnersGrade()
    {
        var settings = new GameSettings { LastSelectedGrade = 1 };
        var youngerLearner = new ProfileData { FirstName = "Carrie", CurrentGrade = 2 };
        var olderLearner = new ProfileData { FirstName = "Laura", CurrentGrade = 7 };

        int firstGrade = ProfileSelectionRules.ActivateGrade(youngerLearner, settings);
        int secondGrade = ProfileSelectionRules.ActivateGrade(olderLearner, settings);

        Assert.Equal(2, firstGrade);
        Assert.Equal(7, secondGrade);
        Assert.Equal(7, settings.LastSelectedGrade);
    }

    [Theory]
    [InlineData(-4, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(8, 8)]
    [InlineData(12, 8)]
    public void ProfileGradeIsClampedAndPersistable(int storedGrade, int expectedGrade)
    {
        var settings = new GameSettings();
        var profile = new ProfileData { CurrentGrade = storedGrade };

        int activeGrade = ProfileSelectionRules.ActivateGrade(profile, settings);

        Assert.Equal(expectedGrade, activeGrade);
        Assert.Equal(expectedGrade, profile.CurrentGrade);
        Assert.Equal(expectedGrade, settings.LastSelectedGrade);
    }
}
