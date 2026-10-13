import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// In development the API runs separately (dotnet run, port 5080); in production the API serves this build.
const apiTarget = 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': apiTarget,
      '/config.json': apiTarget,
    },
  },
  build: {
    chunkSizeWarningLimit: 700,
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    include: ['tests/unit/**/*.spec.{ts,tsx}', 'tests/component/**/*.spec.{ts,tsx}'],
    env: { VITE_API_BASE_URL: '/api/v1' },
    css: false,
  },
});
