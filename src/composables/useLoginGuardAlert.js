import Swal from "sweetalert2";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/user/auth";

export function useLoginGuardAlert() {
  const authStore = useAuthStore();
  const router = useRouter();

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
      router.push("/auth");
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
