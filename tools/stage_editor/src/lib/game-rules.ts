import type { CellData } from '../types/stage';

export type Board = (CellData | null)[][];

export interface StarResult {
  stars: 0 | 1 | 2 | 3;
  clearanceRatio: number;
  allCleared: boolean;
}

export function cloneBoard(board: Board): Board {
  return board.map(row => row.map(cell => (cell ? { ...cell } : null)));
}

export function findGroup(board: Board, row: number, col: number): [number, number][] {
  const cell = board[row]?.[col];
  if (!cell || cell.type === 'Obstacle' || cell.type === 'Void') return [];

  const rows = board.length;
  const cols = board[0]?.length ?? 0;
  const colorId = cell.colorId;
  const visited = new Set<string>();
  const group: [number, number][] = [];
  const queue: [number, number][] = [[row, col]];

  while (queue.length > 0) {
    const [r, c] = queue.shift()!;
    const key = `${r},${c}`;
    if (visited.has(key)) continue;

    const cur = board[r]?.[c];
    if (!cur || cur.type === 'Obstacle' || cur.type === 'Void' || cur.colorId !== colorId) continue;

    visited.add(key);
    group.push([r, c]);

    for (const [dr, dc] of [[-1, 0], [1, 0], [0, -1], [0, 1]] as const) {
      const nr = r + dr, nc = c + dc;
      if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && !visited.has(`${nr},${nc}`)) {
        queue.push([nr, nc]);
      }
    }
  }
  return group;
}

export function applyRemoval(board: Board, group: [number, number][]): Board {
  const b = cloneBoard(board);
  for (const [r, c] of group) {
    const cell = b[r][c];
    if (!cell) continue;
    if (cell.protector > 0) {
      cell.protector = (cell.protector - 1) as 0 | 1 | 2;
    } else {
      b[r][c] = null;
    }
  }
  return b;
}

export function applyGravity(board: Board): Board {
  const b = cloneBoard(board);
  const rows = b.length;
  const cols = b[0]?.length ?? 0;

  // Mirrors C# GravitySystem: Void cells are fixed segment boundaries.
  for (let c = 0; c < cols; c++) {
    let writeRow = rows - 1;
    for (let r = rows - 1; r >= 0; r--) {
      const cell = b[r][c];
      if (cell?.type === 'Void') {
        while (writeRow > r) b[writeRow--][c] = null;
        writeRow = r - 1;
        continue;
      }
      if (cell !== null) b[writeRow--][c] = cell;
    }
    while (writeRow >= 0) b[writeRow--][c] = null;
  }
  return b;
}

export function rotate180(board: Board): Board {
  const rows = board.length;
  const cols = board[0]?.length ?? 0;
  const b: Board = Array.from({ length: rows }, () => Array(cols).fill(null));
  for (let r = 0; r < rows; r++)
    for (let c = 0; c < cols; c++)
      b[rows - 1 - r][cols - 1 - c] = board[r][c] ? { ...board[r][c]! } : null;
  return b;
}

export function countInitialValidCells(board: Board): number {
  let count = 0;
  for (const row of board) {
    for (const cell of row) {
      if (cell && cell.type !== 'Obstacle' && cell.type !== 'Void') count++;
    }
  }
  return count;
}

export function evaluate(
  board: Board,
  initialValid: number,
  star1: number,
  star2: number,
): StarResult {
  let remaining = 0;
  let coreRemaining = false;

  for (const row of board) {
    for (const cell of row) {
      if (cell && cell.type !== 'Obstacle' && cell.type !== 'Void') {
        remaining++;
        if (cell.isCore) coreRemaining = true;
      }
    }
  }

  if (remaining === 0) {
    return { stars: 3, clearanceRatio: 1, allCleared: true };
  }

  const ratio = initialValid > 0 ? (initialValid - remaining) / initialValid : 0;

  if (coreRemaining || ratio < star1) {
    return { stars: 0, clearanceRatio: ratio, allCleared: false };
  }
  if (ratio >= star2) {
    return { stars: 2, clearanceRatio: ratio, allCleared: false };
  }
  return { stars: 1, clearanceRatio: ratio, allCleared: false };
}
