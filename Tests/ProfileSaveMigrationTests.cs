using System.Text.Json;
using Xunit;

public class ProfileSaveMigrationTests
{
    [Theory]
    [InlineData("v0-unversioned.json", 0, ProfileMigrationStatus.Migrated)]
    [InlineData("v1-outcome-aware.json", 1, ProfileMigrationStatus.Migrated)]
    [InlineData("v2-current.json", 2, ProfileMigrationStatus.UpToDate)]
    public void EverySupportedSchemaFixtureLoads(
        string fixtureName,
        int originalVersion,
        ProfileMigrationStatus expectedStatus)
    {
        ProfileData source = LoadFixture(fixtureName);

        ProfileMigrationResult result = ProfileSaveMigration.Migrate(source);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(originalVersion, result.OriginalVersion);
        Assert.Equal(ProfileSaveMigration.CurrentVersion, result.FinalVersion);
        Assert.Equal(ProfileSaveMigration.CurrentVersion, result.Profile!.SchemaVersion);
    }

    [Fact]
    public void UnversionedMigrationPreservesTotalsAndResetsAmbiguousLegacyWindow()
    {
        ProfileData source = LoadFixture("v0-unversioned.json");

        ProfileData migrated = ProfileSaveMigration.Migrate(source).Profile!;

        Assert.Equal(6, migrated.TotalCorrect);
        Assert.Equal(8, migrated.TotalAttempted);
        Assert.Equal(120, migrated.GameScore);
        Assert.Equal(2, migrated.OperationProgress["addition"].Tier);
        Assert.Equal(0, migrated.OperationProgress["addition"].WindowCorrect);
        Assert.Equal(0, migrated.OperationProgress["addition"].WindowAttempted);
        Assert.Equal("Half-Pint", migrated.DisplayName);
        Assert.Equal(0, source.SchemaVersion);
        Assert.Equal(7, source.OperationProgress["addition"].WindowCorrect);
    }

    [Fact]
    public void VersionOneMigrationNormalizesJourneyAndPreservesOutcomeEvidence()
    {
        ProfileData source = LoadFixture("v1-outcome-aware.json");

        ProfileData migrated = ProfileSaveMigration.Migrate(source).Profile!;

        Assert.Equal(12, migrated.Outcomes.FirstTryCorrect);
        Assert.Equal(3, migrated.OperationProgress["multiplication"].WindowFirstTryCorrect);
        Assert.Equal(1, migrated.Journey.Day);
        Assert.Equal(0, migrated.Journey.Miles);
        Assert.Equal(10, migrated.Journey.Morale);
        Assert.Equal(0, migrated.Journey.Health);
        Assert.Equal("big_woods", migrated.Journey.CurrentRegion);
        Assert.Equal("math", migrated.LastPlayedSubject);
        Assert.NotNull(migrated.SavedSession);
        Assert.Equal(new[] { "big_woods", "prairie" }, migrated.MasteredLauraAreas);
    }

    [Fact]
    public void MigrationIsOrderedAndIdempotent()
    {
        ProfileMigrationResult first = ProfileSaveMigration.Migrate(
            LoadFixture("v0-unversioned.json"));
        string afterFirst = JsonSerializer.Serialize(first.Profile);

        ProfileMigrationResult second = ProfileSaveMigration.Migrate(first.Profile!);
        string afterSecond = JsonSerializer.Serialize(second.Profile);

        Assert.Equal(ProfileMigrationStatus.Migrated, first.Status);
        Assert.Equal(ProfileMigrationStatus.UpToDate, second.Status);
        Assert.False(second.RequiresSave);
        Assert.Equal(afterFirst, afterSecond);
    }

    [Fact]
    public void FutureSchemaFailsWithoutMutatingSource()
    {
        var source = new ProfileData
        {
            SchemaVersion = ProfileSaveMigration.CurrentVersion + 1,
            DisplayName = "Future-Reader"
        };

        ProfileMigrationResult result = ProfileSaveMigration.Migrate(source);

        Assert.False(result.Succeeded);
        Assert.Equal(ProfileMigrationStatus.UnsupportedVersion, result.Status);
        Assert.Null(result.Profile);
        Assert.Equal(ProfileSaveMigration.CurrentVersion + 1, source.SchemaVersion);
        Assert.Equal("Future-Reader", source.DisplayName);
    }

    [Fact]
    public void InvalidCurrentSchemaFailsWithoutMutatingSource()
    {
        ProfileData source = LoadFixture("v2-current.json");
        source.Journey.Health = -1;

        ProfileMigrationResult result = ProfileSaveMigration.Migrate(source);

        Assert.False(result.Succeeded);
        Assert.Equal(ProfileMigrationStatus.Invalid, result.Status);
        Assert.Null(result.Profile);
        Assert.Equal(-1, source.Journey.Health);
    }

    private static ProfileData LoadFixture(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "profiles", name);
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProfileData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
