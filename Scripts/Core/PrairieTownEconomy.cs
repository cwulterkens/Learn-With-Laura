#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

public sealed record TownStoreItem(string Id, string Name, string Description, int Price);

public sealed record TownPurchaseResult(bool Succeeded, string Message, TownStoreItem? Item = null);

public static class PrairieTownEconomy
{
    public static readonly IReadOnlyList<TownStoreItem> StoreItems = new[]
    {
        new TownStoreItem("seed_packet", "Prairie Seed Packet", "Wildflowers for the window box.", 4),
        new TownStoreItem("blue_ribbon", "Blue Calico Ribbon", "A bright ribbon for town-day outfits.", 5),
        new TownStoreItem("storybook", "Little Storybook", "A new tale for the prairie-house shelf.", 7),
        new TownStoreItem("red_bonnet", "Red Sunbonnet", "A cheerful bonnet for long walks.", 9),
        new TownStoreItem("tin_lantern", "Tin Lantern", "A warm light for the prairie house.", 12),
        new TownStoreItem("toy_horse", "Carved Toy Horse", "A hand-carved treasure from the general store.", 15)
    };

    public static int CoinsForCorrectAnswer(bool firstTry) => firstTry ? 2 : 1;

    public static int AwardLearningReward(ProfileData profile, bool firstTry)
    {
        ArgumentNullException.ThrowIfNull(profile);
        int reward = CoinsForCorrectAnswer(firstTry);
        profile.PrairiePennies = Math.Max(0, profile.PrairiePennies) + reward;
        return reward;
    }

    public static int PrepareFirstTownVisit(ProfileData profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Normalize(profile);
        if (profile.TownRewardInitialized)
        {
            return 0;
        }

        int starterPurse = Math.Clamp(6 + (profile.PrairieStars / 2), 6, 24);
        profile.PrairiePennies += starterPurse;
        profile.TownRewardInitialized = true;
        return starterPurse;
    }

    public static int RecordDiscovery(ProfileData profile, string discoveryId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Normalize(profile);
        if (string.IsNullOrWhiteSpace(discoveryId)
            || profile.TownDiscoveries.Contains(discoveryId, StringComparer.OrdinalIgnoreCase))
        {
            return 0;
        }

        profile.TownDiscoveries.Add(discoveryId.Trim());
        profile.PrairiePennies += 1;
        return 1;
    }

    public static TownPurchaseResult TryPurchase(ProfileData profile, string itemId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Normalize(profile);
        TownStoreItem? item = StoreItems.FirstOrDefault(candidate =>
            candidate.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            return new TownPurchaseResult(false, "That item is not available today.");
        }
        if (profile.TownInventory.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
        {
            return new TownPurchaseResult(false, $"{item.Name} is already in your satchel.", item);
        }
        if (profile.PrairiePennies < item.Price)
        {
            int needed = item.Price - profile.PrairiePennies;
            return new TownPurchaseResult(false,
                $"You need {needed} more Prairie {(needed == 1 ? "Penny" : "Pennies")} for {item.Name}.", item);
        }

        profile.PrairiePennies -= item.Price;
        profile.TownInventory.Add(item.Id);
        return new TownPurchaseResult(true, $"You bought {item.Name}! It is now in your satchel.", item);
    }

    public static void Normalize(ProfileData profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.PrairiePennies = Math.Max(0, profile.PrairiePennies);
        profile.TownVisitCount = Math.Max(0, profile.TownVisitCount);
        profile.TownInventory ??= new List<string>();
        profile.TownDiscoveries ??= new List<string>();
        profile.TownInventory = NormalizeIds(profile.TownInventory);
        profile.TownDiscoveries = NormalizeIds(profile.TownDiscoveries);
        if (!float.IsFinite(profile.TownPositionX))
        {
            profile.TownPositionX = 170f;
        }
        if (!float.IsFinite(profile.TownPositionY))
        {
            profile.TownPositionY = 900f;
        }
    }

    public static string GetInventorySummary(ProfileData profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Normalize(profile);
        if (profile.TownInventory.Count == 0)
        {
            return "Your satchel is empty. Learn to earn pennies, then visit the general store.";
        }

        IEnumerable<string> names = profile.TownInventory.Select(id =>
            StoreItems.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name
            ?? id.Replace('_', ' '));
        return string.Join("\n", names.Select(name => $"• {name}"));
    }

    private static List<string> NormalizeIds(IEnumerable<string> ids)
    {
        return ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
