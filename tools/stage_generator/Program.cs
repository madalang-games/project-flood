using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

try
{
    if (args.Length != 1)
    {
        Console.Error.WriteLine("Usage: StageGenerator.Cli <request-json-path>");
        return 2;
    }

    var request = JsonSerializer.Deserialize<GeneratorRequest>(File.ReadAllText(args[0]), jsonOptions);
    if (request is null)
    {
        Console.Error.WriteLine("Invalid request JSON.");
        return 2;
    }

    var result = StageGenerator.Generate(request);
    Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

public sealed record GeneratorRequest(
    int Width,
    int Height,
    int Difficulty,
    int TurnLimit,
    double Star1Ratio,
    double Star2Ratio,
    int ColorCount,
    int ObstacleCount,
    int VoidCount,
    int ProtectorLevel1Count,
    int ProtectorLevel2Count,
    int CoreCellCount,
    int MaxAttempts,
    int RotationInterval,
    string? PortalData,
    string? ConveyorData);

public sealed record GeneratorResult(
    CellData[][] Board,
    string VerifiedSolution,
    int Attempts,
    int SolveLength,
    double Score);

public sealed record CandidateResult(CellData[][] Board, List<Coord> Solution, double Score, int Attempt);

public sealed record Recipe(
    int Difficulty,
    int SearchTurnLimit,
    int MinOptimalMoves,
    int MaxOptimalMoves,
    int SandwichDepth,
    int SandwichWidth,
    bool BlockerProtector,
    int DirectGroupCount,
    int ObstacleCount,
    int VoidCount,
    bool UseOffset,
    bool UsePartialBlocker,
    bool UseDecoys);

public sealed record Motif(
    int Width,
    HashSet<string> BlockerKeys,
    HashSet<string> PayloadKeys,
    HashSet<string> ProtectedKeys,
    HashSet<string> BlockerProtectorKeys,
    HashSet<string> DecoyKeys,
    Dictionary<string, Assigned> PreAssigned);

public sealed record Assigned(int Color, int GroupIdx);
public sealed record MotifUsage(bool BlockerTouched, int BlockerTouchCount, bool PayloadTouchedBeforeBlocker, bool MergedPayloadTouched, bool DecoyTouched);
public sealed record BoardStats(int GroupCount, int LargestGroup, int NarrowPocketCount, int SealedBasicCount, int IsolatedBasicCount);
public readonly record struct Coord(int R, int C);

public sealed class CellData
{
    public int ColorId { get; set; }
    public string Type { get; set; } = "Basic";
    public int Protector { get; set; }
    public bool IsCore { get; set; }

    public CellData Clone() => new() { ColorId = ColorId, Type = Type, Protector = Protector, IsCore = IsCore };
}

public static class StageGenerator
{
    private static readonly Coord[] Dirs = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];

    public static GeneratorResult? Generate(GeneratorRequest request)
    {
        if (request.MaxAttempts <= 0) return null;

        CandidateResult? best = null;
        var sync = new object();

        Parallel.For(1, request.MaxAttempts + 1, attempt =>
        {
            var rng = new Random(unchecked(Environment.TickCount * 31 + attempt * 7919));
            var candidate = TryGenerateCandidate(request, attempt, rng);
            if (candidate is null) return;

            lock (sync)
            {
                if (best is null || candidate.Score > best.Score) best = candidate;
            }
        });

        return best is null
            ? null
            : new GeneratorResult(
                best.Board,
                string.Join(';', best.Solution.Select(c => $"{c.R},{c.C}")),
                best.Attempt,
                best.Solution.Count,
                best.Score);
    }

    private static CandidateResult? TryGenerateCandidate(GeneratorRequest request, int attempt, Random rng)
    {
        var recipe = BuildRecipe(request, rng);
        if (recipe is null) return null;

        var grid = MakeGrid(request.Height, request.Width);
        var aColor = rng.Next(request.ColorCount);
        var motif = PlaceSandwich(grid, recipe, aColor, request.ColorCount, rng);

        var positions = Enumerable.Range(0, request.Width * request.Height)
            .Select(i => new Coord(i / request.Width, i % request.Width))
            .ToArray();
        var protectedKeys = motif?.ProtectedKeys ?? [];
        var maxBlocking = Math.Max(0, request.Width * request.Height - protectedKeys.Count - 1);
        var voidCells = Math.Min(recipe.VoidCount, maxBlocking);
        if (!PlaceBlockingCells(grid, positions, voidCells, protectedKeys, rng, () => MakeVoid())) return null;
        var obstacleCells = Math.Min(recipe.ObstacleCount, maxBlocking - voidCells);
        if (!PlaceBlockingCells(grid, positions, obstacleCells, protectedKeys, rng, () => MakeObstacle())) return null;

        var basicPositions = positions
            .Where(p => grid[p.R][p.C].Type == "Basic" && !protectedKeys.Contains(Key(p.R, p.C)))
            .ToArray();

        AssignColorsMultiGroup(basicPositions, grid, request.ColorCount, recipe.DirectGroupCount, motif?.PreAssigned, rng);
        if (HasSealedBasicCell(grid)) return null;

        PlaceProtectors(grid, basicPositions, request.ProtectorLevel1Count, request.ProtectorLevel2Count);
        PlaceCores(grid, basicPositions, request.CoreCellCount, rng);

        var board = CloneInitialBoard(grid);
        var initialValid = BoardRules.CountInitialValidCells(board);
        var flatBoard = FlatBoard.FromNullable(board, request.Width, request.Height);
        var solution = Solver.AutoSolveExact(flatBoard, request.Width, request.Height,
            recipe.SearchTurnLimit, initialValid, request.Star1Ratio, request.Star2Ratio,
            request.PortalData, request.ConveyorData, request.RotationInterval);
        if (solution is null) return null;

        var stats = CollectBoardStats(grid);
        if (stats.SealedBasicCount > 0) return null;
        var usage = AnalyzeMotifUsage(grid, solution, motif, request);
        var score = ScoreCandidate(recipe, stats, solution, motif, usage);

        return new CandidateResult(grid, solution, score, attempt);
    }

    private static Recipe? BuildRecipe(GeneratorRequest request, Random rng)
    {
        if (request.ColorCount < 2 || request.Width < 2) return null;
        var difficulty = Math.Clamp(request.Difficulty, 0, 2);
        var requestedDepth = difficulty == 0 ? 0 : difficulty;
        var sandwichDepth = Math.Min(requestedDepth, Math.Min((request.Height - 1) / 2, request.ColorCount - 1));
        var validCellEstimate = Math.Max(1, request.Width * request.Height - request.ObstacleCount - request.VoidCount);
        var targetGroupSize = difficulty == 0 ? 6 : difficulty == 1 ? 5 : 4;
        var expectedMoves = Math.Clamp((int)Math.Round((double)validCellEstimate / targetGroupSize),
            Math.Max(4, sandwichDepth + 3), Math.Min(24, validCellEstimate));
        var blockerProtector = difficulty >= 2 && sandwichDepth > 0 && rng.NextDouble() < 0.35;
        var sandwichMoves = sandwichDepth > 0 ? sandwichDepth + 1 + (blockerProtector ? 1 : 0) : 0;
        var directGroupCount = Math.Max(1, expectedMoves - sandwichMoves);
        var sandwichWidth = sandwichDepth > 0 ? Math.Max(2, Math.Min(request.Width - 1, difficulty == 1 ? 3 : 4)) : 0;
        var maxOptimalMoves = Math.Min(30, Math.Max(Math.Max(expectedMoves + 4, request.TurnLimit), sandwichMoves + directGroupCount + 2));

        return new Recipe(
            difficulty,
            maxOptimalMoves,
            Math.Max(2, expectedMoves - 3),
            maxOptimalMoves,
            sandwichDepth,
            sandwichWidth,
            blockerProtector,
            directGroupCount,
            Math.Max(0, request.ObstacleCount),
            Math.Max(0, request.VoidCount),
            difficulty > 0,
            difficulty > 0,
            difficulty > 0);
    }

    private static Motif? PlaceSandwich(CellData[][] grid, Recipe recipe, int aColor, int colorCount, Random rng)
    {
        var h = grid.Length;
        var w = grid[0].Length;
        var depth = recipe.SandwichDepth;
        var width = recipe.SandwichWidth;
        if (depth <= 0 || width <= 0) return null;

        var colStart = rng.Next(w - width + 1);
        var preAssigned = new Dictionary<string, Assigned>();
        var blockerKeys = new HashSet<string>();
        var payloadKeys = new HashSet<string>();
        var protectedKeys = new HashSet<string>();
        var blockerProtectorKeys = new HashSet<string>();
        var decoyKeys = new HashSet<string>();
        var layerCols = new List<int[]>();
        var hasOffsetPayload = false;

        for (var layer = 0; layer <= 2 * depth; layer++)
        {
            var isPayload = layer % 2 == 0;
            var shiftRange = Math.Max(0, w - width);
            var start = colStart;
            if (recipe.UseOffset && isPayload && width < w && layer > 0)
            {
                var starts = new[] { colStart - 1, colStart + 1 }.Where(s => s >= 0 && s <= shiftRange).ToArray();
                start = starts.Length > 0 ? starts[rng.Next(starts.Length)] : colStart;
                hasOffsetPayload |= start != colStart;
            }

            if (recipe.UsePartialBlocker && !isPayload && width > 1)
            {
                var trimLeft = rng.NextDouble() < 0.5 ? 1 : 0;
                var trimRight = trimLeft == 1 ? 0 : 1;
                layerCols.Add(Enumerable.Range(0, Math.Max(1, width - trimLeft - trimRight)).Select(i => colStart + trimLeft + i).ToArray());
            }
            else
            {
                layerCols.Add(Enumerable.Range(0, width).Select(i => start + i).ToArray());
            }
        }

        if (recipe.UseOffset && width < w && !hasOffsetPayload) return null;
        var shared = new HashSet<int>(layerCols[0]);
        for (var layer = 2; layer <= 2 * depth; layer += 2) shared.IntersectWith(layerCols[layer]);
        if (shared.Count == 0) return null;
        var gateCol = shared.First();
        for (var layer = 1; layer <= 2 * depth; layer += 2)
        {
            if (!layerCols[layer].Any(shared.Contains)) layerCols[layer][0] = gateCol;
        }

        for (var layer = 0; layer <= 2 * depth; layer++)
        {
            var row = h - 1 - layer;
            var isPayload = layer % 2 == 0;
            var colorId = isPayload ? aColor : (aColor + 1 + (layer / 2 % Math.Max(1, colorCount - 1))) % colorCount;
            foreach (var c in layerCols[layer])
            {
                grid[row][c] = MakeBasic(colorId);
                var key = Key(row, c);
                preAssigned[key] = new Assigned(colorId, -(layer + 1));
                protectedKeys.Add(key);
                if (isPayload)
                {
                    payloadKeys.Add(key);
                }
                else
                {
                    blockerKeys.Add(key);
                    if (recipe.BlockerProtector && (blockerProtectorKeys.Count == 0 || rng.NextDouble() < 0.45))
                    {
                        grid[row][c].Protector = 1;
                        blockerProtectorKeys.Add(key);
                    }
                }
            }
        }

        if (recipe.UseDecoys)
        {
            PlaceSandwichDecoys(grid, colorCount, aColor, layerCols, protectedKeys, preAssigned, decoyKeys, rng);
        }

        return new Motif(width, blockerKeys, payloadKeys, protectedKeys, blockerProtectorKeys, decoyKeys, preAssigned);
    }

    private static void PlaceSandwichDecoys(
        CellData[][] grid,
        int colorCount,
        int payloadColor,
        List<int[]> layerCols,
        HashSet<string> protectedKeys,
        Dictionary<string, Assigned> preAssigned,
        HashSet<string> decoyKeys,
        Random rng)
    {
        var h = grid.Length;
        var w = grid[0].Length;
        var usedCols = layerCols.SelectMany(c => c).ToArray();
        var side = usedCols.Average() < w / 2.0 ? 1 : -1;
        var anchor = side > 0 ? Math.Min(w - 1, usedCols.Max() + 2) : Math.Max(0, usedCols.Min() - 2);
        var cols = new[] { anchor, Math.Clamp(anchor + side, 0, w - 1) }.Distinct();
        var rows = new[] { h - 1, Math.Max(0, h - 3) };

        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            foreach (var col in cols)
            {
                var key = Key(row, col);
                if (protectedKeys.Contains(key)) continue;
                var adjacent = Dirs.Any(d => protectedKeys.Contains(Key(row + d.R, col + d.C)));
                var colorId = i == 0 && !adjacent
                    ? payloadColor
                    : (payloadColor + 1 + rng.Next(Math.Max(1, colorCount - 1))) % colorCount;
                grid[row][col] = MakeBasic(colorId);
                protectedKeys.Add(key);
                decoyKeys.Add(key);
                preAssigned[key] = new Assigned(colorId, -100 - decoyKeys.Count);
            }
        }
    }

    private static bool PlaceBlockingCells(CellData[][] grid, Coord[] candidates, int count, HashSet<string> protectedKeys, Random rng, Func<CellData> makeCell)
    {
        if (count <= 0) return true;
        var shuffled = candidates.Where(p => !protectedKeys.Contains(Key(p.R, p.C))).OrderBy(_ => rng.Next()).ToArray();
        var placed = 0;
        var cursor = 0;
        var relaxed = false;

        while (placed < count)
        {
            if (cursor >= shuffled.Length)
            {
                if (relaxed) return false;
                relaxed = true;
                cursor = 0;
            }

            var (r, c) = shuffled[cursor++];
            if (grid[r][c].Type != "Basic") continue;
            var prev = grid[r][c];
            grid[r][c] = makeCell();
            var fails = relaxed ? HasSealedBasicCell(grid) : ObstacleQualityFails(grid);
            if (fails)
            {
                grid[r][c] = prev;
                continue;
            }

            placed++;
        }

        return true;
    }

    private static void AssignColorsMultiGroup(Coord[] basicPositions, CellData[][] grid, int colorCount, int targetGroupCount, Dictionary<string, Assigned>? preAssigned, Random rng)
    {
        if (basicPositions.Length == 0) return;
        var actualGroupCount = Math.Min(Math.Max(1, targetGroupCount), basicPositions.Length);
        var maxGroupSize = Math.Max(2, (int)Math.Ceiling((double)basicPositions.Length / actualGroupCount));
        var seeds = PickSpreadSeeds(basicPositions, actualGroupCount, rng);
        var assigned = preAssigned is null ? new Dictionary<string, Assigned>() : new Dictionary<string, Assigned>(preAssigned);
        var queues = new Queue<Coord>[seeds.Count];
        var sizes = new int[seeds.Count];

        for (var g = 0; g < seeds.Count; g++)
        {
            var seed = seeds[g];
            var color = g % colorCount;
            assigned[Key(seed.R, seed.C)] = new Assigned(color, g);
            grid[seed.R][seed.C].ColorId = color;
            queues[g] = new Queue<Coord>([seed]);
            sizes[g] = 1;
        }

        var hasMore = true;
        while (hasMore)
        {
            hasMore = false;
            for (var g = 0; g < seeds.Count; g++)
            {
                if (queues[g].Count == 0 || sizes[g] >= maxGroupSize) continue;
                hasMore = true;
                var current = queues[g].Dequeue();
                foreach (var d in Dirs)
                {
                    if (sizes[g] >= maxGroupSize) break;
                    var nr = current.R + d.R;
                    var nc = current.C + d.C;
                    if (!InBounds(grid, nr, nc)) continue;
                    var key = Key(nr, nc);
                    if (assigned.ContainsKey(key) || grid[nr][nc].Type != "Basic") continue;

                    var blocked = Dirs.Any(d2 =>
                    {
                        var ar = nr + d2.R;
                        var ac = nc + d2.C;
                        return InBounds(grid, ar, ac)
                            && assigned.TryGetValue(Key(ar, ac), out var neighbor)
                            && neighbor.Color == g % colorCount
                            && neighbor.GroupIdx != g;
                    });
                    if (blocked) continue;

                    assigned[key] = new Assigned(g % colorCount, g);
                    grid[nr][nc].ColorId = g % colorCount;
                    queues[g].Enqueue(new Coord(nr, nc));
                    sizes[g]++;
                }
            }
        }

        foreach (var p in basicPositions)
        {
            if (!assigned.ContainsKey(Key(p.R, p.C))) grid[p.R][p.C].ColorId = rng.Next(colorCount);
        }

        HealIsolatedBasics(basicPositions, grid, assigned);
    }

    private static List<Coord> PickSpreadSeeds(Coord[] positions, int count, Random rng)
    {
        var shuffled = positions.OrderBy(_ => rng.Next()).ToList();
        if (shuffled.Count <= count) return shuffled;
        var seeds = new List<Coord> { shuffled[0] };
        var remaining = shuffled.Skip(1).ToList();
        while (seeds.Count < count && remaining.Count > 0)
        {
            var bestIdx = 0;
            var bestDist = -1;
            for (var i = 0; i < remaining.Count; i++)
            {
                var p = remaining[i];
                var minDist = seeds.Min(s => Math.Abs(p.R - s.R) + Math.Abs(p.C - s.C));
                if (minDist > bestDist)
                {
                    bestDist = minDist;
                    bestIdx = i;
                }
            }
            seeds.Add(remaining[bestIdx]);
            remaining.RemoveAt(bestIdx);
        }
        return seeds;
    }

    private static void HealIsolatedBasics(Coord[] basicPositions, CellData[][] grid, Dictionary<string, Assigned> assigned)
    {
        foreach (var p in basicPositions)
        {
            var hasBasicNeighbor = false;
            var hasSameColor = false;
            var adjColors = new List<int>();
            foreach (var d in Dirs)
            {
                var nr = p.R + d.R;
                var nc = p.C + d.C;
                if (!InBounds(grid, nr, nc) || grid[nr][nc].Type != "Basic") continue;
                hasBasicNeighbor = true;
                adjColors.Add(grid[nr][nc].ColorId);
                if (grid[nr][nc].ColorId == grid[p.R][p.C].ColorId) hasSameColor = true;
            }
            if (!hasBasicNeighbor || hasSameColor || adjColors.Count == 0) continue;
            var newColor = adjColors.GroupBy(c => c).OrderByDescending(g => g.Count()).First().Key;
            grid[p.R][p.C].ColorId = newColor;
            assigned[Key(p.R, p.C)] = new Assigned(newColor, -1);
        }
    }

    private static void PlaceProtectors(CellData[][] grid, Coord[] basicPositions, int p1Count, int p2Count)
    {
        var candidates = basicPositions.Where(p => grid[p.R][p.C].Protector == 0).ToArray();
        var p2 = Math.Min(p2Count, candidates.Length);
        var p1 = Math.Min(p1Count, candidates.Length - p2);
        var scored = candidates.Select(p => new
        {
            P = p,
            Score = Dirs.Count(d => InBounds(grid, p.R + d.R, p.C + d.C)
                && grid[p.R + d.R][p.C + d.C].Type == "Basic"
                && grid[p.R + d.R][p.C + d.C].ColorId == grid[p.R][p.C].ColorId)
        }).OrderByDescending(x => x.Score).ToArray();
        for (var i = 0; i < p2; i++) grid[scored[i].P.R][scored[i].P.C].Protector = 2;
        for (var i = p2; i < p2 + p1; i++) grid[scored[i].P.R][scored[i].P.C].Protector = 1;
    }

    private static void PlaceCores(CellData[][] grid, Coord[] basicPositions, int coreCount, Random rng)
    {
        var candidates = basicPositions.Where(p => grid[p.R][p.C].Protector == 0).OrderBy(_ => rng.Next()).ToArray();
        for (var i = 0; i < Math.Min(coreCount, candidates.Length); i++) grid[candidates[i].R][candidates[i].C].IsCore = true;
    }

    private static MotifUsage AnalyzeMotifUsage(CellData[][] initialGrid, List<Coord> solution, Motif? motif, GeneratorRequest request)
    {
        if (motif is null) return new MotifUsage(false, 0, false, false, false);
        var board = CloneInitialBoard(initialGrid);
        var markers = BuildMarkerBoard(initialGrid, motif);
        var blockerTouched = false;
        var payloadTouchedBeforeBlocker = false;
        var mergedPayloadTouched = false;
        var blockerTouchCount = 0;
        var decoyTouched = false;

        for (var moveIndex = 0; moveIndex < solution.Count; moveIndex++)
        {
            var move = solution[moveIndex];
            var group = BoardRules.FindGroup(board, move.R, move.C);
            var touchesBlocker = group.Any(p => markers[p.R][p.C] == "blocker");
            var touchesPayload = group.Any(p => markers[p.R][p.C] == "payload");
            var touchesDecoy = group.Any(p => markers[p.R][p.C] == "decoy");
            if (touchesPayload && !blockerTouched) payloadTouchedBeforeBlocker = true;
            if (touchesBlocker)
            {
                blockerTouched = true;
                blockerTouchCount++;
            }
            if (touchesDecoy) decoyTouched = true;
            if (blockerTouched && touchesPayload && group.Count >= motif.Width * 2) mergedPayloadTouched = true;

            if (group.Count > 0)
            {
                (board, markers) = ApplyRemovalPaired(board, markers, group);
                (board, markers) = ApplyConveyorsPaired(board, markers, request.ConveyorData);
                (board, markers) = ApplyGravityPaired(board, markers, request.PortalData);
            }
            var movesMade = moveIndex + 1;
            if (request.RotationInterval > 0 && movesMade % request.RotationInterval == 0)
            {
                board = BoardRules.Rotate180(board);
                markers = RotateMarkers(markers);
                (board, markers) = ApplyGravityPaired(board, markers, request.PortalData);
            }
        }

        return new MotifUsage(blockerTouched, blockerTouchCount, payloadTouchedBeforeBlocker, mergedPayloadTouched, decoyTouched);
    }

    private static double ScoreCandidate(Recipe recipe, BoardStats stats, List<Coord> solution, Motif? motif, MotifUsage usage)
    {
        var difficulty = recipe.Difficulty;
        var score = 1000.0;
        var idealMoves = (recipe.MinOptimalMoves + recipe.MaxOptimalMoves) / 2.0;
        score -= Math.Abs(solution.Count - idealMoves) * (difficulty == 0 ? 12 : difficulty == 1 ? 20 : 28);
        score += Math.Min(solution.Count, 24) * (difficulty == 0 ? 5 : difficulty == 1 ? 10 : 14);
        var idealLargest = difficulty == 0 ? 10 : difficulty == 1 ? 7 : 5;
        score -= Math.Max(0, stats.LargestGroup - idealLargest) * (difficulty == 0 ? 8 : difficulty == 1 ? 18 : 30);
        score -= Math.Max(0, recipe.MinOptimalMoves - stats.GroupCount) * 35;
        score -= stats.SealedBasicCount * 200;
        score -= stats.NarrowPocketCount * (difficulty == 0 ? 8 : 16);
        score -= stats.IsolatedBasicCount * 24;

        if (motif is not null)
        {
            score += 60;
            if (motif.DecoyKeys.Count > 0) score += difficulty == 0 ? 5 : difficulty == 1 ? 35 : 50;
            if (motif.BlockerProtectorKeys.Count > 0) score += difficulty >= 2 ? 45 : 15;
        }
        if (usage.BlockerTouched) score += difficulty == 0 ? 20 : 90;
        if (usage.MergedPayloadTouched) score += difficulty == 0 ? 30 : difficulty == 1 ? 160 : 220;
        if (usage.PayloadTouchedBeforeBlocker) score -= difficulty == 0 ? 20 : 100;
        if (usage.DecoyTouched) score -= difficulty == 0 ? 10 : difficulty == 1 ? 60 : 110;
        var requiredBlockerTouches = motif is not null && motif.BlockerProtectorKeys.Count > 0 ? 2 : 1;
        if (motif is not null && usage.BlockerTouchCount >= requiredBlockerTouches) score += difficulty >= 2 ? 70 : 30;
        if (difficulty > 0 && motif is not null && !usage.MergedPayloadTouched) score -= difficulty == 1 ? 140 : 220;
        return score;
    }

    private static BoardStats CollectBoardStats(CellData[][] grid)
    {
        var board = CloneInitialBoard(grid);
        var visited = new HashSet<string>();
        var groupCount = 0;
        var largestGroup = 0;
        var narrowPocketCount = 0;
        var sealedBasicCount = 0;
        var isolatedBasicCount = 0;

        for (var r = 0; r < grid.Length; r++)
        for (var c = 0; c < grid[0].Length; c++)
        {
            if (grid[r][c].Type != "Basic") continue;
            var open = OpenNeighborCount(grid, r, c);
            if (open == 0) sealedBasicCount++;
            if (open == 1) narrowPocketCount++;
            var basicNeighbors = Dirs.Where(d => InBounds(grid, r + d.R, c + d.C) && grid[r + d.R][c + d.C].Type == "Basic").ToArray();
            if (basicNeighbors.Length > 0 && basicNeighbors.All(d => grid[r + d.R][c + d.C].ColorId != grid[r][c].ColorId)) isolatedBasicCount++;

            var key = Key(r, c);
            if (visited.Contains(key)) continue;
            var group = BoardRules.FindGroup(board, r, c);
            foreach (var p in group) visited.Add(Key(p.R, p.C));
            if (group.Count > 0)
            {
                groupCount++;
                largestGroup = Math.Max(largestGroup, group.Count);
            }
        }

        return new BoardStats(groupCount, largestGroup, narrowPocketCount, sealedBasicCount, isolatedBasicCount);
    }

    private static bool ObstacleQualityFails(CellData[][] grid)
    {
        var basicCount = 0;
        var narrow = 0;
        for (var r = 0; r < grid.Length; r++)
        for (var c = 0; c < grid[0].Length; c++)
        {
            if (grid[r][c].Type != "Basic") continue;
            basicCount++;
            var open = OpenNeighborCount(grid, r, c);
            if (open == 0) return true;
            if (open == 1) narrow++;
        }
        return narrow > Math.Max(2, basicCount / 12);
    }

    private static bool HasSealedBasicCell(CellData[][] grid)
    {
        for (var r = 0; r < grid.Length; r++)
        for (var c = 0; c < grid[0].Length; c++)
        {
            if (grid[r][c].Type == "Basic" && OpenNeighborCount(grid, r, c) == 0) return true;
        }
        return false;
    }

    private static int OpenNeighborCount(CellData[][] grid, int r, int c) =>
        Dirs.Count(d => InBounds(grid, r + d.R, c + d.C) && grid[r + d.R][c + d.C].Type is not ("Obstacle" or "Void"));

    private static string[][] BuildMarkerBoard(CellData[][] grid, Motif motif)
    {
        var markers = Enumerable.Range(0, grid.Length).Select(_ => Enumerable.Repeat("", grid[0].Length).ToArray()).ToArray();
        for (var r = 0; r < grid.Length; r++)
        for (var c = 0; c < grid[0].Length; c++)
        {
            var key = Key(r, c);
            markers[r][c] = motif.BlockerKeys.Contains(key) ? "blocker"
                : motif.PayloadKeys.Contains(key) ? "payload"
                : motif.DecoyKeys.Contains(key) ? "decoy"
                : "";
        }
        return markers;
    }

    private static (CellData?[][], string[][]) ApplyRemovalPaired(CellData?[][] board, string[][] markers, List<Coord> group)
    {
        var b = CloneNullableBoard(board);
        var m = markers.Select(row => row.ToArray()).ToArray();
        foreach (var p in group)
        {
            var cell = b[p.R][p.C];
            if (cell is null) continue;
            if (cell.Protector > 0) cell.Protector--;
            else
            {
                b[p.R][p.C] = null;
                m[p.R][p.C] = "";
            }
        }
        return (b, m);
    }

    private static (CellData?[][], string[][]) ApplyConveyorsPaired(CellData?[][] board, string[][] markers, string? conveyorData)
    {
        var b = CloneNullableBoard(board);
        var m = markers.Select(row => row.ToArray()).ToArray();
        foreach (var path in BoardRules.ParseConveyors(conveyorData))
        {
            var last = path[^1];
            var lastCell = b[last.R][last.C]?.Clone();
            var lastMarker = m[last.R][last.C];
            for (var i = path.Count - 1; i > 0; i--)
            {
                var to = path[i];
                var from = path[i - 1];
                b[to.R][to.C] = b[from.R][from.C];
                m[to.R][to.C] = m[from.R][from.C];
            }
            var first = path[0];
            b[first.R][first.C] = lastCell;
            m[first.R][first.C] = lastMarker;
        }
        return (b, m);
    }

    private static (CellData?[][], string[][]) ApplyGravityPaired(CellData?[][] board, string[][] markers, string? portalData)
    {
        var b = CloneNullableBoard(board);
        var m = markers.Select(row => row.ToArray()).ToArray();
        var rows = b.Length;
        var cols = b[0].Length;
        var portals = BoardRules.ParsePortals(portalData);

        Coord? FindSource(int r, int c)
        {
            var cr = r;
            var cc = c;
            while (true)
            {
                var next = portals.TryGetValue(Key(cr, cc), out var inlet) ? inlet : new Coord(cr - 1, cc);
                if (next.R < 0 || next.R >= rows || next.C < 0 || next.C >= cols) return null;
                var cell = b[next.R][next.C];
                if (cell is not null)
                {
                    if (cell.Type is "Void" or "Obstacle") return null;
                    return next;
                }
                cr = next.R;
                cc = next.C;
            }
        }

        for (var r = rows - 1; r >= 0; r--)
        for (var c = 0; c < cols; c++)
        {
            var cell = b[r][c];
            if (cell is not null && cell.Type is "Void" or "Obstacle") continue;
            if (cell is not null) continue;
            var source = FindSource(r, c);
            if (source is null) continue;
            b[r][c] = b[source.Value.R][source.Value.C];
            m[r][c] = m[source.Value.R][source.Value.C];
            b[source.Value.R][source.Value.C] = null;
            m[source.Value.R][source.Value.C] = "";
        }

        return (b, m);
    }

    private static string[][] RotateMarkers(string[][] markers)
    {
        var rows = markers.Length;
        var cols = markers[0].Length;
        var rotated = Enumerable.Range(0, rows).Select(_ => Enumerable.Repeat("", cols).ToArray()).ToArray();
        for (var r = 0; r < rows; r++)
        for (var c = 0; c < cols; c++)
            rotated[rows - 1 - r][cols - 1 - c] = markers[r][c];
        return rotated;
    }

    private static CellData[][] MakeGrid(int h, int w) =>
        Enumerable.Range(0, h).Select(_ => Enumerable.Range(0, w).Select(_ => MakeBasic()).ToArray()).ToArray();

    private static CellData MakeBasic(int color = 0) => new() { ColorId = color, Type = "Basic" };
    private static CellData MakeObstacle() => new() { Type = "Obstacle" };
    private static CellData MakeVoid() => new() { Type = "Void" };
    private static string Key(int r, int c) => $"{r},{c}";
    private static bool InBounds(CellData[][] grid, int r, int c) => r >= 0 && r < grid.Length && c >= 0 && c < grid[0].Length;

    private static CellData?[][] CloneInitialBoard(CellData[][] grid) => grid.Select(row => row.Select<CellData, CellData?>(cell => cell.Clone()).ToArray()).ToArray();
    private static CellData?[][] CloneNullableBoard(CellData?[][] board) => board.Select(row => row.Select(cell => cell?.Clone()).ToArray()).ToArray();
}

public static class Solver
{
    private const int MaxStates = 80_000;

    public static List<Coord>? AutoSolveExact(
        int[] board, int width, int height,
        int turnLimit, int initialValid,
        double star1, double star2,
        string? portalData, string? conveyorData,
        int rotationInterval)
    {
        var portals = FlatBoard.ParsePortals(portalData, width);
        var conveyors = FlatBoard.ParseConveyors(conveyorData, width);
        var visited = new HashSet<long> { FlatBoard.Hash(board) };
        var queue = new Queue<(int[] Board, List<Coord> Moves)>();
        queue.Enqueue(((int[])board.Clone(), []));

        while (queue.Count > 0)
        {
            if (visited.Count > MaxStates) return null;
            var (b, moves) = queue.Dequeue();
            if (moves.Count >= turnLimit) continue;

            var starts = FlatBoard.FindAllGroupStarts(b, width, height, 2);
            if (starts.Count == 0) starts = FlatBoard.FindAllGroupStarts(b, width, height, 1);

            foreach (var startIdx in starts)
            {
                var group = FlatBoard.FindGroup(b, width, height, startIdx);
                var nb = FlatBoard.ApplyRemoval(b, group);
                nb = FlatBoard.ApplyConveyors(nb, conveyors);
                nb = FlatBoard.ApplyGravity(nb, width, height, portals);
                var newMoves = new List<Coord>(moves) { new(startIdx / width, startIdx % width) };
                if (rotationInterval > 0 && newMoves.Count % rotationInterval == 0)
                {
                    nb = FlatBoard.Rotate180(nb, width, height);
                    nb = FlatBoard.ApplyGravity(nb, width, height, portals);
                }

                if (FlatBoard.EvaluateStars(nb, initialValid, star1, star2) == 3) return newMoves;
                var hash = FlatBoard.Hash(nb);
                if (!visited.Contains(hash) && newMoves.Count < turnLimit)
                {
                    visited.Add(hash);
                    queue.Enqueue((nb, newMoves));
                }
            }
        }

        return null;
    }
}

public static class BoardRules
{
    private static readonly Coord[] Dirs = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];

    public static CellData?[][] Clone(CellData?[][] board) => board.Select(row => row.Select(cell => cell?.Clone()).ToArray()).ToArray();

    public static List<Coord> FindAllGroupStarts(CellData?[][] board, int minSize)
    {
        var visited = new HashSet<string>();
        var starts = new List<Coord>();
        for (var r = 0; r < board.Length; r++)
        for (var c = 0; c < board[0].Length; c++)
        {
            var key = $"{r},{c}";
            if (visited.Contains(key)) continue;
            var cell = board[r][c];
            if (cell is null || cell.Type is "Obstacle" or "Void")
            {
                visited.Add(key);
                continue;
            }
            var group = FindGroup(board, r, c);
            foreach (var p in group) visited.Add($"{p.R},{p.C}");
            if (group.Count >= minSize) starts.Add(new Coord(r, c));
        }
        return starts;
    }

    public static List<Coord> FindGroup(CellData?[][] board, int row, int col)
    {
        var cell = row >= 0 && row < board.Length && col >= 0 && col < board[0].Length ? board[row][col] : null;
        if (cell is null || cell.Type is "Obstacle" or "Void") return [];
        var color = cell.ColorId;
        var visited = new HashSet<string>();
        var group = new List<Coord>();
        var queue = new Queue<Coord>();
        queue.Enqueue(new Coord(row, col));
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            var key = $"{p.R},{p.C}";
            if (visited.Contains(key)) continue;
            if (p.R < 0 || p.R >= board.Length || p.C < 0 || p.C >= board[0].Length) continue;
            var cur = board[p.R][p.C];
            if (cur is null || cur.Type is "Obstacle" or "Void" || cur.ColorId != color) continue;
            visited.Add(key);
            group.Add(p);
            foreach (var d in Dirs) queue.Enqueue(new Coord(p.R + d.R, p.C + d.C));
        }
        return group;
    }

    public static CellData?[][] ApplyRemoval(CellData?[][] board, List<Coord> group)
    {
        var b = Clone(board);
        foreach (var p in group)
        {
            var cell = b[p.R][p.C];
            if (cell is null) continue;
            if (cell.Protector > 0) cell.Protector--;
            else b[p.R][p.C] = null;
        }
        return b;
    }

    public static CellData?[][] ApplyGravity(CellData?[][] board, string? portalData)
    {
        var b = Clone(board);
        var rows = b.Length;
        var cols = b[0].Length;
        var portals = ParsePortals(portalData);

        Coord? FindSource(int r, int c)
        {
            var cr = r;
            var cc = c;
            while (true)
            {
                var next = portals.TryGetValue($"{cr},{cc}", out var inlet) ? inlet : new Coord(cr - 1, cc);
                if (next.R < 0 || next.R >= rows || next.C < 0 || next.C >= cols) return null;
                var cell = b[next.R][next.C];
                if (cell is not null)
                {
                    if (cell.Type is "Void" or "Obstacle") return null;
                    return next;
                }
                cr = next.R;
                cc = next.C;
            }
        }

        for (var r = rows - 1; r >= 0; r--)
        for (var c = 0; c < cols; c++)
        {
            var cell = b[r][c];
            if (cell is not null && cell.Type is "Void" or "Obstacle") continue;
            if (cell is not null) continue;
            var source = FindSource(r, c);
            if (source is null) continue;
            b[r][c] = b[source.Value.R][source.Value.C];
            b[source.Value.R][source.Value.C] = null;
        }
        return b;
    }

    public static CellData?[][] ApplyConveyors(CellData?[][] board, string? conveyorData)
    {
        var b = Clone(board);
        foreach (var path in ParseConveyors(conveyorData))
        {
            var last = path[^1];
            var lastCell = b[last.R][last.C]?.Clone();
            for (var i = path.Count - 1; i > 0; i--)
            {
                var to = path[i];
                var from = path[i - 1];
                b[to.R][to.C] = b[from.R][from.C];
            }
            var first = path[0];
            b[first.R][first.C] = lastCell;
        }
        return b;
    }

    public static CellData?[][] Rotate180(CellData?[][] board)
    {
        var rows = board.Length;
        var cols = board[0].Length;
        var b = Enumerable.Range(0, rows).Select(_ => new CellData?[cols]).ToArray();
        for (var r = 0; r < rows; r++)
        for (var c = 0; c < cols; c++)
            b[rows - 1 - r][cols - 1 - c] = board[r][c]?.Clone();
        return b;
    }

    public static int CountInitialValidCells(CellData?[][] board) =>
        board.Sum(row => row.Count(cell => cell is not null && cell.Type is not ("Obstacle" or "Void")));

    public static int EvaluateStars(CellData?[][] board, int initialValid, double star1, double star2)
    {
        var remaining = 0;
        var coreRemaining = false;
        foreach (var row in board)
        foreach (var cell in row)
        {
            if (cell is null || cell.Type is "Obstacle" or "Void") continue;
            remaining++;
            if (cell.IsCore) coreRemaining = true;
        }
        if (remaining == 0) return 3;
        var ratio = initialValid > 0 ? (double)(initialValid - remaining) / initialValid : 0;
        if (coreRemaining || ratio < star1) return 0;
        return ratio >= star2 ? 2 : 1;
    }

    public static Dictionary<string, Coord> ParsePortals(string? portalData)
    {
        var map = new Dictionary<string, Coord>();
        if (string.IsNullOrWhiteSpace(portalData)) return map;
        foreach (var portal in portalData.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = portal.Split("->");
            if (parts.Length != 2) continue;
            var outlet = ParseCoord(parts[1]);
            var inlet = ParseCoord(parts[0]);
            if (outlet is not null && inlet is not null) map[$"{outlet.Value.R},{outlet.Value.C}"] = inlet.Value;
        }
        return map;
    }

    public static List<List<Coord>> ParseConveyors(string? conveyorData)
    {
        var paths = new List<List<Coord>>();
        if (string.IsNullOrWhiteSpace(conveyorData)) return paths;
        foreach (var rawPath in conveyorData.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var coords = rawPath.Split("->").Select(ParseCoord).Where(c => c is not null).Select(c => c!.Value).ToList();
            if (coords.Count > 1) paths.Add(coords);
        }
        return paths;
    }

    private static Coord? ParseCoord(string raw)
    {
        var parts = raw.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var r) || !int.TryParse(parts[1], out var c)) return null;
        return new Coord(r, c);
    }
}

static class CellEnc
{
    public const int EMPTY = int.MinValue;

    public static int Encode(CellData? cell)
    {
        if (cell is null) return EMPTY;
        var type = cell.Type switch { "Obstacle" => 1, "Void" => 2, _ => 0 };
        return cell.ColorId | (type << 8) | (cell.Protector << 10) | (cell.IsCore ? 1 << 12 : 0);
    }

    public static bool IsEmpty(int c) => c == EMPTY;
    public static bool IsBasic(int c) => c != EMPTY && ((c >> 8) & 3) == 0;
    public static bool IsObstacle(int c) => c != EMPTY && ((c >> 8) & 3) == 1;
    public static bool IsVoid(int c) => c != EMPTY && ((c >> 8) & 3) == 2;
    public static int ColorOf(int c) => c & 0xFF;
    public static int ProtectorOf(int c) => (c >> 10) & 3;
    public static bool IsCoreOf(int c) => (c & (1 << 12)) != 0;
    public static int SetProtector(int c, int p) => (c & ~(3 << 10)) | (p << 10);
}

static class FlatBoard
{
    public static int[] FromNullable(CellData?[][] board, int w, int h)
    {
        var flat = new int[h * w];
        for (var r = 0; r < h; r++)
        for (var c = 0; c < w; c++)
            flat[r * w + c] = CellEnc.Encode(board[r][c]);
        return flat;
    }

    public static long Hash(int[] board)
    {
        var h = unchecked((long)14695981039346656037UL);
        foreach (var cell in board)
        {
            h ^= (uint)cell;
            h = unchecked(h * 1099511628211L);
        }
        return h;
    }

    public static List<int> FindGroup(int[] board, int w, int h, int start)
    {
        var startCell = board[start];
        if (CellEnc.IsEmpty(startCell) || !CellEnc.IsBasic(startCell)) return [];
        var color = CellEnc.ColorOf(startCell);
        var inGroup = new bool[board.Length];
        var queue = new Queue<int>();
        var group = new List<int> { start };
        inGroup[start] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var idx = queue.Dequeue();
            var r = idx / w; var c = idx % w;
            if (r > 0)   TryAdd(idx - w);
            if (r < h-1) TryAdd(idx + w);
            if (c > 0)   TryAdd(idx - 1);
            if (c < w-1) TryAdd(idx + 1);
        }
        return group;

        void TryAdd(int ni)
        {
            if (inGroup[ni]) return;
            var nc = board[ni];
            if (CellEnc.IsEmpty(nc) || !CellEnc.IsBasic(nc) || CellEnc.ColorOf(nc) != color) return;
            inGroup[ni] = true;
            group.Add(ni);
            queue.Enqueue(ni);
        }
    }

    public static List<int> FindAllGroupStarts(int[] board, int w, int h, int minSize)
    {
        var visited = new bool[board.Length];
        var starts = new List<int>();
        for (var idx = 0; idx < board.Length; idx++)
        {
            if (visited[idx]) continue;
            var cell = board[idx];
            if (CellEnc.IsEmpty(cell) || !CellEnc.IsBasic(cell)) { visited[idx] = true; continue; }
            var group = FindGroup(board, w, h, idx);
            foreach (var p in group) visited[p] = true;
            if (group.Count >= minSize) starts.Add(group[0]);
        }
        return starts;
    }

    public static int[] ApplyRemoval(int[] board, List<int> group)
    {
        var b = (int[])board.Clone();
        foreach (var idx in group)
        {
            var cell = b[idx];
            if (CellEnc.IsEmpty(cell)) continue;
            var p = CellEnc.ProtectorOf(cell);
            b[idx] = p > 0 ? CellEnc.SetProtector(cell, p - 1) : CellEnc.EMPTY;
        }
        return b;
    }

    public static int[] ApplyGravity(int[] board, int w, int h, Dictionary<int, int> portals)
    {
        var b = (int[])board.Clone();

        int? FindSource(int r, int c)
        {
            var cr = r; var cc = c;
            while (true)
            {
                var curIdx = cr * w + cc;
                var nextIdx = portals.TryGetValue(curIdx, out var inlet) ? inlet : curIdx - w;
                if (nextIdx < 0 || nextIdx >= b.Length) return null;
                var nr = nextIdx / w; var nc = nextIdx % w;
                var cell = b[nextIdx];
                if (!CellEnc.IsEmpty(cell))
                    return (CellEnc.IsVoid(cell) || CellEnc.IsObstacle(cell)) ? null : nextIdx;
                cr = nr; cc = nc;
            }
        }

        for (var r = h - 1; r >= 0; r--)
        for (var c = 0; c < w; c++)
        {
            var idx = r * w + c;
            var cell = b[idx];
            if (!CellEnc.IsEmpty(cell) && (CellEnc.IsVoid(cell) || CellEnc.IsObstacle(cell))) continue;
            if (!CellEnc.IsEmpty(cell)) continue;
            var src = FindSource(r, c);
            if (src is null) continue;
            b[idx] = b[src.Value];
            b[src.Value] = CellEnc.EMPTY;
        }
        return b;
    }

    public static int[] ApplyConveyors(int[] board, List<List<int>> conveyors)
    {
        var b = (int[])board.Clone();
        foreach (var path in conveyors)
        {
            var last = b[path[^1]];
            for (var i = path.Count - 1; i > 0; i--)
                b[path[i]] = b[path[i - 1]];
            b[path[0]] = last;
        }
        return b;
    }

    public static int[] Rotate180(int[] board, int w, int h)
    {
        var b = new int[board.Length];
        for (var i = 0; i < board.Length; i++)
            b[(h - 1 - i / w) * w + (w - 1 - i % w)] = board[i];
        return b;
    }

    public static int EvaluateStars(int[] board, int initialValid, double star1, double star2)
    {
        var remaining = 0;
        var coreRemaining = false;
        foreach (var cell in board)
        {
            if (CellEnc.IsEmpty(cell) || !CellEnc.IsBasic(cell)) continue;
            remaining++;
            if (CellEnc.IsCoreOf(cell)) coreRemaining = true;
        }
        if (remaining == 0) return 3;
        var ratio = initialValid > 0 ? (double)(initialValid - remaining) / initialValid : 0;
        if (coreRemaining || ratio < star1) return 0;
        return ratio >= star2 ? 2 : 1;
    }

    public static Dictionary<int, int> ParsePortals(string? data, int w)
    {
        var map = new Dictionary<int, int>();
        if (string.IsNullOrWhiteSpace(data)) return map;
        foreach (var portal in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = portal.Split("->");
            if (parts.Length != 2) continue;
            var outlet = ParseCoord(parts[1]);
            var inlet = ParseCoord(parts[0]);
            if (outlet is not null && inlet is not null)
                map[outlet.Value.R * w + outlet.Value.C] = inlet.Value.R * w + inlet.Value.C;
        }
        return map;
    }

    public static List<List<int>> ParseConveyors(string? data, int w)
    {
        var paths = new List<List<int>>();
        if (string.IsNullOrWhiteSpace(data)) return paths;
        foreach (var rawPath in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var coords = rawPath.Split("->")
                .Select(ParseCoord).Where(c => c is not null)
                .Select(c => c!.Value.R * w + c.Value.C).ToList();
            if (coords.Count > 1) paths.Add(coords);
        }
        return paths;
    }

    private static Coord? ParseCoord(string raw)
    {
        var parts = raw.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var c)
            ? new Coord(r, c) : null;
    }
}
