import { fileURLToPath, URL } from "url";
// import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import fs from "fs";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  server: {
    https: {
      key: fs.readFileSync("key.pem"),
      cert: fs.readFileSync("cert.pem"),
    },
    port: 5173,
    host: "0.0.0.0",
    proxy: {
      "/api": {
        target: "https://localhost:7190",
        changeOrigin: true,
        secure: false,
      },
      "/contents": {
        target: "https://localhost:7190",
        changeOrigin: true,
        secure: false,
      },
      "/img": {
        target: "https://localhost:7190",
        changeOrigin: true,
        secure: false,
      },
    },
  },

  css: {
    preprocessorOptions: {
      scss: {
        quietDeps: true,
        silenceDeprecations: [
          "import",
          "global-builtin",
          "color-functions",
          "if-function",
          "function-units",
          "abs-percent",
        ],
      },
    },
  },
});
