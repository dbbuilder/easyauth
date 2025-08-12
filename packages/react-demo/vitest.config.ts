import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      // Force all React imports to use the demo's React version
      'react': resolve(fileURLToPath(new URL('.', import.meta.url)), './node_modules/react'),
      'react-dom': resolve(fileURLToPath(new URL('.', import.meta.url)), './node_modules/react-dom'),
      'react/jsx-runtime': resolve(fileURLToPath(new URL('.', import.meta.url)), './node_modules/react/jsx-runtime'),
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
});