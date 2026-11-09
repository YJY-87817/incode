import { fileURLToPath, URL } from "url";
// import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import vuetify from "vite-plugin-vuetify";
import fs from "fs";

// 開發用 HTTPS 憑證（key.pem / cert.pem，已 gitignore）。
// 只有 dev server 需要；檔案不存在時（例如 CI 執行 npm run build）不設定 https，避免讀檔失敗。
const hasCert = fs.existsSync("key.pem") && fs.existsSync("cert.pem");

// https://vitejs.dev/config/
export default defineConfig({
  // vuetify：只打包模板中用到的 Vuetify 元件與指令（tree-shaking）
  plugins: [vue(), vuetify({ autoImport: true })],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  test: {
    environment: "jsdom",
    include: ["src/**/*.test.js"],
  },
  server: {
    https: hasCert
      ? {
          key: fs.readFileSync("key.pem"),
          cert: fs.readFileSync("cert.pem"),
        }
      : undefined,
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
