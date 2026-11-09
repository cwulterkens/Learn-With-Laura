using System;

public static class ProfileSelectionRules
{
    public static int ActivateGrade(ProfileData profile, GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(settings);

        int grade = Math.Clamp(profile.CurrentGrade, 1, 8);
        profile.CurrentGrade = grade;
        settings.LastSelectedGrade = grade;
        return grade;
    }
}
