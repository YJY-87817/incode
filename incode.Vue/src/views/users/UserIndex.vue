<script setup>
  import { onMounted, ref, shallowRef, computed } from "vue";
  import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
  import DefaultFooter from "@/components/layout/FooterCentered.vue";
  import { useUserStore } from "@/stores/users/user";
  import { useAuthStore } from "@/stores/users/auth";
  import { storeToRefs } from "pinia";
  import axios from "axios";
  import Swal from "sweetalert2";
  import FollowListDialog from "@/components/users/FollowListDialog.vue";
  import UserProfileCard from "@/components/users/UserProfileCard.vue";

  const userStore = useUserStore();
  const authStore = useAuthStore();
  const { userProfile, displayAvatar, isLoading, isFollowingCurrentUser } = storeToRefs(userStore);

  const dialog = shallowRef(false);
  const showFollowDialog = ref(false);
  const followDialogTab = ref("following");

  // --- 判斷權限與按鈕狀態 ---
  const isSelf = computed(() => String(authStore.user?.id) === String(userProfile.value?.id));

  const buttonLabel = computed(() => {
    if (isLoading.value) return "載入中...";
    return isSelf.value ? "編輯資料" : (isFollowingCurrentUser.value ? "已追蹤" : "追蹤");
  });

  // --- 互動邏輯 ---
  const openFollowDialog = async (tab) => {
    if (!userProfile.value?.id) return;
    followDialogTab.value = tab;
    showFollowDialog.value = true;
    tab === "following"
      ? await userStore.fetchUserFollowingList(userProfile.value.id)
      : await userStore.fetchUserFollowersList(userProfile.value.id);
  };

  const handleCardAction = async () => {
    if (isSelf.value) {
      openEditDialog();
    } else {
      const targetId = userProfile.value.id;
      if (!targetId) return;
      try {
        isFollowingCurrentUser.value
          ? await userStore.unfollowUser(targetId)
          : await userStore.followUser(targetId);
        Swal.fire({
          title: isFollowingCurrentUser.value ? "追蹤成功" : "已取消追蹤",
          icon: "success",
          timer: 1500,
          showConfirmButton: false,
        });
      } catch (error) {
        Swal.fire({ icon: "error", title: "操作失敗", text: "請稍後再試" });
      }
    }
  };

  // --- 共用的圖片上傳 API 邏輯 ---
  const uploadImageToServer = async (file) => {
    if (file.size > 2 * 1024 * 1024) {
      Swal.fire({ icon: 'warning', title: '檔案過大', text: '請選擇小於 2MB 的圖片' });
      return null;
    }
    const formData = new FormData();
    formData.append('files', file);
    try {
      const response = await axios.post("/User/images", formData);
      return response.data?.imageUrls?.[0] || null;
    } catch (error) {
      Swal.fire({ icon: 'error', title: '上傳失敗', text: '請檢查網路連線或 API 狀態' });
      return null;
    }
  };

  // 處理【外部卡片】的圖片上傳 (立即上傳並更新資料庫)
  const handleCardAvatarUpload = async (file) => {
    isLoading.value = true;
    const newImageUrl = await uploadImageToServer(file);

    if (newImageUrl) {
      try {
        const payload = {
          username: userProfile.value.username,
          email: userProfile.value.email,
          phone: userProfile.value.phone,
          address: userProfile.value.address,
          birthday: userProfile.value.birthday ? userProfile.value.birthday.split("T")[0] : null,
          imageUrl: newImageUrl
        };

        // 立即呼叫更新 API
        await axios.put("/User/profile", payload);

        // 更新前端畫面與 Pinia 狀態
        userProfile.value.imageUrl = newImageUrl;
        authStore.updateUserInfo({ avatar: newImageUrl });

        Swal.fire({ title: "大頭貼更新成功！", icon: "success", timer: 1500, showConfirmButton: false });
      } catch (error) {
        console.error("卡片大頭貼更新失敗:", error);
        Swal.fire({ icon: "error", title: "更新失敗", text: "系統發生異常，請確認後端必填欄位" });
      }
    }
    isLoading.value = false;
  };

  // --- 編輯表單與 Dialog 邏輯 ---
  const editForm = ref({ username: "", email: "", phone: "", address: "", birthday: null, imageUrl: "" });
  const formValid = ref(false);

  const rules = {
    required: (v) => !!v || "此欄位必填",
    phone: (v) => !v || /^09[0-9]{8}$/.test(v) || "手機號碼格式不正確",
  };

  const openEditDialog = () => {
    editForm.value = {
      username: userProfile.value.username,
      email: userProfile.value.email,
      phone: userProfile.value.phone,
      address: userProfile.value.address,
      birthday: userProfile.value.birthday ? userProfile.value.birthday.split("T")[0] : null,
      imageUrl: userProfile.value.imageUrl || "",
    };
    avatarPreview.value = null;
    selectedFile.value = null; // 清空選擇的檔案
    dialog.value = true;
  };

  const dialogFileInput = ref(null);
  const avatarPreview = ref(null);
  const selectedFile = ref(null); // 新增：用來暫存選擇的圖片
  const isUploadingAvatarDialog = ref(false);

  const triggerDialogFileInput = () => {
    dialogFileInput.value.click();
  };

  // 第一步：選擇圖片並預覽 (不打 API)
  const handleDialogAvatarChange = (event) => {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 2 * 1024 * 1024) {
      Swal.fire({ icon: 'warning', title: '圖片過大', text: '請選擇小於 2MB 的圖片' });
      return;
    }

    selectedFile.value = file; // 暫存檔案，讓雲朵按鈕出現
    avatarPreview.value = URL.createObjectURL(file); // 顯示預覽

    // 清空 input 確保可以重複選同張圖
    if (dialogFileInput.value) dialogFileInput.value.value = '';
  };

  // 第二步：點擊雲朵按鈕確認上傳 (打 API)
  const confirmUploadAvatar = async () => {
    if (!selectedFile.value) return;

    isUploadingAvatarDialog.value = true;

    const newImageUrl = await uploadImageToServer(selectedFile.value);
    if (newImageUrl) {
      editForm.value.imageUrl = newImageUrl;
      Swal.fire({ title: "圖片上傳成功！", icon: "success", timer: 1500, showConfirmButton: false });

      // 上傳成功後，清空 selectedFile 讓雲朵按鈕消失
      selectedFile.value = null;
    } else {
      avatarPreview.value = null;
      selectedFile.value = null;
    }

    isUploadingAvatarDialog.value = false;
  };

  // 送出編輯視窗的更新 (包含文字與已上傳的圖片)
  const handleUpdate = async () => {
    // 防呆：如果選了圖片但還沒按雲朵上傳，提醒使用者
    if (selectedFile.value) {
      Swal.fire({ icon: "warning", text: "您有選擇新大頭貼但尚未點擊「確認上傳大頭貼」，請先上傳！" });
      return;
    }

    if (!formValid.value) return;
    isLoading.value = true;
    try {
      const payload = { ...editForm.value };
      if (!payload.birthday || payload.birthday.trim() === "") {
        payload.birthday = null;
      } else if (payload.birthday.includes("T")) {
        payload.birthday = payload.birthday.split("T")[0];
      }

      // --- 可以在這裡按 F12 看看 Console 送出了什麼 ---
      console.log("準備送給後端的資料:", payload);

      // 送出更新到後端
      await axios.put("/User/profile", payload);

      // 更新前端狀態
      userProfile.value.username = editForm.value.username;
      userProfile.value.phone = editForm.value.phone;
      userProfile.value.address = editForm.value.address;
      userProfile.value.birthday = payload.birthday;
      if (editForm.value.imageUrl) {
        userProfile.value.imageUrl = editForm.value.imageUrl;
      }

      authStore.updateUserInfo({
        username: editForm.value.username,
        avatar: editForm.value.imageUrl
      });

      dialog.value = false;
      Swal.fire({ title: "更新成功！", icon: "success", timer: 1500, showConfirmButton: false });
    } catch (error) {
      console.error("更新 API 失敗:", error);

      let errorMsg = "系統發生異常，請按 F12 查看 Console";
      if (error.response && error.response.data) {
        const resData = error.response.data;
        if (resData.errors) {
          // 如果是 DTO 欄位驗證錯誤 (例如少傳了必填欄位)
          errorMsg = Object.values(resData.errors).flat().join('\n');
        } else if (resData.message) {
          // 如果是自訂的錯誤訊息
          errorMsg = resData.message;
        } else if (typeof resData === 'string') {
          errorMsg = resData;
        }
      }

      Swal.fire({
        icon: "error",
        title: "更新失敗",
        text: errorMsg
      });
    } finally {
      isLoading.value = false;
    }
  };

  onMounted(async () => {
    isLoading.value = true;
    if (!authStore.isAuthenticated) await authStore.checkAuth();

    // UserIndex 預設就是看自己，不用傳 username 給 api
    await userStore.fetchUserProfile();

    if (authStore.isAuthenticated && !isSelf.value) {
      await userStore.fetchMyFollowingList();
    }
  });
</script>

<template>
  <DefaultNavbar light />
  <div class="container mt-6">
    <v-row>
      <v-col cols="12">
        <UserProfileCard :profile="userProfile" :avatar-url="displayAvatar" :loading="isLoading"
          :show-contact-info="isSelf" :action-label="buttonLabel" @click-action="handleCardAction"
          @click-stat="openFollowDialog" @upload-avatar="handleCardAvatarUpload" />
      </v-col>
    </v-row>

    <FollowListDialog v-if="userProfile.id" v-model="showFollowDialog" :user-id="userProfile.id"
      :initial-tab="followDialogTab" />

    <v-dialog v-model="dialog" max-width="800">
      <v-card rounded="xl" class="pa-6">
        <div class="d-flex justify-space-between align-center mb-4">
          <h3 class="text-h6 font-weight-bold text-primary">編輯個人資料</h3>
          <v-btn icon="mdi-close" variant="text" @click="dialog = false"></v-btn>
        </div>

        <v-row>
          <v-col cols="12" md="7" class="pe-md-6">
            <v-form v-model="formValid">
              <v-row dense>
                <v-col cols="12"><v-text-field v-model="editForm.username" label="暱稱" variant="outlined"
                    density="comfortable" prepend-inner-icon="mdi-account-outline"
                    :rules="[rules.required]"></v-text-field></v-col>
                <v-col cols="12"><v-text-field v-model="editForm.phone" label="手機" variant="outlined"
                    density="comfortable" prepend-inner-icon="mdi-phone-outline" :rules="[rules.phone]" maxlength="10"
                    counter></v-text-field></v-col>
                <v-col cols="12"><v-text-field v-model="editForm.birthday" label="生日" type="date" variant="outlined"
                    density="comfortable" prepend-inner-icon="mdi-cake-variant-outline"
                    :active="true"></v-text-field></v-col>
                <v-col cols="12"><v-text-field v-model="editForm.address" label="地址" variant="outlined"
                    density="comfortable" prepend-inner-icon="mdi-map-marker-outline"></v-text-field></v-col>
              </v-row>
            </v-form>
          </v-col>

          <v-col cols="12" md="5" class="d-flex flex-column align-center justify-center ps-md-4 border-start-md">
            <div class="text-center w-100">
              <h3 class="text-subtitle-1 font-weight-bold mb-4 text-grey-darken-1">設定個人大頭貼</h3>
              <div style="position: relative; width: 150px; height: 150px; margin: 0 auto;" class="mb-6">
                <v-avatar size="150" class="border border-4 border-white elevation-2">
                  <v-img :src="avatarPreview || editForm.imageUrl || displayAvatar" cover></v-img>
                </v-avatar>
                <v-btn icon="mdi-camera" size="small" color="primary" variant="elevated"
                  style="position: absolute; bottom: 5px; right: 5px; z-index: 10;"
                  @click="triggerDialogFileInput"></v-btn>
              </div>
              <input type="file" ref="dialogFileInput" class="d-none" accept="image/*"
                @change="handleDialogAvatarChange" />

              <v-btn v-if="selectedFile" variant="tonal" color="primary" size="small" block class="mb-4"
                :loading="isUploadingAvatarDialog" @click="confirmUploadAvatar">
                <v-icon start>mdi-cloud-upload</v-icon>
                確認上傳大頭貼
              </v-btn>

              <p class="text-caption text-grey">建議尺寸: 500x500px <br /> 支援格式: JPG, PNG</p>
            </div>
          </v-col>
        </v-row>

        <v-divider class="my-4"></v-divider>

        <v-card-actions class="pa-0">
          <v-spacer></v-spacer>
          <v-btn variant="text" class="text-body-1 font-weight-bold mr-2" color="grey-darken-1"
            @click="dialog = false">取消</v-btn>
          <v-btn variant="flat" class="text-body-1 font-weight-bold px-6" color="#145369" :loading="isLoading"
            :disabled="!formValid" @click="handleUpdate">保存變更</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-row>
      <v-col cols="12" class="mt-5">
        <v-card class="rounded-xl overflow-hidden" border flat>
          <nav class="text-center py-3" style="font-size: 24px; background-color: #86b0bd">
            <router-link :to="{ name: 'UserCollections' }">討論區</router-link> |
            <router-link :to="{ name: 'UserMission' }">任務</router-link>
          </nav>
          <div class="nested-content">
            <router-view></router-view>
          </div>
        </v-card>
      </v-col>
    </v-row>
  </div>
  <DefaultFooter />
</template>

<style scoped>
  .cursor-pointer {
    cursor: pointer;
  }

  .hover-opacity:hover {
    opacity: 0.7;
    transition: opacity 0.2s;
  }
</style>