import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'path'
import { fileURLToPath, URL } from 'node:url'

// https://vite.dev/config/
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
})
