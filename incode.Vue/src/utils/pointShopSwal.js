// 點數商城共用的 SweetAlert2 設定與快捷函式
// 統一按鈕顏色、文字顏色，避免每個元件重複寫 didOpen
import Swal from "sweetalert2";
import "sweetalert2/dist/sweetalert2.min.css";

const whiteButtonText = (modal) => {
  modal
    .querySelectorAll(".swal2-confirm, .swal2-cancel, .swal2-deny")
    .forEach((btn) => (btn.style.color = "#fff"));
};

export const shopSwal = Swal.mixin({
  confirmButtonColor: "#145369",
  cancelButtonColor: "#d33",
  confirmButtonText: "確定",
  didOpen: whiteButtonText,
});

export const alertSuccess = (title, text) =>
  shopSwal.fire({ title, text, icon: "success" });

export const alertError = (title, text) =>
  shopSwal.fire({ title, text, icon: "error" });

export const alertWarning = (title, text) =>
  shopSwal.fire({ title, text, icon: "warning" });

// 確認對話框，回傳使用者是否按下確認
export const confirmDialog = async ({
  title,
  text,
  confirmText = "確認",
  cancelText = "取消",
  icon = "warning",
}) => {
  const result = await shopSwal.fire({
    title,
    text,
    icon,
    showCancelButton: true,
    confirmButtonText: confirmText,
    cancelButtonText: cancelText,
  });
  return result.isConfirmed;
};

export default shopSwal;
