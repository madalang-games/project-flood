import type { CellData } from '../types/stage';

export function decodeCTM(hex3: string): CellData {
  const c = parseInt(hex3[0], 16);
  const t = parseInt(hex3[1], 16);
  const m = parseInt(hex3[2], 16);
  const type = t === 0 ? 'Basic' : t === 2 ? 'Void' : 'Obstacle';
  return {
    colorId: c,
    type,
    protector: (m & 0x3) as 0 | 1 | 2,
    isCore: (m & 0x4) !== 0,
  };
}

export function encodeCTM(cell: CellData): string {
  const c = cell.colorId.toString(16).toUpperCase();
  const t = cell.type === 'Basic' ? '0' : cell.type === 'Void' ? '2' : '1';
  const m = (cell.protector & 0x3) | (cell.isCore ? 0x4 : 0x0);
  return c + t + m.toString(16).toUpperCase();
}

export function decodeCells(cellsStr: string, width: number, height: number): CellData[][] {
  const grid: CellData[][] = [];
  for (let r = 0; r < height; r++) {
    grid[r] = [];
    for (let c = 0; c < width; c++) {
      const i = r * width + c;
      const chunk = cellsStr.slice(i * 3, i * 3 + 3);
      grid[r][c] = chunk.length === 3 ? decodeCTM(chunk) : { colorId: 0, type: 'Basic', protector: 0, isCore: false };
    }
  }
  return grid;
}

export function encodeCells(grid: CellData[][]): string {
  return grid.map(row => row.map(encodeCTM).join('')).join('');
}

export function deriveColorIds(grid: CellData[][]): string {
  const ids = new Set<number>();
  for (const row of grid) {
    for (const cell of row) {
      if (cell.type !== 'Obstacle' && cell.type !== 'Void') ids.add(cell.colorId);
    }
  }
  const sorted = Array.from(ids).sort((a, b) => a - b);
  return sorted.length > 0 ? sorted.join(',') : '0';
}
