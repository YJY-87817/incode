<template>
  <div class="image-carousel" v-if="images && images.length > 0">
    <div class="carousel-container">
      <!-- 主圖片顯示區 -->
      <div class="image-wrapper">
        <img
          :src="getFullImageUrl(currentImage)"
          :alt="`圖片 ${currentIndex + 1}`"
          class="carousel-image"
          @error="handleImageError"
        />

        <!-- 左右切換按鈕 -->
        <button
          v-if="images.length > 1"
          class="carousel-btn prev-btn"
          @click="prevImage"
          :disabled="currentIndex === 0 && !loop"
        >
          <i class="fas fa-chevron-left"></i>
        </button>

        <button
          v-if="images.length > 1"
          class="carousel-btn next-btn"
          @click="nextImage"
          :disabled="currentIndex === images.length - 1 && !loop"
        >
          <i class="fas fa-chevron-right"></i>
        </button>

        <!-- 圖片計數器 -->
        <div v-if="images.length > 1" class="image-counter">
          {{ currentIndex + 1 }} / {{ images.length }}
        </div>
      </div>

      <!-- 縮圖預覽（可選） -->
      <div v-if="showThumbnails && images.length > 1" class="thumbnails">
        <div
          v-for="(image, index) in images"
          :key="index"
          class="thumbnail"
          :class="{ active: index === currentIndex }"
          @click="goToImage(index)"
        >
          <img :src="getFullImageUrl(image)" :alt="`縮圖 ${index + 1}`" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";

const props = defineProps({
  // 圖片陣列，可以是完整 URL 或相對路徑
  images: {
    type: Array,
    required: true,
    default: () => [],
  },
  // API 基底 URL
  baseUrl: {
    type: String,
    default: "https://localhost:7190",
  },
  // 是否顯示縮圖
  showThumbnails: {
    type: Boolean,
    default: true,
  },
  // 是否循環播放
  loop: {
    type: Boolean,
    default: true,
  },
  // 初始顯示的圖片索引
  initialIndex: {
    type: Number,
    default: 0,
  },
});

const currentIndex = ref(props.initialIndex);

const currentImage = computed(() => props.images[currentIndex.value]);

// 取得完整的圖片 URL
const getFullImageUrl = (imagePath) => {
  if (!imagePath) return "";
  // 如果已經是完整 URL，直接返回
  if (imagePath.startsWith("http://") || imagePath.startsWith("https://")) {
    return imagePath;
  }
  // 如果是相對路徑，加上 baseUrl
  return `${props.baseUrl}${imagePath.startsWith("/") ? "" : "/"}${imagePath}`;
};

// 上一張圖片
const prevImage = () => {
  if (currentIndex.value > 0) {
    currentIndex.value--;
  } else if (props.loop) {
    currentIndex.value = props.images.length - 1;
  }
};

// 下一張圖片
const nextImage = () => {
  if (currentIndex.value < props.images.length - 1) {
    currentIndex.value++;
  } else if (props.loop) {
    currentIndex.value = 0;
  }
};

// 跳到指定圖片
const goToImage = (index) => {
  currentIndex.value = index;
};

// 圖片載入錯誤處理
const handleImageError = (event) => {
  console.error("圖片載入失敗:", event.target.src);
  event.target.src =
    'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="400" height="300"%3E%3Crect fill="%23ddd" width="400" height="300"/%3E%3Ctext fill="%23999" x="50%25" y="50%25" text-anchor="middle" dominant-baseline="middle"%3E圖片載入失敗%3C/text%3E%3C/svg%3E';
};

// 鍵盤事件支援（可選）
const handleKeydown = (event) => {
  if (event.key === "ArrowLeft") prevImage();
  if (event.key === "ArrowRight") nextImage();
};

// 可以暴露方法給父組件使用
defineExpose({
  nextImage,
  prevImage,
  goToImage,
  currentIndex,
});
</script>

<style scoped>
.image-carousel {
  width: 100%;
  max-width: 100%;
}

.carousel-container {
  position: relative;
}

.image-wrapper {
  position: relative;
  width: 100%;
  background: #f8f9fa;
  border-radius: 8px;
  overflow: hidden;
}

.carousel-image {
  width: 100%;
  height: auto;
  max-height: 500px;
  object-fit: contain;
  display: block;
}

.carousel-btn {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  background: rgba(0, 0, 0, 0.5);
  color: white;
  border: none;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s;
  z-index: 10;
}

.carousel-btn:hover:not(:disabled) {
  background: rgba(0, 0, 0, 0.7);
  transform: translateY(-50%) scale(1.1);
}

.carousel-btn:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.prev-btn {
  left: 10px;
}

.next-btn {
  right: 10px;
}

.image-counter {
  position: absolute;
  bottom: 15px;
  right: 15px;
  background: rgba(0, 0, 0, 0.6);
  color: white;
  padding: 5px 12px;
  border-radius: 20px;
  font-size: 14px;
  font-weight: 500;
}

.thumbnails {
  display: flex;
  gap: 10px;
  margin-top: 15px;
  overflow-x: auto;
  padding: 5px 0;
}

.thumbnail {
  flex-shrink: 0;
  width: 80px;
  height: 60px;
  border-radius: 6px;
  overflow: hidden;
  cursor: pointer;
  border: 2px solid transparent;
  transition: all 0.3s;
}

.thumbnail:hover {
  border-color: #e91e63;
  transform: scale(1.05);
}

.thumbnail.active {
  border-color: #e91e63;
  box-shadow: 0 0 0 2px rgba(233, 30, 99, 0.2);
}

.thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* 滾動條樣式 */
.thumbnails::-webkit-scrollbar {
  height: 6px;
}

.thumbnails::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 3px;
}

.thumbnails::-webkit-scrollbar-thumb {
  background: #888;
  border-radius: 3px;
}

.thumbnails::-webkit-scrollbar-thumb:hover {
  background: #555;
}
</style>
