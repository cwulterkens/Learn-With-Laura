using System;
using System.Collections.Generic;

public readonly record struct TownPoint(float X, float Y);
public readonly record struct TownRect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public bool Contains(float x, float y) => x >= X && x <= Right && y >= Y && y <= Bottom;

    public bool Intersects(TownRect other) =>
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;
}

public readonly record struct TownCircle(float X, float Y, float Radius);

public static class PrairieTownCollision
{
    public const float MapMinX = 34f;
    public const float MapMaxX = 1502f;
    public const float MapMinY = 112f;
    public const float MapMaxY = 972f;
    public const float SafeStartX = 170f;
    public const float SafeStartY = 900f;

    private const float PlayerHalfWidth = 14f;
    private const float PlayerFeetTopOffset = 22f;
    private const float PlayerFeetHeight = 14f;

    // Solid architecture and props. Door approaches remain outside these bounds.
    private static readonly IReadOnlyList<TownRect> SolidRects = new[]
    {
        // Buildings and enclosed garden.
        new TownRect(55, 662, 205, 174),       // Prairie house; its garden remains explorable
        new TownRect(397, 82, 166, 168),       // Schoolhouse
        new TownRect(898, 52, 134, 116),       // North town building
        new TownRect(600, 298, 158, 150),      // Post office
        new TownRect(914, 250, 296, 198),      // General store
        new TownRect(1168, 486, 212, 172),     // Bakery
        new TownRect(800, 484, 106, 96),       // Notice board
        new TownRect(485, 758, 64, 114),       // Exploration wagon

        // Schoolyard fence, with open space below the school entrance.
        new TownRect(270, 80, 8, 180),
        new TownRect(270, 77, 366, 8),
        new TownRect(628, 0, 8, 116),

        // Community-garden fence. Plant beds are decorative; the garden can be explored.
        new TownRect(1166, 48, 8, 204),
        new TownRect(1166, 44, 312, 8),
        new TownRect(1470, 48, 8, 204),
        new TownRect(1166, 246, 158, 8),
        new TownRect(1402, 246, 76, 8),

        // Homestead fence with a generous gate around x=145..245.
        new TownRect(40, 684, 8, 210),
        new TownRect(367, 694, 8, 200),
        new TownRect(40, 888, 105, 8),
        new TownRect(245, 888, 130, 8)
    };

    // Only trunks, rocks, and other grounded bases collide; tree canopies may overlap the player.
    private static readonly IReadOnlyList<TownCircle> SolidCircles = new[]
    {
        new TownCircle(335, 174, 18),
        new TownCircle(746, 207, 20),
        new TownCircle(247, 415, 19),
        new TownCircle(1407, 430, 19),
        new TownCircle(612, 571, 18),
        new TownCircle(1123, 602, 17),
        new TownCircle(1422, 607, 16),
        new TownCircle(950, 845, 19),
        new TownCircle(1170, 954, 18),
        new TownCircle(1460, 935, 16),
        new TownCircle(23, 731, 17),
        new TownCircle(95, 196, 17),
        new TownCircle(380, 516, 14),
        new TownCircle(824, 780, 15)
    };

    // Approximate the visible creek from the lower center to the right edge.
    private static readonly IReadOnlyList<TownPoint> RiverPolygon = new[]
    {
        new TownPoint(1536, 680),
        new TownPoint(1450, 720),
        new TownPoint(1360, 755),
        new TownPoint(1265, 798),
        new TownPoint(1170, 842),
        new TownPoint(1060, 885),
        new TownPoint(940, 938),
        new TownPoint(805, 1015),
        new TownPoint(790, 1024),
        new TownPoint(1080, 1024),
        new TownPoint(1120, 980),
        new TownPoint(1160, 930),
        new TownPoint(1230, 890),
        new TownPoint(1310, 850),
        new TownPoint(1390, 805),
        new TownPoint(1460, 765),
        new TownPoint(1536, 730)
    };

    // This corridor overrides the river only where the wooden bridge is visible.
    private static readonly TownRect BridgeWalkway = new(1288, 694, 100, 154);

    public static bool IsBlocked(float playerX, float playerY)
    {
        TownRect feet = PlayerFeet(playerX, playerY);
        foreach (TownRect solid in SolidRects)
        {
            if (feet.Intersects(solid))
            {
                return true;
            }
        }

        foreach (TownCircle circle in SolidCircles)
        {
            if (IntersectsCircle(feet, circle))
            {
                return true;
            }
        }

        bool entirelyOnBridge = BridgeWalkway.Contains(feet.X, feet.Y)
            && BridgeWalkway.Contains(feet.Right, feet.Bottom);
        return !entirelyOnBridge && FootprintTouchesPolygon(feet, RiverPolygon);
    }

    public static float ClampX(float x) => Math.Clamp(x, MapMinX, MapMaxX);
    public static float ClampY(float y) => Math.Clamp(y, MapMinY, MapMaxY);

    private static TownRect PlayerFeet(float x, float y) =>
        new(x - PlayerHalfWidth, y + PlayerFeetTopOffset, PlayerHalfWidth * 2f, PlayerFeetHeight);

    private static bool IntersectsCircle(TownRect rect, TownCircle circle)
    {
        float closestX = Math.Clamp(circle.X, rect.X, rect.Right);
        float closestY = Math.Clamp(circle.Y, rect.Y, rect.Bottom);
        float dx = circle.X - closestX;
        float dy = circle.Y - closestY;
        return (dx * dx) + (dy * dy) < circle.Radius * circle.Radius;
    }

    private static bool FootprintTouchesPolygon(TownRect feet, IReadOnlyList<TownPoint> polygon)
    {
        return PointInPolygon(feet.X, feet.Y, polygon)
            || PointInPolygon(feet.Right, feet.Y, polygon)
            || PointInPolygon(feet.X, feet.Bottom, polygon)
            || PointInPolygon(feet.Right, feet.Bottom, polygon)
            || PointInPolygon(feet.X + feet.Width / 2f, feet.Y + feet.Height / 2f, polygon);
    }

    private static bool PointInPolygon(float x, float y, IReadOnlyList<TownPoint> polygon)
    {
        bool inside = false;
        for (int current = 0, previous = polygon.Count - 1;
             current < polygon.Count;
             previous = current++)
        {
            TownPoint a = polygon[current];
            TownPoint b = polygon[previous];
            bool crosses = (a.Y > y) != (b.Y > y)
                && x < ((b.X - a.X) * (y - a.Y) / (b.Y - a.Y)) + a.X;
            if (crosses)
            {
                inside = !inside;
            }
        }
        return inside;
    }
}
