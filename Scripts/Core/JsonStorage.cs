using Godot;
using System;
using System.Text.Json;

public static class JsonStorage
{
    private enum LoadFailure
    {
        None,
        Missing,
        OpenFailed,
        Empty,
        InvalidJson
    }

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public static T LoadOrDefault<T>(string path, T fallback) where T : class
    {
        if (TryLoad(path, out T value, out _, out LoadFailure primaryFailure, out string primaryError))
        {
            return value;
        }

        if (IsUserPath(path))
        {
            string backupPath = GetBackupPath(path);
            if (TryLoad(backupPath, out value, out _, out LoadFailure backupFailure, out string backupError))
            {
                GD.PushWarning(
                    primaryFailure == LoadFailure.Missing
                        ? $"Recovered JSON from backup because the primary file is missing: {path}"
                        : $"Recovered JSON from backup after the primary failed to load: {path} ({primaryError})");
                return value;
            }

            if (primaryFailure != LoadFailure.Missing)
            {
                GD.PrintErr($"Failed to load JSON at {path}: {primaryError}");
            }

            if (backupFailure != LoadFailure.Missing)
            {
                GD.PrintErr($"Failed to load JSON backup at {backupPath}: {backupError}");
            }

            return fallback;
        }

        if (primaryFailure != LoadFailure.Missing)
        {
            GD.PrintErr($"Failed to load JSON at {path}: {primaryError}");
        }

        return fallback;
    }

    public static bool Save<T>(string path, T value)
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

    private static bool SaveUserFile<T>(string path, string json)
    {
        string temporaryPath = GetTemporaryPath(path);
        string backupPath = GetBackupPath(path);
        string backupTemporaryPath = $"{backupPath}.tmp";
        string corruptPath = $"{path}.corrupt";
        string corruptTemporaryPath = $"{corruptPath}.tmp";

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
                    out LoadFailure primaryFailure,
                    out string primaryError);

                if (primaryIsValid)
                {
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
                else if (primaryFailure == LoadFailure.Empty || primaryFailure == LoadFailure.InvalidJson)
                {
                    // Preserve malformed data for diagnosis/recovery without promoting it to last-known-good.
                    if (!WriteTextFile(corruptTemporaryPath, primaryText))
                    {
                        GD.PrintErr($"Refusing to replace corrupt JSON at {path}; it could not be preserved.");
                        return false;
                    }

                    Error corruptRenameError = DirAccess.RenameAbsolute(corruptTemporaryPath, corruptPath);
                    if (corruptRenameError != Error.Ok)
                    {
                        GD.PrintErr($"Refusing to replace corrupt JSON at {path}; archival failed: {corruptRenameError}");
                        return false;
                    }

                    GD.PushWarning($"Preserved corrupt JSON at {corruptPath}: {primaryError}");
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
            RemoveIfPresent(corruptTemporaryPath);
        }
    }

    private static bool TryLoad<T>(
        string path,
        out T value,
        out string text,
        out LoadFailure failure,
        out string error)
    {
        value = default!;
        text = string.Empty;
        failure = LoadFailure.None;
        error = string.Empty;

        if (!FileAccess.FileExists(path))
        {
            failure = LoadFailure.Missing;
            error = "file does not exist";
            return false;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            failure = LoadFailure.OpenFailed;
            error = $"open failed ({FileAccess.GetOpenError()})";
            return false;
        }

        text = file.GetAsText();
        if (string.IsNullOrWhiteSpace(text))
        {
            failure = LoadFailure.Empty;
            error = "file is empty";
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(text, ReadOptions)!;
            if (value is null)
            {
                failure = LoadFailure.InvalidJson;
                error = "JSON contains null instead of an object";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            failure = LoadFailure.InvalidJson;
            error = ex.Message;
            return false;
        }
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
