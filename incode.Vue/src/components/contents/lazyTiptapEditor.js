import { defineAsyncComponent, h } from "vue";

// Tiptap 編輯器（含 ProseMirror）約 400 KB，只在開啟發文 / 回覆編輯器時才載入，
// 論壇列表與文章頁不需要下載。
export const TiptapEditor = defineAsyncComponent({
  loader: () => import("@/components/contents/TiptapEditor.vue"),
  loadingComponent: {
    name: "TiptapEditorLoading",
    render: () => h("div", { class: "text-muted small py-4 text-center" }, "編輯器載入中…"),
  },
  delay: 150,
});
