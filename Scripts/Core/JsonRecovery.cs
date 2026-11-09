#nullable enable

using System;
using System.Text.Json;

public enum JsonFileStatus
{
    Valid,
    Missing,
    OpenFailed,
    Empty,
    InvalidJson
}

public enum JsonRecoveryDecision
{
    UsePrimary,
    OfferBackupRestore,
    OfferFreshStart,
    NoStoredData
}

public sealed class JsonDocumentResult<T> where T : class
{
    public JsonFileStatus Status { get; init; }
    public T? Value { get; init; }
    public string Error { get; init; } = "";
}

public sealed class JsonRecoveryInspection<T> where T : class
{
    public JsonFileStatus PrimaryStatus { get; init; }
    public T? PrimaryValue { get; init; }
    public string PrimaryError { get; init; } = "";
    public JsonFileStatus BackupStatus { get; init; }
    public T? BackupValue { get; init; }
    public string BackupError { get; init; } = "";
}

public static class JsonDocumentParser
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static JsonDocumentResult<T> Parse<T>(bool exists, string? text) where T : class
    {
        if (!exists)
        {
            return new JsonDocumentResult<T>
            {
                Status = JsonFileStatus.Missing,
                Error = "file does not exist"
            };
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonDocumentResult<T>
            {
                Status = JsonFileStatus.Empty,
                Error = "file is empty"
            };
        }

        try
        {
            T? value = JsonSerializer.Deserialize<T>(text, ReadOptions);
            return value == null
                ? new JsonDocumentResult<T>
                {
                    Status = JsonFileStatus.InvalidJson,
                    Error = "JSON contains null instead of an object"
                }
                : new JsonDocumentResult<T>
                {
                    Status = JsonFileStatus.Valid,
                    Value = value
                };
        }
        catch (Exception ex)
        {
            return new JsonDocumentResult<T>
            {
                Status = JsonFileStatus.InvalidJson,
                Error = ex.Message
            };
        }
    }
}

public static class JsonRecoveryPolicy
{
    public static JsonRecoveryDecision Decide(JsonFileStatus primary, JsonFileStatus backup)
    {
        if (primary == JsonFileStatus.Valid)
        {
            return JsonRecoveryDecision.UsePrimary;
        }

        if (backup == JsonFileStatus.Valid)
        {
            return JsonRecoveryDecision.OfferBackupRestore;
        }

        if (primary == JsonFileStatus.Missing && backup == JsonFileStatus.Missing)
        {
            return JsonRecoveryDecision.NoStoredData;
        }

        return JsonRecoveryDecision.OfferFreshStart;
    }
}
