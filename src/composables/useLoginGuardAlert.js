import Swal from "sweetalert2";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "@/stores/user/auth";

const LOGIN_SCROLL_STORAGE_KEY_PREFIX = "login-redirect-scroll:";

export function useLoginGuardAlert() {
  const authStore = useAuthStore();
  const router = useRouter();
  const route = useRoute();

  const getCurrentUserId = () => {
    const numericUserId = Number(authStore.user?.id);
    if (!Number.isFinite(numericUserId) || numericUserId <= 0) return null;
    return numericUserId;
  };

  const isLoggedIn = () => {
    const userId = getCurrentUserId();
    if (userId) return true;
    return Boolean(authStore.isAuthenticated);
  };

  const showLoginRequiredAlert = async (message = "請先登入後操作") => {
    const result = await Swal.fire({
      icon: "warning",
      title: "請先登入",
      text: message,
      confirmButtonText: "知道了",
      showCloseButton: true,
      confirmButtonColor: "#145369",
    });

    if (result.isConfirmed) {
      const redirectPath = route.fullPath || "/";
      const scrollY =
        typeof window !== "undefined"
          ? Math.max(window.scrollY || window.pageYOffset || 0, 0)
          : 0;

      sessionStorage.setItem(
        `${LOGIN_SCROLL_STORAGE_KEY_PREFIX}${redirectPath}`,
        String(scrollY),
      );

      router.push({
        name: "Auth",
        query: { redirect: redirectPath },
      });
    }
  };

  const ensureLoggedIn = async (message = "請先登入後操作") => {
    if (isLoggedIn()) return true;
    await showLoginRequiredAlert(message);
    return false;
  };

  return {
    getCurrentUserId,
    isLoggedIn,
    showLoginRequiredAlert,
    ensureLoggedIn,
  };
}
