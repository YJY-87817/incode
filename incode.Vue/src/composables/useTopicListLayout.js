import { computed, nextTick, onBeforeUnmount, onMounted, ref } from "vue";

// 外層卡片的寬度（不是視窗寬度）決定列表要用手機卡片還是桌機表格，以及表頭分頁的緊縮程度
export const TOPIC_LIST_LAYOUT_KEY = Symbol("topicListLayout");
const TOPIC_LIST_MOBILE_BREAKPOINT = 768;
const TOPIC_LIST_DENSE_BREAKPOINT = 992;
const TOPIC_LIST_COMPACT_BREAKPOINT = 1200;
const HEADER_COMPACT_BREAKPOINT = 878;
const HEADER_DENSE_BREAKPOINT = 1200;

/**
 * 量測外層卡片寬度並換算出版面狀態。
 * 外層元件把 `topicListCardRef` 綁在卡片上，再以 provide(TOPIC_LIST_LAYOUT_KEY, ...) 提供給列表元件。
 */
export function useTopicListLayout() {

  const viewportWidth = ref(
    typeof window !== "undefined" ? window.innerWidth : Number.MAX_SAFE_INTEGER,
  );

  const topicListCardRef = ref(null);

  const topicListCardWidth = ref(Number.MAX_SAFE_INTEGER);

  let topicListResizeObserver = null;

  const effectiveTopicListWidth = computed(() => {
    const measuredWidth = Number(topicListCardWidth.value || 0);
    if (Number.isFinite(measuredWidth) && measuredWidth > 0) {
      return measuredWidth;
    }
    return viewportWidth.value;
  });

  const isMobileTopicListWidth = computed(
    () => effectiveTopicListWidth.value < TOPIC_LIST_MOBILE_BREAKPOINT,
  );

  const isDenseTopicListWidth = computed(
    () =>
      !isMobileTopicListWidth.value &&
      effectiveTopicListWidth.value < TOPIC_LIST_DENSE_BREAKPOINT,
  );

  const isCompactTopicListWidth = computed(
    () =>
      !isMobileTopicListWidth.value &&
      effectiveTopicListWidth.value < TOPIC_LIST_COMPACT_BREAKPOINT,
  );

  const showMobileTopicList = computed(() => isMobileTopicListWidth.value);

  const topicListResponsiveClasses = computed(() => ({
    "topic-list--container-max-1200": isCompactTopicListWidth.value,
    "topic-list--container-max-992": isDenseTopicListWidth.value,
    "topic-list--container-max-768": isMobileTopicListWidth.value,
  }));

  const isCompactDesktopHeaderPagination = computed(
    () => effectiveTopicListWidth.value <= HEADER_COMPACT_BREAKPOINT,
  );

  const isDenseDesktopHeaderPagination = computed(
    () => effectiveTopicListWidth.value <= HEADER_DENSE_BREAKPOINT,
  );

  const handleWindowResize = () => {
    if (typeof window === "undefined") return;
    viewportWidth.value = window.innerWidth;

    const cardEl = topicListCardRef.value;
    if (!cardEl) return;

    const nextWidth = Math.round(cardEl.getBoundingClientRect().width || 0);
    if (Number.isFinite(nextWidth) && nextWidth > 0) {
      topicListCardWidth.value = nextWidth;
    }
  };

  onMounted(async () => {
    await nextTick();
    handleWindowResize();

    if (
      typeof window !== "undefined" &&
      typeof ResizeObserver !== "undefined" &&
      topicListCardRef.value
    ) {
      topicListResizeObserver = new ResizeObserver((entries) => {
        const [entry] = entries;
        const width = Math.round(entry?.contentRect?.width || 0);
        if (Number.isFinite(width) && width > 0) {
          topicListCardWidth.value = width;
        }
      });
      topicListResizeObserver.observe(topicListCardRef.value);
    }

    window.addEventListener("resize", handleWindowResize);
  });

  onBeforeUnmount(() => {
    if (topicListResizeObserver) {
      topicListResizeObserver.disconnect();
      topicListResizeObserver = null;
    }

    window.removeEventListener("resize", handleWindowResize);
  });

  return {
    topicListCardRef,
    topicListResponsiveClasses,
    showMobileTopicList,
    isCompactDesktopHeaderPagination,
    isDenseDesktopHeaderPagination,
  };
}
