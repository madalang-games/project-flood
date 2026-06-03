import * as fs from 'fs';
import * as path from 'path';
import type { StageRow, PaletteColor } from '../types/stage';

const PROJECT_ROOT = process.env.PROJECT_ROOT ?? path.join(process.cwd(), '..');
const STAGE_CSV = path.join(PROJECT_ROOT, 'shared', 'datas', 'stage', 'stage.csv');
const PALETTE_CSV = path.join(PROJECT_ROOT, 'shared', 'datas', 'common', 'color_palette.csv');

function parseCSVLine(line: string): string[] {
  const fields: string[] = [];
  let i = 0;
  while (i <= line.length) {
    if (i === line.length) { fields.push(''); break; }
    if (line[i] === '"') {
      let field = '';
      i++;
      while (i < line.length) {
        if (line[i] === '"' && line[i + 1] === '"') { field += '"'; i += 2; }
        else if (line[i] === '"') { i++; break; }
        else { field += line[i++]; }
      }
      if (i < line.length && line[i] === ',') i++;
      fields.push(field);
    } else {
      const end = line.indexOf(',', i);
      if (end === -1) { fields.push(line.slice(i)); break; }
      fields.push(line.slice(i, end));
      i = end + 1;
    }
  }
  return fields;
}

function serializeField(f: string): string {
  if (f.includes(',') || f.includes('"') || f.includes('\n')) {
    return '"' + f.replace(/"/g, '""') + '"';
  }
  return f;
}

function serializeCSVLine(fields: string[]): string {
  return fields.map(serializeField).join(',');
}

function rowToStage(fields: string[]): StageRow {
  return {
    stage_id:          parseInt(fields[0]),
    board_width:       parseInt(fields[1]),
    board_height:      parseInt(fields[2]),
    turn_limit:        parseInt(fields[3]),
    difficulty:        parseInt(fields[4]),
    color_ids:         fields[5] ?? '0',
    star1_ratio:       parseFloat(fields[6]),
    star2_ratio:       parseFloat(fields[7]),
    cells:             fields[8] ?? '',
    verified_solution: fields[9] ?? '',
    ruleset_version:   parseInt(fields[10]) || 1,
  };
}

function stageToRow(s: StageRow): string[] {
  return [
    String(s.stage_id),
    String(s.board_width),
    String(s.board_height),
    String(s.turn_limit),
    String(s.difficulty),
    s.color_ids,
    s.star1_ratio.toFixed(2),
    s.star2_ratio.toFixed(2),
    s.cells,
    s.verified_solution,
    String(s.ruleset_version),
  ];
}

function readCSV(csvPath: string): { headers: string[]; data: string[][] } {
  const content = fs.readFileSync(csvPath, 'utf-8');
  const lines = content.split('\n').map(l => l.trimEnd()).filter(l => l.length > 0);
  const headers = lines.slice(0, 4);
  const data = lines.slice(4).map(parseCSVLine);
  return { headers, data };
}

export function readStages(): StageRow[] {
  const { data } = readCSV(STAGE_CSV);
  return data.map(rowToStage);
}

export function writeStages(stages: StageRow[]): void {
  const content = fs.readFileSync(STAGE_CSV, 'utf-8');
  const lines = content.split('\n').map(l => l.trimEnd()).filter(l => l.length > 0);
  const headers = lines.slice(0, 4);
  const dataLines = stages.map(s => serializeCSVLine(stageToRow(s)));
  fs.writeFileSync(STAGE_CSV, [...headers, ...dataLines].join('\n') + '\n', 'utf-8');
}

export function readPalette(): PaletteColor[] {
  const { data } = readCSV(PALETTE_CSV);
  return data.map(f => ({
    color_id: parseInt(f[0]),
    r: parseInt(f[1]),
    g: parseInt(f[2]),
    b: parseInt(f[3]),
    name: f[4] ?? '',
  }));
}
