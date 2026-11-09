using Godot;
using System;
using System.Text.Json;

public static class JsonStorage
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public static T LoadOrDefault<T>(string path, T fallback) where T : class
    {
        if (TryLoad(path, out T value, out _, out JsonFileStatus primaryFailure, out string primaryError))
        {
            return value;
        }

        if (IsUserPath(path))
        {
            string backupPath = GetBackupPath(path);
            if (TryLoad(backupPath, out value, out _, out JsonFileStatus backupFailure, out string backupError))
            {
                GD.PushWarning(
                    primaryFailure == JsonFileStatus.Missing
                        ? $"Recovered JSON from backup because the primary file is missing: {path}"
                        : $"Recovered JSON from backup after the primary failed to load: {path} ({primaryError})");
                return value;
            }

            if (primaryFailure != JsonFileStatus.Missing)
            {
                GD.PrintErr($"Failed to load JSON at {path}: {primaryError}");
            }

            if (backupFailure != JsonFileStatus.Missing)
            {
                GD.PrintErr($"Failed to load JSON backup at {backupPath}: {backupError}");
            }

            return fallback;
        }

        if (primaryFailure != JsonFileStatus.Missing)
        {
            GD.PrintErr($"Failed to load JSON at {path}: {primaryError}");
        }

        return fallback;
    }

    public static JsonRecoveryInspection<T> InspectUserFile<T>(string path) where T : class
    {
        TryLoad(
            path,
            out T primaryValue,
            out _,
            out JsonFileStatus primaryStatus,
            out string primaryError);
        TryLoad(
            GetBackupPath(path),
            out T backupValue,
            out _,
            out JsonFileStatus backupStatus,
            out string backupError);

        return new JsonRecoveryInspection<T>
        {
            PrimaryStatus = primaryStatus,
            PrimaryValue = primaryStatus == JsonFileStatus.Valid ? primaryValue : null,
            PrimaryError = primaryError,
            BackupStatus = backupStatus,
            BackupValue = backupStatus == JsonFileStatus.Valid ? backupValue : null,
            BackupError = backupError
        };
    }

    public static bool Save<T>(string path, T value) where T : class
    {
        string json;
        try
        {
            json = JsonSerializer.Serialize(value, WriteOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to serialize JSON for {path}: {ex.Message}");
            return false;
        }

        if (!EnsureDirectory(path))
        {
            return false;
        }

        return IsUserPath(path)
            ? SaveUserFile<T>(path, json)
            : WriteTextFile(path, json);
    }

    public static void CopyIfMissing(string fromResPath, string toUserPath)
    {
        if (FileAccess.FileExists(toUserPath) || !FileAccess.FileExists(fromResPath))
        {
            return;
        }

        using var source = FileAccess.Open(fromResPath, FileAccess.ModeFlags.Read);
        if (source == null)
        {
            return;
        }

        string data = source.GetAsText();
        if (!EnsureDirectory(toUserPath))
        {
            return;
        }

        string temporaryPath = GetTemporaryPath(toUserPath);
        try
        {
            if (!WriteTextFile(temporaryPath, data))
            {
                return;
            }

            Error renameError = DirAccess.RenameAbsolute(temporaryPath, toUserPath);
            if (renameError != Error.Ok)
            {
                GD.PrintErr($"Failed to install default JSON at {toUserPath}: {renameError}");
            }
        }
        finally
        {
            RemoveIfPresent(temporaryPath);
        }
    }

    private static bool SaveUserFile<T>(string path, string json) where T : class
    {
        string temporaryPath = GetTemporaryPath(path);
        string backupPath = GetBackupPath(path);
        string backupTemporaryPath = $"{backupPath}.tmp";

        try
        {
            if (!WriteTextFile(temporaryPath, json))
            {
                return false;
            }

            if (!TryLoad(temporaryPath, out T _, out _, out _, out string temporaryError))
            {
                GD.PrintErr($"Refusing to replace {path}; temporary JSON validation failed: {temporaryError}");
                return false;
            }

            bool primaryExists = FileAccess.FileExists(path);
            if (primaryExists)
            {
                bool primaryIsValid = TryLoad(
                    path,
                    out T _,
                    out string primaryText,
                    out JsonFileStatus primaryFailure,
                    out string primaryError);

                if (primaryIsValid)
                {
                    if (FileAccess.FileExists(backupPath))
                    {
                        bool backupIsValid = TryLoad(
                            backupPath,
                            out T _,
                            out string backupText,
                            out JsonFileStatus backupFailure,
                            out string existingBackupError);
                        if (!backupIsValid)
                        {
                            if (backupFailure is JsonFileStatus.Empty or JsonFileStatus.InvalidJson)
                            {
                                if (!PreserveMalformedJson(backupPath, backupText, existingBackupError))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                GD.PrintErr($"Refusing to replace unreadable JSON backup at {backupPath}: {existingBackupError}");
                                return false;
                            }
                        }
                    }

                    if (!WriteTextFile(backupTemporaryPath, primaryText))
                    {
                        return false;
                    }

                    if (!TryLoad(backupTemporaryPath, out T _, out _, out _, out string backupError))
                    {
                        GD.PrintErr($"Refusing to replace {path}; backup validation failed: {backupError}");
                        return false;
                    }

                    Error backupRenameError = DirAccess.RenameAbsolute(backupTemporaryPath, backupPath);
                    if (backupRenameError != Error.Ok)
                    {
                        GD.PrintErr($"Refusing to replace {path}; could not update backup: {backupRenameError}");
                        return false;
                    }
                }
                else if (primaryFailure == JsonFileStatus.Empty || primaryFailure == JsonFileStatus.InvalidJson)
                {
                    // Preserve malformed data for diagnosis/recovery without promoting it to last-known-good.
                    if (!PreserveMalformedJson(path, primaryText, primaryError))
                    {
                        return false;
                    }
                }
                else
                {
                    GD.PrintErr($"Refusing to replace unreadable JSON at {path}: {primaryError}");
                    return false;
                }
            }

            Error renameError = DirAccess.RenameAbsolute(temporaryPath, path);
            if (renameError != Error.Ok)
            {
                GD.PrintErr($"Failed to replace JSON at {path}: {renameError}");
                return false;
            }

            return true;
        }
        finally
        {
            RemoveIfPresent(temporaryPath);
            RemoveIfPresent(backupTemporaryPath);
        }
    }

    private static bool PreserveMalformedJson(string sourcePath, string text, string error)
    {
        string corruptPath = GetAvailableCorruptPath(sourcePath);
        string corruptTemporaryPath = $"{corruptPath}.tmp";
        try
        {
            if (!WriteTextFile(corruptTemporaryPath, text))
            {
                GD.PrintErr($"Refusing to replace corrupt JSON at {sourcePath}; it could not be preserved.");
                return false;
            }

            Error corruptRenameError = DirAccess.RenameAbsolute(corruptTemporaryPath, corruptPath);
            if (corruptRenameError != Error.Ok)
            {
                GD.PrintErr($"Refusing to replace corrupt JSON at {sourcePath}; archival failed: {corruptRenameError}");
                return false;
            }

            GD.PushWarning($"Preserved corrupt JSON at {corruptPath}: {error}");
            return true;
        }
        finally
        {
            RemoveIfPresent(corruptTemporaryPath);
        }
    }

    private static string GetAvailableCorruptPath(string sourcePath)
    {
        string candidate = $"{sourcePath}.corrupt";
        int suffix = 1;
        while (FileAccess.FileExists(candidate))
        {
            candidate = $"{sourcePath}.corrupt.{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static bool TryLoad<T>(
        string path,
        out T value,
        out string text,
        out JsonFileStatus failure,
        out string error) where T : class
    {
        value = default!;
        text = string.Empty;
        failure = JsonFileStatus.Valid;
        error = string.Empty;

        if (!FileAccess.FileExists(path))
        {
            failure = JsonFileStatus.Missing;
            error = "file does not exist";
            return false;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            failure = JsonFileStatus.OpenFailed;
            error = $"open failed ({FileAccess.GetOpenError()})";
            return false;
        }

        text = file.GetAsText();
        JsonDocumentResult<T> parsed = JsonDocumentParser.Parse<T>(true, text);
        failure = parsed.Status;
        error = parsed.Error;
        value = parsed.Value!;
        return parsed.Status == JsonFileStatus.Valid;
    }

    private static bool EnsureDirectory(string path)
    {
        string directory = path.GetBaseDir();
        if (string.IsNullOrEmpty(directory) || DirAccess.DirExistsAbsolute(directory))
        {
            return true;
        }

        Error error = DirAccess.MakeDirRecursiveAbsolute(directory);
        if (error == Error.Ok)
        {
            return true;
        }

        GD.PrintErr($"Failed to create directory for {path}: {error}");
        return false;
    }

    private static bool WriteTextFile(string path, string text)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"Failed to open path for write: {path} ({FileAccess.GetOpenError()})");
            return false;
        }

        file.StoreString(text);
        file.Flush();
        Error error = file.GetError();
        if (error == Error.Ok)
        {
            return true;
        }

        GD.PrintErr($"Failed while writing JSON at {path}: {error}");
        return false;
    }

    private static bool IsUserPath(string path)
    {
        return path.StartsWith("user://", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTemporaryPath(string path)
    {
        return $"{path}.tmp";
    }

    private static string GetBackupPath(string path)
    {
        return $"{path}.bak";
    }

    private static void RemoveIfPresent(string path)
    {
        if (FileAccess.FileExists(path))
        {
            DirAccess.RemoveAbsolute(path);
        }
    }
}
