<script setup>
  import { computed, ref } from "vue";

  const props = defineProps({
    loading: Boolean,
    profile: {
      type: Object,
      required: true,
      default: () => ({}),
    },
    actionLabel: {
      type: String,
      default: "編輯資料",
    },
    // 控制是否顯示聯絡資訊與「相機按鈕」(只有看自己時才顯示)
    showContactInfo: {
      type: Boolean,
      default: false,
    },
    avatarUrl: String,
  });

  // 定義 Emits，新增 "upload-avatar" 用來把外部卡片選的檔案傳給父層
  const emit = defineEmits(["click-action", "click-stat", "upload-avatar"]);

  const displayAvatar = computed(
    () => props.avatarUrl || "https://via.placeholder.com/150",
  );

  const handleStatClick = (type) => {
    emit("click-stat", type);
  };

  // --- 外部卡片的檔案上傳邏輯 ---
  const cardFileInput = ref(null);
  // 觸發隱藏的 input
  const triggerCardFileInput = () => {
    cardFileInput.value.click();
  };
  // 當檔案改變時，發送事件給父層
  const onCardFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      emit("upload-avatar", file);
    }
    // 清空 input，確保下次選同一張圖也能觸發
    if (cardFileInput.value) cardFileInput.value.value = "";
  };
</script>

<template>
  <v-card class="rounded-xl overflow-hidden" elevation="0" border>
    <div class="d-flex align-center pa-6" style="background-color: #86b0bd">
      <div style="position: relative; width: 150px; height: 150px;" class="mb-0 mr-6">
        <v-avatar size="150" class="border border-4 border-white elevation-2">
          <v-img :src="displayAvatar" alt="User Avatar" cover></v-img>
        </v-avatar>

        <v-btn v-if="showContactInfo" icon="mdi-camera" size="small" color="white" variant="elevated"
          style="position: absolute; bottom: 5px; right: 5px; z-index: 10;" :loading="loading"
          @click="triggerCardFileInput"></v-btn>

        <input type="file" ref="cardFileInput" class="d-none" accept="image/*" @change="onCardFileChange" />
      </div>

      <div class="d-flex flex-column justify-center flex-grow-1">
        <h2 class="text-h4 font-weight-bold text-grey-darken-4 mb-2">
          {{ profile.username || profile.name }}
        </h2>

        <div class="d-flex flex-wrap align-center gap-4 mt-2">
          <div class="text-center mr-4">
            <span class="text-h5 font-weight-bold text-grey-darken-3">{{ profile.articleCount || 0 }}</span>
            <span class="text-subtitle-1 text-white ml-1">文章</span>
          </div>
          <div class="text-center mr-4">
            <span class="text-h5 font-weight-bold text-grey-darken-3">{{ profile.missionCount || 0 }}</span>
            <span class="text-subtitle-1 text-white ml-1">任務</span>
          </div>
          <div class="text-center mr-4">
            <span class="text-h5 font-weight-bold text-grey-darken-3">{{ profile.points || 0 }}</span>
            <span class="text-subtitle-1 text-white ml-1">點數</span>
          </div>
          <div class="text-center mr-4" style="cursor: pointer" @click="handleStatClick('following')">
            <span class="text-h5 font-weight-bold text-grey-darken-3">{{ profile.following || 0 }}</span>
            <span class="text-subtitle-1 text-white ml-1">關注</span>
          </div>
          <div class="text-center" style="cursor: pointer" @click="handleStatClick('fans')">
            <span class="text-h5 font-weight-bold text-grey-darken-3">{{ profile.followers || 0 }}</span>
            <span class="text-subtitle-1 text-white ml-1">粉絲</span>
          </div>
        </div>
      </div>

      <div class="mt-2 mb-2 mr-6 d-flex justify-end">
        <v-btn :prepend-icon="
              actionLabel === '編輯資料' ? 'mdi-pencil-outline' : actionLabel === '已追蹤' ? 'mdi-check' : 'mdi-plus'
            " variant="flat" :color="actionLabel === '已追蹤' ? 'grey-lighten-2' : '#145369'" :class="
              actionLabel === '已追蹤' ? 'text-grey-darken-3' : 'text-white'
            " rounded="pill" size="large" :loading="loading" @click="emit('click-action')">
          {{ actionLabel }}
        </v-btn>
      </div>
    </div>

    <v-row v-if="showContactInfo" class="text-body-2 mt-2 pa-4">
      <v-col cols="12" sm="6" class="d-flex align-center py-2">
        <v-icon color="grey" class="mr-3">mdi-email-outline</v-icon>
        <span class="text-grey-darken-1">Email：</span>
        <span class="font-weight-medium ml-1">{{ profile.email }}</span>
      </v-col>
      <v-col cols="12" sm="6" class="d-flex align-center py-2">
        <v-icon color="grey" class="mr-3">mdi-phone-outline</v-icon>
        <span class="text-grey-darken-1">手機：</span>
        <span class="font-weight-medium ml-1">{{ profile.phone || "未設定" }}</span>
      </v-col>
      <v-col cols="12" sm="6" class="d-flex align-center py-2">
        <v-icon color="grey" class="mr-3">mdi-map-marker-outline</v-icon>
        <span class="text-grey-darken-1">地址：</span>
        <span class="font-weight-medium ml-1">{{ profile.address || "未設定" }}</span>
      </v-col>
      <v-col cols="12" sm="6" class="d-flex align-center py-2">
        <v-icon color="grey" class="mr-3">mdi-cake-variant-outline</v-icon>
        <span class="text-grey-darken-1">生日：</span>
        <span class="font-weight-medium ml-1">{{ profile.birthday || "未設定" }}</span>
      </v-col>
    </v-row>
  </v-card>
</template>