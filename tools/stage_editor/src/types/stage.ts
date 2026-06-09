export type CellType = 'Basic' | 'Obstacle' | 'Void';

export interface CellData {
  colorId: number;
  type: CellType;
  protector: 0 | 1 | 2;
  isCore: boolean;
}

export interface StageRow {
  stage_id: number;
  chapter_id: number;
  stage_order: number;
  board_width: number;
  board_height: number;
  turn_limit: number;
  difficulty: number;
  color_ids: string;
  star1_ratio: number;
  star2_ratio: number;
  cells: string;
  verified_solution: string;
  ruleset_version: number;
  reward_group_id: number;
  rotation_interval: number;
  portal_data: string;
  conveyor_data: string;
}

export interface PaletteColor {
  color_id: number;
  r: number;
  g: number;
  b: number;
  name: string;
}

export interface BrushSettings {
  type: CellType;
  colorId: number;
  protector: 0 | 1 | 2;
  isCore: boolean;
}

export type StageMeta = Omit<StageRow, 'cells' | 'color_ids'>;
