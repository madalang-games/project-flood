'use client';

import type { CellData, CellType, BrushSettings, PaletteColor } from '../types/stage';

interface Props {
  selectedCell: { r: number; c: number } | null;
  grid: CellData[][];
  brush: BrushSettings;
  palette: PaletteColor[];
  onBrushChange: (b: BrushSettings) => void;
  onCellChange: (r: number, c: number, cell: CellData) => void;
}

export default function CellInspector({
  selectedCell,
  grid,
  brush,
  palette,
  onBrushChange,
  onCellChange,
}: Props) {
  const cell = selectedCell ? grid[selectedCell.r]?.[selectedCell.c] : null;
  const values: BrushSettings = cell
    ? { type: cell.type, colorId: cell.colorId, protector: cell.protector, isCore: cell.isCore }
    : brush;

  function update(patch: Partial<BrushSettings>) {
    const next: BrushSettings = { ...values, ...patch };
    if (next.type === 'Obstacle') {
      next.protector = 0;
      next.isCore = false;
    }
    onBrushChange(next);
    if (selectedCell) {
      onCellChange(selectedCell.r, selectedCell.c, {
        colorId: next.type === 'Obstacle' ? 0 : next.colorId,
        type: next.type,
        protector: next.protector,
        isCore: next.isCore,
      });
    }
  }

  return (
    <div className="flex flex-col h-full">
      <div className="p-2 border-b border-gray-700 flex-shrink-0">
        <span className="text-sm font-semibold text-gray-300">
          {selectedCell ? `Cell (${selectedCell.r},${selectedCell.c})` : 'Brush'}
        </span>
      </div>
      <div className="p-3 flex flex-col gap-3 overflow-y-auto">
        {/* Type */}
        <div>
          <div className="text-xs text-gray-400 mb-1">Type</div>
          <div className="flex gap-1">
            {(['Basic', 'Obstacle'] as CellType[]).map(t => (
              <button
                key={t}
                onClick={() => update({ type: t })}
                className={`text-xs px-2 py-1 rounded border ${
                  values.type === t
                    ? 'bg-blue-600 border-blue-500 text-white'
                    : 'bg-gray-700 border-gray-600 text-gray-300 hover:bg-gray-600'
                }`}
              >
                {t}
              </button>
            ))}
          </div>
        </div>

        {/* Color */}
        {values.type === 'Basic' && (
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
                    values.colorId === p.color_id ? 'border-white' : 'border-transparent'
                  }`}
                />
              ))}
            </div>
          </div>
        )}

        {/* Protector */}
        {values.type === 'Basic' && (
          <div>
            <div className="text-xs text-gray-400 mb-1">Protector</div>
            <div className="flex gap-1">
              {([0, 1, 2] as const).map(p => (
                <button
                  key={p}
                  onClick={() => update({ protector: p })}
                  className={`text-xs px-2 py-1 rounded border ${
                    values.protector === p
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

        {/* Core */}
        {values.type === 'Basic' && (
          <div>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={values.isCore}
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
