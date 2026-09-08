import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'https://apiweb-versioncontrol.sabzevar.ir:5023',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
