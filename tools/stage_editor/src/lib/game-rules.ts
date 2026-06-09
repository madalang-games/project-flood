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

function parsePortals(portalData?: string): Map<string, [number, number]> {
  const map = new Map<string, [number, number]>();
  if (!portalData) return map;
  const portals = portalData.split(';');
  for (const p of portals) {
    if (!p.trim()) continue;
    const parts = p.split('->');
    if (parts.length === 2) {
      const inletParts = parts[0].split(',');
      const outletParts = parts[1].split(',');
      if (inletParts.length === 2 && outletParts.length === 2) {
        const inR = parseInt(inletParts[0].trim());
        const inC = parseInt(inletParts[1].trim());
        const outR = parseInt(outletParts[0].trim());
        const outC = parseInt(outletParts[1].trim());
        map.set(`${outR},${outC}`, [inR, inC]);
      }
    }
  }
  return map;
}

export function applyGravity(board: Board, portalData?: string): Board {
  const b = cloneBoard(board);
  const rows = b.length;
  const cols = b[0]?.length ?? 0;
  const outletToInlet = parsePortals(portalData);

  const findGravitySource = (r: number, c: number): [number, number] | null => {
    let currR = r;
    let currC = c;
    while (true) {
      let next: [number, number];
      const inlet = outletToInlet.get(`${currR},${currC}`);
      if (inlet) {
        next = inlet;
      } else {
        next = [currR - 1, currC];
      }

      const [nextR, nextC] = next;
      if (nextR < 0 || nextR >= rows || nextC < 0 || nextC >= cols) {
        return null;
      }

      const cell = b[nextR][nextC];
      if (cell) {
        if (cell.type === 'Void' || cell.type === 'Obstacle') {
          return null;
        }
        return next;
      }
      currR = nextR;
      currC = nextC;
    }
  };

  for (let r = rows - 1; r >= 0; r--) {
    for (let c = 0; c < cols; c++) {
      const cell = b[r][c];
      if (cell && cell.type === 'Void') continue;
      if (cell && cell.type === 'Obstacle') continue;

      if (cell === null) {
        const source = findGravitySource(r, c);
        if (source) {
          const [sr, sc] = source;
          b[r][c] = b[sr][sc];
          b[sr][sc] = null;
        }
      }
    }
  }

  return b;
}

export function applyConveyors(board: Board, conveyorData?: string): Board {
  const b = cloneBoard(board);
  if (!conveyorData) return b;

  const paths = conveyorData.split(';');
  for (const path of paths) {
    if (!path.trim()) continue;
    const parts = path.split('->');
    const coords: [number, number][] = [];
    for (const part of parts) {
      const xy = part.split(',');
      if (xy.length === 2) {
        coords.push([parseInt(xy[0].trim()), parseInt(xy[1].trim())]);
      }
    }
    if (coords.length > 1) {
      const lastIdx = coords.length - 1;
      const [lastR, lastC] = coords[lastIdx];
      const lastCell = b[lastR][lastC] ? { ...b[lastR][lastC]! } : null;

      for (let i = lastIdx; i > 0; i--) {
        const [toR, toC] = coords[i];
        const [fromR, fromC] = coords[i - 1];
        b[toR][toC] = b[fromR][fromC];
      }

      const [firstR, firstC] = coords[0];
      b[firstR][firstC] = lastCell;
    }
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
