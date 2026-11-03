<script setup>
import { useEditor, EditorContent } from "@tiptap/vue-3";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import ImageExtension from "@tiptap/extension-image";
import YouTube from "@tiptap/extension-youtube";
import Placeholder from "@tiptap/extension-placeholder";
import { Extension } from "@tiptap/core";
import { NodeSelection, TextSelection } from "prosemirror-state";
import { ref, watch, nextTick, onBeforeUnmount } from "vue";
import { uploadImage } from "@/api/imageApi.js";
import Cropper from "cropperjs";
import "cropperjs/dist/cropper.css";

const props = defineProps({
  modelValue: String,
  placeholder: {
    type: String,
    default: "請輸入內容...",
  },
});

const emit = defineEmits(["update:modelValue"]);

const isUploadingImages = ref(false);
const showCropper = ref(false);
const cropSource = ref("");
const cropper = ref(null);
const cropImageEl = ref(null);
const selectedImageNode = ref(null);
const selectedImagePos = ref(null);
const editorContentRef = ref(null);
let imageClickTimer = null;

const ResizableImage = ImageExtension.extend({
  addAttributes() {
    return {
      ...this.parent?.(),
      width: {
        default: "25%",
        parseHTML: (element) =>
          element.style.width || element.getAttribute("width") || "25%",
        renderHTML: (attributes) => ({
          style: `width: ${attributes.width};`,
        }),
      },
    };
  },
});

const adjustEditorHeight = () => {
  if (!editorContentRef.value) return;
  const containerEl = editorContentRef.value?.$el || editorContentRef.value;
  if (!containerEl || typeof containerEl.querySelector !== "function") return;
  const proseMirror = containerEl.querySelector(".ProseMirror");
  if (proseMirror) {
    proseMirror.style.height = "auto";
    const scrollHeight = proseMirror.scrollHeight;
    proseMirror.style.height = scrollHeight + "px";
  }
};

const PreserveImageOnShiftEnter = Extension.create({
  name: "preserveImageOnShiftEnter",
  addKeyboardShortcuts() {
    return {
      "Shift-Enter": () => {
        const { state, dispatch } = this.editor.view;
        const { selection } = state;

        if (
          selection instanceof NodeSelection &&
          selection.node?.type?.name === "image"
        ) {
          const paragraph = state.schema.nodes.paragraph;
          if (!paragraph) return false;

          const insertPos = selection.to;
          const tr = state.tr.insert(insertPos, paragraph.create());
          tr.setSelection(TextSelection.create(tr.doc, insertPos + 1));
          dispatch(tr);
          this.editor.view.focus();
          return true;
        }

        return false;
      },
    };
  },
});

const editor = useEditor({
  content: props.modelValue || "",
  extensions: [
    StarterKit.configure({
      heading: false,
      blockquote: false,
      codeBlock: false,
      horizontalRule: false,
    }),
    PreserveImageOnShiftEnter,
    Underline,
    ResizableImage.configure({
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
    emit("update:modelValue", html);
    nextTick(() => adjustEditorHeight());
  },
  onCreate: ({ editor: editorInstance }) => {
    // 添加雙擊圖片事件監聽
    const editorElement = editorInstance.view.dom;
    editorElement.addEventListener("click", handleImageClick);
    editorElement.addEventListener("dblclick", handleImageDoubleClick);
    nextTick(() => adjustEditorHeight());
  },
});

const getImageFileFromSrc = async (imgSrc) => {
  try {
    const response = await fetch(imgSrc, { mode: "cors" });
    if (!response.ok) throw new Error("fetch failed");
    const blob = await response.blob();
    return new File([blob], "image.jpg", {
      type: blob.type || "image/jpeg",
    });
  } catch (error) {
    return await new Promise((resolve, reject) => {
      const img = new window.Image();
      img.crossOrigin = "anonymous";
      img.onload = () => {
        try {
          const canvas = document.createElement("canvas");
          const ctx = canvas.getContext("2d");
          canvas.width = img.naturalWidth || img.width;
          canvas.height = img.naturalHeight || img.height;
          ctx.drawImage(img, 0, 0);
          canvas.toBlob(
            (blob) => {
              if (!blob) {
                reject(new Error("無法創建圖片 Blob"));
                return;
              }
              resolve(
                new File([blob], "image.jpg", {
                  type: "image/jpeg",
                }),
              );
            },
            "image/jpeg",
            0.95,
          );
        } catch (canvasError) {
          reject(canvasError);
        }
      };
      img.onerror = () => reject(new Error("圖片載入失敗"));
      img.src = imgSrc;
    });
  }
};

const openCropperFromEvent = async (event) => {
  if (!editor.value) return;
  const target = event.target?.closest?.("img");
  if (!target) return;
  event.preventDefault();

  const imgSrc = target.getAttribute("src");
  if (!imgSrc) return;

  try {
    const pos = editor.value.view.posAtDOM(target, 0);
    selectedImagePos.value = pos;
    selectedImageNode.value = target;
    editor.value.commands.setNodeSelection(pos);

    const file = await getImageFileFromSrc(imgSrc);
    openCropperForEdit(file);
  } catch (error) {
    console.error("[TiptapEditor] 無法載入圖片進行編輯", error);
    alert("無法載入圖片，請稍後再試");
  }
};

const resizeImageFromEvent = (event) => {
  if (!editor.value) return;
  const target = event.target?.closest?.("img");
  if (!target) return;
  event.preventDefault();

  const pos = editor.value.view.posAtDOM(target, 0);
  editor.value.commands.setNodeSelection(pos);

  const currentWidth =
    target.style.width || target.getAttribute("width") || "25%";
  const widthList = ["25%", "50%", "100%"];
  const index = widthList.indexOf(currentWidth);
  const nextWidth = widthList[(index + 1) % widthList.length];

  editor.value.commands.updateAttributes("image", { width: nextWidth });
  nextTick(() => adjustEditorHeight());
};

// 單擊圖片 -> 調整大小
const handleImageClick = (event) => {
  if (imageClickTimer) return;
  imageClickTimer = window.setTimeout(() => {
    resizeImageFromEvent(event);
    imageClickTimer = null;
  }, 200);
};

// 雙擊圖片
const handleImageDoubleClick = (event) => {
  if (imageClickTimer) {
    clearTimeout(imageClickTimer);
    imageClickTimer = null;
  }
  openCropperFromEvent(event);
};

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
      editor.value.commands.setContent(newValue);
    }
  },
  { flush: "post" },
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

    const validUrls = urlList.filter(Boolean);
    if (!validUrls.length) {
      console.warn("[TiptapEditor] insertImage - 所有 URL 皆為空");
      return;
    }

    const content = validUrls.flatMap((url, index) => {
      const nodes = [{ type: "image", attrs: { src: url } }];
      if (index < validUrls.length - 1) {
        nodes.push({ type: "paragraph" });
      }
      return nodes;
    });

    try {
      const result = currentEditor.chain().focus().insertContent(content).run();

      if (result) {
        setTimeout(() => {
          const html = currentEditor.getHTML();
          if (html.includes("<img")) {
            adjustEditorHeight();
          } else {
            console.warn(
              "[TiptapEditor] insertImage - ✗ 圖片插入後未找到 <img 標籤",
            );
          }
        }, 50);
      }
    } catch (err) {
      console.error("[TiptapEditor] insertImage - 插入圖片失敗:", err);
    }
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
    nextTick(() => adjustEditorHeight());
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

// 打開裁剪器進行編輯
const openCropperForEdit = (file) => {
  const url = URL.createObjectURL(file);
  cropSource.value = url;
  showCropper.value = true;
  nextTick(() => {
    if (cropper.value) {
      cropper.value.destroy();
      cropper.value = null;
    }
    if (!cropImageEl.value) return;
    cropper.value = new Cropper(cropImageEl.value, {
      viewMode: 1,
      aspectRatio: NaN,
      autoCropArea: 1,
      background: false,
      responsive: true,
    });
  });
};

// 關閉裁剪器
const closeCropper = () => {
  if (cropper.value) {
    cropper.value.destroy();
    cropper.value = null;
  }
  if (cropSource.value) URL.revokeObjectURL(cropSource.value);
  showCropper.value = false;
  cropSource.value = "";
  selectedImageNode.value = null;
  selectedImagePos.value = null;
};

// 插入裁剪後的圖片
const insertCroppedImage = async () => {
  if (!cropper.value || !editor.value) return;
  const canvas = cropper.value.getCroppedCanvas();
  if (!canvas) {
    closeCropper();
    return;
  }

  isUploadingImages.value = true;
  try {
    const blob = await new Promise((resolve) =>
      canvas.toBlob(resolve, "image/jpeg", 0.9),
    );

    // 嘗試上傳圖片
    let src;
    try {
      const urls = await uploadImage([
        new File([blob], "cropped.jpg", { type: "image/jpeg" }),
      ]);
      src = Array.isArray(urls) ? urls[0] : urls;
    } catch (uploadError) {
      console.error(
        "[TiptapEditor] 裁剪圖片上傳失敗，使用 base64:",
        uploadError,
      );
      src = canvas.toDataURL("image/jpeg", 0.9);
    }

    // 如果有選中的圖片位置，則替換該圖片，否則插入新圖片
    if (selectedImagePos.value !== null) {
      const doc = editor.value.view.state.doc;
      let pos = selectedImagePos.value;
      let node = doc.nodeAt(pos);

      if (!node || node.type.name !== "image") {
        const resolved = doc.resolve(pos);
        if (resolved.nodeAfter && resolved.nodeAfter.type.name === "image") {
          node = resolved.nodeAfter;
          pos = resolved.pos;
        } else if (
          resolved.nodeBefore &&
          resolved.nodeBefore.type.name === "image"
        ) {
          node = resolved.nodeBefore;
          pos = resolved.pos - resolved.nodeBefore.nodeSize;
        }
      }

      if (node && node.type.name === "image") {
        editor.value.commands.insertContentAt(
          { from: pos, to: pos + node.nodeSize },
          { type: "image", attrs: { src } },
        );
      } else {
        editor.value.commands.setImage({ src });
      }
    } else {
      // 插入新圖片
      editor.value.commands.setImage({ src });
    }
  } catch (error) {
    console.error("[TiptapEditor] 插入裁剪圖片失敗:", error);
    alert("插入裁剪圖片失敗，請稍後再試");
  } finally {
    isUploadingImages.value = false;
    closeCropper();
    nextTick(() => adjustEditorHeight());
  }
};

// 組件銷毀前清理
onBeforeUnmount(() => {
  if (editor.value) {
    const editorElement = editor.value.view.dom;
    editorElement.removeEventListener("click", handleImageClick);
    editorElement.removeEventListener("dblclick", handleImageDoubleClick);
    editor.value.destroy();
  }
  if (cropper.value) cropper.value.destroy();
  if (cropSource.value) URL.revokeObjectURL(cropSource.value);
});
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

      <div v-if="isUploadingImages" class="upload-status">上傳中...</div>
    </div>

    <EditorContent
      v-if="editor"
      ref="editorContentRef"
      :editor="editor"
      class="editor-content"
    />
  </div>

  <!-- 圖片裁剪器彈窗 -->
  <div v-if="showCropper" class="cropper-dialog" @click.self="closeCropper">
    <div class="cropper-container">
      <div class="cropper-header">
        <h5>裁剪圖片</h5>
        <button type="button" class="close-btn" @click="closeCropper">✕</button>
      </div>
      <div class="cropper-body">
        <img ref="cropImageEl" :src="cropSource" alt="Crop Image" />
      </div>
      <div class="cropper-footer">
        <button type="button" class="btn-cancel" @click="closeCropper">
          取消
        </button>
        <button
          type="button"
          class="btn-confirm"
          @click="insertCroppedImage"
          :disabled="isUploadingImages"
        >
          {{ isUploadingImages ? "處理中..." : "確認裁剪" }}
        </button>
      </div>
    </div>
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
  min-width: 44px;
  height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
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
  min-width: 44px;
  height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
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
  background-color: #fff;
}

:deep(.ProseMirror) {
  outline: none;
  min-height: 4.5rem;
  padding: 0;
  resize: none;
  overflow-y: hidden;
  line-height: 1.5rem;
}

:deep(.ProseMirror p) {
  margin: 0.5rem 0;
}

:deep(.ProseMirror img) {
  width: 25%;
  max-width: 100%;
  height: auto;
  margin: 0.5rem 0;
  border-radius: 0.25rem;
  cursor: pointer;
  pointer-events: auto;
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

/* 裁剪器模態框樣式 */
.cropper-dialog {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.35);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}

/* Cropper.js 遮罩（裁切區外的深色層） */
:deep(.cropper-modal) {
  background-color: rgba(0, 0, 0, 0.25);
  opacity: 1;
}

.cropper-container {
  background: #fff;
  border-radius: 0.5rem;
  max-width: 90vw;
  max-height: 90vh;
  width: 800px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 10px 40px rgba(0, 0, 0);
}

.cropper-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  border-bottom: 1px solid #d1d3d4;
  background-color: rgba(255, 255, 255, 0.95);
}

.cropper-header h5 {
  margin: 0;
  font-size: 1.25rem;
  color: #145369;
}

.close-btn {
  background: transparent;
  border: none;
  font-size: 1.5rem;
  color: #666;
  cursor: pointer;
  padding: 0;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.25rem;
  transition: all 0.2s;
}

.close-btn:hover {
  background: #f0f0f0;
  color: #145369;
}

.cropper-body {
  padding: 1rem;
  overflow: auto;
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(248, 249, 250, 0.95);
}

.cropper-body img {
  max-width: 100%;
  display: block;
}

.cropper-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1rem 1.5rem;
  border-top: 1px solid #d1d3d4;
}

.btn-cancel,
.btn-confirm {
  padding: 0.5rem 1.5rem;
  border-radius: 0.25rem;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid #d1d3d4;
}

.btn-cancel {
  background: #fff;
  color: #145369;
}

.btn-cancel:hover {
  background: #f8f9fa;
  border-color: #86b0bd;
}

.btn-confirm {
  background: #145369;
  color: #fff;
  border-color: #145369;
}

.btn-confirm:hover:not(:disabled) {
  background: #86b0bd;
  border-color: #86b0bd;
}

.btn-confirm:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .cropper-container {
    width: 95vw;
    max-height: 85vh;
  }

  .cropper-header,
  .cropper-footer {
    padding: 0.75rem 1rem;
  }
}
</style>
