'use client';

import type { CellType, BrushSettings, PaletteColor } from '../types/stage';

interface Props {
  selectedCell: { r: number; c: number } | null;
  brush: BrushSettings;
  palette: PaletteColor[];
  onBrushChange: (b: BrushSettings) => void;
}

export default function CellInspector({ selectedCell, brush, palette, onBrushChange }: Props) {
  function update(patch: Partial<BrushSettings>) {
    const next: BrushSettings = { ...brush, ...patch };
    if (next.type === 'Obstacle') {
      next.protector = 0;
      next.isCore = false;
    }
    onBrushChange(next);
  }

  return (
    <div className="flex flex-col h-full">
      <div className="p-2 border-b border-gray-700 flex-shrink-0">
        <span className="text-sm font-semibold text-gray-300">
          {selectedCell ? `Cell (${selectedCell.r},${selectedCell.c})` : 'Brush'}
        </span>
      </div>
      <div className="p-3 flex flex-col gap-3 overflow-y-auto">
        <div>
          <div className="text-xs text-gray-400 mb-1">Type</div>
          <div className="flex gap-1">
            {(['Basic', 'Obstacle'] as CellType[]).map(t => (
              <button
                key={t}
                onClick={() => update({ type: t })}
                className={`text-xs px-2 py-1 rounded border ${
                  brush.type === t
                    ? 'bg-blue-600 border-blue-500 text-white'
                    : 'bg-gray-700 border-gray-600 text-gray-300 hover:bg-gray-600'
                }`}
              >
                {t}
              </button>
            ))}
          </div>
        </div>

        {brush.type === 'Basic' && (
          <div>
            <div className="text-xs text-gray-400 mb-1">Color</div>
            <div className="grid grid-cols-4 gap-1">
              {palette.map(p => (
                <button
                  key={p.color_id}
                  title={p.name}
                  onClick={() => update({ colorId: p.color_id })}
                  style={{ backgroundColor: `rgb(${p.r},${p.g},${p.b})` }}
                  className={`w-8 h-8 rounded border-2 ${
                    brush.colorId === p.color_id ? 'border-white' : 'border-transparent'
                  }`}
                />
              ))}
            </div>
          </div>
        )}

        {brush.type === 'Basic' && (
          <div>
            <div className="text-xs text-gray-400 mb-1">Protector</div>
            <div className="flex gap-1">
              {([0, 1, 2] as const).map(p => (
                <button
                  key={p}
                  onClick={() => update({ protector: p })}
                  className={`text-xs px-2 py-1 rounded border ${
                    brush.protector === p
                      ? 'bg-blue-600 border-blue-500 text-white'
                      : 'bg-gray-700 border-gray-600 text-gray-300 hover:bg-gray-600'
                  }`}
                >
                  {p}
                </button>
              ))}
            </div>
          </div>
        )}

        {brush.type === 'Basic' && (
          <div>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={brush.isCore}
                onChange={e => update({ isCore: e.target.checked })}
                className="w-4 h-4"
              />
              <span className="text-xs text-gray-300">Core cell</span>
            </label>
          </div>
        )}
      </div>
    </div>
  );
}
