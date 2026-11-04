// 篩選面板：桌面版 (>= md) 預設展開，手機版維持收合
// 只操作 class，不需引入 Bootstrap JS 物件
import { onMounted, ref } from "vue";

const MD_BREAKPOINT = 768;

export function useDesktopExpand() {
  const collapseRef = ref(null);
  const headerRef = ref(null);

  onMounted(() => {
    if (window.innerWidth < MD_BREAKPOINT) return;
    if (!collapseRef.value || !headerRef.value) return;

    collapseRef.value.classList.add("show");
    headerRef.value.classList.remove("collapsed");
    headerRef.value.setAttribute("aria-expanded", "true");
  });

  return { collapseRef, headerRef };
}
