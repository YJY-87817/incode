import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useForumStore } from "@/stores/forumStore.js";

const SORT_CONTROL_ANIMATION_MS = 160;

const sortOptions = [
  { value: "latest", label: "最新發佈時間" },
  { value: "latest-reply", label: "最新回應時間" },
  { value: "most-replies", label: "最多回應數量" },
  { value: "most-likes", label: "最多按讚數量" },
];

/**
 * 主題排序下拉：展開 / 收合動畫、點外面或按 Esc 關閉。
 * 手機與桌機版面各有一個下拉，所以 `desktopSortDropdownRef` / `mobileSortDropdownRef` 由列表元件綁在各自的容器上。
 */
export function useTopicSort() {
  const forumStore = useForumStore();

  const sortOption = computed({
    get: () => forumStore.sortOption,
    set: (value) => forumStore.setSortOption(value),
  });

  const showSortDropdown = ref(false);

  const desktopSortDropdownRef = ref(null);

  const mobileSortDropdownRef = ref(null);

  const hasSelectedSortOption = ref(false);

  const shouldHideSortIcon = ref(false);

  const isSortControlCompact = ref(false);

  let restoreSortIconTimer = null;

  const sortLabel = computed(() => {
    const match = sortOptions.find((option) => option.value === sortOption.value);
    return match?.label || "";
  });

  const sortInputLabel = computed(() => {
    return hasSelectedSortOption.value ? sortLabel.value : "主題排序依據";
  });

  const sortControlWidth = computed(() => {
    const longestLabelLength = sortOptions.reduce(
      (maxLength, option) =>
        Math.max(maxLength, String(option?.label || "").length),
      0,
    );
    const iconReservedWidth = isSortControlCompact.value ? 1.3 : 2.3;
    return `${longestLabelLength + iconReservedWidth}em`;
  });

  const clearRestoreSortIconTimer = () => {
    if (restoreSortIconTimer) {
      clearTimeout(restoreSortIconTimer);
      restoreSortIconTimer = null;
    }
  };

  const closeSortDropdown = () => {
    if (!showSortDropdown.value && !isSortControlCompact.value) return;

    clearRestoreSortIconTimer();
    showSortDropdown.value = false;
    isSortControlCompact.value = false;
    restoreSortIconTimer = setTimeout(() => {
      shouldHideSortIcon.value = false;
      restoreSortIconTimer = null;
    }, SORT_CONTROL_ANIMATION_MS);
  };

  const openSortDropdown = () => {
    if (showSortDropdown.value) return;

    clearRestoreSortIconTimer();
    shouldHideSortIcon.value = true;

    const runOpenTransition = () => {
      isSortControlCompact.value = true;
      showSortDropdown.value = true;
    };

    if (
      typeof window !== "undefined" &&
      typeof window.requestAnimationFrame === "function"
    ) {
      window.requestAnimationFrame(runOpenTransition);
      return;
    }

    setTimeout(runOpenTransition, 0);
  };

  const selectSortOption = (option) => {
    if (!option) return;
    sortOption.value = option.value;
    hasSelectedSortOption.value = true;
    closeSortDropdown();
  };

  const toggleSortDropdown = () => {
    if (showSortDropdown.value) {
      closeSortDropdown();
      return;
    }
    openSortDropdown();
  };

  const handleDocumentClick = (event) => {
    if (!showSortDropdown.value) return;
    const isClickInsideSortControls = [
      desktopSortDropdownRef.value,
      mobileSortDropdownRef.value,
    ].some((container) => container && container.contains(event.target));

    if (!isClickInsideSortControls) {
      closeSortDropdown();
    }
  };

  const handleDocumentKeydown = (event) => {
    if (event.key !== "Escape") return;
    if (showSortDropdown.value) {
      closeSortDropdown();
    }
  };

  onMounted(() => {
    document.addEventListener("click", handleDocumentClick);
    document.addEventListener("keydown", handleDocumentKeydown);
  });

  onBeforeUnmount(() => {
    clearRestoreSortIconTimer();

    document.removeEventListener("click", handleDocumentClick);
    document.removeEventListener("keydown", handleDocumentKeydown);
  });

  return {
    sortOption,
    sortOptions,
    showSortDropdown,
    desktopSortDropdownRef,
    mobileSortDropdownRef,
    shouldHideSortIcon,
    isSortControlCompact,
    sortInputLabel,
    sortControlWidth,
    selectSortOption,
    toggleSortDropdown,
  };
}
