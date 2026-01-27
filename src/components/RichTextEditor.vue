<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from "vue";
import { useEditor, EditorContent } from "@tiptap/vue-3";
import StarterKit from "@tiptap/starter-kit";
import Link from "@tiptap/extension-link";
import Placeholder from "@tiptap/extension-placeholder";
import Youtube from "@tiptap/extension-youtube";
import Image from "@tiptap/extension-image";
import Cropper from "cropperjs";
import "cropperjs/dist/cropper.css";

// Extend the Image node to accept width styling for simple resizing.
const ResizableImage = Image.extend({
  addAttributes() {
    return {
      ...this.parent?.(),
      width: {
        default: "100%",
        parseHTML: (element) => element.style.width || element.getAttribute("width") || "100%",
        renderHTML: (attributes) => ({ style: `width: ${attributes.width};` }),
      },
    };
  },
});

const props = defineProps({
  modelValue: { type: String, default: "" },
  placeholder: { type: String, default: "在此輸入內容..." },
  imageUpload: {
    // Optional async hook: (file: Blob | File) => Promise<string>
    type: Function,
    default: null,
  },
});

const emit = defineEmits(["update:modelValue"]);

const widthPreset = ref("100%");
const showCropper = ref(false);
const cropSource = ref("");
const cropper = ref(null);
const cropImageEl = ref(null);
const pendingFile = ref(null);
const youtubeUrl = ref("");

const editor = useEditor({
  content: props.modelValue,
  extensions: [
    StarterKit,
    Link.configure({ openOnClick: false, autolink: true, linkOnPaste: true }),
    Placeholder.configure({ placeholder: props.placeholder }),
    ResizableImage.configure({ allowBase64: true }),
    Youtube.configure({ controls: true, nocookie: true }),
  ],
  onUpdate: ({ editor }) => {
    emit("update:modelValue", editor.getHTML());
  },
  onCreate: ({ editor }) => {
    // 添加雙擊圖片事件監聽
    const editorElement = editor.view.dom;
    editorElement.addEventListener('dblclick', handleImageDoubleClick);
  },
});

const handleImageDoubleClick = async (event) => {
  const target = event.target;
  if (target.tagName === 'IMG' && editor.value) {
    event.preventDefault();
    
    // 獲取圖片的 src
    const imgSrc = target.getAttribute('src');
    if (!imgSrc) return;
    
    try {
      // 將圖片轉換為 Blob
      const response = await fetch(imgSrc);
      const blob = await response.blob();
      const file = new File([blob], 'image.jpg', { type: blob.type });
      
      // 保存當前選中的圖片節點位置，以便替換
      const pos = editor.value.view.posAtDOM(target, 0);
      editor.value.commands.setNodeSelection(pos);
      
      // 打開裁切器
      openCropperForEdit(file);
    } catch (error) {
      console.error('無法載入圖片進行編輯', error);
    }
  }
};

watch(
  () => props.modelValue,
  (value) => {
    if (!editor.value) return;
    if (editor.value.getHTML() === value) return;
    editor.value.commands.setContent(value || "", false);
  }
);

const isBold = computed(() => editor.value?.isActive("bold"));
const isItalic = computed(() => editor.value?.isActive("italic"));
const isStrike = computed(() => editor.value?.isActive("strike"));
const isBullet = computed(() => editor.value?.isActive("bulletList"));
const isOrdered = computed(() => editor.value?.isActive("orderedList"));
const isQuote = computed(() => editor.value?.isActive("blockquote"));

const runCommand = (fn) => {
  if (!editor.value) return;
  fn(editor.value.chain().focus());
};

const openImagePicker = () => {
  if (!editor.value) return;
  const input = document.createElement("input");
  input.type = "file";
  input.accept = "image/*";
  input.multiple = true;
  input.onchange = (event) => handleImageSelect(event.target.files);
  input.click();
};

const handleImageSelect = async (files) => {
  if (!files || files.length === 0) return;
  
  // 批量插入所有選中的圖片
  for (const file of files) {
    await insertImageDirectly(file);
  }
};

const insertImageDirectly = async (file) => {
  if (!file || !editor.value) return;
  
  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onload = async (e) => {
      let src = e.target.result;
      
      // 如果有上傳功能，嘗試上傳
      if (props.imageUpload) {
        try {
          const uploaded = await props.imageUpload(file);
          if (uploaded) src = uploaded;
        } catch (error) {
          console.error("Image upload failed", error);
        }
      }
      
      editor.value.commands.setImage({ src, alt: file.name || "image", width: widthPreset.value });
      
      // 等待 DOM 更新後再繼續
      await nextTick();
      setTimeout(() => resolve(), 10);
    };
    reader.readAsDataURL(file);
  });
};

const openCropperForEdit = (file) => {
  pendingFile.value = file;
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
    });
  });
};

const closeCropper = () => {
  if (cropper.value) {
    cropper.value.destroy();
    cropper.value = null;
  }
  if (cropSource.value) URL.revokeObjectURL(cropSource.value);
  showCropper.value = false;
  cropSource.value = "";
  pendingFile.value = null;
};

const insertCroppedImage = async () => {
  if (!cropper.value || !editor.value) return;
  const canvas = cropper.value.getCroppedCanvas();
  if (!canvas) {
    closeCropper();
    return;
  }

  const blob = await new Promise((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.9));
  let src = canvas.toDataURL("image/jpeg", 0.9);

  if (props.imageUpload && blob) {
    try {
      const uploaded = await props.imageUpload(blob);
      if (uploaded) src = uploaded;
    } catch (error) {
      console.error("Image upload failed", error);
    }
  }

  // 檢查是否已選中圖片（編輯模式），如果是則替換，否則插入新圖片
  const { selection } = editor.value.state;
  const node = selection.$from.parent;
  
  if (node && node.type.name === 'image') {
    // 更新現有圖片
    editor.value
      .chain()
      .focus()
      .updateAttributes('image', { src, alt: pendingFile.value?.name || "image", width: widthPreset.value })
      .run();
  } else {
    // 插入新圖片
    editor.value
      .chain()
      .focus()
      .setImage({ src, alt: pendingFile.value?.name || "image", width: widthPreset.value })
      .run();
  }

  closeCropper();
};

const setImageWidth = (width) => {
  widthPreset.value = width;
  if (!editor.value) return;
  editor.value.commands.updateAttributes("image", { width });
};

const insertYoutube = () => {
  if (!editor.value || !youtubeUrl.value) return;
  const url = youtubeUrl.value.trim();
  if (!isYoutubeUrl(url)) return;
  editor.value
    .chain()
    .focus()
    .setYoutubeVideo({ src: url, width: 640, height: 360 })
    .run();
  youtubeUrl.value = "";
};

const isYoutubeUrl = (url) => {
  const pattern = /^(https?:\/\/)?(www\.)?(youtube\.com\/watch\?v=|youtu\.be\/).+/i;
  return pattern.test(url);
};

onBeforeUnmount(() => {
  if (editor.value) {
    const editorElement = editor.value.view.dom;
    editorElement.removeEventListener('dblclick', handleImageDoubleClick);
    editor.value.destroy();
  }
  if (cropper.value) cropper.value.destroy();
  if (cropSource.value) URL.revokeObjectURL(cropSource.value);
});
</script>

<template>
  <div class="rte">
    <div class="toolbar">
      <button :class="{ active: isBold }" @click="runCommand((c) => c.toggleBold().run())">B</button>
      <button :class="{ active: isItalic }" @click="runCommand((c) => c.toggleItalic().run())">I</button>
      <button :class="{ active: isStrike }" @click="runCommand((c) => c.toggleStrike().run())">S</button>
      <button @click="runCommand((c) => c.toggleHeading({ level: 1 }).run())">H1</button>
      <button @click="runCommand((c) => c.toggleHeading({ level: 2 }).run())">H2</button>
      <button :class="{ active: isBullet }" @click="runCommand((c) => c.toggleBulletList().run())">•</button>
      <button :class="{ active: isOrdered }" @click="runCommand((c) => c.toggleOrderedList().run())">1.</button>
      <button :class="{ active: isQuote }" @click="runCommand((c) => c.toggleBlockquote().run())">❝</button>
      <button @click="runCommand((c) => c.toggleCodeBlock().run())">{}</button>
      <button @click="runCommand((c) => c.undo().run())">⟲</button>
      <button @click="runCommand((c) => c.redo().run())">⟳</button>

      <div class="divider" />

      <div class="image-controls">
        <span>圖片寬度:</span>
        <button :class="{ active: widthPreset === '25%' }" @click="setImageWidth('25%')">25%</button>
        <button :class="{ active: widthPreset === '50%' }" @click="setImageWidth('50%')">50%</button>
        <button :class="{ active: widthPreset === '75%' }" @click="setImageWidth('75%')">75%</button>
        <button :class="{ active: widthPreset === '100%' }" @click="setImageWidth('100%')">100%</button>
      </div>

      <button class="accent" @click="openImagePicker">插入圖片</button>

      <div class="youtube-input">
        <input
          v-model="youtubeUrl"
          placeholder="貼上 YouTube 連結"
          @keyup.enter="insertYoutube"
        />
        <button class="accent" @click="insertYoutube">插入影片</button>
      </div>
    </div>

    <div class="editor-shell">
      <EditorContent :editor="editor" class="editor" />
    </div>

    <div v-if="showCropper" class="cropper-backdrop">
      <div class="cropper-card">
        <div class="cropper-header">裁切圖片</div>
        <div class="cropper-body">
          <img ref="cropImageEl" :src="cropSource" alt="crop" />
        </div>
        <div class="cropper-actions">
          <button @click="closeCropper">取消</button>
          <button class="accent" @click="insertCroppedImage">完成並插入</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.rte {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  background: #fff;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.04);
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 10px;
  border-bottom: 1px solid #e0e0e0;
  background: #fafafa;
  align-items: center;
}

.toolbar button {
  border: 1px solid #d0d0d0;
  background: #fff;
  padding: 6px 10px;
  border-radius: 6px;
  cursor: pointer;
  min-width: 36px;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.toolbar button:hover {
  background: #f0f0f0;
}

.toolbar button.active {
  background: #1976d2;
  color: #fff;
  border-color: #1976d2;
}

.toolbar .accent {
  background: #4caf50;
  color: #fff;
  border-color: #4caf50;
}

.toolbar .accent:hover {
  background: #449d48;
}

.divider {
  width: 1px;
  height: 24px;
  background: #d9d9d9;
  margin: 0 8px;
}

.image-controls {
  display: flex;
  gap: 6px;
  align-items: center;
}

.image-controls span {
  font-size: 14px;
  color: #555;
}

.youtube-input {
  display: flex;
  gap: 6px;
  align-items: center;
  flex: 1;
  min-width: 220px;
}

.youtube-input input {
  flex: 1;
  min-width: 160px;
  padding: 7px 10px;
  border: 1px solid #d0d0d0;
  border-radius: 6px;
}

.editor-shell {
  padding: 12px;
}

.editor {
  min-height: 260px;
}

:deep(.ProseMirror) {
  outline: none;
}

:deep(.ProseMirror > * + *) {
  margin-top: 0.75em;
}

:deep(.ProseMirror img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 12px auto;
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  cursor: pointer;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

:deep(.ProseMirror img:hover) {
  transform: scale(1.02);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

:deep(.ProseMirror blockquote) {
  border-left: 4px solid #1976d2;
  padding-left: 12px;
  color: #444;
}

:deep(.ProseMirror ul),
:deep(.ProseMirror ol) {
  padding-left: 22px;
}

.cropper-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 999;
  padding: 16px;
}

.cropper-card {
  background: #fff;
  border-radius: 10px;
  width: min(85vw, 600px);
  max-height: 85vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
}

.cropper-header {
  padding: 12px 16px;
  font-weight: 600;
  border-bottom: 1px solid #eee;
}

.cropper-body {
  padding: 12px;
  max-height: calc(85vh - 120px);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}

.cropper-body img {
  max-width: 100%;
  max-height: calc(85vh - 120px);
  display: block;
}

.cropper-actions {
  padding: 12px 16px;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  border-top: 1px solid #eee;
}
</style>
