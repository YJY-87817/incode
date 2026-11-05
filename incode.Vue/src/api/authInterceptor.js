import Swal from "sweetalert2";
import { useAuthStore } from "@/stores/users/auth.js";

let expiredNoticeShown = false;

/**
 * 所有 axios（全域 axios、apiClient、pointClient）共用的 401 處理。
 *
 * - 前端以為已登入卻收到 401（Cookie 過期 / 被登出）：清除登入狀態、提示並導向登入頁（登入後回到原頁）
 * - 本來就未登入：預設交給呼叫端處理（例如論壇頁面未登入時背景查詢互動狀態會 401，不能把訪客踢去登入頁）；
 *   redirectWhenAnonymous = true 時也導向登入頁（全域 axios 維持原本行為）
 *
 * 錯誤一律繼續往外拋，元件自己的 catch 仍會收到。
 */
export function installAuthInterceptor(instance, { redirectWhenAnonymous = false } = {}) {
  instance.interceptors.response.use(
    (response) => response,
    async (error) => {
      if (error?.response?.status === 401) {
        await handleUnauthorized(redirectWhenAnonymous);
      }
      return Promise.reject(error);
    },
  );
}

async function handleUnauthorized(redirectWhenAnonymous) {
  const authStore = useAuthStore();
  const wasAuthenticated = authStore.isAuthenticated;

  if (!wasAuthenticated && !redirectWhenAnonymous) return;

  authStore.user = null;
  authStore.isAuthenticated = false;

  // router 延遲載入：router → 頁面 → store → apiClient → 本檔，直接 import 會形成循環
  const { default: router } = await import("@/router");
  const current = router.currentRoute.value;
  if (current.name === "Auth") return; // 已經在登入頁

  // 同時有多個請求 401 時只提示 / 導向一次
  if (expiredNoticeShown) return;
  expiredNoticeShown = true;
  setTimeout(() => (expiredNoticeShown = false), 2000);

  Swal.fire({
    icon: "warning",
    title: wasAuthenticated ? "登入已逾時" : "請先登入會員",
    text: wasAuthenticated ? "為了您的帳號安全，請重新登入" : undefined,
    timer: 2000,
    showConfirmButton: false,
  });

  router.push({ name: "Login", query: { redirect: current.fullPath } });
}
