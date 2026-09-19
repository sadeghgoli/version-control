import { defineConfig, type ProxyOptions } from 'vite';
import react from '@vitejs/plugin-react';

const remoteApi = process.env.VITE_PROXY_TARGET ?? 'http://127.0.0.1:5300';

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
	    host: '0.0.0.0',

    port: 5310,
    proxy: {
      '/api': proxyOptions,
    },
  },
  preview: {
	  host: '0.0.0.0',  
    port: 5310,
    proxy: {
      '/api': proxyOptions,
    },
  },
});
