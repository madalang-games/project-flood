'use client';

import { useState, useEffect, useCallback } from 'react';
import type { StageRow, PaletteColor, CellData, BrushSettings, StageMeta, ChapterRow } from '../types/stage';
import type { Board, StarResult } from '../lib/game-rules';
import { decodeCells, encodeCells, deriveColorIds } from '../lib/ctm';
import {
  findGroup,
  applyRemoval,
  applyGravity,
  applyConveyors,
  rotate180,
  evaluate,
  countInitialValidCells,
} from '../lib/game-rules';
import { validate } from '../lib/validator';
import type { ValidationResult } from '../lib/validator';
import type { GenerateResult, GeneratorSettings } from '../lib/generator';
import StageList from '../components/StageList';
import ChapterPanel from '../components/ChapterPanel';
import BoardEditor from '../components/BoardEditor';
import CellInspector from '../components/CellInspector';
import MetadataPanel from '../components/MetadataPanel';
import PlaytestPanel from '../components/PlaytestPanel';
import GeneratorPanel from '../components/GeneratorPanel';
import type { GeneratorStatus } from '../components/GeneratorPanel';
import { DIFFICULTY_REWARD } from '../components/GeneratorPanel';

type PlaytestState = {
  board: Board;
  turns: number;
  initialValid: number;
  moves: [number, number][];
  isRecording: boolean;
  result: StarResult | null;
};

type SimulateState = {
  states: Board[];
  taps: [number, number][];
  stepIndex: number;
};

type HistoryEntry = { grid: CellData[][], width: number, height: number };

function makeDefaultGrid(w: number, h: number): CellData[][] {
  return Array.from({ length: h }, () =>
    Array.from({ length: w }, () => ({ colorId: 0, type: 'Basic' as const, protector: 0 as const, isCore: false }))
  );
}

// RGB to LAB color mapping helper
function rgbToLab(r: number, g: number, b: number): [number, number, number] {
  let rNorm = r / 255;
  let gNorm = g / 255;
  let bNorm = b / 255;

  rNorm = rNorm > 0.04045 ? Math.pow((rNorm + 0.055) / 1.055, 2.4) : rNorm / 12.92;
  gNorm = gNorm > 0.04045 ? Math.pow((gNorm + 0.055) / 1.055, 2.4) : gNorm / 12.92;
  bNorm = bNorm > 0.04045 ? Math.pow((bNorm + 0.055) / 1.055, 2.4) : bNorm / 12.92;

  rNorm *= 100;
  gNorm *= 100;
  bNorm *= 100;

  const x = rNorm * 0.4124 + gNorm * 0.3576 + bNorm * 0.1805;
  const y = rNorm * 0.2126 + gNorm * 0.7152 + bNorm * 0.0722;
  const z = rNorm * 0.0193 + gNorm * 0.1192 + bNorm * 0.9505;

  let xNorm = x / 95.047;
  let yNorm = y / 100.000;
  let zNorm = z / 108.883;

  xNorm = xNorm > 0.008856 ? Math.pow(xNorm, 1/3) : (7.787 * xNorm) + (16 / 116);
  yNorm = yNorm > 0.008856 ? Math.pow(yNorm, 1/3) : (7.787 * yNorm) + (16 / 116);
  zNorm = zNorm > 0.008856 ? Math.pow(zNorm, 1/3) : (7.787 * zNorm) + (16 / 116);

  const L = (116 * yNorm) - 16;
  const a = 500 * (xNorm - yNorm);
  const bVal = 200 * (yNorm - zNorm);

  return [L, a, bVal];
}

function colorDistLab(lab1: [number, number, number], lab2: [number, number, number]): number {
  return Math.sqrt(
    Math.pow(lab1[0] - lab2[0], 2) +
    Math.pow(lab1[1] - lab2[1], 2) +
    Math.pow(lab1[2] - lab2[2], 2)
  );
}

export default function EditorPage() {
  const [stages, setStages] = useState<StageRow[]>([]);
  const [chapters, setChapters] = useState<ChapterRow[]>([]);
  const [selectedChapterId, setSelectedChapterId] = useState<number | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [palette, setPalette] = useState<PaletteColor[]>([]);
  const [grid, setGrid] = useState<CellData[][]>([]);
  const [meta, setMeta] = useState<StageMeta | null>(null);
  const [brush, setBrush] = useState<BrushSettings>({ type: 'Basic', colorId: 0, protector: 0, isCore: false });
  const [selectedCell, setSelectedCell] = useState<{ r: number; c: number } | null>(null);
  const [playtestState, setPlaytestState] = useState<PlaytestState | null>(null);
  const [simulateState, setSimulateState] = useState<SimulateState | null>(null);
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);
  const [showGenerator, setShowGenerator] = useState(false);
  const [generatorStatus, setGeneratorStatus] = useState<GeneratorStatus>('idle');
  const [generatorInfo, setGeneratorInfo] = useState<{ attempts: number; solveLength: number; score?: number } | null>(null);
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [redoHistory, setRedoHistory] = useState<HistoryEntry[]>([]);

  useEffect(() => {
    fetch('/api/stages').then(r => r.json()).then(setStages).catch(console.error);
    fetch('/api/palette').then(r => r.json()).then(setPalette).catch(console.error);
    fetch('/api/chapters').then(r => r.json()).then((chs: ChapterRow[]) => {
      setChapters(chs);
      if (chs.length > 0) setSelectedChapterId(chs[0].chapter_id);
    }).catch(console.error);
  }, []);

  // Keyboard shortcut listener for Undo/Redo
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (document.activeElement?.tagName === 'INPUT' || document.activeElement?.tagName === 'TEXTAREA') {
        return;
      }
      if (e.ctrlKey && e.key.toLowerCase() === 'z') {
        e.preventDefault();
        setHistory(prevHistory => {
          if (prevHistory.length === 0) return prevHistory;
          const prev = prevHistory[prevHistory.length - 1];
          setGrid(currentGrid => {
            setRedoHistory(prevRedo => [...prevRedo, {
              grid: currentGrid.map(r => r.map(c => ({ ...c }))),
              width: currentGrid[0]?.length ?? 0,
              height: currentGrid.length,
            }]);
            return prev.grid.map(r => r.map(c => ({ ...c })));
          });
          setMeta(m => m ? { ...m, board_width: prev.width, board_height: prev.height } : m);
          return prevHistory.slice(0, -1);
        });
      } else if (e.ctrlKey && e.key.toLowerCase() === 'y') {
        e.preventDefault();
        setRedoHistory(prevRedo => {
          if (prevRedo.length === 0) return prevRedo;
          const next = prevRedo[prevRedo.length - 1];
          setGrid(currentGrid => {
            setHistory(prevHistory => [...prevHistory, {
              grid: currentGrid.map(r => r.map(c => ({ ...c }))),
              width: currentGrid[0]?.length ?? 0,
              height: currentGrid.length,
            }]);
            return next.grid.map(r => r.map(c => ({ ...c })));
          });
          setMeta(m => m ? { ...m, board_width: next.width, board_height: next.height } : m);
          return prevRedo.slice(0, -1);
        });
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const loadStage = useCallback((stage: StageRow) => {
    setSelectedId(stage.stage_id);
    setGrid(decodeCells(stage.cells, stage.board_width, stage.board_height));
    setMeta({
      stage_id: stage.stage_id,
      chapter_id: stage.chapter_id ?? 1,
      stage_order: stage.stage_order ?? 1,
      board_width: stage.board_width,
      board_height: stage.board_height,
      turn_limit: stage.turn_limit,
      difficulty: stage.difficulty,
      star1_ratio: stage.star1_ratio,
      star2_ratio: stage.star2_ratio,
      verified_solution: stage.verified_solution,
      ruleset_version: stage.ruleset_version,
      reward_group_id: DIFFICULTY_REWARD[stage.difficulty] ?? stage.reward_group_id,
      rotation_interval: stage.rotation_interval ?? 0,
      portal_data: stage.portal_data ?? '',
      conveyor_data: stage.conveyor_data ?? '',
    });
    setPlaytestState(null);
    setValidationResult(null);
    setSelectedCell(null);
    setHistory([]);
    setRedoHistory([]);
  }, []);

  const handleSelect = useCallback((id: number) => {
    const stage = stages.find(s => s.stage_id === id);
    if (stage) loadStage(stage);
  }, [stages, loadStage]);

  const handleNew = useCallback(async () => {
    const defaults = await fetch('/api/generator-defaults').then(r => r.json()).catch(() => ({}));
    const w = defaults.boardWidth ?? 6;
    const h = defaults.boardHeight ?? 6;
    const turnLimit = defaults.turnLimit ?? 20;
    const difficulty = defaults.difficulty ?? 0;
    const cells = '000'.repeat(w * h);
    const chapterId = selectedChapterId ?? 1;
    const stagesInChapter = stages.filter(s => s.chapter_id === chapterId);
    const maxOrder = stagesInChapter.reduce((m, s) => Math.max(m, s.stage_order), 0);
    const payload = {
      chapter_id: chapterId, stage_order: maxOrder + 1,
      board_width: w, board_height: h, turn_limit: turnLimit, difficulty,
      color_ids: '0', star1_ratio: 0.80, star2_ratio: 0.90,
      cells, verified_solution: '', ruleset_version: 1, reward_group_id: DIFFICULTY_REWARD[difficulty] ?? DIFFICULTY_REWARD[0],
      rotation_interval: 0, portal_data: '', conveyor_data: '',
    };
    const res = await fetch('/api/stages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    const created: StageRow = await res.json();
    setStages(prev => [...prev, created]);
    loadStage(created);
  }, [loadStage, selectedChapterId, stages]);

  const handleInsertAfter = useCallback(async (afterOrder: number) => {
    const defaults = await fetch('/api/generator-defaults').then(r => r.json()).catch(() => ({}));
    const w = defaults.boardWidth ?? 6;
    const h = defaults.boardHeight ?? 6;
    const turnLimit = defaults.turnLimit ?? 20;
    const difficulty = defaults.difficulty ?? 0;
    const cells = '000'.repeat(w * h);
    const chapterId = selectedChapterId ?? 1;

    // shift subsequent stages upward sequentially to avoid CSV race
    const toShift = stages
      .filter(s => s.chapter_id === chapterId && s.stage_order > afterOrder)
      .sort((a, b) => b.stage_order - a.stage_order);
    const shifted: StageRow[] = [];
    for (const s of toShift) {
      const updated = { ...s, stage_order: s.stage_order + 1 };
      await fetch(`/api/stages/${s.stage_id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updated),
      });
      shifted.push(updated);
    }

    const payload = {
      chapter_id: chapterId, stage_order: afterOrder + 1,
      board_width: w, board_height: h, turn_limit: turnLimit, difficulty,
      color_ids: '0', star1_ratio: 0.80, star2_ratio: 0.90,
      cells, verified_solution: '', ruleset_version: 1, reward_group_id: DIFFICULTY_REWARD[difficulty] ?? DIFFICULTY_REWARD[0],
      rotation_interval: 0, portal_data: '', conveyor_data: '',
    };
    const res = await fetch('/api/stages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    const created: StageRow = await res.json();

    setStages(prev => {
      const shiftedIds = new Set(shifted.map(s => s.stage_id));
      return [
        ...prev.filter(s => !shiftedIds.has(s.stage_id)).map(s => s),
        ...shifted,
        created,
      ];
    });
    loadStage(created);
  }, [loadStage, selectedChapterId, stages]);

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

  const handleSelectChapter = useCallback((id: number) => {
    setSelectedChapterId(id);
    if (selectedId !== null) {
      const stage = stages.find(s => s.stage_id === selectedId);
      if (stage && stage.chapter_id !== id) {
        setSelectedId(null);
        setGrid([]);
        setMeta(null);
        setPlaytestState(null);
        setValidationResult(null);
      }
    }
  }, [selectedId, stages]);

  const handleNewChapter = useCallback(async () => {
    const maxId = chapters.reduce((m, c) => Math.max(m, c.chapter_id), 0);
    const payload = {
      display_order: maxId + 1,
      unlock_chapter_id: maxId > 0 ? maxId : null,
      reward_group_id: 0,
      bg_theme_id: 1,
    };
    const res = await fetch('/api/chapters', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    const created: ChapterRow = await res.json();
    setChapters(prev => [...prev, created]);
    setSelectedChapterId(created.chapter_id);
  }, [chapters]);

  const handleDeleteChapter = useCallback(async (id: number) => {
    const stageCount = stages.filter(s => s.chapter_id === id).length;
    const msg = stageCount > 0
      ? `Chapter ${id} has ${stageCount} stage(s).\nDelete chapter and all its stages?`
      : `Delete Chapter ${id}?`;
    if (!window.confirm(msg)) return;

    const stagesInChapter = stages.filter(s => s.chapter_id === id);
    for (const s of stagesInChapter) {
      await fetch(`/api/stages/${s.stage_id}`, { method: 'DELETE' });
    }
    await fetch(`/api/chapters/${id}`, { method: 'DELETE' });

    setStages(prev => prev.filter(s => s.chapter_id !== id));
    setChapters(prev => prev.filter(c => c.chapter_id !== id));
    if (selectedChapterId === id) {
      setSelectedChapterId(null);
      setSelectedId(null);
      setGrid([]);
      setMeta(null);
      setPlaytestState(null);
      setValidationResult(null);
    }
  }, [stages, selectedChapterId]);

  const handleDragStart = useCallback(() => {
    setGrid(currentGrid => {
      setHistory(prev => [...prev, {
        grid: currentGrid.map(r => r.map(c => ({ ...c }))),
        width: currentGrid[0]?.length ?? 0,
        height: currentGrid.length,
      }]);
      setRedoHistory([]);
      return currentGrid;
    });
  }, []);

  const handleImageDrop = useCallback((file: File) => {
    if (!meta) return;
    const reader = new FileReader();
    reader.onload = (event) => {
      const img = new Image();
      img.onload = () => {
        const w = meta.board_width;
        const h = meta.board_height;

        const canvas = document.createElement('canvas');
        canvas.width = w;
        canvas.height = h;
        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        ctx.drawImage(img, 0, 0, w, h);
        const imgData = ctx.getImageData(0, 0, w, h);
        const data = imgData.data;

        const paletteLabs = palette.map(p => rgbToLab(p.r, p.g, p.b));

        const tempGrid: CellData[][] = Array.from({ length: h }, () =>
          Array.from({ length: w }, () => ({ colorId: 0, type: 'Basic', protector: 0, isCore: false }))
        );

        for (let r = 0; r < h; r++) {
          for (let c = 0; c < w; c++) {
            const idx = (r * w + c) * 4;
            const rVal = data[idx];
            const gVal = data[idx + 1];
            const bVal = data[idx + 2];
            const aVal = data[idx + 3];

            if (aVal < 50) {
              tempGrid[r][c] = { colorId: 0, type: 'Void', protector: 0, isCore: false };
            } else {
              const lab = rgbToLab(rVal, gVal, bVal);
              let bestIndex = 0;
              let bestDist = Infinity;

              paletteLabs.forEach((pLab, pIdx) => {
                const dist = colorDistLab(lab, pLab);
                if (dist < bestDist) {
                  bestDist = dist;
                  bestIndex = pIdx;
                }
              });

              tempGrid[r][c] = { colorId: palette[bestIndex].color_id, type: 'Basic', protector: 0, isCore: false };
            }
          }
        }

        // Isolated pixel removal filter
        const finalGrid = tempGrid.map(row => row.map(cell => ({ ...cell })));

        for (let r = 0; r < h; r++) {
          for (let c = 0; c < w; c++) {
            const centerCell = tempGrid[r][c];
            if (centerCell.type !== 'Basic') continue;

            let sameNeighbors = 0;
            const colorCounts: Record<number, number> = {};
            let voidCount = 0;
            let obstacleCount = 0;

            for (let dr = -1; dr <= 1; dr++) {
              for (let dc = -1; dc <= 1; dc++) {
                if (dr === 0 && dc === 0) continue;
                const nr = r + dr;
                const nc = c + dc;
                if (nr >= 0 && nr < h && nc >= 0 && nc < w) {
                  const neighbor = tempGrid[nr][nc];
                  if (neighbor.type === 'Basic') {
                    if (neighbor.colorId === centerCell.colorId) {
                      sameNeighbors++;
                    }
                    colorCounts[neighbor.colorId] = (colorCounts[neighbor.colorId] || 0) + 1;
                  } else if (neighbor.type === 'Void') {
                    voidCount++;
                  } else if (neighbor.type === 'Obstacle') {
                    obstacleCount++;
                  }
                }
              }
            }

            if (sameNeighbors === 0) {
              let bestColorId = centerCell.colorId;
              let maxCount = 0;
              let bestType: 'Basic' | 'Void' | 'Obstacle' = 'Basic';

              for (const [cidStr, count] of Object.entries(colorCounts)) {
                const cid = parseInt(cidStr);
                if (count > maxCount) {
                  maxCount = count;
                  bestColorId = cid;
                  bestType = 'Basic';
                }
              }

              if (voidCount > maxCount) {
                maxCount = voidCount;
                bestType = 'Void';
              }
              if (obstacleCount > maxCount) {
                maxCount = obstacleCount;
                bestType = 'Obstacle';
              }

              if (bestType === 'Basic') {
                finalGrid[r][c] = { colorId: bestColorId, type: 'Basic', protector: 0, isCore: false };
              } else {
                finalGrid[r][c] = { colorId: 0, type: bestType, protector: 0, isCore: false };
              }
            }
          }
        }

        setGrid(currentGrid => {
          setHistory(prev => [...prev, {
            grid: currentGrid.map(r => r.map(c => ({ ...c }))),
            width: currentGrid[0]?.length ?? 0,
            height: currentGrid.length,
          }]);
          setRedoHistory([]);
          return finalGrid;
        });
      };
      img.src = event.target?.result as string;
    };
    reader.readAsDataURL(file);
  }, [meta, palette]);

  const handleLeftClick = useCallback((r: number, c: number) => {
    if (simulateState) return;
    if (playtestState) {
      if (playtestState.result) return;
      const group = findGroup(playtestState.board, r, c);
      if (group.length === 0) return;

      let b = applyRemoval(playtestState.board, group);
      b = applyConveyors(b, meta?.conveyor_data);
      b = applyGravity(b, meta?.portal_data);
      const turns = playtestState.turns - 1;
      const movesMade = meta!.turn_limit - turns;

      if (meta?.rotation_interval && meta.rotation_interval > 0 && movesMade % meta.rotation_interval === 0) {
        b = rotate180(b);
        b = applyGravity(b, meta?.portal_data);
      }

      const result = evaluate(b, playtestState.initialValid, meta!.star1_ratio, meta!.star2_ratio);
      const isEnd = turns === 0 || result.stars === 3;
      const newMoves = playtestState.isRecording
        ? [...playtestState.moves, [r, c] as [number, number]]
        : playtestState.moves;

      if (isEnd && result.stars === 3 && playtestState.isRecording) {
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
      const isNonBasic = brush.type === 'Obstacle' || brush.type === 'Void';
      const cell: CellData = {
        colorId: isNonBasic ? 0 : brush.colorId,
        type: brush.type,
        protector: isNonBasic ? 0 : brush.protector,
        isCore: isNonBasic ? false : brush.isCore,
      };
      setRedoHistory([]);
      setGrid(prev => {
        const next = prev.map(row => [...row]);
        next[r][c] = cell;
        return next;
      });
      setSelectedCell({ r, c });
    }
  }, [playtestState, brush, meta]);

  const handleRightClick = useCallback((r: number, c: number) => {
    if (simulateState || playtestState) return;
    setRedoHistory([]);
    setGrid(prev => {
      const next = prev.map(row => [...row]);
      next[r][c] = { colorId: 0, type: 'Obstacle', protector: 0, isCore: false };
      return next;
    });
  }, [playtestState]);

  const handleFieldChange = useCallback((key: keyof StageMeta, value: number) => {
    setMeta(prev => {
      if (!prev) return prev;
      if (key === 'difficulty') {
        return { ...prev, difficulty: value, reward_group_id: DIFFICULTY_REWARD[value] ?? prev.reward_group_id };
      }
      return { ...prev, [key]: value };
    });
  }, []);

  const handleGeneratorMetaChange = useCallback((patch: { difficulty?: number; turn_limit?: number; reward_group_id?: number }) => {
    setMeta(prev => prev ? { ...prev, ...patch } : prev);
  }, []);

  const handleResize = useCallback((w: number, h: number) => {
    setGrid(currentGrid => {
      setHistory(prev => [...prev, {
        grid: currentGrid.map(r => r.map(c => ({ ...c }))),
        width: currentGrid[0]?.length ?? 0,
        height: currentGrid.length,
      }]);
      setRedoHistory([]);
      const next: CellData[][] = [];
      for (let r = 0; r < h; r++) {
        next[r] = [];
        for (let c = 0; c < w; c++) {
          next[r][c] = currentGrid[r]?.[c] ?? { colorId: 0, type: 'Basic', protector: 0, isCore: false };
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

  const handleStartSimulate = useCallback(() => {
    if (!meta?.verified_solution) return;
    const taps = meta.verified_solution.split(';')
      .filter(Boolean)
      .map(s => { const [r, c] = s.split(',').map(Number); return [r, c] as [number, number]; });

    let board: Board = grid.map(row => row.map(cell => ({ ...cell })));
    board = applyGravity(board, meta.portal_data);
    const cloneBoard = (b: Board): Board => b.map(row => row.map(cell => cell ? { ...cell } : null));
    const states: Board[] = [cloneBoard(board)];

    for (let i = 0; i < taps.length; i++) {
      const [r, c] = taps[i];
      const group = findGroup(board, r, c);
      if (group.length > 0) {
        board = applyRemoval(board, group);
        board = applyConveyors(board, meta.conveyor_data);
        board = applyGravity(board, meta.portal_data);
      }
      const movesMade = i + 1;
      if (meta.rotation_interval && meta.rotation_interval > 0 && movesMade % meta.rotation_interval === 0) {
        board = rotate180(board);
        board = applyGravity(board, meta.portal_data);
      }
      states.push(cloneBoard(board));
    }

    setSimulateState({ states, taps, stepIndex: 0 });
  }, [meta, grid]);

  const handleStopSimulate = useCallback(() => setSimulateState(null), []);

  const handleSimStep = useCallback((delta: number) => {
    setSimulateState(prev => {
      if (!prev) return prev;
      const newIdx = Math.max(0, Math.min(prev.states.length - 1, prev.stepIndex + delta));
      return { ...prev, stepIndex: newIdx };
    });
  }, []);

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

  const handleRotate180 = useCallback(() => {
    setPlaytestState(prev => {
      if (!prev) return prev;
      const rotated = rotate180(prev.board);
      const withGravity = applyGravity(rotated, meta?.portal_data);
      return { ...prev, board: withGravity };
    });
  }, [meta]);

  const handleGenerate = useCallback(async (settings: GeneratorSettings) => {
    if (!meta) return;
    setGeneratorStatus('running');
    setGeneratorInfo(null);
    await new Promise(r => setTimeout(r, 0));

    let result: GenerateResult | null = null;
    try {
      const res = await fetch('/api/generate-board', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...settings,
          width: meta.board_width,
          height: meta.board_height,
          difficulty: meta.difficulty,
          turnLimit: meta.turn_limit,
          star1Ratio: meta.star1_ratio,
          star2Ratio: meta.star2_ratio,
          rotationInterval: meta.rotation_interval,
          portalData: meta.portal_data,
          conveyorData: meta.conveyor_data,
        }),
      });
      if (!res.ok) {
        throw new Error(await res.text());
      }
      result = await res.json();
    } catch (error) {
      console.error(error);
    }

    if (result) {
      setGeneratorStatus('success');
      setGeneratorInfo({ attempts: result.attempts, solveLength: result.solveLength, score: result.score });
      setGrid(currentGrid => {
        setHistory(prev => [...prev, {
          grid: currentGrid.map(r => r.map(c => ({ ...c }))),
          width: currentGrid[0]?.length ?? 0,
          height: currentGrid.length,
        }]);
        setRedoHistory([]);
        return result.board;
      });
      setMeta(prev => prev ? { ...prev, verified_solution: result.verifiedSolution } : prev);
    } else {
      setGeneratorStatus('failed');
    }
    setValidationResult(null);
  }, [meta]);

  const filteredStages = selectedChapterId !== null
    ? stages.filter(s => s.chapter_id === selectedChapterId)
    : stages;

  const displayGrid: Board = simulateState
    ? simulateState.states[simulateState.stepIndex]
    : playtestState
      ? playtestState.board
      : grid;

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Chapter + Stage List */}
      <div className="w-44 flex-shrink-0 border-r border-gray-700 bg-gray-900 flex flex-col">
        <ChapterPanel
          chapters={chapters}
          stages={stages}
          selectedChapterId={selectedChapterId}
          onSelect={handleSelectChapter}
          onNew={handleNewChapter}
          onDelete={handleDeleteChapter}
        />
        <div className="flex-1 min-h-0 overflow-hidden">
          <StageList
            stages={filteredStages}
            selectedId={selectedId}
            onSelect={handleSelect}
            onNew={handleNew}
            onDelete={handleDelete}
            onInsertAfter={handleInsertAfter}
          />
        </div>
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
                isPlaytest={!!playtestState}
                onLeftClick={handleLeftClick}
                onRightClick={handleRightClick}
                onDragStart={handleDragStart}
                onImageDrop={handleImageDrop}
              />
            </div>
            <div className="flex-shrink-0">
              <MetadataPanel
                meta={meta}
                onFieldChange={handleFieldChange}
                onResize={handleResize}
              />
              <div className="border-t border-gray-700 px-3 py-1 flex justify-end">
                <button
                  onClick={() => setShowGenerator(p => !p)}
                  className={`text-xs px-2 py-1 rounded ${showGenerator ? 'bg-purple-700 hover:bg-purple-600' : 'bg-gray-700 hover:bg-gray-600'}`}
                >
                  ⚙ Generator
                </button>
              </div>
              {showGenerator && (
                <GeneratorPanel
                  boardWidth={meta.board_width}
                  boardHeight={meta.board_height}
                  metaDifficulty={meta.difficulty}
                  metaTurnLimit={meta.turn_limit}
                  onGenerate={handleGenerate}
                  onMetaChange={handleGeneratorMetaChange}
                  status={generatorStatus}
                  info={generatorInfo}
                />
              )}
              <PlaytestPanel
                isPlaytest={!!playtestState}
                isRecording={playtestState?.isRecording ?? false}
                playtestTurns={playtestState?.turns ?? 0}
                playtestResult={playtestState?.result ?? null}
                validationResult={validationResult}
                hasVerifiedSolution={!!meta.verified_solution}
                isSimulate={!!simulateState}
                simulateStep={simulateState?.stepIndex ?? 0}
                simulateTotal={simulateState ? simulateState.taps.length : 0}
                onStartSimulate={handleStartSimulate}
                onStopSimulate={handleStopSimulate}
                onSimStep={handleSimStep}
                onStartPlaytest={() => {
                  let board: Board = grid.map(row => row.map(cell => ({ ...cell })));
                  board = applyGravity(board, meta?.portal_data);
                  setPlaytestState({
                    board,
                    turns: meta.turn_limit,
                    initialValid: countInitialValidCells(board),
                    moves: [],
                    isRecording: true,
                    result: null,
                  });
                }}
                onStopPlaytest={() => setPlaytestState(null)}
                onToggleRecord={() =>
                  setPlaytestState(prev => prev ? { ...prev, isRecording: !prev.isRecording } : prev)
                }
                onRotate180={handleRotate180}
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
          brush={brush}
          palette={palette}
          onBrushChange={setBrush}
        />
      </div>
    </div>
  );
}
