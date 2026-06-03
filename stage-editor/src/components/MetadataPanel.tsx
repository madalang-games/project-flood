'use client';

import type { StageMeta } from '../types/stage';

interface Props {
  meta: StageMeta;
  onFieldChange: (key: keyof StageMeta, value: number) => void;
  onResize: (width: number, height: number) => void;
}

const DIFFICULTY_LABELS = ['Easy', 'Normal', 'Hard'];

function NumField({
  label,
  value,
  min,
  max,
  step = 1,
  onChange,
}: {
  label: string;
  value: number;
  min: number;
  max: number;
  step?: number;
  onChange: (v: number) => void;
}) {
  return (
    <label className="flex items-center gap-1 text-xs text-gray-300">
      <span className="w-16 flex-shrink-0">{label}</span>
      <input
        type="number"
        value={value}
        min={min}
        max={max}
        step={step}
        onChange={e => onChange(parseFloat(e.target.value))}
        className="w-16 bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-white text-xs"
      />
    </label>
  );
}

export default function MetadataPanel({ meta, onFieldChange, onResize }: Props) {
  return (
    <div className="p-3 border-t border-gray-700 bg-gray-800 flex flex-wrap gap-3 items-center">
      <NumField
        label="Width"
        value={meta.board_width}
        min={1}
        max={16}
        onChange={v => onResize(Math.round(v), meta.board_height)}
      />
      <NumField
        label="Height"
        value={meta.board_height}
        min={1}
        max={16}
        onChange={v => onResize(meta.board_width, Math.round(v))}
      />
      <NumField
        label="Turns"
        value={meta.turn_limit}
        min={1}
        max={999}
        onChange={v => onFieldChange('turn_limit', Math.round(v))}
      />
      <label className="flex items-center gap-1 text-xs text-gray-300">
        <span className="w-20 flex-shrink-0">Difficulty</span>
        <select
          value={meta.difficulty}
          onChange={e => onFieldChange('difficulty', parseInt(e.target.value))}
          className="bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-white text-xs"
        >
          {DIFFICULTY_LABELS.map((l, i) => (
            <option key={i} value={i}>{l}</option>
          ))}
        </select>
      </label>
      <NumField
        label="Star1"
        value={meta.star1_ratio}
        min={0}
        max={1}
        step={0.01}
        onChange={v => onFieldChange('star1_ratio', v)}
      />
      <NumField
        label="Star2"
        value={meta.star2_ratio}
        min={0}
        max={1}
        step={0.01}
        onChange={v => onFieldChange('star2_ratio', v)}
      />
    </div>
  );
}
