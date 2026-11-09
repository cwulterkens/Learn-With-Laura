public enum ProfileActivationChoice
{
    Normal,
    RestoreBackup,
    StartFresh
}

public enum ProfileActivationStatus
{
    Activated,
    Created,
    RecoveredBackup,
    BackupAvailable,
    Corrupt,
    NotFound,
    InvalidName,
    SaveFailed
}

public sealed class ProfileActivationResult
{
    public ProfileActivationStatus Status { get; init; }
    public string ProfileName { get; init; } = "";
    public string Error { get; init; } = "";
    public bool Succeeded => Status is ProfileActivationStatus.Activated
        or ProfileActivationStatus.Created
        or ProfileActivationStatus.RecoveredBackup;
}
