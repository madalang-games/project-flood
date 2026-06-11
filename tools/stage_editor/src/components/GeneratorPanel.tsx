'use client';

import { useState, useEffect } from 'react';
import type { GeneratorSettings } from '../lib/generator';

export type { GeneratorSettings };

export type GeneratorStatus = 'idle' | 'running' | 'success' | 'failed';

const DIFFICULTY_LABELS = ['Easy', 'Normal', 'Hard'];
export const DIFFICULTY_REWARD: Record<number, number> = { 0: 2001, 1: 2002, 2: 2003 };

const FALLBACK: GeneratorSettings = {
  colorCount: 3,
  obstacleCount: 0,
  protectorLevel1Count: 0,
  protectorLevel2Count: 0,
  coreCellCount: 0,
  maxAttempts: 500,
  difficultyMargin: 3,
};

interface MetaPatch {
  difficulty?: number;
  turn_limit?: number;
  reward_group_id?: number;
}

interface Props {
  boardWidth: number;
  boardHeight: number;
  metaDifficulty: number;
  metaTurnLimit: number;
  onGenerate: (settings: GeneratorSettings) => void;
  onMetaChange: (patch: MetaPatch) => void;
  status: GeneratorStatus;
  info: { attempts: number; solveLength: number } | null;
}

export default function GeneratorPanel({
  boardWidth, boardHeight,
  metaDifficulty, metaTurnLimit,
  onGenerate, onMetaChange,
  status, info,
}: Props) {
  const [s, setS] = useState<GeneratorSettings>(FALLBACK);
  const [loading, setLoading] = useState(true);
  const total = boardWidth * boardHeight;

  useEffect(() => {
    fetch('/api/generator-defaults')
      .then(r => r.json())
      .then((defaults: GeneratorSettings) => setS(defaults))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const set = (key: keyof GeneratorSettings, value: number) =>
    setS(prev => ({ ...prev, [key]: value }));

  if (loading) {
    return (
      <div className="p-3 border-t border-gray-700 bg-gray-900 text-xs text-gray-500">
        Loading generator defaults…
      </div>
    );
  }

  const statusEl =
    status === 'running' ? (
      <span className="text-yellow-400">Generating…</span>
    ) : status === 'success' && info ? (
      <span className="text-green-400">✓ {info.solveLength} moves (attempt {info.attempts})</span>
    ) : status === 'failed' ? (
      <span className="text-red-400">✗ No solution found</span>
    ) : null;

  return (
    <div className="p-3 border-t border-gray-700 bg-gray-900">
      <div className="text-xs font-semibold text-gray-300 mb-2">
        ⚙ Generator ({boardWidth}×{boardHeight})
      </div>

      {/* Stage data — live sync with meta */}
      <div className="grid grid-cols-2 gap-x-3 gap-y-1 text-xs items-center mb-2 pb-2 border-b border-gray-700">
        <label className="text-gray-400">Turns</label>
        <input
          type="number" min={1} max={999} value={metaTurnLimit}
          onChange={e => onMetaChange({ turn_limit: Math.max(1, parseInt(e.target.value) || 1) })}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Difficulty</label>
        <select
          value={metaDifficulty}
          onChange={e => {
            const diff = parseInt(e.target.value);
            onMetaChange({ difficulty: diff, reward_group_id: DIFFICULTY_REWARD[diff] ?? 2001 });
          }}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        >
          {DIFFICULTY_LABELS.map((l, i) => (
            <option key={i} value={i}>{l}</option>
          ))}
        </select>
      </div>

      {/* Generator algorithm settings */}
      <div className="grid grid-cols-2 gap-x-3 gap-y-1 text-xs items-center">
        <label className="text-gray-400">Colors (1–6)</label>
        <input
          type="number" min={1} max={6} value={s.colorCount}
          onChange={e => set('colorCount', Math.min(6, Math.max(1, parseInt(e.target.value) || 1)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Obstacles</label>
        <input
          type="number" min={0} max={total - 1} value={s.obstacleCount}
          onChange={e => set('obstacleCount', Math.min(total - 1, Math.max(0, parseInt(e.target.value) || 0)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Protector Lv1</label>
        <input
          type="number" min={0} max={total} value={s.protectorLevel1Count}
          onChange={e => set('protectorLevel1Count', Math.min(total, Math.max(0, parseInt(e.target.value) || 0)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Protector Lv2</label>
        <input
          type="number" min={0} max={total} value={s.protectorLevel2Count}
          onChange={e => set('protectorLevel2Count', Math.min(total, Math.max(0, parseInt(e.target.value) || 0)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Core cells</label>
        <input
          type="number" min={0} max={total} value={s.coreCellCount}
          onChange={e => set('coreCellCount', Math.min(total, Math.max(0, parseInt(e.target.value) || 0)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Margin (turns)</label>
        <input
          type="number" min={1} max={Math.max(1, metaTurnLimit - 1)} value={s.difficultyMargin}
          onChange={e => set('difficultyMargin', Math.min(metaTurnLimit - 1, Math.max(1, parseInt(e.target.value) || 1)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Max attempts</label>
        <input
          type="number" min={1} max={2000} value={s.maxAttempts}
          onChange={e => set('maxAttempts', Math.min(2000, Math.max(1, parseInt(e.target.value) || 500)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
      </div>
      {statusEl && <div className="mt-1 text-xs">{statusEl}</div>}
      <button
        onClick={() => onGenerate(s)}
        disabled={status === 'running'}
        className="mt-2 w-full text-xs bg-purple-700 hover:bg-purple-600 disabled:opacity-50 px-3 py-1.5 rounded"
      >
        {status === 'running' ? 'Generating…' : 'Generate'}
      </button>
    </div>
  );
}
