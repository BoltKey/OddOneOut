import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173, // Standard Vite port
    proxy: {
      "/api": {
        target: "http://127.0.0.1:5017", // <--- CHECK YOUR .NET PORT in launchSettings.json
        changeOrigin: true,
        secure: false, // Allows self-signed SSL from .NET
      },
    },
  },
});
