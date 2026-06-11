import * as fs from 'fs';
import * as path from 'path';
import type { StageRow, PaletteColor, ChapterRow } from '../types/stage';

const PROJECT_ROOT = process.env.PROJECT_ROOT ?? path.join(process.cwd(), '..');
const STAGE_CSV = path.join(PROJECT_ROOT, 'shared', 'datas', 'stage', 'stage.csv');
const CHAPTER_CSV = path.join(PROJECT_ROOT, 'shared', 'datas', 'stage', 'chapter.csv');
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
    chapter_id:        parseInt(fields[1]) || 1,
    stage_order:       parseInt(fields[2]) || 1,
    board_width:       parseInt(fields[3]),
    board_height:      parseInt(fields[4]),
    turn_limit:        parseInt(fields[5]),
    difficulty:        parseInt(fields[6]),
    color_ids:         fields[7] ?? '0',
    star1_ratio:       parseFloat(fields[8]),
    star2_ratio:       parseFloat(fields[9]),
    cells:             fields[10] ?? '',
    verified_solution: fields[11] ?? '',
    ruleset_version:   parseInt(fields[12]) || 1,
    reward_group_id:   parseInt(fields[13]) || 0,
    rotation_interval: parseInt(fields[14]) || 0,
    portal_data:       fields[15] ?? '',
    conveyor_data:     fields[16] ?? '',
  };
}

function stageToRow(s: StageRow): string[] {
  return [
    String(s.stage_id),
    String(s.chapter_id ?? 1),
    String(s.stage_order ?? 1),
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
    String(s.reward_group_id ?? 0),
    String(s.rotation_interval ?? 0),
    s.portal_data ?? '',
    s.conveyor_data ?? '',
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

function rowToChapter(fields: string[]): ChapterRow {
  return {
    chapter_id:        parseInt(fields[0]),
    display_order:     parseInt(fields[1]) || 1,
    unlock_chapter_id: fields[2] ? parseInt(fields[2]) : null,
    reward_group_id:   parseInt(fields[3]) || 0,
    bg_theme_id:       parseInt(fields[4]) || 1,
  };
}

function chapterToRow(c: ChapterRow): string[] {
  return [
    String(c.chapter_id),
    String(c.display_order),
    c.unlock_chapter_id != null ? String(c.unlock_chapter_id) : '',
    String(c.reward_group_id),
    String(c.bg_theme_id),
  ];
}

export function readChapters(): ChapterRow[] {
  const { data } = readCSV(CHAPTER_CSV);
  return data.map(rowToChapter);
}

export function writeChapters(chapters: ChapterRow[]): void {
  const content = fs.readFileSync(CHAPTER_CSV, 'utf-8');
  const lines = content.split('\n').map(l => l.trimEnd()).filter(l => l.length > 0);
  const headers = lines.slice(0, 4);
  const dataLines = chapters.map(c => serializeCSVLine(chapterToRow(c)));
  fs.writeFileSync(CHAPTER_CSV, [...headers, ...dataLines].join('\n') + '\n', 'utf-8');
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
