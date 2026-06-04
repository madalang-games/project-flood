import { decodeCells } from './ctm';
import { findGroup, applyRemoval, applyGravity, evaluate, countInitialValidCells } from './game-rules';
import type { Board } from './game-rules';
import type { StageRow } from '../types/stage';

export interface ValidationResult {
  hasVerifiedSolution: boolean;
  solutionReplaySucceeds: boolean | null;
  warnings: string[];
  canExport: boolean;
}

const DIRS = [[-1, 0], [1, 0], [0, -1], [0, 1]] as const;

export function validate(stage: StageRow): ValidationResult {
  const hasVerifiedSolution = !!stage.verified_solution;
  let solutionReplaySucceeds: boolean | null = null;
  const warnings: string[] = [];

  const grid = decodeCells(stage.cells, stage.board_width, stage.board_height);
  const board: Board = grid;
  const rows = stage.board_height;
  const cols = stage.board_width;

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const cell = board[r]?.[c];
      if (!cell?.isCore) continue;
      const allBlocked = DIRS.every(([dr, dc]) => {
        const nr = r + dr, nc = c + dc;
        if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) return true;
        const n = board[nr]?.[nc];
        return !n || n.type === 'Obstacle';
      });
      if (allBlocked) warnings.push(`Core at (${r},${c}) surrounded by obstacles`);
    }
  }

  if (hasVerifiedSolution) {
    try {
      const moves = stage.verified_solution
        .split(';')
        .map(s => {
          const [r, c] = s.split(',').map(Number);
          return [r, c] as [number, number];
        })
        .filter(([r, c]) => !isNaN(r) && !isNaN(c));

      let b: Board = grid.map(row => row.map(cell => ({ ...cell })));
      const initial = countInitialValidCells(b);

      for (const [r, c] of moves) {
        const group = findGroup(b, r, c);
        if (group.length > 0) {
          b = applyRemoval(b, group);
          b = applyGravity(b);
        }
      }

      const result = evaluate(b, initial, stage.star1_ratio, stage.star2_ratio);
      solutionReplaySucceeds = result.stars === 3;
    } catch {
      solutionReplaySucceeds = false;
    }
  }

  const canExport = hasVerifiedSolution && solutionReplaySucceeds === true;
  return { hasVerifiedSolution, solutionReplaySucceeds, warnings, canExport };
}
