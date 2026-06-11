import { NextResponse } from 'next/server';
import * as fs from 'fs';
import * as path from 'path';
import { parseIni } from '../../../lib/ini';

const PROJECT_ROOT = process.env.PROJECT_ROOT ?? path.join(process.cwd(), '..');
const INI_PATH = path.join(PROJECT_ROOT, 'template.ini');

function intVal(raw: string | undefined, fallback: number): number {
  const n = parseInt(raw ?? '');
  return isNaN(n) ? fallback : n;
}

export async function GET() {
  try {
    const ini = parseIni(fs.readFileSync(INI_PATH, 'utf-8'));
    const s = ini['stage-editor-generator'] ?? {};
    return NextResponse.json({
      boardWidth:           intVal(s['board_width'],            6),
      boardHeight:          intVal(s['board_height'],           6),
      turnLimit:            intVal(s['turn_limit'],             20),
      difficulty:           intVal(s['difficulty'],             0),
      colorCount:           intVal(s['color_count'],            3),
      obstacleCount:        intVal(s['obstacle_count'],         0),
      protectorLevel1Count: intVal(s['protector_level1_count'], 0),
      protectorLevel2Count: intVal(s['protector_level2_count'], 0),
      coreCellCount:        intVal(s['core_cell_count'],        0),
      maxAttempts:          intVal(s['max_attempts'],           500),
      difficultyMargin:     intVal(s['difficulty_margin'],      3),
    });
  } catch {
    return NextResponse.json({
      boardWidth: 6, boardHeight: 6, turnLimit: 20, difficulty: 0,
      colorCount: 3, obstacleCount: 0,
      protectorLevel1Count: 0, protectorLevel2Count: 0,
      coreCellCount: 0, maxAttempts: 500, difficultyMargin: 3,
    });
  }
}
