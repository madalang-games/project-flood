import type { CellData } from '../types/stage';
import {
  countInitialValidCells,
  findGroup,
  rotate180,
} from './game-rules';
import type { Board } from './game-rules';
import { autoSolveExact } from './solver';

export interface GeneratorSettings {
  colorCount: number;
  obstacleCount: number;
  voidCount: number;
  protectorLevel1Count: number;
  protectorLevel2Count: number;
  coreCellCount: number;
  maxAttempts: number;
  difficultyMargin: number;
}

export interface GeneratorConfig extends GeneratorSettings {
  width: number;
  height: number;
  difficulty: number;
  turnLimit: number;
  star1Ratio: number;
  star2Ratio: number;
  rotationInterval?: number;
  portalData?: string;
  conveyorData?: string;
}

export interface GenerateResult {
  board: CellData[][];
  verifiedSolution: string;
  attempts: number;
  solveLength: number;
  score: number;
}

type Coord = [number, number];
type Assigned = Map<string, { color: number; groupIdx: number }>;

interface GeneratorRecipe {
  difficulty: number;
  searchTurnLimit: number;
  minOptimalMoves: number;
  maxOptimalMoves: number;
  sandwichDepth: number;
  sandwichWidth: number;
  blockerProtector: boolean;
  directGroupCount: number;
  obstacleCount: number;
  voidCount: number;
  useOffset: boolean;
  usePartialBlocker: boolean;
  useDecoys: boolean;
}

interface SandwichMotif {
  depth: number;
  width: number;
  preAssigned: Assigned;
  blockerKeys: Set<string>;
  payloadKeys: Set<string>;
  protectedKeys: Set<string>;
  blockerProtectorKeys: Set<string>;
  decoyKeys: Set<string>;
}

type Marker = 'blocker' | 'payload' | 'decoy' | null;
type MarkerBoard = Marker[][];

interface MotifUsage {
  blockerTouched: boolean;
  blockerTouchCount: number;
  payloadTouchedBeforeBlocker: boolean;
  mergedPayloadTouched: boolean;
  decoyTouched: boolean;
}

interface BoardStats {
  groupCount: number;
  largestGroup: number;
  narrowPocketCount: number;
  sealedBasicCount: number;
  isolatedBasicCount: number;
}

interface CandidateResult {
  board: CellData[][];
  solution: Coord[];
  score: number;
}

const DIRS = [[-1, 0], [1, 0], [0, -1], [0, 1]] as const;

function keyOf(r: number, c: number): string {
  return `${r},${c}`;
}

function shuffle<T>(arr: T[]): T[] {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

function makeBasic(colorId = 0): CellData {
  return { colorId, type: 'Basic', protector: 0, isCore: false };
}

function makeObstacle(): CellData {
  return { colorId: 0, type: 'Obstacle', protector: 0, isCore: false };
}

function makeVoid(): CellData {
  return { colorId: 0, type: 'Void', protector: 0, isCore: false };
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function toBoard(grid: CellData[][]): Board {
  return grid.map(row => row.map(cell => ({ ...cell })));
}

function pickSpreadSeeds(positions: Coord[], count: number): Coord[] {
  if (positions.length <= count) return [...positions];
  const pool = shuffle([...positions]);
  const seeds: Coord[] = [pool[0]];
  const remaining = [...pool.slice(1)];
  while (seeds.length < count && remaining.length > 0) {
    let bestIdx = 0, bestDist = -1;
    for (let i = 0; i < remaining.length; i++) {
      const [r, c] = remaining[i];
      let minDist = Infinity;
      for (const [sr, sc] of seeds) {
        const d = Math.abs(r - sr) + Math.abs(c - sc);
        if (d < minDist) minDist = d;
      }
      if (minDist > bestDist) { bestDist = minDist; bestIdx = i; }
    }
    seeds.push(remaining[bestIdx]);
    remaining.splice(bestIdx, 1);
  }
  return seeds;
}

function buildRecipe(config: GeneratorConfig): GeneratorRecipe | null {
  if (config.colorCount < 2 || config.width < 2) return null;

  const difficulty = clamp(Math.floor(config.difficulty), 0, 2);
  const requestedDepth = difficulty === 0 ? 0 : difficulty;
  const sandwichDepth = Math.min(
    requestedDepth,
    Math.floor((config.height - 1) / 2),
    config.colorCount - 1,
  );

  const validCellEstimate = Math.max(1, config.width * config.height - config.obstacleCount - config.voidCount);
  const targetGroupSize = difficulty === 0 ? 6 : difficulty === 1 ? 5 : 4;
  const expectedMoves = clamp(
    Math.round(validCellEstimate / targetGroupSize),
    Math.max(4, sandwichDepth + 3),
    Math.min(24, validCellEstimate),
  );

  const blockerProtector = difficulty >= 2 && sandwichDepth > 0 && Math.random() < 0.35;
  const sandwichMoves = sandwichDepth > 0 ? sandwichDepth + 1 + (blockerProtector ? 1 : 0) : 0;
  const directGroupCount = Math.max(1, expectedMoves - sandwichMoves);
  const sandwichWidth = sandwichDepth > 0
    ? Math.max(2, Math.min(config.width - 1, difficulty === 1 ? 3 : 4))
    : 0;
  const maxOptimalMoves = Math.min(30, Math.max(expectedMoves + 4, config.turnLimit, sandwichMoves + directGroupCount + 2));

  return {
    difficulty,
    searchTurnLimit: maxOptimalMoves,
    minOptimalMoves: Math.max(2, expectedMoves - 3),
    maxOptimalMoves,
    sandwichDepth,
    sandwichWidth,
    blockerProtector,
    directGroupCount,
    obstacleCount: Math.max(0, config.obstacleCount),
    voidCount: Math.max(0, config.voidCount),
    useOffset: difficulty > 0,
    usePartialBlocker: difficulty > 0,
    useDecoys: difficulty > 0,
  };
}

function placeSandwich(
  grid: CellData[][],
  H: number,
  W: number,
  depth: number,
  width: number,
  aColor: number,
  colorCount: number,
  useBlockerProtector: boolean,
  useOffset: boolean,
  usePartialBlocker: boolean,
  useDecoys: boolean,
): SandwichMotif | null {
  if (depth <= 0 || width <= 0) return null;

  const colStart = Math.floor(Math.random() * (W - width + 1));
  const preAssigned: Assigned = new Map();
  const blockerKeys = new Set<string>();
  const payloadKeys = new Set<string>();
  const protectedKeys = new Set<string>();
  const blockerProtectorKeys = new Set<string>();
  const decoyKeys = new Set<string>();
  const layerCols: number[][] = [];
  let hasOffsetPayload = false;

  for (let layer = 0; layer <= 2 * depth; layer++) {
    const isPayload = layer % 2 === 0;
    const shiftRange = Math.max(0, W - width);
    let start = colStart;

    if (useOffset && isPayload && width < W && layer > 0) {
      const starts = [colStart - 1, colStart + 1].filter(s => s >= 0 && s <= shiftRange);
      start = starts.length > 0 ? starts[Math.floor(Math.random() * starts.length)] : colStart;
      hasOffsetPayload = hasOffsetPayload || start !== colStart;
    }

    if (usePartialBlocker && !isPayload && width > 1) {
      const trimLeft = Math.random() < 0.5 ? 1 : 0;
      const trimRight = trimLeft === 1 ? 0 : 1;
      layerCols[layer] = Array.from(
        { length: Math.max(1, width - trimLeft - trimRight) },
        (_, i) => colStart + trimLeft + i,
      );
    } else {
      layerCols[layer] = Array.from({ length: width }, (_, i) => start + i);
    }
  }

  if (useOffset && width < W && !hasOffsetPayload) return null;

  const sharedPayloadCols = new Set<number>(layerCols[0]);
  for (let layer = 2; layer <= 2 * depth; layer += 2) {
    const cols = new Set(layerCols[layer]);
    for (const col of [...sharedPayloadCols]) {
      if (!cols.has(col)) sharedPayloadCols.delete(col);
    }
  }
  if (sharedPayloadCols.size === 0) return null;
  const gateCol = sharedPayloadCols.values().next().value!;
  for (let layer = 1; layer <= 2 * depth; layer += 2) {
    if (!layerCols[layer].some(c => sharedPayloadCols.has(c))) {
      layerCols[layer][0] = gateCol;
    }
  }

  for (let layer = 0; layer <= 2 * depth; layer++) {
    const row = H - 1 - layer;
    const isPayload = layer % 2 === 0;
    const colorId = isPayload
      ? aColor
      : (aColor + 1 + (Math.floor(layer / 2) % (colorCount - 1))) % colorCount;

    for (const c of layerCols[layer]) {
      grid[row][c] = makeBasic(colorId);
      const key = keyOf(row, c);
      preAssigned.set(key, { color: colorId, groupIdx: -(layer + 1) });
      protectedKeys.add(key);
      if (isPayload) {
        payloadKeys.add(key);
      } else {
        blockerKeys.add(key);
        if (useBlockerProtector && (blockerProtectorKeys.size === 0 || Math.random() < 0.45)) {
          grid[row][c].protector = 1;
          blockerProtectorKeys.add(key);
        }
      }
    }
  }

  if (useDecoys) {
    placeSandwichDecoys(
      grid,
      H,
      W,
      colorCount,
      aColor,
      layerCols,
      protectedKeys,
      preAssigned,
      decoyKeys,
    );
  }

  return { depth, width, preAssigned, blockerKeys, payloadKeys, protectedKeys, blockerProtectorKeys, decoyKeys };
}

function placeSandwichDecoys(
  grid: CellData[][],
  H: number,
  W: number,
  colorCount: number,
  payloadColor: number,
  layerCols: number[][],
  protectedKeys: Set<string>,
  preAssigned: Assigned,
  decoyKeys: Set<string>,
): void {
  const rows = [H - 1, Math.max(0, H - 3)];
  const usedCols = new Set(layerCols.flat());
  const side = [...usedCols].reduce((sum, c) => sum + c, 0) / Math.max(1, usedCols.size) < W / 2 ? 1 : -1;
  const anchor = side > 0
    ? Math.min(W - 1, Math.max(...usedCols) + 2)
    : Math.max(0, Math.min(...usedCols) - 2);
  const cols = [anchor, clamp(anchor + side, 0, W - 1)].filter((c, i, arr) => arr.indexOf(c) === i);

  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    if (row < 0 || row >= H) continue;
    for (const col of cols) {
      const key = keyOf(row, col);
      if (protectedKeys.has(key)) continue;
      const adjacentToPayload = DIRS.some(([dr, dc]) => protectedKeys.has(keyOf(row + dr, col + dc)));
      const colorId = i === 0 && !adjacentToPayload
        ? payloadColor
        : (payloadColor + 1 + Math.floor(Math.random() * Math.max(1, colorCount - 1))) % colorCount;
      grid[row][col] = makeBasic(colorId);
      protectedKeys.add(key);
      decoyKeys.add(key);
      preAssigned.set(key, { color: colorId, groupIdx: -100 - decoyKeys.size });
    }
  }
}

function openNeighborCount(grid: CellData[][], r: number, c: number): number {
  let count = 0;
  for (const [dr, dc] of DIRS) {
    const nr = r + dr, nc = c + dc;
    if (nr < 0 || nr >= grid.length || nc < 0 || nc >= (grid[0]?.length ?? 0)) continue;
    if (grid[nr][nc].type !== 'Obstacle' && grid[nr][nc].type !== 'Void') count++;
  }
  return count;
}

function obstacleQualityFails(grid: CellData[][]): boolean {
  let basicCount = 0;
  let narrowPocketCount = 0;

  for (let r = 0; r < grid.length; r++) {
    for (let c = 0; c < (grid[0]?.length ?? 0); c++) {
      if (grid[r][c].type !== 'Basic') continue;
      basicCount++;
      const open = openNeighborCount(grid, r, c);
      if (open === 0) return true;
      if (open === 1) narrowPocketCount++;
    }
  }

  return narrowPocketCount > Math.max(2, Math.floor(basicCount * 0.08));
}

function placeBlockingCells(
  grid: CellData[][],
  candidates: Coord[],
  count: number,
  protectedKeys: Set<string>,
  makeCell: () => CellData,
): boolean {
  if (count <= 0) return true;
  const shuffled = shuffle(candidates.filter(([r, c]) => !protectedKeys.has(keyOf(r, c))));
  let placed = 0;
  let cursor = 0;
  let relaxed = false;

  while (placed < count) {
    if (cursor >= shuffled.length) {
      if (relaxed) return false;
      relaxed = true;
      cursor = 0;
    }

    const [r, c] = shuffled[cursor++];
    if (grid[r][c].type !== 'Basic') continue;

    const prev = grid[r][c];
    grid[r][c] = makeCell();
    const fails = relaxed ? hasSealedBasicCell(grid) : obstacleQualityFails(grid);
    if (fails) {
      grid[r][c] = prev;
      continue;
    }

    placed++;
  }

  return true;
}

function hasSealedBasicCell(grid: CellData[][]): boolean {
  for (let r = 0; r < grid.length; r++) {
    for (let c = 0; c < (grid[0]?.length ?? 0); c++) {
      if (grid[r][c].type === 'Basic' && openNeighborCount(grid, r, c) === 0) return true;
    }
  }
  return false;
}

function assignColorsMultiGroup(
  basicPositions: Coord[],
  grid: CellData[][],
  H: number,
  W: number,
  colorCount: number,
  targetGroupCount: number,
  preAssigned: Assigned = new Map(),
): void {
  const totalBasic = basicPositions.length;
  if (totalBasic === 0) return;

  const actualGroupCount = Math.min(Math.max(1, targetGroupCount), totalBasic);
  const maxGroupSize = Math.max(2, Math.ceil(totalBasic / actualGroupCount));

  const seeds = pickSpreadSeeds(basicPositions, actualGroupCount);
  const seedColors = seeds.map((_, i) => i % colorCount);

  const assigned: Assigned = new Map(preAssigned);
  const queues: Coord[][] = [];
  const sizes: number[] = [];

  for (let g = 0; g < seeds.length; g++) {
    const [r, c] = seeds[g];
    const k = keyOf(r, c);
    assigned.set(k, { color: seedColors[g], groupIdx: g });
    grid[r][c] = { ...grid[r][c], colorId: seedColors[g] };
    queues.push([[r, c]]);
    sizes.push(1);
  }

  let hasMore = true;
  while (hasMore) {
    hasMore = false;
    for (let g = 0; g < seeds.length; g++) {
      const q = queues[g];
      if (q.length === 0 || sizes[g] >= maxGroupSize) continue;
      hasMore = true;
      const [r, c] = q.shift()!;
      for (const [dr, dc] of DIRS) {
        if (sizes[g] >= maxGroupSize) break;
        const nr = r + dr, nc = c + dc;
        if (nr < 0 || nr >= H || nc < 0 || nc >= W) continue;
        const k = keyOf(nr, nc);
        if (assigned.has(k) || grid[nr][nc].type !== 'Basic') continue;

        let blocked = false;
        for (const [dr2, dc2] of DIRS) {
          const ar = nr + dr2, ac = nc + dc2;
          if (ar < 0 || ar >= H || ac < 0 || ac >= W) continue;
          const neighbor = assigned.get(keyOf(ar, ac));
          if (neighbor && neighbor.color === seedColors[g] && neighbor.groupIdx !== g) {
            blocked = true;
            break;
          }
        }
        if (blocked) continue;

        assigned.set(k, { color: seedColors[g], groupIdx: g });
        grid[nr][nc] = { ...grid[nr][nc], colorId: seedColors[g] };
        q.push([nr, nc]);
        sizes[g]++;
      }
    }
  }

  const remainingSet = new Set<string>();
  for (const [r, c] of basicPositions) {
    if (!assigned.has(keyOf(r, c))) remainingSet.add(keyOf(r, c));
  }

  const bfsQueue: Coord[] = [];
  const inQueue = new Set<string>();
  for (const [r, c] of basicPositions) {
    if (!assigned.has(keyOf(r, c))) continue;
    for (const [dr, dc] of DIRS) {
      const nr = r + dr, nc = c + dc;
      const nk = keyOf(nr, nc);
      if (nr >= 0 && nr < H && nc >= 0 && nc < W && remainingSet.has(nk) && !inQueue.has(nk)) {
        bfsQueue.push([nr, nc]);
        inQueue.add(nk);
      }
    }
  }

  while (bfsQueue.length > 0) {
    const [r, c] = bfsQueue.shift()!;
    const k = keyOf(r, c);
    if (!remainingSet.has(k)) continue;

    const adjColors = new Set<number>();
    for (const [dr, dc] of DIRS) {
      const neighbor = assigned.get(keyOf(r + dr, c + dc));
      if (neighbor) adjColors.add(neighbor.color);
    }

    let color = Math.floor(Math.random() * colorCount);
    for (let ci = 0; ci < colorCount; ci++) {
      if (!adjColors.has(ci)) { color = ci; break; }
    }

    assigned.set(k, { color, groupIdx: -1 });
    grid[r][c] = { ...grid[r][c], colorId: color };
    remainingSet.delete(k);

    for (const [dr, dc] of DIRS) {
      const nr = r + dr, nc = c + dc;
      const nk = keyOf(nr, nc);
      if (nr >= 0 && nr < H && nc >= 0 && nc < W && remainingSet.has(nk) && !inQueue.has(nk)) {
        bfsQueue.push([nr, nc]);
        inQueue.add(nk);
      }
    }
  }

  for (const [r, c] of basicPositions) {
    if (!assigned.has(keyOf(r, c))) {
      grid[r][c] = { ...grid[r][c], colorId: Math.floor(Math.random() * colorCount) };
    }
  }

  healIsolatedBasics(basicPositions, grid, H, W, assigned);
}

function healIsolatedBasics(
  basicPositions: Coord[],
  grid: CellData[][],
  H: number,
  W: number,
  assigned: Assigned,
): void {
  for (const [r, c] of basicPositions) {
    let hasSameColor = false, hasBasicNeighbor = false;
    const adjGroupsByColor = new Map<number, Set<number>>();

    for (const [dr, dc] of DIRS) {
      const nr = r + dr, nc = c + dc;
      if (nr < 0 || nr >= H || nc < 0 || nc >= W) continue;
      if (grid[nr][nc].type !== 'Basic') continue;

      hasBasicNeighbor = true;
      const color = grid[nr][nc].colorId;
      if (color === grid[r][c].colorId) {
        hasSameColor = true;
        break;
      }

      const groupIdx = assigned.get(keyOf(nr, nc))?.groupIdx ?? -1;
      if (!adjGroupsByColor.has(color)) adjGroupsByColor.set(color, new Set());
      adjGroupsByColor.get(color)!.add(groupIdx);
    }

    if (!hasBasicNeighbor || hasSameColor) continue;

    let newColor = -1;
    for (const [adjColor, groups] of adjGroupsByColor) {
      if (groups.size === 1) { newColor = adjColor; break; }
    }
    if (newColor === -1) newColor = adjGroupsByColor.keys().next().value!;

    grid[r][c] = { ...grid[r][c], colorId: newColor };
    assigned.set(keyOf(r, c), { color: newColor, groupIdx: -1 });
  }
}

function hasArtificialIsolation(grid: CellData[][]): boolean {
  for (let r = 0; r < grid.length; r++) {
    for (let c = 0; c < (grid[0]?.length ?? 0); c++) {
      if (grid[r][c].type !== 'Basic') continue;
      let basicNeighbor = false, sameColorNeighbor = false;

      for (const [dr, dc] of DIRS) {
        const nr = r + dr, nc = c + dc;
        if (nr < 0 || nr >= grid.length || nc < 0 || nc >= (grid[0]?.length ?? 0)) continue;
        if (grid[nr][nc].type !== 'Basic') continue;
        basicNeighbor = true;
        if (grid[nr][nc].colorId === grid[r][c].colorId) {
          sameColorNeighbor = true;
          break;
        }
      }

      if (basicNeighbor && !sameColorNeighbor) return true;
    }
  }
  return false;
}

function countInitialGroups(grid: CellData[][]): number {
  const board = toBoard(grid);
  const visited = new Set<string>();
  let groups = 0;

  for (let r = 0; r < board.length; r++) {
    for (let c = 0; c < (board[0]?.length ?? 0); c++) {
      const k = keyOf(r, c);
      if (visited.has(k)) continue;
      const cell = board[r][c];
      if (!cell || cell.type !== 'Basic') {
        visited.add(k);
        continue;
      }
      const group = findGroup(board, r, c);
      for (const [gr, gc] of group) visited.add(keyOf(gr, gc));
      if (group.length > 0) groups++;
    }
  }

  return groups;
}

function collectBoardStats(grid: CellData[][]): BoardStats {
  const board = toBoard(grid);
  const visited = new Set<string>();
  let groupCount = 0;
  let largestGroup = 0;
  let narrowPocketCount = 0;
  let sealedBasicCount = 0;
  let isolatedBasicCount = 0;

  for (let r = 0; r < grid.length; r++) {
    for (let c = 0; c < (grid[0]?.length ?? 0); c++) {
      const cell = grid[r][c];
      if (cell.type !== 'Basic') continue;

      const open = openNeighborCount(grid, r, c);
      if (open === 0) sealedBasicCount++;
      if (open === 1) narrowPocketCount++;

      let hasBasicNeighbor = false;
      let hasSameColorNeighbor = false;
      for (const [dr, dc] of DIRS) {
        const nr = r + dr, nc = c + dc;
        if (nr < 0 || nr >= grid.length || nc < 0 || nc >= (grid[0]?.length ?? 0)) continue;
        if (grid[nr][nc].type !== 'Basic') continue;
        hasBasicNeighbor = true;
        if (grid[nr][nc].colorId === cell.colorId) hasSameColorNeighbor = true;
      }
      if (hasBasicNeighbor && !hasSameColorNeighbor) isolatedBasicCount++;

      const key = keyOf(r, c);
      if (visited.has(key)) continue;
      const group = findGroup(board, r, c);
      for (const [gr, gc] of group) visited.add(keyOf(gr, gc));
      if (group.length > 0) {
        groupCount++;
        largestGroup = Math.max(largestGroup, group.length);
      }
    }
  }

  return { groupCount, largestGroup, narrowPocketCount, sealedBasicCount, isolatedBasicCount };
}

function placeProtectors(grid: CellData[][], basicPositions: Coord[], p1Count: number, p2Count: number): void {
  const candidates = basicPositions.filter(([r, c]) => grid[r][c].protector === 0);
  const p2 = Math.min(p2Count, candidates.length);
  const p1 = Math.min(p1Count, candidates.length - p2);
  const scored = candidates
    .map(([r, c]) => {
      const color = grid[r][c].colorId;
      let score = 0;
      for (const [dr, dc] of DIRS) {
        const nr = r + dr, nc = c + dc;
        if (nr >= 0 && nr < grid.length && nc >= 0 && nc < (grid[0]?.length ?? 0) &&
            grid[nr][nc].type === 'Basic' && grid[nr][nc].colorId === color) {
          score++;
        }
      }
      return { r, c, score };
    })
    .sort((a, b) => b.score - a.score);

  for (let i = 0; i < p2; i++) {
    grid[scored[i].r][scored[i].c] = { ...grid[scored[i].r][scored[i].c], protector: 2 };
  }
  for (let i = p2; i < p2 + p1; i++) {
    grid[scored[i].r][scored[i].c] = { ...grid[scored[i].r][scored[i].c], protector: 1 };
  }
}

function placeCores(grid: CellData[][], basicPositions: Coord[], coreCellCount: number): void {
  const coreCandidates = shuffle(basicPositions.filter(([r, c]) => grid[r][c].protector === 0));
  for (let i = 0; i < Math.min(coreCellCount, coreCandidates.length); i++) {
    const [r, c] = coreCandidates[i];
    grid[r][c] = { ...grid[r][c], isCore: true };
  }
}

function cloneMarkers(markers: MarkerBoard): MarkerBoard {
  return markers.map(row => [...row]);
}

function buildMarkerBoard(grid: CellData[][], motif: SandwichMotif): MarkerBoard {
  return grid.map((row, r) =>
    row.map((_, c) => {
      const key = keyOf(r, c);
      if (motif.blockerKeys.has(key)) return 'blocker';
      if (motif.payloadKeys.has(key)) return 'payload';
      if (motif.decoyKeys.has(key)) return 'decoy';
      return null;
    })
  );
}

function parsePortals(portalData?: string): Map<string, Coord> {
  const map = new Map<string, Coord>();
  if (!portalData) return map;

  for (const portal of portalData.split(';')) {
    if (!portal.trim()) continue;
    const parts = portal.split('->');
    if (parts.length !== 2) continue;

    const inlet = parts[0].split(',').map(v => parseInt(v.trim()));
    const outlet = parts[1].split(',').map(v => parseInt(v.trim()));
    if (inlet.length !== 2 || outlet.length !== 2) continue;
    map.set(keyOf(outlet[0], outlet[1]), [inlet[0], inlet[1]]);
  }

  return map;
}

function parseConveyorPaths(conveyorData?: string): Coord[][] {
  if (!conveyorData) return [];

  return conveyorData
    .split(';')
    .map(path => path
      .split('->')
      .map(part => part.split(',').map(v => parseInt(v.trim())))
      .filter(parts => parts.length === 2 && !Number.isNaN(parts[0]) && !Number.isNaN(parts[1]))
      .map(parts => [parts[0], parts[1]] as Coord)
    )
    .filter(path => path.length > 1);
}

function applyRemovalPaired(board: Board, markers: MarkerBoard, group: Coord[]): [Board, MarkerBoard] {
  const b = board.map(row => row.map(cell => cell ? { ...cell } : null));
  const m = cloneMarkers(markers);

  for (const [r, c] of group) {
    const cell = b[r][c];
    if (!cell) continue;
    if (cell.protector > 0) {
      cell.protector = (cell.protector - 1) as 0 | 1 | 2;
    } else {
      b[r][c] = null;
      m[r][c] = null;
    }
  }

  return [b, m];
}

function applyConveyorsPaired(board: Board, markers: MarkerBoard, conveyorData?: string): [Board, MarkerBoard] {
  const b = board.map(row => row.map(cell => cell ? { ...cell } : null));
  const m = cloneMarkers(markers);

  for (const path of parseConveyorPaths(conveyorData)) {
    const lastIdx = path.length - 1;
    const [lastR, lastC] = path[lastIdx];
    const lastCell = b[lastR][lastC] ? { ...b[lastR][lastC]! } : null;
    const lastMarker = m[lastR][lastC];

    for (let i = lastIdx; i > 0; i--) {
      const [toR, toC] = path[i];
      const [fromR, fromC] = path[i - 1];
      b[toR][toC] = b[fromR][fromC];
      m[toR][toC] = m[fromR][fromC];
    }

    const [firstR, firstC] = path[0];
    b[firstR][firstC] = lastCell;
    m[firstR][firstC] = lastMarker;
  }

  return [b, m];
}

function applyGravityPaired(board: Board, markers: MarkerBoard, portalData?: string): [Board, MarkerBoard] {
  const b = board.map(row => row.map(cell => cell ? { ...cell } : null));
  const m = cloneMarkers(markers);
  const rows = b.length;
  const cols = b[0]?.length ?? 0;
  const outletToInlet = parsePortals(portalData);

  const findGravitySource = (r: number, c: number): Coord | null => {
    let currR = r;
    let currC = c;

    while (true) {
      const inlet = outletToInlet.get(keyOf(currR, currC));
      const [nextR, nextC] = inlet ?? [currR - 1, currC];
      if (nextR < 0 || nextR >= rows || nextC < 0 || nextC >= cols) return null;

      const cell = b[nextR][nextC];
      if (cell) {
        if (cell.type === 'Void' || cell.type === 'Obstacle') return null;
        return [nextR, nextC];
      }

      currR = nextR;
      currC = nextC;
    }
  };

  for (let r = rows - 1; r >= 0; r--) {
    for (let c = 0; c < cols; c++) {
      const cell = b[r][c];
      if (cell && (cell.type === 'Void' || cell.type === 'Obstacle')) continue;

      if (cell === null) {
        const source = findGravitySource(r, c);
        if (!source) continue;
        const [sr, sc] = source;
        b[r][c] = b[sr][sc];
        m[r][c] = m[sr][sc];
        b[sr][sc] = null;
        m[sr][sc] = null;
      }
    }
  }

  return [b, m];
}

function rotateMarkers(markers: MarkerBoard): MarkerBoard {
  const rows = markers.length;
  const cols = markers[0]?.length ?? 0;
  const rotated: MarkerBoard = Array.from({ length: rows }, () => Array(cols).fill(null));

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      rotated[rows - 1 - r][cols - 1 - c] = markers[r][c];
    }
  }

  return rotated;
}

function analyzeMotifUsage(
  initialGrid: CellData[][],
  solution: Coord[],
  motif: SandwichMotif | null,
  config: GeneratorConfig,
): MotifUsage {
  if (!motif) {
    return {
      blockerTouched: false,
      blockerTouchCount: 0,
      payloadTouchedBeforeBlocker: false,
      mergedPayloadTouched: false,
      decoyTouched: false,
    };
  }

  let board: Board = toBoard(initialGrid);
  let markers = buildMarkerBoard(initialGrid, motif);
  let blockerTouched = false;
  let payloadTouchedBeforeBlocker = false;
  let mergedPayloadTouched = false;
  let blockerTouchCount = 0;
  let decoyTouched = false;

  for (let moveIndex = 0; moveIndex < solution.length; moveIndex++) {
    const [r, c] = solution[moveIndex];
    const group = findGroup(board, r, c);
    const touchesBlocker = group.some(([gr, gc]) => markers[gr]?.[gc] === 'blocker');
    const touchesPayload = group.some(([gr, gc]) => markers[gr]?.[gc] === 'payload');
    const touchesDecoy = group.some(([gr, gc]) => markers[gr]?.[gc] === 'decoy');

    if (touchesPayload && !blockerTouched) payloadTouchedBeforeBlocker = true;
    if (touchesBlocker) {
      blockerTouched = true;
      blockerTouchCount++;
    }
    if (touchesDecoy) decoyTouched = true;
    if (blockerTouched && touchesPayload && group.length >= motif.width * 2) {
      mergedPayloadTouched = true;
    }

    if (group.length > 0) {
      [board, markers] = applyRemovalPaired(board, markers, group);
      [board, markers] = applyConveyorsPaired(board, markers, config.conveyorData);
      [board, markers] = applyGravityPaired(board, markers, config.portalData);
    }

    const movesMade = moveIndex + 1;
    if (config.rotationInterval && config.rotationInterval > 0 && movesMade % config.rotationInterval === 0) {
      board = rotate180(board);
      markers = rotateMarkers(markers);
      [board, markers] = applyGravityPaired(board, markers, config.portalData);
    }
  }

  return {
    blockerTouched,
    blockerTouchCount,
    payloadTouchedBeforeBlocker,
    mergedPayloadTouched,
    decoyTouched,
  };
}

function scoreCandidate(
  recipe: GeneratorRecipe,
  stats: BoardStats,
  solution: Coord[],
  motif: SandwichMotif | null,
  usage: MotifUsage,
): number {
  const difficulty = recipe.difficulty;
  let score = 1000;

  const idealMoves = (recipe.minOptimalMoves + recipe.maxOptimalMoves) / 2;
  score -= Math.abs(solution.length - idealMoves) * (difficulty === 0 ? 12 : difficulty === 1 ? 20 : 28);
  score += Math.min(solution.length, 24) * (difficulty === 0 ? 5 : difficulty === 1 ? 10 : 14);

  const idealLargest = difficulty === 0 ? 10 : difficulty === 1 ? 7 : 5;
  score -= Math.max(0, stats.largestGroup - idealLargest) * (difficulty === 0 ? 8 : difficulty === 1 ? 18 : 30);
  score -= Math.max(0, recipe.minOptimalMoves - stats.groupCount) * 35;

  score -= stats.sealedBasicCount * 200;
  score -= stats.narrowPocketCount * (difficulty === 0 ? 8 : 16);
  score -= stats.isolatedBasicCount * 24;

  if (motif) {
    score += 60;
    if (motif.decoyKeys.size > 0) score += difficulty === 0 ? 5 : difficulty === 1 ? 35 : 50;
    if (motif.blockerProtectorKeys.size > 0) score += difficulty >= 2 ? 45 : 15;
  }

  if (usage.blockerTouched) score += difficulty === 0 ? 20 : 90;
  if (usage.mergedPayloadTouched) score += difficulty === 0 ? 30 : difficulty === 1 ? 160 : 220;
  if (usage.payloadTouchedBeforeBlocker) score -= difficulty === 0 ? 20 : 100;
  if (usage.decoyTouched) score -= difficulty === 0 ? 10 : difficulty === 1 ? 60 : 110;

  const requiredBlockerTouches = motif && motif.blockerProtectorKeys.size > 0 ? 2 : 1;
  if (motif && usage.blockerTouchCount >= requiredBlockerTouches) {
    score += difficulty >= 2 ? 70 : 30;
  }

  if (difficulty > 0 && motif && !usage.mergedPayloadTouched) score -= difficulty === 1 ? 140 : 220;

  return score;
}

function tryGenerateCandidate(config: GeneratorConfig): CandidateResult | null {
  const recipe = buildRecipe(config);
  if (!recipe) return null;

  const {
    width: W, height: H, colorCount,
    protectorLevel1Count, protectorLevel2Count, coreCellCount,
    star1Ratio, star2Ratio,
  } = config;

  const grid: CellData[][] = Array.from({ length: H }, () =>
    Array.from({ length: W }, () => makeBasic())
  );

  const aColor = Math.floor(Math.random() * colorCount);
  const motif = placeSandwich(
    grid,
    H,
    W,
    recipe.sandwichDepth,
    recipe.sandwichWidth,
    aColor,
    colorCount,
    recipe.blockerProtector,
    recipe.useOffset,
    recipe.usePartialBlocker,
    recipe.useDecoys,
  );

  const allPositions: Coord[] = Array.from({ length: H * W }, (_, i) => [Math.floor(i / W), i % W]);
  const protectedKeys = motif?.protectedKeys ?? new Set<string>();
  const maxBlockingCells = Math.max(0, H * W - protectedKeys.size - 1);
  const voidCells = Math.min(recipe.voidCount, maxBlockingCells);
  if (!placeBlockingCells(grid, allPositions, voidCells, protectedKeys, makeVoid)) {
    return null;
  }
  const obstacleCells = Math.min(recipe.obstacleCount, maxBlockingCells - voidCells);
  if (!placeBlockingCells(grid, allPositions, obstacleCells, protectedKeys, makeObstacle)) {
    return null;
  }

  const basicPositions = allPositions.filter(([r, c]) =>
    grid[r][c].type === 'Basic' && !protectedKeys.has(keyOf(r, c))
  );

  if (basicPositions.length > 0) {
    assignColorsMultiGroup(
      basicPositions,
      grid,
      H,
      W,
      colorCount,
      recipe.directGroupCount,
      motif?.preAssigned,
    );
  }

  if (hasSealedBasicCell(grid)) return null;

  placeProtectors(grid, basicPositions, protectorLevel1Count, protectorLevel2Count);
  placeCores(grid, basicPositions, coreCellCount);

  const board = toBoard(grid);
  const initialValid = countInitialValidCells(board);
  const solution = autoSolveExact(board, recipe.searchTurnLimit, initialValid, star1Ratio, star2Ratio,
    config.portalData, config.conveyorData, config.rotationInterval);
  if (!solution) return null;

  const stats = collectBoardStats(grid);
  if (stats.sealedBasicCount > 0) return null;
  const usage = analyzeMotifUsage(grid, solution, motif, config);
  const score = scoreCandidate(recipe, stats, solution, motif, usage);

  return {
    board: grid,
    solution,
    score,
  };
}

export function generateBoard(config: GeneratorConfig): GenerateResult | null {
  let best: GenerateResult | null = null;
  for (let attempt = 1; attempt <= config.maxAttempts; attempt++) {
    const result = generateBoardAttempt(config, attempt);
    if (result && (!best || result.score > best.score)) best = result;
  }
  return best;
}

export function generateBoardAttempt(config: GeneratorConfig, attempt: number): GenerateResult | null {
  const result = tryGenerateCandidate(config);
  return result ? {
    board: result.board,
    verifiedSolution: result.solution.map(([r, c]) => `${r},${c}`).join(';'),
    attempts: attempt,
    solveLength: result.solution.length,
    score: result.score,
  } : null;
}
