import { generateBoardAttempt } from '../lib/generator';
import type { GenerateResult, GeneratorConfig } from '../lib/generator';

interface GenerateWorkerRequest {
  id: number;
  attempt: number;
  config: GeneratorConfig;
}

interface GenerateWorkerResponse {
  id: number;
  attempt: number;
  result: GenerateResult | null;
  error?: string;
}

type WorkerScope = {
  onmessage: ((event: MessageEvent<GenerateWorkerRequest>) => void) | null;
  postMessage: (message: GenerateWorkerResponse) => void;
};

const workerScope = self as unknown as WorkerScope;

workerScope.onmessage = (event: MessageEvent<GenerateWorkerRequest>) => {
  const { id, attempt, config } = event.data;
  try {
    workerScope.postMessage({
      id,
      attempt,
      result: generateBoardAttempt(config, attempt),
    });
  } catch (error) {
    workerScope.postMessage({
      id,
      attempt,
      result: null,
      error: error instanceof Error ? error.message : String(error),
    });
  }
};
