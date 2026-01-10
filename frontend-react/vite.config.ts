import { defineConfig, type Plugin } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

// Plugin to remove crossorigin attributes from HTML (needed for itch.io)
function removeCrossorigin(): Plugin {
  return {
    name: 'remove-crossorigin',
    transformIndexHtml(html) {
      return html.replace(/ crossorigin/g, '');
    }
  };
}

// https://vitejs.dev/config/
export default defineConfig(({ command, mode }) => ({
  // Use relative paths for itch.io compatibility (serves from subdirectory)
  base: mode === "itchio" ? "./" : "/",
  // Disable crossorigin attributes for itch.io (causes CORS issues in their iframe/CDN)
  build: mode === "itchio" ? {
    modulePreload: { polyfill: false },
    rollupOptions: {
      output: {
        // Use simpler file names without hashes for itch.io compatibility
        entryFileNames: 'assets/[name].js',
        chunkFileNames: 'assets/[name].js',
        assetFileNames: 'assets/[name].[ext]'
      }
    }
  } : {},
  plugins: [
    react(),
    // Remove crossorigin attributes for itch.io builds
    ...(mode === "itchio" ? [removeCrossorigin()] : []),
    // Disable PWA for itch.io builds (service workers don't work in iframes)
    ...(mode !== "itchio"
      ? [
          VitePWA({
            registerType: "autoUpdate",
            includeAssets: [
              "favicon.ico",
              "apple-touch-icon.png",
              "mask-icon.svg",
            ],
            workbox: {
              // Don't let the service worker intercept /api routes
              // This is critical for OAuth popups to work correctly
              navigateFallbackDenylist: [/^\/api/],
            },
            manifest: {
              name: "Misfit",
              short_name: "Misfit",
              description:
                "A word game about guessing if your word is Match or Misfit.",
              theme_color: "#ffffff",
              icons: [
                {
                  src: "misfiticon.png",
                  sizes: "192x192",
                  type: "image/png",
                },
                {
                  src: "misfiticon.png",
                  sizes: "512x512",
                  type: "image/png",
                },
              ],
            },
          }),
        ]
      : []),
  ],
  server: {
    proxy: {
      "/api": {
        target: "http://localhost:5017",
        changeOrigin: false, // <--- CHANGE THIS TO FALSE
        secure: false,
      },
    },
  },
}));
