'use client';

import type { StageRow } from '../types/stage';

const DIFFICULTY_LABELS = ['Easy', 'Normal', 'Hard'];

interface Props {
  stages: StageRow[];
  selectedId: number | null;
  onSelect: (id: number) => void;
  onNew: () => void;
  onDelete: (id: number) => void;
}

export default function StageList({ stages, selectedId, onSelect, onNew, onDelete }: Props) {
  return (
    <div className="flex flex-col h-full">
      <div className="p-2 border-b border-gray-700 flex items-center justify-between flex-shrink-0">
        <span className="text-sm font-semibold text-gray-300">Stages</span>
        <button
          onClick={onNew}
          className="text-xs bg-blue-600 hover:bg-blue-500 px-2 py-1 rounded"
        >
          + New
        </button>
      </div>
      <div className="flex-1 overflow-y-auto">
        {stages.length === 0 && (
          <div className="p-3 text-xs text-gray-500">No stages yet</div>
        )}
        {stages.map(s => (
          <div
            key={s.stage_id}
            className={`flex items-center justify-between px-3 py-2 cursor-pointer hover:bg-gray-700 border-b border-gray-800 ${
              selectedId === s.stage_id ? 'bg-gray-700' : ''
            }`}
            onClick={() => onSelect(s.stage_id)}
          >
            <div>
              <div className="text-sm text-white">Stage {s.stage_id}</div>
              <div className="text-xs text-gray-400">
                {DIFFICULTY_LABELS[s.difficulty] ?? '?'} {s.board_width}×{s.board_height}
              </div>
              {s.verified_solution && (
                <div className="text-xs text-green-500">✓ Verified</div>
              )}
            </div>
            <button
              onClick={e => { e.stopPropagation(); onDelete(s.stage_id); }}
              className="text-gray-600 hover:text-red-400 text-sm ml-2 flex-shrink-0"
            >
              ✕
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
