using System;
using System.Collections.Generic;

public enum CampChoice
{
    GatherSupplies,
    RestTogether
}

public enum LearningBreakChoice
{
    Stretch,
    Breathing
}

public sealed class JourneyChangeResult
{
    public string Summary { get; init; } = "";
    public int DayChange { get; init; }
    public int MilesChange { get; init; }
    public int FoodChange { get; init; }
    public int WoodChange { get; init; }
    public int HealthChange { get; init; }
    public int MoraleChange { get; init; }
}

public static class JourneyResources
{
    public const int FoodCapacity = 30;
    public const int WoodCapacity = 20;
    public const int MaxHealth = 10;
    public const int MaxMorale = 10;
    public const int LowFood = 3;
    public const int LowWood = 1;
    public const int LowHealth = 2;
    public const int LowMorale = 2;

    public static void Normalize(JourneyProgress journey)
    {
        ArgumentNullException.ThrowIfNull(journey);

        journey.Day = Math.Max(1, journey.Day);
        journey.Miles = Math.Max(0, journey.Miles);
        // Older saves could accumulate supplies without a cap. Preserve that earned surplus,
        // while new rewards stop at the comfortable carrying capacity.
        journey.Food = Math.Max(0, journey.Food);
        journey.Wood = Math.Max(0, journey.Wood);
        journey.Health = Math.Clamp(journey.Health, 0, MaxHealth);
        journey.Morale = Math.Clamp(journey.Morale, 0, MaxMorale);
        journey.LessonsAnswered = Math.Max(0, journey.LessonsAnswered);
        journey.FamilyActivitiesCompleted = Math.Max(0, journey.FamilyActivitiesCompleted);
        if (string.IsNullOrWhiteSpace(journey.CurrentRegion))
        {
            journey.CurrentRegion = "big_woods";
        }
    }

    public static JourneyChangeResult AdvanceTravelStop(
        JourneyProgress journey,
        int baseMiles,
        int foodUsed,
        int woodUsed)
    {
        ArgumentNullException.ThrowIfNull(journey);
        Normalize(journey);
        JourneySnapshot before = new(journey);
        var notes = new List<string>();

        bool neededSafetyNet = journey.Food == 0
            || journey.Wood == 0
            || journey.Health == 0
            || journey.Morale == 0;
        ApplySafetyNet(journey, notes);

        bool needsGentlePace = neededSafetyNet
            || journey.Food <= LowFood
            || journey.Wood <= LowWood
            || journey.Health <= LowHealth
            || journey.Morale <= LowMorale;
        int miles = Math.Clamp(baseMiles, 1, 30);
        if (needsGentlePace)
        {
            miles = Math.Max(4, miles - 5);
            notes.Add("The family chooses a gentler pace while supplies or energy are low.");
        }

        journey.Day += 1;
        journey.Miles += miles;
        journey.Food = Math.Max(0, journey.Food - Math.Clamp(foodUsed, 0, 5));
        journey.Wood = Math.Max(0, journey.Wood - Math.Clamp(woodUsed, 0, 3));

        if (journey.Food == 0)
        {
            journey.Health = Math.Max(0, journey.Health - 1);
            notes.Add("The food basket is empty, so the next camp check-in should gather supplies.");
        }
        if (journey.Wood == 0)
        {
            journey.Morale = Math.Max(0, journey.Morale - 1);
            notes.Add("The wood pile is empty, so camp takes extra work until it is replenished.");
        }

        Normalize(journey);
        return BuildResult(before, journey, string.Join(" ", notes));
    }

    public static JourneyChangeResult ApplyCampChoice(
        JourneyProgress journey,
        CampChoice choice)
    {
        ArgumentNullException.ThrowIfNull(journey);
        Normalize(journey);
        JourneySnapshot before = new(journey);
        string summary;

        if (choice == CampChoice.RestTogether)
        {
            if (journey.Food == 0)
            {
                journey.Food = 2;
            }

            journey.Day += 1;
            journey.Food = Math.Max(0, journey.Food - 1);
            journey.Health = Math.Min(MaxHealth, journey.Health + 3);
            journey.Morale = Math.Min(MaxMorale, journey.Morale + 3);
            summary = "The family takes a calm rest day, shares a meal, and feels ready to learn again.";
        }
        else
        {
            (int food, int wood, int morale, string regionSummary) = GetGatherRewards(journey.CurrentRegion);
            journey.Day += 1;
            journey.Food = AddSupply(journey.Food, food, FoodCapacity);
            journey.Wood = AddSupply(journey.Wood, wood, WoodCapacity);
            journey.Morale = Math.Min(MaxMorale, journey.Morale + morale);
            summary = regionSummary;
        }

        return BuildResult(before, journey, summary);
    }

    public static JourneyChangeResult ApplyLearningBreak(
        JourneyProgress journey,
        LearningBreakChoice choice)
    {
        ArgumentNullException.ThrowIfNull(journey);
        Normalize(journey);
        JourneySnapshot before = new(journey);

        if (choice == LearningBreakChoice.Stretch)
        {
            journey.Health = Math.Min(MaxHealth, journey.Health + 2);
            journey.Morale = Math.Min(MaxMorale, journey.Morale + 1);
            return BuildResult(before, journey, "Movement restored health and lifted morale.");
        }

        journey.Health = Math.Min(MaxHealth, journey.Health + 1);
        journey.Morale = Math.Min(MaxMorale, journey.Morale + 2);
        return BuildResult(before, journey, "Calm breathing restored morale and a little health.");
    }

    public static JourneyChangeResult ApplyFamilyActivityReward(
        JourneyProgress journey,
        string activity)
    {
        ArgumentNullException.ThrowIfNull(journey);
        Normalize(journey);
        JourneySnapshot before = new(journey);
        string summary;

        switch (activity)
        {
            case "tracking":
                journey.Food = AddSupply(journey.Food, 4, FoodCapacity);
                summary = "Careful observation helps organize useful trail food.";
                break;
            case "baking":
                journey.Food = AddSupply(journey.Food, 3, FoodCapacity);
                journey.Morale = Math.Min(MaxMorale, journey.Morale + 1);
                summary = "Bread fills the food basket and lifts everyone's spirits.";
                break;
            case "shared_chores":
                journey.Wood = AddSupply(journey.Wood, 4, WoodCapacity);
                summary = "Shared chores replenish the wood pile and keep camp ready.";
                break;
            default:
                journey.Morale = Math.Min(MaxMorale, journey.Morale + 2);
                summary = "The fiddle tune lifts everyone's morale.";
                break;
        }

        return BuildResult(before, journey, summary);
    }

    public static string GetNeedsMessage(JourneyProgress journey)
    {
        ArgumentNullException.ThrowIfNull(journey);
        Normalize(journey);

        var needs = new List<string>();
        if (journey.Food <= LowFood)
        {
            needs.Add("food is low");
        }
        if (journey.Wood <= LowWood)
        {
            needs.Add("wood is low");
        }
        if (journey.Health <= LowHealth)
        {
            needs.Add("the family needs rest");
        }
        if (journey.Morale <= LowMorale)
        {
            needs.Add("spirits need a lift");
        }

        return needs.Count == 0
            ? "Supplies and energy are steady. Choose a chapter or prepare camp for the trail ahead."
            : $"Camp check-in: {string.Join(", ", needs)}. Gathering or resting will always keep the journey moving.";
    }

    private static void ApplySafetyNet(JourneyProgress journey, List<string> notes)
    {
        if (journey.Food == 0)
        {
            journey.Food = 4;
            notes.Add("A shared pantry provides 4 food so the journey can continue.");
        }
        if (journey.Wood == 0)
        {
            journey.Wood = 3;
            notes.Add("The family gathers safe fallen branches for 3 wood.");
        }
        if (journey.Health == 0)
        {
            journey.Health = 3;
            notes.Add("A supported rest restores health before travel.");
        }
        if (journey.Morale == 0)
        {
            journey.Morale = 3;
            notes.Add("Stories and encouragement restore morale before travel.");
        }
    }

    private static (int Food, int Wood, int Morale, string Summary) GetGatherRewards(string region)
    {
        return region switch
        {
            "big_woods" => (3, 5, 0, "In the Big Woods, the family organizes the pantry and gathers fallen wood."),
            "prairie" => (5, 3, 0, "On the prairie, the family tends supplies and gathers safe camp fuel."),
            "plum_creek" => (4, 4, 0, "Near Plum Creek, shared chores replenish both the food basket and wood pile."),
            "schoolhouse" => (3, 4, 1, "Schoolhouse neighbors share chores, supplies, and encouragement."),
            _ => (5, 2, 1, "Near town, careful planning replenishes supplies and lifts morale.")
        };
    }

    private static JourneyChangeResult BuildResult(
        JourneySnapshot before,
        JourneyProgress after,
        string summary)
    {
        var changes = new List<string>();
        AddChange(changes, "day", after.Day - before.Day);
        AddChange(changes, "miles", after.Miles - before.Miles);
        AddChange(changes, "food", after.Food - before.Food);
        AddChange(changes, "wood", after.Wood - before.Wood);
        AddChange(changes, "health", after.Health - before.Health);
        AddChange(changes, "morale", after.Morale - before.Morale);
        string changeText = changes.Count == 0 ? "No resource change." : string.Join(" · ", changes);

        return new JourneyChangeResult
        {
            Summary = $"{summary} {changeText}".Trim(),
            DayChange = after.Day - before.Day,
            MilesChange = after.Miles - before.Miles,
            FoodChange = after.Food - before.Food,
            WoodChange = after.Wood - before.Wood,
            HealthChange = after.Health - before.Health,
            MoraleChange = after.Morale - before.Morale
        };
    }

    private static void AddChange(List<string> changes, string label, int amount)
    {
        if (amount != 0)
        {
            changes.Add($"{label} {(amount > 0 ? "+" : "")}{amount}");
        }
    }

    private static int AddSupply(int current, int amount, int capacity)
    {
        return current >= capacity
            ? current
            : Math.Min(capacity, current + Math.Max(0, amount));
    }

    private readonly struct JourneySnapshot
    {
        public JourneySnapshot(JourneyProgress journey)
        {
            Day = journey.Day;
            Miles = journey.Miles;
            Food = journey.Food;
            Wood = journey.Wood;
            Health = journey.Health;
            Morale = journey.Morale;
        }

        public int Day { get; }
        public int Miles { get; }
        public int Food { get; }
        public int Wood { get; }
        public int Health { get; }
        public int Morale { get; }
    }
}
