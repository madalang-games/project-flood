'use client';

import { useState, useEffect, useCallback } from 'react';
import type { StageRow, PaletteColor, CellData, BrushSettings, StageMeta } from '../types/stage';
import type { Board, StarResult } from '../lib/game-rules';
import { decodeCells, encodeCells, deriveColorIds } from '../lib/ctm';
import {
  findGroup,
  applyRemoval,
  applyGravity,
  evaluate,
  countInitialValidCells,
} from '../lib/game-rules';
import { validate } from '../lib/validator';
import type { ValidationResult } from '../lib/validator';
import StageList from '../components/StageList';
import BoardEditor from '../components/BoardEditor';
import CellInspector from '../components/CellInspector';
import MetadataPanel from '../components/MetadataPanel';
import PlaytestPanel from '../components/PlaytestPanel';

type PlaytestState = {
  board: Board;
  turns: number;
  initialValid: number;
  moves: [number, number][];
  isRecording: boolean;
  result: StarResult | null;
};

function makeDefaultGrid(w: number, h: number): CellData[][] {
  return Array.from({ length: h }, () =>
    Array.from({ length: w }, () => ({ colorId: 0, type: 'Basic' as const, protector: 0 as const, isCore: false }))
  );
}

export default function EditorPage() {
  const [stages, setStages] = useState<StageRow[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [palette, setPalette] = useState<PaletteColor[]>([]);
  const [grid, setGrid] = useState<CellData[][]>([]);
  const [meta, setMeta] = useState<StageMeta | null>(null);
  const [brush, setBrush] = useState<BrushSettings>({ type: 'Basic', colorId: 0, protector: 0, isCore: false });
  const [selectedCell, setSelectedCell] = useState<{ r: number; c: number } | null>(null);
  const [playtestState, setPlaytestState] = useState<PlaytestState | null>(null);
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);

  useEffect(() => {
    fetch('/api/stages').then(r => r.json()).then(setStages).catch(console.error);
    fetch('/api/palette').then(r => r.json()).then(setPalette).catch(console.error);
  }, []);

  const loadStage = useCallback((stage: StageRow) => {
    setSelectedId(stage.stage_id);
    setGrid(decodeCells(stage.cells, stage.board_width, stage.board_height));
    setMeta({
      stage_id: stage.stage_id,
      board_width: stage.board_width,
      board_height: stage.board_height,
      turn_limit: stage.turn_limit,
      difficulty: stage.difficulty,
      star1_ratio: stage.star1_ratio,
      star2_ratio: stage.star2_ratio,
      verified_solution: stage.verified_solution,
      ruleset_version: stage.ruleset_version,
    });
    setPlaytestState(null);
    setValidationResult(null);
    setSelectedCell(null);
  }, []);

  const handleSelect = useCallback((id: number) => {
    const stage = stages.find(s => s.stage_id === id);
    if (stage) loadStage(stage);
  }, [stages, loadStage]);

  const handleNew = useCallback(async () => {
    const w = 4, h = 4;
    const cells = '000'.repeat(w * h);
    const payload = {
      board_width: w, board_height: h, turn_limit: 20, difficulty: 1,
      color_ids: '0', star1_ratio: 0.80, star2_ratio: 0.90,
      cells, verified_solution: '', ruleset_version: 1,
    };
    const res = await fetch('/api/stages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    const created: StageRow = await res.json();
    setStages(prev => [...prev, created]);
    loadStage(created);
  }, [loadStage]);

  const handleDelete = useCallback(async (id: number) => {
    await fetch(`/api/stages/${id}`, { method: 'DELETE' });
    setStages(prev => prev.filter(s => s.stage_id !== id));
    if (selectedId === id) {
      setSelectedId(null);
      setGrid([]);
      setMeta(null);
      setPlaytestState(null);
    }
  }, [selectedId]);

  const handleLeftClick = useCallback((r: number, c: number) => {
    if (playtestState) {
      if (playtestState.result) return;
      const group = findGroup(playtestState.board, r, c);
      if (group.length === 0) return;

      let b = applyRemoval(playtestState.board, group);
      b = applyGravity(b);
      const turns = playtestState.turns - 1;
      const result = evaluate(b, playtestState.initialValid, meta!.star1_ratio, meta!.star2_ratio);
      const isEnd = turns === 0 || result.stars === 3;
      const newMoves = playtestState.isRecording
        ? [...playtestState.moves, [r, c] as [number, number]]
        : playtestState.moves;

      if (isEnd && result.stars >= 1 && playtestState.isRecording) {
        const solutionStr = newMoves.map(([mr, mc]) => `${mr},${mc}`).join(';');
        setMeta(prev => prev ? { ...prev, verified_solution: solutionStr } : prev);
      }

      setPlaytestState(prev => prev ? {
        ...prev,
        board: b,
        turns,
        moves: newMoves,
        result: isEnd ? result : null,
      } : null);
    } else {
      const cell: CellData = {
        colorId: brush.type === 'Obstacle' ? 0 : brush.colorId,
        type: brush.type,
        protector: brush.type === 'Obstacle' ? 0 : brush.protector,
        isCore: brush.type === 'Obstacle' ? false : brush.isCore,
      };
      setGrid(prev => {
        const next = prev.map(row => [...row]);
        next[r][c] = cell;
        return next;
      });
      setSelectedCell({ r, c });
    }
  }, [playtestState, brush, meta]);

  const handleRightClick = useCallback((r: number, c: number) => {
    if (playtestState) return;
    setGrid(prev => {
      const next = prev.map(row => [...row]);
      next[r][c] = { colorId: 0, type: 'Obstacle', protector: 0, isCore: false };
      return next;
    });
  }, [playtestState]);

  const handleCellChange = useCallback((r: number, c: number, cell: CellData) => {
    setGrid(prev => {
      const next = prev.map(row => [...row]);
      next[r][c] = cell;
      return next;
    });
  }, []);

  const handleFieldChange = useCallback((key: keyof StageMeta, value: number) => {
    setMeta(prev => prev ? { ...prev, [key]: value } : prev);
  }, []);

  const handleResize = useCallback((w: number, h: number) => {
    setGrid(prev => {
      const next: CellData[][] = [];
      for (let r = 0; r < h; r++) {
        next[r] = [];
        for (let c = 0; c < w; c++) {
          next[r][c] = prev[r]?.[c] ?? { colorId: 0, type: 'Basic', protector: 0, isCore: false };
        }
      }
      return next;
    });
    setMeta(prev => prev ? { ...prev, board_width: w, board_height: h } : prev);
  }, []);

  const buildStageRow = useCallback((): StageRow | null => {
    if (!meta) return null;
    return { ...meta, cells: encodeCells(grid), color_ids: deriveColorIds(grid) };
  }, [meta, grid]);

  const handleValidate = useCallback(() => {
    const stage = buildStageRow();
    if (!stage) return;
    setValidationResult(validate(stage));
  }, [buildStageRow]);

  const handleExport = useCallback(async () => {
    const stage = buildStageRow();
    if (!stage || !selectedId) return;
    const vr = validate(stage);
    setValidationResult(vr);
    if (!vr.canExport) return;
    await fetch(`/api/stages/${selectedId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(stage),
    });
    setStages(prev => prev.map(s => s.stage_id === selectedId ? stage : s));
  }, [buildStageRow, selectedId]);

  const handleSave = useCallback(async () => {
    const stage = buildStageRow();
    if (!stage || !selectedId) return;
    await fetch(`/api/stages/${selectedId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(stage),
    });
    setStages(prev => prev.map(s => s.stage_id === selectedId ? stage : s));
  }, [buildStageRow, selectedId]);

  const displayGrid: Board = playtestState ? playtestState.board : grid;

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Stage List */}
      <div className="w-44 flex-shrink-0 border-r border-gray-700 bg-gray-900 flex flex-col">
        <StageList
          stages={stages}
          selectedId={selectedId}
          onSelect={handleSelect}
          onNew={handleNew}
          onDelete={handleDelete}
        />
      </div>

      {/* Center */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {meta ? (
          <>
            <div className="flex-1 flex items-center justify-center overflow-auto p-4">
              <BoardEditor
                displayGrid={displayGrid}
                palette={palette}
                selectedCell={selectedCell}
                playtestResult={playtestState?.result ?? null}
                onLeftClick={handleLeftClick}
                onRightClick={handleRightClick}
              />
            </div>
            <div className="flex-shrink-0">
              <MetadataPanel
                meta={meta}
                onFieldChange={handleFieldChange}
                onResize={handleResize}
              />
              <PlaytestPanel
                isPlaytest={!!playtestState}
                isRecording={playtestState?.isRecording ?? false}
                playtestTurns={playtestState?.turns ?? 0}
                playtestResult={playtestState?.result ?? null}
                validationResult={validationResult}
                onStartPlaytest={() => {
                  const board: Board = grid.map(row => row.map(cell => ({ ...cell })));
                  setPlaytestState({
                    board,
                    turns: meta.turn_limit,
                    initialValid: countInitialValidCells(board),
                    moves: [],
                    isRecording: false,
                    result: null,
                  });
                }}
                onStopPlaytest={() => setPlaytestState(null)}
                onToggleRecord={() =>
                  setPlaytestState(prev => prev ? { ...prev, isRecording: !prev.isRecording } : prev)
                }
                onValidate={handleValidate}
                onExport={handleExport}
                onSave={handleSave}
              />
            </div>
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center text-gray-500 text-sm">
            Select or create a stage
          </div>
        )}
      </div>

      {/* Cell Inspector */}
      <div className="w-48 flex-shrink-0 border-l border-gray-700 bg-gray-900 flex flex-col">
        <CellInspector
          selectedCell={playtestState ? null : selectedCell}
          grid={grid}
          brush={brush}
          palette={palette}
          onBrushChange={setBrush}
          onCellChange={handleCellChange}
        />
      </div>
    </div>
  );
}
