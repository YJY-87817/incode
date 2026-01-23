<!-- 使用者資訊欄 -->
<script setup>
import { defineProps } from "vue";
import CityProfile from "@/assets/img/city-profile.jpg";
import Team1 from "@/assets/img/team-1.jpg";

// 模擬 Store 資料 (實際開發時請換回 store.user)
// const user = ref({
//   name: "Chanhao",
//   level: 3,
//   id: "Chanhao122",
//   title: "尚未取得",
//   exp: 75,
// });

// 定義 Props，讓父元件可以傳入 user 資料
const props = defineProps({
  user:{
    type:Object,
    required:true,
    default:()=>({
      name:"Guest",
      level:1,
      id:"Guest001",
      title:"新手",
      exp:0,
    })
  }
})
</script>

<template>
  <div class="profile-container">
    <div
      class="header-banner"
      :style="{
        backgroundImage: `url(${CityProfile})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center',
      }"
    >
      <span class="mask"></span>
    </div>

    <div class="card profile-card mx-auto mt-n7 shadow-lg">
      <div class="card-body p-4">
        <div class="row align-items-center">
          <div class="col-auto">
            <div class="avatar-wrapper mt-n6">
              <img :src="Team1" class="avatar-img shadow" alt="avatar" />
            </div>
          </div>

          <div class="col my-auto">
            <div class="d-flex align-items-center flex-wrap gap-2">
              <h3 class="user-name mb-0">{{ user.name }}</h3>

              <div class="anime-lv-badge">
                <span class="lv-label">LV</span>
                <span class="lv-num">{{ user.level }}</span>
              </div>
            </div>

            <div class="user-meta-group mt-2">
              <div class="meta-tag">
                <span class="tag-label">ID</span>
                <span class="tag-value">{{ user.id }}</span>
              </div>
              <div class="meta-tag">
                <span class="tag-label">稱號</span>
                <span class="tag-value title-placeholder">{{
                  user.title
                }}</span>
              </div>
            </div>
          </div>

          <div class="col-lg-4 my-auto">
            <div class="exp-section">
              <div class="d-flex justify-content-between mb-1">
                <span class="exp-text">EXP</span>
                <span class="exp-percent">{{ user.exp }}%</span>
              </div>
              <div class="progress anime-progress">
                <div
                  class="progress-bar progress-bar-striped progress-bar-animated"
                  role="progressbar"
                  :style="{ width: user.exp + '%' }"
                ></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 1. 確保背景大圖夠高 */
.header-banner {
  min-height: 300px !important; /* 強制高度 */
  width: 100%;
  position: relative;
  background-size: cover;
  background-position: center;
}

/* 2. 修正卡片位置，確保它往上疊 */
.profile-card {
  width: 90%;
  max-width: 1000px;
  margin-top: -80px !important; /* 核心：負向邊距向上抽 */
  border: none !important;
  border-radius: 16px !important;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15) !important;
  z-index: 2;
  position: relative;
}

/* 3. 大頭貼外框 - 加上二次元發光 */
.avatar-wrapper {
  width: 110px;
  height: 110px;
  margin-top: -60px; /* 讓頭像一半在卡片外 */
  border: 4px solid #fff;
  border-radius: 15px;
  overflow: hidden;
  box-shadow: 0 4px 20px rgba(0, 210, 255, 0.4); /* 藍色發光 */
  background: white;
}

.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* 4. 等級方塊 (LV3) - 完全重寫樣式 */
.anime-lv-badge {
  display: inline-flex !important;
  align-items: baseline;
  background: #17a2b8 !important;
  color: white !important;
  padding: 2px 12px !important;
  /* 平行四邊形剪裁 */
  clip-path: polygon(15% 0, 100% 0, 85% 100%, 0% 100%);
  font-family: "Arial Black", sans-serif;
}

.lv-label {
  font-size: 0.6rem;
  font-weight: 900;
  margin-right: 2px;
}

.lv-num {
  font-size: 1.2rem;
  font-weight: 900;
}

/* 5. ID 與 稱號 */
.user-meta-group {
  display: flex;
  gap: 15px;
  margin-top: 8px;
}

.tag-label {
  font-size: 10px;
  background: #333;
  color: #00d2ff;
  padding: 0 4px;
  border-radius: 2px;
  margin-right: 5px;
}

.tag-value {
  font-size: 13px;
  color: #666;
}

/* 6. 進度條 - 霓虹感 */
.anime-progress {
  height: 12px !important;
  border-radius: 10px !important;
  background-color: #eee !important;
}

.progress-bar {
  background: linear-gradient(90deg, #17a2b8, #00d2ff) !important;
  box-shadow: 0 0 10px rgba(0, 210, 255, 0.5);
}
</style>
