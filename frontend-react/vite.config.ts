import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["favicon.ico", "apple-touch-icon.png", "mask-icon.svg"],
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
});
