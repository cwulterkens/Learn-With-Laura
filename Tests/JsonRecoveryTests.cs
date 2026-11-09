using Xunit;

public class JsonRecoveryTests
{
    [Fact]
    public void MissingFileIsReportedSeparately()
    {
        JsonDocumentResult<ProfileData> result = JsonDocumentParser.Parse<ProfileData>(
            false,
            null);

        Assert.Equal(JsonFileStatus.Missing, result.Status);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void EmptyFileIsReportedSeparately(string text)
    {
        JsonDocumentResult<ProfileData> result = JsonDocumentParser.Parse<ProfileData>(
            true,
            text);

        Assert.Equal(JsonFileStatus.Empty, result.Status);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("{\"FirstName\":")]
    [InlineData("not json")]
    [InlineData("null")]
    public void TruncatedInvalidOrNullJsonIsRejected(string text)
    {
        JsonDocumentResult<ProfileData> result = JsonDocumentParser.Parse<ProfileData>(
            true,
            text);

        Assert.Equal(JsonFileStatus.InvalidJson, result.Status);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public void ValidBackupIsOfferedWhenPrimaryIsDamaged()
    {
        JsonRecoveryDecision decision = JsonRecoveryPolicy.Decide(
            JsonFileStatus.InvalidJson,
            JsonFileStatus.Valid);

        Assert.Equal(JsonRecoveryDecision.OfferBackupRestore, decision);
    }

    [Theory]
    [InlineData(JsonFileStatus.Empty, JsonFileStatus.Missing)]
    [InlineData(JsonFileStatus.InvalidJson, JsonFileStatus.InvalidJson)]
    [InlineData(JsonFileStatus.OpenFailed, JsonFileStatus.Missing)]
    public void FreshStartRequiresExplicitChoiceWhenNoBackupIsUsable(
        JsonFileStatus primary,
        JsonFileStatus backup)
    {
        JsonRecoveryDecision decision = JsonRecoveryPolicy.Decide(primary, backup);

        Assert.Equal(JsonRecoveryDecision.OfferFreshStart, decision);
    }

    [Fact]
    public void MissingPrimaryAndBackupDoesNotLookCorrupt()
    {
        JsonRecoveryDecision decision = JsonRecoveryPolicy.Decide(
            JsonFileStatus.Missing,
            JsonFileStatus.Missing);

        Assert.Equal(JsonRecoveryDecision.NoStoredData, decision);
    }
}
