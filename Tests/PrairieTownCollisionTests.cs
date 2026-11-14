using System;
using System.Collections.Generic;
using Xunit;

public class PrairieTownCollisionTests
{
    [Theory]
    [InlineData(475, 150)]   // Schoolhouse interior
    [InlineData(1050, 330)]  // General store interior
    [InlineData(335, 150)]   // Tree trunk
    [InlineData(1168, 150)]  // Community-garden fence
    [InlineData(1100, 900)]  // Creek water
    [InlineData(517, 800)]   // Exploration wagon
    public void SolidFeaturesAreBlocked(float x, float y)
    {
        Assert.True(PrairieTownCollision.IsBlocked(x, y));
    }

    [Theory]
    [InlineData(480, 270)]   // School door approach
    [InlineData(830, 400)]   // Passage between post office and store
    [InlineData(1070, 560)]  // Central town plaza
    [InlineData(1360, 270)]  // Community-garden gate
    [InlineData(1360, 170)]  // Inside the community garden
    [InlineData(195, 860)]   // Homestead gate
    [InlineData(315, 790)]   // Inside the homestead garden
    [InlineData(420, 820)]   // Passage beside the wagon
    [InlineData(1338, 760)]  // Bridge walkway
    [InlineData(1450, 820)]  // South bank path beyond the creek
    public void IntendedPathsAndDoorApproachesStayWalkable(float x, float y)
    {
        Assert.False(PrairieTownCollision.IsBlocked(x, y));
    }

    [Fact]
    public void HubDestinationsRemainConnectedFromTheHomestead()
    {
        (float X, float Y) start = (PrairieTownCollision.SafeStartX, PrairieTownCollision.SafeStartY);
        var destinations = new[]
        {
            (480f, 270f),   // School
            (1050f, 470f),  // General store entrance
            (420f, 820f),   // Wagon
            (1250f, 680f),  // Bakery entrance
            (1338f, 760f),  // Bridge
            (1360f, 170f),  // Community garden interior
            (315f, 790f)    // Homestead garden interior
        };

        foreach ((float x, float y) in destinations)
        {
            Assert.True(CanReach(start, (x, y)), $"Could not reach ({x}, {y}) from the homestead.");
        }
    }

    private static bool CanReach((float X, float Y) start, (float X, float Y) target)
    {
        const int step = 20;
        var queue = new Queue<(int X, int Y)>();
        var visited = new HashSet<(int X, int Y)>();
        (int X, int Y) startCell = ((int)(start.X / step), (int)(start.Y / step));
        queue.Enqueue(startCell);
        visited.Add(startCell);

        (int X, int Y)[] directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        while (queue.Count > 0)
        {
            (int cellX, int cellY) = queue.Dequeue();
            float x = cellX * step;
            float y = cellY * step;
            if (Math.Abs(x - target.X) <= step && Math.Abs(y - target.Y) <= step)
            {
                return true;
            }

            foreach ((int dx, int dy) in directions)
            {
                int nextX = cellX + dx;
                int nextY = cellY + dy;
                float worldX = nextX * step;
                float worldY = nextY * step;
                if (worldX < PrairieTownCollision.MapMinX
                    || worldX > PrairieTownCollision.MapMaxX
                    || worldY < PrairieTownCollision.MapMinY
                    || worldY > PrairieTownCollision.MapMaxY
                    || PrairieTownCollision.IsBlocked(worldX, worldY)
                    || !visited.Add((nextX, nextY)))
                {
                    continue;
                }
                queue.Enqueue((nextX, nextY));
            }
        }
        return false;
    }
}
