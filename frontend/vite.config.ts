import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      // Forward API calls to the ASP.NET Core backend during development.
      "/api": {
        target: "http://localhost:5080",
        changeOrigin: true,
      },
    },
  },
});
