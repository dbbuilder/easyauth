import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      name: 'EasyAuthVue',
      formats: ['es', 'umd'],
      fileName: (format) => `index.${format === 'es' ? 'esm.js' : 'js'}`
    },
    rollupOptions: {
      // Externalize deps that shouldn't be bundled
      external: ['vue'],
      output: {
        // Global vars for UMD build
        globals: {
          vue: 'Vue'
        }
      }
    },
    sourcemap: true
  },
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  }
})