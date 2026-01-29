import { defineStore } from "pinia";
import { ref } from "vue";

export const useAlertStore = defineStore("alert", () => {
    const show = ref(false);
    const type = ref("success"); // success, error, warning, info
    const title = ref("");
    const text = ref("");
    const timeout = ref(3000); // 預設 3 秒後消失
    const color = ref(undefined);

    // 通用顯示方法
    const open = (options) => {
        type.value = options.type || "success";
        title.value = options.title || getDefaultTitle(type.value);
        text.value = options.text || "";
        timeout.value = options.timeout || 3000;
        color.value = options.color || undefined;
        show.value = true;
    };

    //  helper: 成功捷徑
    const showSuccess = (message) => {
        open({ type: "success", text: message });
    };

    // helper: 失敗捷徑
    const showError = (message) => {
        open({ type: "error", text: message });
    };

    // 根據類型給預設標題
    function getDefaultTitle(t) {
        switch (t) {
            case "success": return "成功";
            case "error": return "錯誤";
            case "warning": return "警告";
            case "info": return "提示";
            default: return "通知";
        }
    }

    return {
        show,
        type,
        title,
        text,
        timeout,
        color,
        open,
        showSuccess,
        showError
    };
});