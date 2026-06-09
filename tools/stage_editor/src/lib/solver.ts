import { cloneBoard, findGroup, applyRemoval, applyGravity, applyConveyors, rotate180, evaluate } from './game-rules';
import type { Board } from './game-rules';

const MAX_BFS_STATES = 5000;

function boardHash(board: Board): string {
  return board.map(row =>
    row.map(cell =>
      cell ? `${cell.colorId}${cell.type === 'Obstacle' ? 'O' : 'B'}${cell.protector}${cell.isCore ? 1 : 0}` : '_'
    ).join('')
  ).join('|');
}

function findAllGroupStarts(board: Board, minSize = 2): [number, number][] {
  const rows = board.length;
  const cols = board[0]?.length ?? 0;
  const visited = new Set<string>();
  const starts: [number, number][] = [];

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const key = `${r},${c}`;
      if (visited.has(key)) continue;
      const cell = board[r][c];
      if (!cell || cell.type === 'Obstacle') { visited.add(key); continue; }
      const group = findGroup(board, r, c);
      group.forEach(([gr, gc]) => visited.add(`${gr},${gc}`));
      if (group.length >= minSize) starts.push([r, c]);
    }
  }
  return starts;
}

function scoreGroup(board: Board, r: number, c: number): number {
  const group = findGroup(board, r, c);
  const hasCore = group.some(([gr, gc]) => board[gr]?.[gc]?.isCore);
  return (hasCore ? 10000 : 0) + group.length;
}

function greedySolve(
  board: Board,
  turnLimit: number,
  initialValid: number,
  star1: number,
  star2: number,
  portalData?: string,
  conveyorData?: string,
  rotationInterval?: number,
): [number, number][] | null {
  let b = cloneBoard(board);
  const moves: [number, number][] = [];

  for (let t = 0; t < turnLimit; t++) {
    let starts = findAllGroupStarts(b, 2);
    if (starts.length === 0) starts = findAllGroupStarts(b, 1);
    if (starts.length === 0) break;

    let bestR = starts[0][0], bestC = starts[0][1];
    let bestScore = scoreGroup(b, bestR, bestC);
    for (const [r, c] of starts.slice(1)) {
      const score = scoreGroup(b, r, c);
      if (score > bestScore) { bestScore = score; bestR = r; bestC = c; }
    }

    moves.push([bestR, bestC]);
    b = applyRemoval(b, findGroup(b, bestR, bestC));
    b = applyConveyors(b, conveyorData);
    b = applyGravity(b, portalData);

    const newMovesCount = moves.length;
    if (rotationInterval && rotationInterval > 0 && newMovesCount % rotationInterval === 0) {
      b = rotate180(b);
      b = applyGravity(b, portalData);
    }

    if (evaluate(b, initialValid, star1, star2).stars === 3) return moves;
  }
  return null;
}

type BFSNode = { board: Board; moves: [number, number][] };

export function autoSolve(
  board: Board,
  turnLimit: number,
  initialValid: number,
  star1: number,
  star2: number,
  portalData?: string,
  conveyorData?: string,
  rotationInterval?: number,
): [number, number][] | null {
  const visited = new Set<string>([boardHash(board)]);
  const queue: BFSNode[] = [{ board: cloneBoard(board), moves: [] }];

  while (queue.length > 0) {
    if (visited.size > MAX_BFS_STATES) {
      // State space too large — fall back to greedy
      return greedySolve(board, turnLimit, initialValid, star1, star2, portalData, conveyorData, rotationInterval);
    }

    const { board: b, moves } = queue.shift()!;
    if (moves.length >= turnLimit) continue;

    let starts = findAllGroupStarts(b, 2);
    if (starts.length === 0) starts = findAllGroupStarts(b, 1);

    for (const [r, c] of starts) {
      let nb = applyRemoval(b, findGroup(b, r, c));
      nb = applyConveyors(nb, conveyorData);
      nb = applyGravity(nb, portalData);

      const newMoves: [number, number][] = [...moves, [r, c]];
      const newMovesCount = newMoves.length;

      if (rotationInterval && rotationInterval > 0 && newMovesCount % rotationInterval === 0) {
        nb = rotate180(nb);
        nb = applyGravity(nb, portalData);
      }

      if (evaluate(nb, initialValid, star1, star2).stars === 3) return newMoves;

      const hash = boardHash(nb);
      if (!visited.has(hash) && newMoves.length < turnLimit) {
        visited.add(hash);
        queue.push({ board: nb, moves: newMoves });
      }
    }
  }

  return null;
}
