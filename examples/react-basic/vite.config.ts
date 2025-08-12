import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    open: true
  },
  define: {
    // Define environment variables for development
    'process.env.REACT_APP_API_URL': JSON.stringify(process.env.REACT_APP_API_URL || 'https://localhost:5001'),
    'process.env.REACT_APP_GOOGLE_CLIENT_ID': JSON.stringify(process.env.REACT_APP_GOOGLE_CLIENT_ID || ''),
    'process.env.REACT_APP_FACEBOOK_CLIENT_ID': JSON.stringify(process.env.REACT_APP_FACEBOOK_CLIENT_ID || ''),
    'process.env.REACT_APP_FACEBOOK_APP_SECRET': JSON.stringify(process.env.REACT_APP_FACEBOOK_APP_SECRET || ''),
    'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'development')
  }
})