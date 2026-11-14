#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

public sealed record HuntingTripResult(
    int FoodGained,
    int MoraleGained,
    int PenniesGained,
    bool NewJournalEntry,
    string Summary);

public static class HuntingWithPaProgress
{
    public const int PennyReward = 3;
    public const int FoodReward = 4;

    public static HuntingTripResult CompleteTrip(
        ProfileData profile,
        string animalId,
        string journalEntry,
        bool responsiblyTookGame)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Normalize(profile);
        if (string.IsNullOrWhiteSpace(animalId) || string.IsNullOrWhiteSpace(journalEntry))
        {
            throw new ArgumentException("A field-journal animal and entry are required.");
        }

        int foodBefore = profile.Journey.Food;
        int moraleBefore = profile.Journey.Morale;
        profile.HuntingTripsCompleted += 1;
        profile.PrairiePennies += PennyReward;
        profile.GameScore += 75;

        if (responsiblyTookGame)
        {
            profile.Journey.Food = Math.Min(
                JourneyResources.FoodCapacity,
                profile.Journey.Food + FoodReward);
            profile.Journey.Morale = Math.Min(
                JourneyResources.MaxMorale,
                profile.Journey.Morale + 1);
        }
        else
        {
            profile.Journey.Morale = Math.Min(
                JourneyResources.MaxMorale,
                profile.Journey.Morale + 2);
        }

        string cleanId = animalId.Trim();
        bool newEntry = !profile.FieldJournalEntries.Any(entry =>
            entry.StartsWith($"{cleanId}|", StringComparison.OrdinalIgnoreCase));
        if (newEntry)
        {
            profile.FieldJournalEntries.Add($"{cleanId}|{journalEntry.Trim()}");
        }

        int foodGained = profile.Journey.Food - foodBefore;
        int moraleGained = profile.Journey.Morale - moraleBefore;
        string choiceSummary = responsiblyTookGame
            ? $"The careful hunt added {foodGained} food to the family supplies."
            : "Choosing to let the animal pass showed patience and good judgment.";
        string journalSummary = newEntry
            ? " A new observation was added to the field journal."
            : " The field journal already held this animal's observation.";

        return new HuntingTripResult(
            foodGained,
            moraleGained,
            PennyReward,
            newEntry,
            $"{choiceSummary} +{PennyReward} Prairie Pennies{journalSummary}");
    }

    public static void Normalize(ProfileData profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.HuntingTripsCompleted = Math.Max(0, profile.HuntingTripsCompleted);
        profile.FieldJournalEntries ??= new List<string>();
        profile.FieldJournalEntries = profile.FieldJournalEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry) && entry.Contains('|'))
            .Select(entry => entry.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string BuildJournal(ProfileData profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Normalize(profile);
        if (profile.FieldJournalEntries.Count == 0)
        {
            return "No observations yet. Complete a responsible field trip with Pa to begin the journal.";
        }

        return string.Join("\n\n", profile.FieldJournalEntries.Select(entry =>
        {
            string[] parts = entry.Split('|', 2);
            return $"• {parts[1]}";
        }));
    }
}
