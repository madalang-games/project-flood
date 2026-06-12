import type { GenerateResult, GeneratorConfig } from './generator';

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

function getWorkerCount(maxAttempts: number): number {
  const hardwareThreads = typeof navigator === 'undefined'
    ? 1
    : navigator.hardwareConcurrency || 1;
  return Math.max(1, Math.min(maxAttempts, hardwareThreads));
}

async function generateSequential(config: GeneratorConfig): Promise<GenerateResult | null> {
  const { generateBoard } = await import('./generator');
  return generateBoard(config);
}

function createWorker(): Worker {
  return new Worker(new URL('../workers/generator.worker.ts', import.meta.url), { type: 'module' });
}

export async function generateBoardParallel(config: GeneratorConfig): Promise<GenerateResult | null> {
  if (config.maxAttempts <= 0) return null;
  if (typeof Worker === 'undefined') return generateSequential(config);

  let workers: Worker[];
  try {
    workers = Array.from({ length: getWorkerCount(config.maxAttempts) }, createWorker);
  } catch {
    return generateSequential(config);
  }

  return new Promise((resolve, reject) => {
    let nextAttempt = 1;
    let active = 0;
    let requestId = 0;
    let settled = false;
    let best: GenerateResult | null = null;

    const cleanup = () => {
      for (const worker of workers) worker.terminate();
    };

    const finish = (result: GenerateResult | null) => {
      if (settled) return;
      settled = true;
      cleanup();
      resolve(result);
    };

    const fail = (error: unknown) => {
      if (settled) return;
      settled = true;
      cleanup();
      reject(error);
    };

    const assign = (worker: Worker) => {
      if (settled) return;
      if (nextAttempt > config.maxAttempts) {
        if (active === 0) finish(best);
        return;
      }

      const message: GenerateWorkerRequest = {
        id: ++requestId,
        attempt: nextAttempt++,
        config,
      };
      active++;
      worker.postMessage(message);
    };

    for (const worker of workers) {
      worker.onmessage = (event: MessageEvent<GenerateWorkerResponse>) => {
        if (settled) return;

        active--;
        const { result, error } = event.data;
        if (error) {
          fail(new Error(error));
          return;
        }

        if (result && (!best || result.score > best.score)) best = result;

        assign(worker);
        if (nextAttempt > config.maxAttempts && active === 0) finish(best);
      };

      worker.onerror = event => {
        fail(new Error(event.message || 'Generator worker failed.'));
      };

      worker.onmessageerror = () => {
        fail(new Error('Generator worker message could not be deserialized.'));
      };

      assign(worker);
    }
  });
}
