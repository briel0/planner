import { defineConfig } from 'vitest/config';

// Lido pelo builder de testes do Angular (angular.json: test.options.runnerConfig).
export default defineConfig({
  test: {
    // Por padrão o Vitest abre um processo por núcleo (8 aqui), cada um com jsdom e o compilador do Angular:
    // numa máquina de 7,5 GB, junto com o editor e o navegador, isso esgota a memória e trava o sistema.
    maxWorkers: 2,
  },
});
