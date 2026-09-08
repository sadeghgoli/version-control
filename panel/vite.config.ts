import { defineConfig, type ProxyOptions } from 'vite';
import react from '@vitejs/plugin-react';

const remoteApi = 'https://apiweb-versioncontrol.sabzevar.ir:5023';

function rewriteSetCookie(value: string): string {
  return value
    .replace(/;\s*Secure/gi, '')
    .replace(/;\s*SameSite=None/gi, '; SameSite=Lax');
}

const proxyOptions: ProxyOptions = {
  target: remoteApi,
  changeOrigin: true,
  secure: false,
  configure(proxy) {
    proxy.on('proxyRes', (proxyRes) => {
      const cookies = proxyRes.headers['set-cookie'];
      if (!cookies) {
        return;
      }
      proxyRes.headers['set-cookie'] = cookies.map(rewriteSetCookie);
    });
  },
};

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    proxy: {
      '/api': proxyOptions,
    },
  },
  preview: {
    port: 5174,
    proxy: {
      '/api': proxyOptions,
    },
  },
});
