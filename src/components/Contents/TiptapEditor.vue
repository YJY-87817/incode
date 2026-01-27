<script setup>
import { useEditor, EditorContent } from "@tiptap/vue-3";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import Image from "@tiptap/extension-image";
import YouTube from "@tiptap/extension-youtube";
import Placeholder from "@tiptap/extension-placeholder";
import { ref, watch } from "vue";
import { uploadImage } from "@/api/imageapi.js";

const props = defineProps({
  modelValue: String,
  placeholder: {
    type: String,
    default: "請輸入內容...",
  },
});

const emit = defineEmits(["update:modelValue"]);

const isUploadingImages = ref(false);

const editor = useEditor({
  content: props.modelValue || "",
  extensions: [
    StarterKit.configure({
      heading: false,
      blockquote: false,
      codeBlock: false,
      horizontalRule: false,
    }),
    Underline,
    Image.configure({
      allowBase64: true,
    }),
    YouTube.configure({
      width: 640,
      height: 480,
      controls: true,
      nocookie: false,
    }),
    Placeholder.configure({
      placeholder: props.placeholder,
    }),
  ],
  onUpdate: ({ editor: editorInstance }) => {
    const html = editorInstance.getHTML();
    console.log("[TiptapEditor] onUpdate - 發送 HTML:", html);
    emit("update:modelValue", html);
  },
});

// 監聽 props.modelValue 變化，同步到編輯器
// 只在編輯器完全初始化後同步，避免頻繁更新
watch(
  () => props.modelValue,
  (newValue) => {
    if (
      editor.value &&
      newValue &&
      typeof editor.value.getHTML === "function" &&
      editor.value.getHTML() !== newValue
    ) {
      console.log(
        "[TiptapEditor] watch - props.modelValue 改變，更新編輯器內容"
      );
      editor.value.commands.setContent(newValue);
    }
  },
  { flush: "post" }
);

const insertImage = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;

  // useEditor 返回 ref，需要使用 .value 訪問實際的編輯器實例
  const currentEditor = editor.value;
  if (!currentEditor) {
    console.error("[TiptapEditor] insertImage - 編輯器尚未初始化");
    return;
  }

  isUploadingImages.value = true;
  try {
    const urls = await uploadImage(files);
    const urlList = Array.isArray(urls) ? urls : [urls];

    console.log("[TiptapEditor] insertImage - 收到的 URLs:", urlList);

    urlList.forEach((url) => {
      if (!url) {
        console.warn("[TiptapEditor] insertImage - URL 為空");
        return;
      }

      console.log("[TiptapEditor] insertImage - 插入圖片:", url);

      try {
        // 使用 chain 來執行命令序列
        const result = currentEditor
          .chain()
          .focus()
          .setImage({ src: url })
          .run();

        console.log("[TiptapEditor] insertImage - setImage 執行結果:", result);

        // 驗證圖片是否已插入
        if (result) {
          setTimeout(() => {
            const html = currentEditor.getHTML();
            console.log(
              "[TiptapEditor] insertImage - 插入後的編輯器 HTML:",
              html
            );

            // 檢查是否包含圖片標籤
            if (html.includes("<img")) {
              console.log(
                "[TiptapEditor] insertImage - ✓ 圖片已成功插入到編輯器"
              );
            } else {
              console.warn(
                "[TiptapEditor] insertImage - ✗ 圖片插入後未找到 <img 標籤"
              );
            }
          }, 50);
        }
      } catch (err) {
        console.error("[TiptapEditor] insertImage - 插入圖片失敗:", err);
      }
    });
  } catch (error) {
    console.error("[TiptapEditor] insertImage - 上傳失敗:", error);
    console.error("[TiptapEditor] insertImage - 錯誤詳情:", {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status,
    });
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    isUploadingImages.value = false;
    if (event.target) event.target.value = "";
  }
};

const insertYouTubeVideo = () => {
  const url = prompt("請輸入 YouTube 影片網址:");
  if (!url || !editor.value) return;

  editor.value.chain().focus().setYoutubeVideo({ src: url }).run();
};

const toggleBold = () => {
  editor.value?.chain().focus().toggleBold().run();
};

const toggleItalic = () => {
  editor.value?.chain().focus().toggleItalic().run();
};

const toggleUnderline = () => {
  editor.value?.chain().focus().toggleUnderline().run();
};

const undo = () => {
  editor.value?.chain().focus().undo().run();
};

const redo = () => {
  editor.value?.chain().focus().redo().run();
};
</script>

<template>
  <div class="tiptap-editor-wrapper">
    <div class="editor-toolbar">
      <div class="toolbar-group">
        <button
          type="button"
          @click="toggleBold"
          :class="{ 'is-active': editor?.value?.isActive('bold') }"
          class="toolbar-btn"
          title="粗體"
        >
          <strong>B</strong>
        </button>
        <button
          type="button"
          @click="toggleItalic"
          :class="{ 'is-active': editor?.value?.isActive('italic') }"
          class="toolbar-btn"
          title="斜體"
        >
          <em>I</em>
        </button>
        <button
          type="button"
          @click="toggleUnderline"
          :class="{ 'is-active': editor?.value?.isActive('underline') }"
          class="toolbar-btn"
          title="底線"
        >
          <u>U</u>
        </button>
      </div>

      <div class="toolbar-group">
        <label class="toolbar-btn toolbar-file-input" title="上傳圖片">
          <span>🖼️ 圖片</span>
          <input
            type="file"
            accept="image/*"
            multiple
            @change="insertImage"
            :disabled="isUploadingImages"
          />
        </label>
        <button
          type="button"
          @click="insertYouTubeVideo"
          class="toolbar-btn"
          title="嵌入YouTube影片"
        >
          🎥 影片
        </button>
      </div>

      <div class="toolbar-group">
        <button
          type="button"
          @click="undo"
          :disabled="!editor?.value?.can().undo()"
          class="toolbar-btn"
          title="復原"
        >
          ↶
        </button>
        <button
          type="button"
          @click="redo"
          :disabled="!editor?.value?.can().redo()"
          class="toolbar-btn"
          title="重做"
        >
          ↷
        </button>
      </div>

      <div v-if="isUploadingImages" class="upload-status">上傳中...</div>
    </div>

    <EditorContent v-if="editor" :editor="editor" class="editor-content" />
  </div>
</template>

<style scoped>
.tiptap-editor-wrapper {
  border: 1px solid #d1d3d4;
  border-radius: 0.25rem;
  overflow: hidden;
}

.editor-toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.75rem;
  background-color: #f8f9fa;
  border-bottom: 1px solid #d1d3d4;
  align-items: center;
}

.toolbar-group {
  display: flex;
  gap: 0.25rem;
  border-right: 1px solid #d1d3d4;
  padding-right: 0.5rem;
}

.toolbar-group:last-child {
  border-right: none;
}

.toolbar-btn {
  background-color: #fff;
  border: 1px solid #d1d3d4;
  padding: 0.4rem 0.6rem;
  border-radius: 0.2rem;
  cursor: pointer;
  font-size: 0.875rem;
  transition: all 0.2s ease;
  min-width: 32px;
  text-align: center;
}

.toolbar-btn:hover:not(:disabled) {
  background-color: #e9ecef;
  border-color: #86b0bd;
}

.toolbar-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.toolbar-btn.is-active {
  background-color: #145369;
  color: #fff;
  border-color: #145369;
}

.toolbar-file-input {
  position: relative;
  overflow: hidden;
}

.toolbar-file-input input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
  width: 100%;
  height: 100%;
}

.toolbar-file-input:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.upload-status {
  color: #145369;
  font-size: 0.875rem;
  margin-left: auto;
}

.editor-content {
  padding: 0.75rem;
  min-height: 200px;
  background-color: #fff;
}

:deep(.ProseMirror) {
  outline: none;
  min-height: 200px;
  padding: 0;
}

:deep(.ProseMirror p) {
  margin: 0.5rem 0;
}

:deep(.ProseMirror img) {
  max-width: 100%;
  height: auto;
  margin: 0.5rem 0;
  border-radius: 0.25rem;
}

:deep(.ProseMirror iframe) {
  margin: 0.5rem 0;
  border-radius: 0.25rem;
}

:deep(.ProseMirror strong) {
  font-weight: bold;
}

:deep(.ProseMirror em) {
  font-style: italic;
}

:deep(.ProseMirror u) {
  text-decoration: underline;
}

:deep(.ProseMirror ul) {
  padding-left: 1.5rem;
  margin: 0.5rem 0;
}

:deep(.ProseMirror ol) {
  padding-left: 1.5rem;
  margin: 0.5rem 0;
}

:deep(.ProseMirror li) {
  margin: 0.25rem 0;
}
</style>
