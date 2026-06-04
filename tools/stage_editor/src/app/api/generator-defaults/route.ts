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
      colorCount:     intVal(s['color_count'],     3),
      obstacleCount:  intVal(s['obstacle_count'],  0),
      protectorCount: intVal(s['protector_count'], 0),
      protectorLevel: Math.max(1, Math.min(2, intVal(s['protector_level'], 1))) as 1 | 2,
      coreCellCount:  intVal(s['core_cell_count'], 0),
    });
  } catch {
    return NextResponse.json({ colorCount: 3, obstacleCount: 0, protectorCount: 0, protectorLevel: 1, coreCellCount: 0 });
  }
}
