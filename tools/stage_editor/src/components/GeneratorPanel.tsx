'use client';

import { useState, useEffect } from 'react';

export interface GeneratorSettings {
  colorCount: number;
  obstacleCount: number;
  protectorCount: number;
  protectorLevel: 1 | 2;
  coreCellCount: number;
}

const FALLBACK: GeneratorSettings = {
  colorCount: 3,
  obstacleCount: 0,
  protectorCount: 0,
  protectorLevel: 1,
  coreCellCount: 0,
};

interface Props {
  boardWidth: number;
  boardHeight: number;
  onGenerate: (settings: GeneratorSettings) => void;
}

export default function GeneratorPanel({ boardWidth, boardHeight, onGenerate }: Props) {
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

  return (
    <div className="p-3 border-t border-gray-700 bg-gray-900">
      <div className="text-xs font-semibold text-gray-300 mb-2">
        ⚙ Generator ({boardWidth}×{boardHeight})
      </div>
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
        <label className="text-gray-400">Protectors</label>
        <input
          type="number" min={0} max={total} value={s.protectorCount}
          onChange={e => set('protectorCount', Math.min(total, Math.max(0, parseInt(e.target.value) || 0)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
        <label className="text-gray-400">Protector lv</label>
        <select
          value={s.protectorLevel}
          onChange={e => setS(prev => ({ ...prev, protectorLevel: parseInt(e.target.value) as 1 | 2 }))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        >
          <option value={1}>1</option>
          <option value={2}>2</option>
        </select>
        <label className="text-gray-400">Core cells</label>
        <input
          type="number" min={0} max={total} value={s.coreCellCount}
          onChange={e => set('coreCellCount', Math.min(total, Math.max(0, parseInt(e.target.value) || 0)))}
          className="bg-gray-700 text-white px-1 py-0.5 rounded w-full"
        />
      </div>
      <button
        onClick={() => onGenerate(s)}
        className="mt-2 w-full text-xs bg-purple-700 hover:bg-purple-600 px-3 py-1.5 rounded"
      >
        Generate
      </button>
    </div>
  );
}
