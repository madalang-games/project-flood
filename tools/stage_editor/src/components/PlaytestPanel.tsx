'use client';

import type { StarResult } from '../lib/game-rules';
import type { ValidationResult } from '../lib/validator';

interface Props {
  isPlaytest: boolean;
  isRecording: boolean;
  playtestTurns: number;
  playtestResult: StarResult | null;
  validationResult: ValidationResult | null;
  hasVerifiedSolution: boolean;
  isSimulate: boolean;
  simulateStep: number;
  simulateTotal: number;
  onStartPlaytest: () => void;
  onStopPlaytest: () => void;
  onToggleRecord: () => void;
  onRotate180: () => void;
  onValidate: () => void;
  onExport: () => void;
  onSave: () => void;
  onStartSimulate: () => void;
  onStopSimulate: () => void;
  onSimStep: (delta: number) => void;
}

export default function PlaytestPanel({
  isPlaytest,
  isRecording,
  playtestTurns,
  playtestResult,
  validationResult,
  hasVerifiedSolution,
  isSimulate,
  simulateStep,
  simulateTotal,
  onStartPlaytest,
  onStopPlaytest,
  onToggleRecord,
  onRotate180,
  onValidate,
  onExport,
  onSave,
  onStartSimulate,
  onStopSimulate,
  onSimStep,
}: Props) {
  return (
    <div className="p-3 border-t border-gray-700 bg-gray-800">
      <div className="flex flex-wrap gap-2 items-center">
        {isSimulate ? (
          <>
            <button
              onClick={onStopSimulate}
              className="text-xs bg-red-700 hover:bg-red-600 px-3 py-1.5 rounded"
            >
              ■ Stop
            </button>
            <button
              onClick={() => onSimStep(-1)}
              disabled={simulateStep === 0}
              className="text-xs bg-gray-600 hover:bg-gray-500 disabled:opacity-40 px-2 py-1.5 rounded"
            >
              ◀
            </button>
            <span className="text-xs text-gray-300 tabular-nums">
              {simulateStep === 0 ? 'Initial' : `Tap ${simulateStep} / ${simulateTotal}`}
            </span>
            <button
              onClick={() => onSimStep(1)}
              disabled={simulateStep === simulateTotal}
              className="text-xs bg-gray-600 hover:bg-gray-500 disabled:opacity-40 px-2 py-1.5 rounded"
            >
              ▶
            </button>
          </>
        ) : !isPlaytest ? (
          <>
            <button
              onClick={onStartPlaytest}
              className="text-xs bg-green-700 hover:bg-green-600 px-3 py-1.5 rounded"
            >
              ▶ Playtest
            </button>
            {hasVerifiedSolution && (
              <button
                onClick={onStartSimulate}
                className="text-xs bg-indigo-700 hover:bg-indigo-600 px-3 py-1.5 rounded"
              >
                ⏩ Simulate
              </button>
            )}
            <button
              onClick={onValidate}
              className="text-xs bg-gray-600 hover:bg-gray-500 px-3 py-1.5 rounded"
            >
              ✓ Validate
            </button>
            <button
              onClick={onExport}
              className="text-xs bg-blue-700 hover:bg-blue-600 px-3 py-1.5 rounded"
            >
              ⬇ Export
            </button>
            <button
              onClick={onSave}
              className="text-xs bg-gray-600 hover:bg-gray-500 px-3 py-1.5 rounded"
            >
              Save
            </button>
          </>
        ) : (
          <>
            <button
              onClick={onStopPlaytest}
              className="text-xs bg-red-700 hover:bg-red-600 px-3 py-1.5 rounded"
            >
              ■ Stop
            </button>
            <button
              onClick={onToggleRecord}
              className={`text-xs px-3 py-1.5 rounded ${
                isRecording ? 'bg-red-600 hover:bg-red-500' : 'bg-gray-600 hover:bg-gray-500'
              }`}
            >
              {isRecording ? '⏺ Recording' : '⏺ Record'}
            </button>
            {!playtestResult && (
              <span className="text-xs text-gray-300 ml-1">Turns: {playtestTurns}</span>
            )}
            {!playtestResult && (
              <button
                onClick={onRotate180}
                className="text-xs bg-indigo-700 hover:bg-indigo-600 px-3 py-1.5 rounded"
                title="Rotate board 180° and apply gravity"
              >
                ↻ 180°
              </button>
            )}
          </>
        )}
      </div>

      {validationResult && (
        <div className="mt-2 text-xs space-y-1">
          {!validationResult.hasVerifiedSolution ? (
            <div className="text-yellow-400">⚠ No solution recorded — playtest with recording on</div>
          ) : validationResult.solutionReplaySucceeds ? (
            <div className="text-green-400">✓ Verified solution</div>
          ) : (
            <>
              <div className="text-red-400">✗ Solution replay failed</div>
              <div className="text-xs text-gray-400">Re-record solution in playtest</div>
            </>
          )}
          {validationResult.warnings.map((w, i) => (
            <div key={i} className="text-yellow-400">⚠ {w}</div>
          ))}
          <div className={validationResult.canExport ? 'text-green-400' : 'text-gray-400'}>
            {validationResult.canExport ? '✓ Ready to export' : '✗ Cannot export'}
          </div>
        </div>
      )}
    </div>
  );
}
