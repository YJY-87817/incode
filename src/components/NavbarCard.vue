<script setup>
import { onMounted, ref } from "vue";

const navbarCollapseRef = ref(null);
const navbarHeaderRef = ref(null);

onMounted(() => {
    // 設定斷點，768px 對應 Bootstrap 的 md
    const mdBreakpoint = 768;

    // 檢查目前視窗寬度是否大於等於 md
    if (window.innerWidth >= mdBreakpoint) {
        if (navbarCollapseRef.value && navbarHeaderRef.value) {
            // 1. 內容區塊加上 'show' 讓它顯示
            navbarCollapseRef.value.classList.add("show");

            // 2. Header 去除 'collapsed' (這是 Bootstrap 控制箭頭旋轉的 class)
            navbarHeaderRef.value.classList.remove("collapsed");

            // 3. 更新無障礙屬性
            navbarHeaderRef.value.setAttribute("aria-expanded", "true");
        }
    }
});
</script>

<template>
    <div class="card shadow-sm border-0 sticky-top mb-3" style="top: 80px">
        <div ref="navbarHeaderRef"
            class="card-header text-white d-flex justify-content-between align-items-center collapsed "
            style="cursor: pointer; background-color: #86B0BD" data-bs-toggle="collapse"
            data-bs-target="#collapseNavbar" aria-expanded="false" aria-controls="collapseNavbar">
            <h6 class="mb-0 "><i class="bi bi-compass"></i> 網站導覽</h6>
            <i class="bi bi-chevron-down transition-icon"></i>
        </div>

        <div class="collapse" id="collapseNavbar" ref="navbarCollapseRef">
            <div class="card-body p-3">
                <router-link :to="{ name: 'UserIndex' }" class="text-decoration-none text-dark">
                    <div class="mb-2 border-bottom pb-2 mouse-pointer">
                        <label class="form-label fw-bold small mb-1" style="cursor: pointer;">
                            <i class="bi bi-person-badge"></i> 會員中心
                        </label>
                    </div>
                </router-link>
                <router-link :to="{ name: 'forum' }" class="text-decoration-none text-dark">
                    <div class="mb-2 border-bottom pb-2 mouse-pointer">
                        <label class="form-label fw-bold small mb-1" style="cursor: pointer;">
                            <i class="bi bi-chat-dots"></i> 討論區
                        </label>
                    </div>
                </router-link>
                <router-link :to="{ name: 'mission' }" class="text-decoration-none text-dark">
                    <div class="mb-2 border-bottom pb-2 mouse-pointer">
                        <label class="form-label fw-bold small mb-1" style="cursor: pointer;">
                            <i class="bi bi-trophy"></i> 任務大廳
                        </label>
                    </div>
                </router-link>
                <router-link :to="{ name: 'PointShop' }" class="text-decoration-none text-dark">
                    <div class="mb-2 mouse-pointer">
                        <label class="form-label fw-bold small mb-1" style="cursor: pointer;">
                            <i class="bi bi-shop"></i> 點數商城
                        </label>
                    </div>
                </router-link>
            </div>
        </div>
    </div>
</template>

<style scoped>
.sticky-top {
    z-index: 10;
}

/* 當 header 沒有 .collapsed class 時 (代表展開中)，箭頭旋轉 180度 */
.card-header:not(.collapsed) .bi-chevron-down {
    transform: rotate(180deg);
}

/* 加上過渡動畫效果 */
.bi-chevron-down {
    transition: transform 0.3s ease;
}

/* 修正收合時底部圓角消失的問題 */
.card-header.collapsed {
    border-bottom-left-radius: 0.375rem;
    border-bottom-right-radius: 0.375rem;
}

.mouse-pointer:hover {
    background-color: rgba(0, 0, 0, 0.03);
    border-radius: 5px;
    padding-left: 5px;
    transition: all 0.2s ease;
}
</style>
