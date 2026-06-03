'use client';

import type { CellData, PaletteColor } from '../types/stage';
import type { Board, StarResult } from '../lib/game-rules';

const CELL_SIZE = 40;

interface Props {
  displayGrid: Board;
  palette: PaletteColor[];
  selectedCell: { r: number; c: number } | null;
  playtestResult: StarResult | null;
  onLeftClick: (r: number, c: number) => void;
  onRightClick: (r: number, c: number) => void;
}

function getCellStyle(cell: CellData | null, palette: PaletteColor[]): React.CSSProperties {
  if (!cell) return { backgroundColor: '#111' };
  if (cell.type === 'Obstacle') return { backgroundColor: '#2a2a2a' };
  const p = palette[cell.colorId];
  if (!p) return { backgroundColor: '#555' };
  return { backgroundColor: `rgb(${p.r},${p.g},${p.b})` };
}

function CellView({
  cell,
  isSelected,
  palette,
  r,
  c,
  onLeftClick,
  onRightClick,
}: {
  cell: CellData | null;
  isSelected: boolean;
  palette: PaletteColor[];
  r: number;
  c: number;
  onLeftClick: (r: number, c: number) => void;
  onRightClick: (r: number, c: number) => void;
}) {
  return (
    <div
      style={{
        width: CELL_SIZE,
        height: CELL_SIZE,
        border: isSelected ? '2px solid white' : '1px solid rgba(0,0,0,0.35)',
        cursor: 'pointer',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        position: 'relative',
        flexShrink: 0,
        ...getCellStyle(cell, palette),
      }}
      onClick={() => onLeftClick(r, c)}
      onContextMenu={e => { e.preventDefault(); onRightClick(r, c); }}
    >
      {cell?.type === 'Obstacle' && (
        <span style={{ fontSize: 18, color: '#555', userSelect: 'none' }}>■</span>
      )}
      {cell?.isCore && (
        <span style={{ position: 'absolute', top: 1, right: 2, fontSize: 9, color: 'gold', userSelect: 'none' }}>★</span>
      )}
      {cell && cell.protector > 0 && (
        <span style={{ position: 'absolute', bottom: 1, left: 2, fontSize: 9, color: 'rgba(255,255,255,0.85)', userSelect: 'none' }}>
          {'◈'.repeat(cell.protector)}
        </span>
      )}
    </div>
  );
}

export default function BoardEditor({
  displayGrid,
  palette,
  selectedCell,
  playtestResult,
  onLeftClick,
  onRightClick,
}: Props) {
  const rows = displayGrid.length;
  const cols = displayGrid[0]?.length ?? 0;

  return (
    <div style={{ position: 'relative' }}>
      {playtestResult && (
        <div
          style={{
            position: 'absolute',
            inset: 0,
            backgroundColor: 'rgba(0,0,0,0.75)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 10,
            borderRadius: 4,
          }}
        >
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: 48 }}>
              {playtestResult.stars === 0 ? '✗' : '★'.repeat(playtestResult.stars)}
            </div>
            <div style={{ fontSize: 20, marginTop: 8 }}>
              {playtestResult.stars === 0 ? 'FAIL' : `${playtestResult.stars} Star${playtestResult.stars !== 1 ? 's' : ''}`}
            </div>
            <div style={{ fontSize: 13, color: '#aaa', marginTop: 4 }}>
              {(playtestResult.clearanceRatio * 100).toFixed(1)}% cleared
            </div>
          </div>
        </div>
      )}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: `repeat(${cols}, ${CELL_SIZE}px)`,
          gridTemplateRows: `repeat(${rows}, ${CELL_SIZE}px)`,
        }}
      >
        {displayGrid.map((row, r) =>
          row.map((cell, c) => (
            <CellView
              key={`${r},${c}`}
              cell={cell}
              isSelected={selectedCell?.r === r && selectedCell?.c === c}
              palette={palette}
              r={r}
              c={c}
              onLeftClick={onLeftClick}
              onRightClick={onRightClick}
            />
          ))
        )}
      </div>
    </div>
  );
}
