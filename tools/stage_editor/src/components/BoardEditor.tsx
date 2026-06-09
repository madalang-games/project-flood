'use client';

import { useRef } from 'react';
import type { CellData, PaletteColor } from '../types/stage';
import type { Board, StarResult } from '../lib/game-rules';

const CELL_SIZE = 40;

interface Props {
  displayGrid: Board;
  palette: PaletteColor[];
  selectedCell: { r: number; c: number } | null;
  playtestResult: StarResult | null;
  isPlaytest: boolean;
  onLeftClick: (r: number, c: number) => void;
  onRightClick: (r: number, c: number) => void;
  onDragStart?: () => void;
  onImageDrop?: (file: File) => void;
}

// Hyper-casual pixel-art palette
const BOARD_BG = '#1e1e2e';     // board panel background (deep navy)
const SOCKET_COLOR = '#2a2a3e'; // empty cell slot (slightly lighter)
const VOID_COLOR = BOARD_BG;    // void blends into board background

function getCellBg(cell: CellData | null, palette: PaletteColor[]): React.CSSProperties {
  if (!cell) return { backgroundColor: SOCKET_COLOR, boxShadow: 'inset 0 2px 4px rgba(0,0,0,0.45)' };
  if (cell.type === 'Obstacle') return { backgroundColor: '#2a2a3e' };
  if (cell.type === 'Void') return { backgroundColor: VOID_COLOR };
  const p = palette[cell.colorId];
  if (!p) return { backgroundColor: '#555' };
  return { backgroundColor: `rgb(${p.r},${p.g},${p.b})` };
}

function getCellBorder(cell: CellData | null, isSelected: boolean): string {
  if (isSelected) return '2px solid white';
  if (!cell) return '1px solid rgba(0,0,0,0.3)';
  if (cell.type === 'Void') return '1px dashed #2e2e4e';
  if (cell.type === 'Obstacle') return '1px solid #3a3a5e';
  return '1px solid rgba(0,0,0,0.3)';
}

function CellView({
  cell,
  isSelected,
  palette,
  r,
  c,
  onMouseDown,
  onMouseEnter,
}: {
  cell: CellData | null;
  isSelected: boolean;
  palette: PaletteColor[];
  r: number;
  c: number;
  onMouseDown: (r: number, c: number, button: number) => void;
  onMouseEnter: (r: number, c: number) => void;
}) {
  return (
    <div
      style={{
        width: CELL_SIZE,
        height: CELL_SIZE,
        border: getCellBorder(cell, isSelected),
        cursor: 'pointer',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        position: 'relative',
        flexShrink: 0,
        ...getCellBg(cell, palette),
      }}
      onMouseDown={e => { e.preventDefault(); onMouseDown(r, c, e.button); }}
      onMouseEnter={() => onMouseEnter(r, c)}
      onContextMenu={e => e.preventDefault()}
    >
      {cell?.type === 'Obstacle' && (
        <span style={{ fontSize: 18, color: '#5a5a8a', userSelect: 'none' }}>■</span>
      )}
      {cell?.type === 'Void' && (
        <span style={{ fontSize: 10, color: '#333', userSelect: 'none' }}>✕</span>
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
  isPlaytest,
  onLeftClick,
  onRightClick,
  onDragStart,
  onImageDrop,
}: Props) {
  const rows = displayGrid.length;
  const cols = displayGrid[0]?.length ?? 0;
  const isDragging = useRef(false);
  const dragButton = useRef<number>(0);

  const handleMouseDown = (r: number, c: number, button: number) => {
    if (isPlaytest) {
      if (button === 0) onLeftClick(r, c);
      return;
    }
    isDragging.current = true;
    dragButton.current = button;
    onDragStart?.();
    if (button === 0) onLeftClick(r, c);
    else if (button === 2) onRightClick(r, c);
  };

  const handleMouseEnter = (r: number, c: number) => {
    if (!isDragging.current || isPlaytest) return;
    if (dragButton.current === 0) onLeftClick(r, c);
    else if (dragButton.current === 2) onRightClick(r, c);
  };

  const stopDrag = () => { isDragging.current = false; };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (isPlaytest) return;
    const file = e.dataTransfer.files[0];
    if (file && file.type.startsWith('image/')) {
      onImageDrop?.(file);
    }
  };

  return (
    <div
      style={{
        position: 'relative',
        userSelect: 'none',
        backgroundColor: BOARD_BG,
        padding: 8,
        borderRadius: 6,
        boxShadow: '0 4px 24px rgba(0,0,0,0.6)',
      }}
      onMouseUp={stopDrag}
      onMouseLeave={stopDrag}
      onDragOver={handleDragOver}
      onDrop={handleDrop}
    >
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
              onMouseDown={handleMouseDown}
              onMouseEnter={handleMouseEnter}
            />
          ))
        )}
      </div>
    </div>
  );
}
