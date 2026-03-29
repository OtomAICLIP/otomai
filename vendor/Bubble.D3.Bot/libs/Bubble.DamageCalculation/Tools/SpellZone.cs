using System.Drawing;
using System.Globalization;

namespace Bubble.DamageCalculation.Tools;

public class SpellZone
{
    public const int DefaultRadius = 1;

    public const int DefaultMinRadius = 0;

    public const int DefaultDegression = 10;

    public const int DefaultMaxDegressionTicks = 4;

    public const int GlobalRadius = 63;

    public const int MaxRadiusDegression = 50;

    public char Shape { get; private set; }

    public int Radius { get; private set; }

    public int MinRadius { get; private set; }

    public int MaxDegressionTicks { get; private set; }
    public int Direction { get; private set; } = -1;

    public Func<int, int, int, bool>? IsCellInZone { get; private set; }

    public Func<int, int, IList<int>>? GetCells { get; private set; }

    public int Degression { get; private set; }

    public SpellZone()
    {
        Shape              = 'P';
        MaxDegressionTicks = DefaultMaxDegressionTicks;
        Degression         = DefaultDegression;
        MinRadius          = DefaultMinRadius;
        Radius             = DefaultRadius;
    }

    public static SpellZone FromRawZone(string rawZone)
    {
        if (string.IsNullOrWhiteSpace(rawZone))
        {
            rawZone = "P";
        }

        var spellZone = new SpellZone
        {
            Shape = rawZone[0],
        };

        var parameters = rawZone[1..].Split(',')
                                     .Where(x => x.Length > 0)
                                     .ToArray();

        switch (spellZone.Shape)
        {
            case ';':
            {
                var cells = parameters.Where(x => x.Length > 0)
                                      .Select(x => int.Parse(x, CultureInfo.InvariantCulture))
                                      .ToArray();

                spellZone.GetCells     = (_, _) => cells;
                spellZone.IsCellInZone = (cellId, _, _) => cells.Contains(cellId);
                return spellZone;
            }
            case 'l':
                (parameters[0], parameters[1]) = (parameters[1], parameters[0]);
                break;
        }

        if (parameters.Length > 0)
        {
            spellZone.Radius = int.Parse(parameters[0], CultureInfo.InvariantCulture);
        }

        var stopAtTarget = false;

        FillFromParameters(spellZone, parameters, ref stopAtTarget);
        FillZoneFunctions(spellZone, stopAtTarget);

        return spellZone;
    }

    private static void FillFromParameters(SpellZone spellZone, IReadOnlyList<string> parameters, ref bool stopAtTarget)
    {
        if (HasMinSize(spellZone.Shape))
        {
            if (parameters.Count > 1)
            {
                spellZone.MinRadius = int.Parse(parameters[1], CultureInfo.InvariantCulture);
            }

            if (parameters.Count > 2)
            {
                spellZone.Degression = int.Parse(parameters[2], CultureInfo.InvariantCulture);
            }
        }
        else
        {
            if (parameters.Count > 1)
            {
                spellZone.Degression = int.Parse(parameters[1], CultureInfo.InvariantCulture);
            }

            if (parameters.Count > 2)
            {
                spellZone.MaxDegressionTicks = int.Parse(parameters[2], CultureInfo.InvariantCulture);
            }
        }

        if (parameters.Count > 3)
        {
            spellZone.MaxDegressionTicks = int.Parse(parameters[3], CultureInfo.InvariantCulture);
            spellZone.Direction          = int.Parse(parameters[3], CultureInfo.InvariantCulture);
        }

        if (parameters.Count > 4)
        {
            stopAtTarget = int.Parse(parameters[4], CultureInfo.InvariantCulture).Equals(1);
        }
    }

    private static void FillZoneFunctions(SpellZone spellZone, bool stopAtTarget)
    {
        switch (spellZone.Shape)
        {
            case ' ':
                spellZone.GetCells     = FillEmptyCells;
                spellZone.IsCellInZone = IsCellInEmptyZone;
                break;
            case '#':
                spellZone.GetCells = (x, y) =>
                    FillCrossCells(spellZone, MapDirection.MapCardinalDirections, true, x, y);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInCrossZone(spellZone, MapDirection.MapCardinalDirections, true, x, y, z);
                break;
            case '*':
                spellZone.GetCells = (x, y) => FillCrossCells(spellZone, MapDirection.MapDirections, false, x, y);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInCrossZone(spellZone, MapDirection.MapDirections, false, x, y, z);
                break;
            case '+':
                spellZone.GetCells = (x, y) =>
                    FillCrossCells(spellZone, MapDirection.MapCardinalDirections, false, x, y);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInCrossZone(spellZone, MapDirection.MapCardinalDirections, false, x, y, z);
                break;
            case '-':
                spellZone.GetCells     = (x, y) => FillPerpLineCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInPerpLineZone(spellZone, x, y, z);
                break;
            case '/':
                spellZone.GetCells = (x, y) => FillLineCells(spellZone, stopAtTarget, false, x, y, -1);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInLineZone(spellZone, stopAtTarget, false, x, y, z);
                break;
            case 'A':
            case 'a':
                spellZone.GetCells     = FillWholeMap;
                spellZone.IsCellInZone = IsCellInWholeMapZone;
                return;
            case 'B':
                spellZone.GetCells     = (x, y) => FillBoomerang(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInBoomerangZone(spellZone, x, y, z);
                break;
            case 'C':
                spellZone.GetCells     = (x, y) => FillCircleCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInCircleZone(spellZone, x, y, z);
                break;
            case 'D':
                spellZone.GetCells     = (x, y) => FillCheckerboard(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInCheckerboardZone(spellZone, x, y, z);
                break;
            case 'F':
                spellZone.GetCells     = (x, y) => FillForkCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInForkZone(spellZone, x, y, z);
                break;
            case 'G':
                spellZone.GetCells     = (x, y) => FillSquareCells(spellZone, false, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInSquareZone(spellZone, false, x, y);
                break;
            case 'I':
                spellZone.MinRadius    = spellZone.Radius;
                spellZone.Radius       = GlobalRadius;
                spellZone.GetCells     = (x, y) => FillCircleCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInCircleZone(spellZone, x, y, z);
                break;
            case 'L':
                spellZone.GetCells = (x, y) => FillLineCells(spellZone, stopAtTarget, false, x, y, spellZone.Direction);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInLineZone(spellZone, stopAtTarget, false, x, y, z, spellZone.Direction);
                break;
            case 'O':
                spellZone.MinRadius    = spellZone.Radius;
                spellZone.GetCells     = (x, y) => FillCircleCells(spellZone, x, y);
                spellZone.IsCellInZone = (cellX, cellY, cellZ) => IsCellInCircleZone(spellZone, cellX, cellY, cellZ);
                break;
            case 'P':
                spellZone.Radius       = 0;
                spellZone.GetCells     = FillPointCells;
                spellZone.IsCellInZone = IsCellInPointZone;
                break;
            case 'Q':
                spellZone.GetCells = (x, y) =>
                    FillCrossCells(spellZone, MapDirection.MapOrthogonalDirections, true, x, y);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInCrossZone(spellZone, MapDirection.MapOrthogonalDirections, true, x, y, z);
                break;
            case 'R':
                spellZone.GetCells     = (x, y) => FillRectangleCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInRectangleZone(spellZone, x, y, z);
                break;
            case 'T':
                spellZone.GetCells     = (x, y) => FillPerpLineCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInPerpLineZone(spellZone, x, y, z);
                break;
            case 'U':
                spellZone.GetCells     = (x, y) => FillHalfCircle(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInHalfCircleZone(spellZone, x, y, z);
                break;
            case 'V':
                spellZone.GetCells     = (x, y) => FillConeCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInConeZone(spellZone, x, y, z);
                break;
            case 'W':
                spellZone.GetCells     = (x, y) => FillSquareCells(spellZone, true, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInSquareZone(spellZone, true, x, y);
                break;
            case 'X':
                spellZone.GetCells = (x, y) =>
                    FillCrossCells(spellZone, MapDirection.MapOrthogonalDirections, false, x, y);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInCrossZone(spellZone, MapDirection.MapOrthogonalDirections, false, x, y, z);
                break;
            case 'Z':
                spellZone.GetCells     = (x, y) => FillReversedTrueCircleCells(spellZone, x, y);
                spellZone.IsCellInZone = (x, y, z) => IsCellInReversedTrueCircleZone(spellZone, x, y, z);
                break;
            case 'l':
                spellZone.GetCells = (x, y) => FillLineCells(spellZone, stopAtTarget, true, x, y, -1);
                spellZone.IsCellInZone = (x, y, z) =>
                    IsCellInLineZone(spellZone, stopAtTarget, true, x, y, z);
                break;
            case ';':
                break;
        }
    }

    /// <summary>
    /// Fills an array with a single cell if the provided cell ID is valid.
    /// </summary>
    /// <param name="cellId">The cell ID to check.</param>
    /// <param name="targetCellId">The target cell ID (not used in this function, but kept for consistency).</param>
    /// <returns>A list containing the cell ID if it's valid, otherwise an empty list.</returns>
    public static int[] FillPointCells(int cellId, int targetCellId)
    {
        return !MapTools.IsValidCellId(cellId) ? Array.Empty<int>() : new[] { cellId, };
    }

    /// <summary>
    /// Checks if a cell is in a point zone.
    /// </summary>
    /// <param name="cellId">The cell ID to check.</param>
    /// <param name="casterCellId">The caster's cell ID.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <returns>True if the cell is in the point zone, otherwise false.</returns>
    public static bool IsCellInPointZone(int cellId, int casterCellId, int targetCellId)
    {
        return cellId == casterCellId;
    }

    /// <summary>
    /// Returns an empty list, as no cells are filled in an empty zone.
    /// </summary>
    /// <param name="casterCellId">The caster's cell ID (not used in this function, but kept for consistency).</param>
    /// <param name="targetCellId">The target cell ID (not used in this function, but kept for consistency).</param>
    /// <returns>An empty list.</returns>
    public static int[] FillEmptyCells(int casterCellId, int targetCellId)
    {
        return Array.Empty<int>();
    }

    /// <summary>
    /// Checks if a cell is in an empty zone.
    /// </summary>
    /// <param name="cellId">The cell ID to check (not used in this function, but kept for consistency).</param>
    /// <param name="casterCellId">The caster's cell ID (not used in this function, but kept for consistency).</param>
    /// <param name="targetCellId">The target cell ID (not used in this function, but kept for consistency).</param>
    /// <returns>Always returns false, as no cells are in an empty zone.</returns>
    public static bool IsCellInEmptyZone(int cellId, int casterCellId, int targetCellId)
    {
        return false;
    }

    /// <summary>
    /// Fills a list of cell IDs that form a circle based on the given spell zone, center, and target cell ID.
    /// </summary>
    /// <param name="spellZone">The spell zone defining the circle's radius and minimum radius.</param>
    /// <param name="center">The center of the circle (not used in this function).</param>
    /// <param name="targetCellId">The target cell ID used to calculate the circle's coordinates.</param>
    /// <returns>A list of integers representing cell IDs within the circle.</returns>
    private static List<int> FillCircleCells(SpellZone spellZone, int center, int targetCellId)
    {
        var outputCellIds = new List<int>();

        var targetCoord = MapTools.GetCellCoordById(targetCellId);

        if (targetCoord == null)
        {
            return outputCellIds;
        }

        var minRadius = -spellZone.Radius;
        var maxRadius = spellZone.Radius + 1;

        for (var i = minRadius; i <= maxRadius; i++)
        {
            var minRadius2 = -spellZone.Radius;
            var maxRadius2 = spellZone.Radius + 1;

            for (var j = minRadius2; j <= maxRadius2; j++)
            {
                if (MapTools.IsValidCoord(targetCoord.Value.X + i, targetCoord.Value.Y + j) &&
                    Math.Abs(i) + Math.Abs(j) <= spellZone.Radius &&
                    Math.Abs(i) + Math.Abs(j) >= spellZone.MinRadius)
                {
                    outputCellIds.Add(
                        MapTools.GetCellIdByCoord(targetCoord.Value.X + i, targetCoord.Value.Y + j));
                }
            }
        }

        return outputCellIds;
    }

    /// <summary>
    /// Determines whether a given cell is within a circle-shaped zone.
    /// </summary>
    /// <param name="spellZone">The SpellZone object defining the zone's shape and size.</param>
    /// <param name="casterCellId">The caster's cell ID.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <param name="cellToCheck">Not used but kept for consistency.</param>
    /// <returns>True if the cell is within the circle-shaped zone, false otherwise.</returns>
    public static bool IsCellInCircleZone(SpellZone spellZone, int casterCellId, int targetCellId, int cellToCheck)
    {
        var distance = MapTools.GetDistance(casterCellId, targetCellId);

        return distance <= spellZone.Radius && distance >= spellZone.MinRadius;
    }

    /// <summary>
    /// Fills the cells in a checkerboard pattern within the specified zone.
    /// </summary>
    /// <param name="spellZone">The SpellZone object defining the zone's shape and size.</param>
    /// <param name="casterCellId">The caster's cell ID.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <returns>A list of cell IDs in the checkerboard pattern within the specified zone.</returns>
    public static IList<int> FillCheckerboard(SpellZone spellZone, int casterCellId, int targetCellId)
    {
        var filledCells = new List<int>();
        var cellCoord   = MapTools.GetCellCoordById(casterCellId);

        if (cellCoord == null)
        {
            return filledCells;
        }

        var casterCoord  = cellCoord.Value;
        var isEvenRadius = spellZone.Radius % 2 == 0;
        var minRadius    = -spellZone.Radius;
        var maxRadius    = spellZone.Radius + 1;

        for (var x = minRadius; x < maxRadius; x++)
        {
            for (var y = minRadius; y < maxRadius; y++)
            {
                var newX = casterCoord.X + x;
                var newY = casterCoord.Y + y;
                var absX = Math.Abs(x);
                var absY = Math.Abs(y);

                if (MapTools.IsValidCoord(newX, newY) &&
                    absX + absY <= spellZone.Radius &&
                    absX + absY >= spellZone.MinRadius &&
                    (isEvenRadius && (x + y % 2) % 2 == 0 || !isEvenRadius && (x + 1 + y % 2) % 2 == 0))
                {
                    filledCells.Add(MapTools.GetCellIdByCoord(newX, newY));
                }
            }
        }

        return filledCells;
    }

    /// <summary>
    /// Determines if a cell is within the checkerboard pattern of a specified zone.
    /// </summary>
    /// <param name="spellZone">The SpellZone object defining the zone's shape and size.</param>
    /// <param name="casterCellId">The caster's cell ID.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <param name="cellToCheck">Not used but kept for consistency.</param>
    /// <returns>True if the cell is within the checkerboard pattern, otherwise false.</returns>
    public static bool IsCellInCheckerboardZone(SpellZone spellZone, int casterCellId, int targetCellId,
        int cellToCheck)
    {
        var distance     = MapTools.GetDistance(casterCellId, targetCellId);
        var isEvenRadius = spellZone.Radius % 2 == 0;
        var xCell        = MapTools.GetCellIdXCoord(targetCellId);
        var yCell        = MapTools.GetCellIdYCoord(targetCellId);
        var xEvenOffset  = (xCell + 1) / 2;
        var yEvenOffset  = (yCell + 1) / 2;
        var xOddOffset   = xCell - xEvenOffset;
        var yOddOffset   = yCell - yEvenOffset;

        if (distance >= spellZone.MinRadius)
        {
            if (!(isEvenRadius && (xEvenOffset + yOddOffset % 2) % 2 == 0))
            {
                if (!isEvenRadius)
                {
                    return (xOddOffset + 1 + yOddOffset % 2) % 2 == 0;
                }

                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Fills the cells in a line zone.
    /// </summary>
    /// <param name="spellZone">The SpellZone object defining the zone's shape and size.</param>
    /// <param name="stopAtTarget">Determines if the line should stop at the target cell.</param>
    /// <param name="targetIsCenter">Determines if the caster cell is the center of the zone.</param>
    /// <param name="casterCellId">The caster's cell ID.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <param name="direction"></param>
    /// <returns>A list of cell IDs within the line zone.</returns>
    public static IList<int> FillLineCells(SpellZone spellZone, bool stopAtTarget, bool targetIsCenter,
        int casterCellId, int targetCellId, int direction)
    {
        var cellIds       = new List<int>();
        var currentCellId = targetIsCenter ? targetCellId : casterCellId;
        var maxDistance   = targetIsCenter ? spellZone.Radius + spellZone.MinRadius - 1 : spellZone.Radius;

        var casterCoord = MapTools.GetCellCoordById(casterCellId);
        var targetCoord = MapTools.GetCellCoordById(targetCellId);

        if (!casterCoord.HasValue || !targetCoord.HasValue)
        {
            return cellIds;
        }

        if (direction == -1)
        {
            direction = MapTools.GetLookDirection8ExactByCoord(targetCoord.Value, casterCoord.Value);
        }

        if (targetIsCenter && stopAtTarget)
        {
            var distance = MapTools.GetDistance(targetCellId, casterCellId);
            if (distance < maxDistance)
            {
                maxDistance = distance;
            }
        }

        for (var i = 0; i < spellZone.MinRadius; i++)
        {
            currentCellId = MapTools.GetNextCellByDirection(currentCellId, direction);
        }

        for (var i = spellZone.MinRadius; i <= maxDistance; i++)
        {
            if (MapTools.IsValidCellId(currentCellId))
            {
                cellIds.Add(currentCellId);
            }

            currentCellId = MapTools.GetNextCellByDirection(currentCellId, direction);
        }

        return cellIds;
    }

    /// <summary>
    /// Determines if a cell is in a line zone.
    /// </summary>
    /// <param name="spellZone">The spell zone.</param>
    /// <param name="stopAtTarget">If true, use the caster cell, otherwise use the target cell.</param>
    /// <param name="fromCaster">If true, minimize the target distance.</param>
    /// <param name="casterCellId">The caster's cell ID.</param>
    /// <param name="targetCellId">The target's cell ID.</param>
    /// <param name="cellToCheckId">The cell ID to check.</param>
    /// <returns>True if the cell is in the line zone, otherwise false.</returns>
    public static bool IsCellInLineZone(SpellZone spellZone, bool stopAtTarget, bool fromCaster, int casterCellId,
        int targetCellId, int cellToCheckId, int direction = -1)
    {
        int distanceToCaster;

        if (cellToCheckId == casterCellId)
        {
            return false;
        }

        var cellToCheckCoord = MapTools.GetCellCoordById(cellToCheckId);
        var targetCoord      = MapTools.GetCellCoordById(targetCellId);
        var casterCoord      = MapTools.GetCellCoordById(casterCellId);

        if (!cellToCheckCoord.HasValue || !targetCoord.HasValue || !casterCoord.HasValue)
        {
            return false;
        }

        var lookDirection = MapTools.GetLookDirection8ExactByCoord(cellToCheckCoord.Value, targetCoord.Value);

        var maxRadius = spellZone.Radius;

        if (fromCaster)
        {
            if (direction == -1)
            {
                direction = MapTools.GetLookDirection8ExactByCoord(cellToCheckCoord.Value, casterCoord.Value);
            }

            distanceToCaster = MapTools.GetDistance(cellToCheckId, casterCellId);

            if (stopAtTarget)
            {
                var distanceToTarget = MapTools.GetDistance(cellToCheckId, targetCellId);
                if (distanceToTarget < maxRadius)
                {
                    maxRadius = distanceToTarget;
                }
            }
        }
        else
        {
            direction        = MapTools.GetLookDirection8ExactByCoord(targetCoord.Value, casterCoord.Value);
            distanceToCaster = MapTools.GetDistance(targetCellId, casterCellId);
        }

        if (MapDirection.IsCardinal(direction) && distanceToCaster > 1)
        {
            distanceToCaster >>= 1;
        }

        if ((lookDirection == direction || distanceToCaster == 0) && distanceToCaster >= spellZone.MinRadius)
        {
            return distanceToCaster <= maxRadius;
        }

        return false;
    }

    /// <summary>
    /// Fills the cells in a cross-shaped zone.
    /// </summary>
    /// <param name="zone">The spell zone defining the shape of the cross.</param>
    /// <param name="directions">An array of directions (0 to 7) for each line of the cross.</param>
    /// <param name="ignoreCenter">Indicates whether the center cell should be ignored.</param>
    /// <param name="centerCellId">The cell id of the center of the cross-shaped zone.</param>
    /// <param name="targetCellId">The cell id of the target.</param>
    /// <returns>A list of cell ids in the cross-shaped zone.</returns>
    public static IList<int> FillCrossCells(SpellZone zone, int[] directions, bool ignoreCenter, int centerCellId,
        int targetCellId)
    {
        var cells     = new List<int>();
        var minRadius = zone.MinRadius;

        if (zone.MinRadius == 0)
        {
            minRadius = 1;
            if (!ignoreCenter)
            {
                cells.Add(centerCellId);
            }
        }

        var cellsDirection = new List<int>(directions.Length);

        for (var i = 0; i < directions.Length; i++)
        {
            cellsDirection.Add(centerCellId);
        }

        for (var i = 1; i <= zone.Radius; i++)
        {
            for (var j = 0; j < directions.Length; j++)
            {
                cellsDirection[j] = MapTools.GetNextCellByDirection(cellsDirection[j], directions[j]);
                if (i >= minRadius && MapTools.IsValidCellId(cellsDirection[j]))
                {
                    cells.Add(cellsDirection[j]);
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Determines if a cell is in a cross-shaped zone.
    /// </summary>
    /// <param name="zone">The spell zone defining the shape of the cross.</param>
    /// <param name="directions">An array of directions (0 to 7) for each line of the cross.</param>
    /// <param name="ignoreCenter">Indicates whether the center cell should be ignored.</param>
    /// <param name="centerCellId">The cell id of the center of the cross-shaped zone.</param>
    /// <param name="targetCellId">The cell id of the target.</param>
    /// <param name="unusedArg">An unused argument.</param>
    /// <returns>True if the target cell is in the cross-shaped zone, false otherwise.</returns>
    public static bool IsCellInCrossZone(SpellZone zone, IList<int> directions, bool ignoreCenter, int centerCellId,
        int targetCellId, int unusedArg)
    {
        var targetCoord = MapTools.GetCellCoordById(targetCellId);
        var centerCoord = MapTools.GetCellCoordById(centerCellId);

        if (!centerCoord.HasValue || !targetCoord.HasValue)
        {
            return false;
        }

        var lookDirection = MapTools.GetLookDirection8ExactByCoord(targetCoord.Value, centerCoord.Value);
        var distance      = MapTools.GetDistance(centerCellId, targetCellId);

        if (MapDirection.IsCardinal(lookDirection) && distance > 1)
        {
            distance >>= 1;
        }

        if ((directions.IndexOf(lookDirection) != -1 || distance == 0) &&
            distance >= zone.MinRadius + (ignoreCenter && zone.MinRadius == 0 ? 1 : 0))
        {
            return distance <= zone.Radius;
        }

        return false;
    }

    /// <summary>
    /// Fills the perpendicular line cells with respect to the given spell zone, center cell, and target cell.
    /// </summary>
    /// <param name="zone">The spell zone defining the shape of the line.</param>
    /// <param name="centerCellId">The cell id of the center of the perpendicular line.</param>
    /// <param name="targetCellId">The cell id of the target.</param>
    /// <returns>A list of cell ids forming the perpendicular line.</returns>
    public static IList<int> FillPerpLineCells(SpellZone zone, int centerCellId, int targetCellId)
    {
        var lineCells = new List<int>();

        var centerCoord = MapTools.GetCellCoordById(centerCellId);
        var targetCoord = MapTools.GetCellCoordById(targetCellId);

        if (!centerCoord.HasValue || !targetCoord.HasValue)
        {
            return lineCells;
        }

        var lookDirection = MapTools.GetLookDirection8ExactByCoord(targetCoord.Value, centerCoord.Value);
        var direction1    = (lookDirection + 2) % 8;
        var direction2    = (lookDirection - 2 + 8) % 8;

        var minRadius = zone.MinRadius;
        if (zone.MinRadius == 0)
        {
            minRadius = 1;
            if (MapTools.IsValidCellId(centerCellId))
            {
                lineCells.Add(centerCellId);
            }
        }

        var currentCell1 = centerCellId;
        var currentCell2 = centerCellId;
        int distance;

        for (distance = minRadius; distance <= zone.Radius; distance++)
        {
            currentCell1 = MapTools.GetNextCellByDirection(currentCell1, direction1);
            currentCell2 = MapTools.GetNextCellByDirection(currentCell2, direction2);

            if (MapTools.IsValidCellId(currentCell1))
            {
                lineCells.Add(currentCell1);
            }

            if (MapTools.IsValidCellId(currentCell2))
            {
                lineCells.Add(currentCell2);
            }
        }

        return lineCells;
    }

    /// <summary>
    /// Determines if a cell is in the perpendicular line zone of a spell.
    /// </summary>
    /// <param name="spellZone">The spell zone.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <param name="casterCellId">The caster cell ID.</param>
    /// <param name="impactCellId">The impact cell ID.</param>
    /// <returns>Returns true if the cell is in the perpendicular line zone, false otherwise.</returns>
    public static bool IsCellInPerpLineZone(SpellZone spellZone, int targetCellId, int casterCellId, int impactCellId)
    {
        var impactCoord = MapTools.GetCellCoordById(impactCellId);
        var casterCoord = MapTools.GetCellCoordById(casterCellId);
        var targetCoord = MapTools.GetCellCoordById(targetCellId);

        if (impactCoord == null || casterCoord == null || targetCoord == null)
        {
            return false;
        }

        var lookDirection8Exact = MapTools.GetLookDirection8ExactByCoord(impactCoord.Value, casterCoord.Value);
        var lookDirectionPlus2  = (lookDirection8Exact + 2) % 8;
        var lookDirectionMinus2 = (lookDirection8Exact - 2 + 8) % 8;

        var lookDirectionTargetCaster = MapTools.GetLookDirection8ExactByCoord(casterCoord.Value, targetCoord.Value);
        var distanceCasterTarget      = MapTools.GetDistance(casterCellId, targetCellId);

        if (MapDirection.IsCardinal(lookDirectionTargetCaster) && distanceCasterTarget > 1)
        {
            distanceCasterTarget >>= 1;
        }

        if ((lookDirectionTargetCaster == lookDirectionPlus2 || lookDirectionTargetCaster == lookDirectionMinus2 ||
             distanceCasterTarget == 0) && distanceCasterTarget >= spellZone.MinRadius)
        {
            return distanceCasterTarget <= spellZone.Radius;
        }

        return false;
    }

    /// <summary>
    /// Fills the cone-shaped area with cells for the given SpellZone.
    /// </summary>
    /// <param name="spellZone">The SpellZone instance.</param>
    /// <param name="originCellId">The origin cell id.</param>
    /// <param name="targetCellId">The target cell id.</param>
    /// <returns>A list of cell ids within the cone-shaped area.</returns>
    public static IList<int> FillConeCells(SpellZone spellZone, int originCellId, int targetCellId)
    {
        var coneCells   = new List<int>();
        var originCoord = MapTools.GetCellCoordById(originCellId);
        var targetCoord = MapTools.GetCellCoordById(targetCellId);

        if (!originCoord.HasValue || !targetCoord.HasValue)
        {
            return coneCells;
        }

        var lookDirection = MapTools.GetLookDirection8ExactByCoord(targetCoord.Value, originCoord.Value);
        var directionA    = (lookDirection + 2) % 8;
        var directionB    = (lookDirection - 2 + 8) % 8;

        var currentCellId = originCellId;
        for (var i = 0; i <= spellZone.Radius; i++)
        {
            coneCells.Add(currentCellId);
            var cellA = currentCellId;
            var cellB = currentCellId;

            for (var j = 0; j < i; j++)
            {
                cellA = MapTools.GetNextCellByDirection(cellA, directionA);
                cellB = MapTools.GetNextCellByDirection(cellB, directionB);

                if (MapTools.IsValidCellId(cellA))
                {
                    coneCells.Add(cellA);
                }

                if (MapTools.IsValidCellId(cellB))
                {
                    coneCells.Add(cellB);
                }
            }

            currentCellId = MapTools.GetNextCellByDirection(currentCellId, lookDirection);
        }

        return coneCells;
    }

    /// <summary>
    /// Determines if a cell is within the cone-shaped area of the given SpellZone.
    /// </summary>
    /// <param name="spellZone">The SpellZone object.</param>
    /// <param name="targetCellId">The ID of the target cell to check.</param>
    /// <param name="casterCellId">The ID of the caster cell.</param>
    /// <param name="cellId">The ID of the cell.</param>
    /// <returns>Returns true if the target cell is within the cone-shaped area, false otherwise.</returns>
    public static bool IsCellInConeZone(SpellZone spellZone, int targetCellId, int casterCellId, int cellId)
    {
        var lookDirection = MapTools.GetLookDirection4(cellId, casterCellId);
        var casterCoord   = MapTools.GetCellCoordById(casterCellId);
        var targetCoord   = MapTools.GetCellCoordById(targetCellId);

        if (!casterCoord.HasValue || !targetCoord.HasValue)
        {
            return false;
        }

        var deltaX = targetCoord.Value.X - casterCoord.Value.X;
        var deltaY = targetCoord.Value.Y - casterCoord.Value.Y;

        return lookDirection switch
               {
                   1 => deltaX >= 0 && deltaX <= spellZone.Radius && Math.Abs(deltaY) <= deltaX,
                   3 => deltaY <= 0 && deltaY >= -spellZone.Radius && Math.Abs(deltaX) <= -deltaY,
                   5 => deltaX <= 0 && deltaX >= -spellZone.Radius && Math.Abs(deltaY) <= -deltaX,
                   7 => deltaY >= 0 && deltaY <= spellZone.Radius && Math.Abs(deltaX) <= deltaY,
                   _ => false,
               };
    }

    /// <summary>
    /// Fill fork cells for a given spell zone, starting cell, and end cell.
    /// </summary>
    /// <param name="spellZone">The spell zone to calculate fork cells for.</param>
    /// <param name="startCellId">The starting cell ID.</param>
    /// <param name="endCellId">The ending cell ID.</param>
    /// <returns>An IList of cell IDs that make up the fork cells.</returns>
    public static IList<int> FillForkCells(SpellZone spellZone, int startCellId, int endCellId)
    {
        var forkCells  = new List<int>();
        var startPoint = MapTools.GetCellCoordById(startCellId);
        if (!startPoint.HasValue)
        {
            return forkCells;
        }

        var startCoord          = startPoint.Value;
        var direction           = GetForkDirection(startCellId, endCellId);
        var isDirectionDiagonal = direction is 5 or 1;
        var sign                = direction is 5 or 3 ? -1 : 1;

        var maxRadius = spellZone.Radius + 1;
        forkCells.Add(startCellId);

        for (var currentRadius = 1; currentRadius < maxRadius; currentRadius++)
        {
            AddForkCellsInDirection(forkCells, startCoord, currentRadius, sign, isDirectionDiagonal);
        }

        return forkCells;
    }

    private static int GetForkDirection(int startCellId, int endCellId)
    {
        var startPoint = MapTools.GetCellCoordById(startCellId);
        var endPoint   = MapTools.GetCellCoordById(endCellId);

        if (!startPoint.HasValue || !endPoint.HasValue)
        {
            return 0;
        }

        return MapTools.GetLookDirection8ExactByCoord(startPoint.Value, endPoint.Value);
    }

    private static void AddForkCellsInDirection(List<int> forkCells, Point startCoord, int currentRadius, int sign,
        bool isDirectionDiagonal)
    {
        for (var i = -1; i <= 1; i++)
        {
            var x = startCoord.X + (isDirectionDiagonal ? currentRadius * sign : i * currentRadius);
            var y = startCoord.Y + (isDirectionDiagonal ? i * currentRadius : currentRadius * sign);

            if (MapTools.IsValidCellId(MapTools.GetCellIdByCoord(x, y)))
            {
                forkCells.Add(MapTools.GetCellIdByCoord(x, y));
            }
        }
    }

    /// <summary>
    /// Determines if a cell is in the fork zone of a given spell zone.
    /// </summary>
    /// <param name="spellZone">The spell zone to check.</param>
    /// <param name="cellId">The cell ID to check if it's in the fork zone.</param>
    /// <param name="startCellId">The starting cell ID of the fork zone.</param>
    /// <param name="endCellId">The ending cell ID of the fork zone.</param>
    /// <returns>True if the cell is in the fork zone, false otherwise.</returns>
    public static bool IsCellInForkZone(SpellZone spellZone, int cellId, int startCellId, int endCellId)
    {
        var startPoint = MapTools.GetCellCoordById(startCellId);
        var endPoint   = MapTools.GetCellCoordById(endCellId);
        var cellPoint  = MapTools.GetCellCoordById(cellId);

        if (!startPoint.HasValue || !endPoint.HasValue || !cellPoint.HasValue)
        {
            return false;
        }

        var direction = MapTools.GetLookDirection4(endCellId, startCellId);
        var sign      = direction is 5 or 3 ? -1 : 1;
        var maxRadius = spellZone.Radius + 1;

        int deltaX, deltaY;

        if (direction is 5 or 1)
        {
            deltaX = (cellPoint.Value.X - startPoint.Value.X) * sign;
            deltaY = cellPoint.Value.Y - startPoint.Value.Y;
        }
        else
        {
            deltaX = (cellPoint.Value.Y - startPoint.Value.Y) * sign;
            deltaY = cellPoint.Value.X - startPoint.Value.X;
        }

        if (deltaX >= 0 && deltaX <= maxRadius)
        {
            if (deltaY != deltaX && deltaY != 0)
            {
                return deltaY == -deltaX;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Fills the cells of a square-shaped spell zone.
    /// </summary>
    /// <param name="spellZone">The spell zone to fill.</param>
    /// <param name="ignoreDiagonal">If true, the diagonal cells will not be filled.</param>
    /// <param name="startCellId">The starting cell ID of the square zone.</param>
    /// <returns>A list of cell IDs in the square zone.</returns>
    public static IList<int> FillSquareCells(SpellZone spellZone, bool ignoreDiagonal, int startCellId)
    {
        var cells      = new List<int>();
        var startPoint = MapTools.GetCellCoordById(startCellId);
        if (!startPoint.HasValue)
        {
            return cells;
        }

        for (var xOffset = -spellZone.Radius; xOffset <= spellZone.Radius; xOffset++)
        {
            for (var yOffset = -spellZone.Radius; yOffset <= spellZone.Radius; yOffset++)
            {
                if (MapTools.IsValidCoord(startPoint.Value.X + xOffset, startPoint.Value.Y + yOffset) &&
                    (!ignoreDiagonal || Math.Abs(xOffset) != Math.Abs(yOffset)))
                {
                    cells.Add(MapTools.GetCellIdByCoord(startPoint.Value.X + xOffset,
                        startPoint.Value.Y + yOffset));
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Determines if a cell is in a square-shaped spell zone.
    /// </summary>
    /// <param name="spellZone">The spell zone to check.</param>
    /// <param name="ignoreDiagonal">If true, diagonal cells will not be considered.</param>
    /// <param name="cellId">The cell ID to check if it's in the square zone.</param>
    /// <param name="startCellId">The starting cell ID of the square zone.</param>
    /// <returns>True if the cell is in the square zone, false otherwise.</returns>
    public static bool IsCellInSquareZone(SpellZone spellZone, bool ignoreDiagonal, int cellId, int startCellId)
    {
        var startPoint = MapTools.GetCellCoordById(startCellId);
        var cellPoint  = MapTools.GetCellCoordById(cellId);
        if (!startPoint.HasValue || !cellPoint.HasValue)
        {
            return false;
        }

        var absX = Math.Abs(cellPoint.Value.X - startPoint.Value.X);
        var absY = Math.Abs(cellPoint.Value.Y - startPoint.Value.Y);

        if (!ignoreDiagonal || absX != absY)
        {
            if (absX <= spellZone.Radius && absY <= spellZone.Radius && absX >= spellZone.MinRadius)
            {
                return absY >= spellZone.MinRadius;
            }
        }

        return false;
    }

    /// <summary>
    /// Fills the cells of a rectangular-shaped spell zone.
    /// </summary>
    /// <param name="spellZone">The spell zone to fill.</param>
    /// <param name="startCellId">The starting cell ID of the rectangular zone.</param>
    /// <param name="endCellId">The ending cell ID of the rectangular zone.</param>
    /// <returns>A list of cell IDs in the rectangular zone.</returns>
    public static IList<int> FillRectangleCells(SpellZone spellZone, int startCellId, int endCellId)
    {
        if (spellZone.Radius < 1)
        {
            spellZone.Radius = 1;
        }

        if (spellZone.MinRadius < 1)
        {
            spellZone.MinRadius = 1;
        }

        var cells = new List<int>();

        var startPoint = MapTools.GetCellCoordById(startCellId);
        var endPoint   = MapTools.GetCellCoordById(endCellId);

        if (!startPoint.HasValue || !endPoint.HasValue)
        {
            return cells;
        }

        var lookDirection8 = MapTools.GetLookDirection8ExactByCoord(endPoint.Value, startPoint.Value);
        var rowSign        = lookDirection8 is 5 or 3 ? -1 : 1;
        var isDiagonal     = lookDirection8 is 7 or 3;

        var zoneWidth  = 1 + spellZone.Radius * 2;
        var zoneHeight = 1 + spellZone.MinRadius;

        for (var row = 0; row < zoneHeight; row++)
        {
            for (var column = 0; column < zoneWidth; column++)
            {
                int xCoord, yCoord;

                if (isDiagonal)
                {
                    xCoord = startPoint.Value.X + column - zoneWidth / 2;
                    yCoord = startPoint.Value.Y + row * rowSign;
                }
                else
                {
                    xCoord = startPoint.Value.X + row * rowSign;
                    yCoord = startPoint.Value.Y + column - zoneWidth / 2;
                }

                if (MapTools.IsValidCoord(xCoord, yCoord))
                {
                    cells.Add(MapTools.GetCellIdByCoord(xCoord, yCoord));
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Checks if a cell is in a rectangle zone defined by a spell zone, center cell, and end cell.
    /// </summary>
    /// <param name="spellZone">The spell zone defining the rectangle zone.</param>
    /// <param name="cellId1">The ID of the center cell.</param>
    /// <param name="cellId2">The ID of the cell to check.</param>
    /// <param name="cellId3">The ID of the end cell.</param>
    /// <returns>True if the cell is in the rectangle zone, otherwise false.</returns>
    public static bool IsCellInRectangleZone(SpellZone spellZone, int cellId1, int cellId2, int cellId3)
    {
        if (spellZone.Radius < 1)
        {
            spellZone.Radius = 1;
        }

        if (spellZone.MinRadius < 1)
        {
            spellZone.MinRadius = 1;
        }

        var coord2 = MapTools.GetCellCoordById(cellId2);
        var coord1 = MapTools.GetCellCoordById(cellId1);
        var coord3 = MapTools.GetCellCoordById(cellId3);

        if (!coord1.HasValue || !coord2.HasValue || !coord3.HasValue)
        {
            return false;
        }

        var direction8Exact = MapTools.GetLookDirection8ExactByCoord(coord3.Value, coord2.Value);

        var sign = direction8Exact is 5 or 3 ? -1 : 1;

        var radius    = 1 + spellZone.Radius * 2;
        var minRadius = 1 + spellZone.MinRadius;

        int diff1;
        int diff2;

        if (direction8Exact is 7 or 3)
        {
            diff1 = Math.Abs(coord1.Value.X - coord2.Value.X);
            diff2 = (coord1.Value.Y - coord2.Value.Y) * sign;
        }
        else
        {
            diff1 = Math.Abs(coord1.Value.Y - coord2.Value.Y);
            diff2 = (coord1.Value.X - coord2.Value.X) * sign;
        }

        if (diff1 <= (int)Math.Floor(radius / 2f))
        {
            if (diff2 >= 0)
            {
                return diff2 < minRadius;
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// Fills the half circle zone of a spell.
    /// </summary>
    /// <param name="spellZone">The spell zone.</param>
    /// <param name="centerCellId">The center cell ID.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <returns>A list of cell IDs in the half circle zone.</returns>
    public static IList<int> FillHalfCircle(SpellZone spellZone, int centerCellId, int targetCellId)
    {
        var cellIds     = new List<int>();
        var centerCoord = MapTools.GetCellCoordById(centerCellId);
        var targetCoord = MapTools.GetCellCoordById(targetCellId);

        if (!centerCoord.HasValue || !targetCoord.HasValue)
        {
            return cellIds;
        }

        var direction8  = MapTools.GetLookDirection8ExactByCoord(targetCoord.Value, centerCoord.Value);
        var direction20 = (direction8 + 3) % 8;
        var direction21 = (direction8 - 3 + 8) % 8;

        var minRadius = spellZone.MinRadius;
        if (spellZone.MinRadius == 0)
        {
            minRadius = 1;
            cellIds.Add(centerCellId);
        }

        var currentCell23 = centerCellId;
        var currentCell24 = centerCellId;

        for (var radius = minRadius; radius <= spellZone.Radius; radius++)
        {
            currentCell23 = MapTools.GetNextCellByDirection(currentCell23, direction20);
            currentCell24 = MapTools.GetNextCellByDirection(currentCell24, direction21);

            if (MapTools.IsValidCellId(currentCell23))
            {
                cellIds.Add(currentCell23);
            }

            if (MapTools.IsValidCellId(currentCell24))
            {
                cellIds.Add(currentCell24);
            }
        }

        return cellIds;
    }

    /// <summary>
    /// Determines if a given cell is in a half circle zone.
    /// </summary>
    /// <param name="zone">The half circle SpellZone.</param>
    /// <param name="targetCellId">The target cell ID.</param>
    /// <param name="originCellId">The origin cell ID.</param>
    /// <param name="directionCellId">The direction cell ID.</param>
    /// <returns>True if the cell is in the half circle zone, false otherwise.</returns>
    public static bool IsCellInHalfCircleZone(SpellZone zone, int targetCellId, int originCellId, int directionCellId)
    {
        var directionCoord = MapTools.GetCellCoordById(directionCellId);
        var originCoord    = MapTools.GetCellCoordById(originCellId);
        var targetCoord    = MapTools.GetCellCoordById(targetCellId);

        if (directionCoord == null || originCoord == null || targetCoord == null)
        {
            return false;
        }

        var direction8Exact = MapTools.GetLookDirection8ExactByCoord(directionCoord.Value, originCoord.Value);
        var directionMin    = (direction8Exact - 3 + 8) % 8;
        var directionMax    = (direction8Exact + 3) % 8;

        var lookDirection8Exact = MapTools.GetLookDirection8ExactByCoord(originCoord.Value, targetCoord.Value);
        var distance            = MapTools.GetDistance(originCellId, targetCellId);

        if (MapDirection.IsCardinal(lookDirection8Exact) && distance > 1)
        {
            distance >>= 1;
        }

        var isInHalfCircle = (directionMin == lookDirection8Exact ||
                              directionMax == lookDirection8Exact || distance == 0)
                             && distance <= zone.Radius;


        return isInHalfCircle && distance >= zone.MinRadius;
    }

    /// <summary>
    /// Fills the boomerang shape for a given spell zone.
    /// </summary>
    /// <param name="spellZone">The spell zone.</param>
    /// <param name="cellId1">The first cell ID.</param>
    /// <param name="cellId2">The second cell ID.</param>
    /// <returns>A list of cell IDs in the boomerang shape.</returns>
    public static IList<int> FillBoomerang(SpellZone spellZone, int cellId1, int cellId2)
    {
        var cellIds = new List<int>();

        var coord1 = MapTools.GetCellCoordById(cellId1);
        var coord2 = MapTools.GetCellCoordById(cellId2);

        if (!coord1.HasValue || !coord2.HasValue)
        {
            return cellIds;
        }

        var lookDirection = MapTools.GetLookDirection8ExactByCoord(coord2.Value, coord1.Value);
        int[] directions =
        {
            (lookDirection + 2) % 8,
            (lookDirection + 3) % 8,
            (lookDirection - 2 + 8) % 8,
            (lookDirection - 3 + 8) % 8,
        };

        var minRadius = spellZone.MinRadius;
        if (spellZone.MinRadius == 0)
        {
            minRadius = 1;
            cellIds.Add(cellId1);
        }

        var currentCell1  = cellId1;
        var currentCell2  = cellId1;
        var currentRadius = minRadius;
        var maxRadius     = spellZone.Radius;

        while (currentRadius < maxRadius)
        {
            currentCell1 = MapTools.GetNextCellByDirection(currentCell1, directions[0]);
            currentCell2 = MapTools.GetNextCellByDirection(currentCell2, directions[2]);

            if (MapTools.IsValidCellId(currentCell1))
            {
                cellIds.Add(currentCell1);
            }

            if (MapTools.IsValidCellId(currentCell2))
            {
                cellIds.Add(currentCell2);
            }

            currentRadius++;
        }

        if (spellZone.Radius == 0)
        {
            return cellIds;
        }

        currentCell1 = MapTools.GetNextCellByDirection(currentCell1, directions[1]);
        currentCell2 = MapTools.GetNextCellByDirection(currentCell2, directions[3]);

        if (MapTools.IsValidCellId(currentCell1))
        {
            cellIds.Add(currentCell1);
        }

        if (MapTools.IsValidCellId(currentCell2))
        {
            cellIds.Add(currentCell2);
        }

        return cellIds;
    }

    public static bool IsCellInBoomerangZone(SpellZone spellZone, int cellId1, int cellId2, int cellId3)
    {
        var coord1 = MapTools.GetCellCoordById(cellId1);
        var coord2 = MapTools.GetCellCoordById(cellId2);
        var coord3 = MapTools.GetCellCoordById(cellId3);

        if (!coord1.HasValue || !coord2.HasValue || !coord3.HasValue)
        {
            return false;
        }

        var lookDirection = MapTools.GetLookDirection8ExactByCoord(coord3.Value, coord2.Value);
        var direction1    = (lookDirection + 2) % 8;
        var direction2    = (lookDirection - 2 + 8) % 8;

        var lookDirection2 = MapTools.GetLookDirection8ExactByCoord(coord2.Value, coord1.Value);
        var distance       = MapTools.GetDistance(cellId3, cellId1);

        if (MapDirection.IsCardinal(lookDirection2) && distance > 1)
        {
            distance >>= 1;
        }

        if ((lookDirection2 == direction1 || lookDirection2 == direction2) && distance >= spellZone.MinRadius)
        {
            return distance == spellZone.Radius;
        }

        var nextCellId = MapTools.GetNextCellByDirection(cellId1, lookDirection);
        var nextCell   = MapTools.GetCellCoordById(nextCellId);

        if (nextCell == null)
        {
            return false;
        }

        var lookDirection3 = MapTools.GetLookDirection8ExactByCoord(coord2.Value, nextCell.Value);
        var distance2      = MapTools.GetDistance(cellId2, nextCellId);

        if (MapDirection.IsCardinal(lookDirection3) && distance2 > 1)
        {
            distance2 >>= 1;
        }

        if ((lookDirection3 == direction1 || lookDirection3 == direction2) && distance2 >= spellZone.MinRadius)
        {
            return distance2 == spellZone.Radius;
        }

        return false;
    }

    /// <summary>
    /// Fills a list of cell IDs that are in the reversed true circle zone.
    /// </summary>
    /// <param name="spellZone">The spell zone definition.</param>
    /// <param name="centerCellId">The center cell ID of the circle.</param>
    /// <param name="unused">Unused parameter.</param>
    /// <returns>An IList of int representing the cell IDs in the reversed true circle zone.</returns>
    public static IList<int> FillReversedTrueCircleCells(SpellZone spellZone, int centerCellId, int unused)
    {
        var    cellIds     = new List<int>();
        var    centerCoord = MapTools.GetCellCoordById(centerCellId);
        double radius      = spellZone.Radius;

        if (!centerCoord.HasValue)
        {
            return cellIds;
        }

        for (var currentCellId = 0; currentCellId < MapTools.MapCountCell; currentCellId++)
        {
            var currentCoord = MapTools.GetCellCoordById(currentCellId);

            if (!currentCoord.HasValue)
            {
                continue;
            }

            var delta = new Point(currentCoord.Value.X - centerCoord.Value.X,
                currentCoord.Value.Y - centerCoord.Value.Y);

            if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) >= radius)
            {
                cellIds.Add(currentCellId);
            }
        }

        return cellIds;
    }

    /// <summary>
    /// Determines if a cell is in the reversed true circle zone.
    /// </summary>
    /// <param name="spellZone">The spell zone definition.</param>
    /// <param name="cellId">The cell ID to check.</param>
    /// <param name="centerCellId">The center cell ID of the circle.</param>
    /// <param name="unused">Unused parameter.</param>
    /// <returns>True if the cell is in the reversed true circle zone, otherwise false.</returns>
    public static bool IsCellInReversedTrueCircleZone(SpellZone spellZone, int cellId, int centerCellId, int unused)
    {
        var centerCoord = MapTools.GetCellCoordById(centerCellId);
        var cellCoord   = MapTools.GetCellCoordById(cellId);

        if (!centerCoord.HasValue || !cellCoord.HasValue)
        {
            return false;
        }

        var delta = new Point(cellCoord.Value.X - centerCoord.Value.X, cellCoord.Value.Y - centerCoord.Value.Y);

        return Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) >= spellZone.Radius;
    }

    public static IList<int> FillWholeMap(int cellId1, int cellId2)
    {
        return MapTools.EveryCellId;
    }

    public static bool IsCellInWholeMapZone(int cellId1, int cellId2, int cellId3)
    {
        return true;
    }


    /// <summary>
    /// Check if the spell zone can have a minimum size
    /// This is extracted from client code at mapTools/SpellZone.as
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static bool HasMinSize(char shape)
    {
        return shape is '#' or '+' or 'C' or 'Q' or 'R' or 'X' or 'l';
    }

    /// <summary>
    /// Calculates the Area of Effect (AOE) malus for the SpellZone.
    /// </summary>
    /// <param name="sourceCell">The ID of the source cell.</param>
    /// <param name="casterCellId">The ID of the caster cell.</param>
    /// <param name="affectedCellId">The ID of the affected cell.</param>
    /// <returns>The calculated area of effect malus as an integer.</returns>
    public int GetAoeMalus(int sourceCell, int casterCellId, int affectedCellId)
    {
        int distance;

        if (Radius > MaxRadiusDegression)
        {
            return 0;
        }

        switch (Shape)
        {
            case ';':
            case 'A':
            case 'I':
            case 'a':
                distance = 0;
                break;
            case 'G':
            case 'R':
            case 'W':
                var cellCoord1 = MapTools.GetCellCoordById(sourceCell)!.Value;
                var cellCoord2 = MapTools.GetCellCoordById(affectedCellId)!.Value;
                distance = Math.Max(Math.Abs(cellCoord1.X - cellCoord2.X),
                    Math.Abs(cellCoord1.Y - cellCoord2.Y));
                break;
            case '#':
            case '+':
            case '-':
            case '/':
            case 'U':
                distance = MapTools.GetDistance(sourceCell, affectedCellId) >> 1;
                break;
            case 'F':
            case 'V':
                var targetedCoord = MapTools.GetCellCoordById(sourceCell)!.Value;
                var casterCoord   = MapTools.GetCellCoordById(casterCellId)!.Value;
                var affectedCoord = MapTools.GetCellCoordById(affectedCellId)!.Value;

                var dir = MapTools.GetLookDirection8ExactByCoord(casterCoord.X, casterCoord.Y,                    targetedCoord.X, targetedCoord.Y);

                switch (dir)
                {
                    case 0:
                    case 4:
                        distance = Math.Abs(Math.Abs(targetedCoord.X - targetedCoord.Y) + Math.Abs(affectedCoord.X - affectedCoord.Y));
                        break;
                    case 1:
                    case 5:
                        distance = Math.Abs(targetedCoord.X - affectedCoord.X);
                        break;
                    case 2:
                    case 6:
                        distance = Math.Abs(Math.Abs(targetedCoord.X - targetedCoord.Y) -
                                            Math.Abs(affectedCoord.X - affectedCoord.Y));
                        break;
                    case 3:
                    case 7:
                        distance = Math.Abs(targetedCoord.Y - affectedCoord.Y);
                        break;
                    default:
                        distance = 0;
                        break;
                }

                break;
            default:
                distance = MapTools.GetDistance(sourceCell, affectedCellId);
                break;
        }

        var minEffectiveRadius = Shape == 'R' ? 0 : MinRadius;
        distance = Math.Max(distance - minEffectiveRadius, 0);
        return Math.Min(Math.Min(distance, MaxDegressionTicks) * Degression, 100);
    }
}