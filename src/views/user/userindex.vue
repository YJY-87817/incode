<script setup>
import { onMounted, ref, shallowRef, computed } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import { useUserStore } from "@/stores/user/user";
import { useAuthStore } from "@/stores/user/auth";
import { storeToRefs } from "pinia";
import axios from "axios";
import FollowListDialog from "@/views/user/components/FollowListDialog.vue";
import UserProfileCard from "@/views/user/components/UserProfileCard.vue";

const userStore = useUserStore();
const authStore = useAuthStore();
const { userProfile, displayAvatar, isLoading, isFollowingCurrentUser } =
  storeToRefs(userStore);
const dialog = shallowRef(false);
const showFollowDialog = ref(false);
const followDialogTab = ref("following");

const openFollowDialog = async (tab) => {
  if (!userProfile.value || !userProfile.value.id) return;

  followDialogTab.value = tab;
  showFollowDialog.value = true;

  // 依據 tab 載入對應清單 (呼叫 Store)
  if (tab === "following") {
    await userStore.fetchUserFollowingList(userProfile.value.id);
  } else {
    await userStore.fetchUserFollowersList(userProfile.value.id);
  }
};

// 3. 編輯表單用的資料 (避免直接修改顯示資料)
const editForm = ref({
  username: "",
  email: "",
  phone: "",
  address: "",
  ageRange: "", // 假設這是前端特有或對應後端某欄位
});

const formValid = ref(false);

// 驗證規則
const rules = {
  required: (value) => !!value || "此欄位必填",
  email: (value) => {
    const pattern = /^[^@]+@[^@]+\.[^@]+$/;
    return pattern.test(value) || "Email 格式不正確";
  },
  phone: (value) => {
    if (!value) return true;
    const pattern = /^09[0-9]{8}$/;
    return pattern.test(value) || "手機號碼格式不正確";
  },
};

// 開啟編輯視窗時，把現有資料複製進表單
const openEditDialog = () => {
  editForm.value = {
    username: userProfile.value.username,
    email: userProfile.value.email,
    phone: userProfile.value.phone,
    address: userProfile.value.address,
    birthday: userProfile.value.birthday
      ? userProfile.value.birthday.split("T")[0]
      : null,
  };
  dialog.value = true;
};

const handleEditClick = () => {
  openEditDialog();
};

// 5. 送出更新
const handleUpdate = async () => {
  if (!formValid.value) return;
  isLoading.value = true;
  try {
    const payload = { ...editForm.value };
    if (!payload.birthday || payload.birthday.trim() === "") {
      // 如果是空字串、undefined，轉成 null (讓後端知道這是空值)
      payload.birthday = null;
    } else if (payload.birthday.includes("T")) {
      payload.birthday = payload.birthday.split("T")[0];
    }
    await axios.put("/User/profile", payload);
    userProfile.value.username = editForm.value.username;
    userProfile.value.email = editForm.value.email;
    userProfile.value.phone = editForm.value.phone;
    userProfile.value.address = editForm.value.address;
    userProfile.value.birthday = payload.birthday;

    authStore.updateUserInfo({
      username: editForm.value.username,
    });

    dialog.value = false;
    Swal.fire({
      title: "個人資料更新成功！",
      icon: "success",
      draggable: true,
    });
  } catch (error) {
    Swal.fire({
      icon: "error",
      title: "更新失敗",
      text: "請稍後再試一次",
    });
  } finally {
    isLoading.value = false;
  }
};

const isSelf = computed(() => {
  // 這裡需要你自己實作判斷是否為本人的邏輯，
  // 例如比較 store 裡的 userProfile.id 和登入者的 ID
  return true; // 暫時假設是本人
});

const buttonLabel = computed(() => {
  if (isSelf.value) return "編輯資料";
  return isFollowingCurrentUser.value ? "已追蹤" : "追蹤";
});

const handleCardAction = async () => {
  if (isSelf.value) {
    openEditDialog();
  } else {
    // 執行追蹤或取消追蹤
    if (isFollowingCurrentUser.value) {
      await userStore.unfollowUser(userProfile.value.id);
    } else {
      await userStore.followUser(userProfile.value.id);
    }
  }
};

onMounted(() => {
  userStore.fetchUserProfile();
});
</script>

<template>
  <DefaultNavbar light />
  <div class="container mt-6">
    <v-row>
      <v-col cols="12">
        <UserProfileCard
          :profile="userProfile"
          :avatar-url="displayAvatar"
          :loading="isLoading"
          :show-contact-info="true"
          :action-label="buttonLabel"
          @click-action="handleCardAction"
          @click-stat="openFollowDialog"
      /></v-col>
    </v-row>
    <v-dialog v-model="dialog" max-width="600"> </v-dialog>

    <FollowListDialog
      v-if="userProfile.id"
      v-model="showFollowDialog"
      :user-id="userProfile.id"
      :initial-tab="followDialogTab"
    />

    <v-dialog v-model="dialog" max-width="600">
      <v-card rounded="xl" class="pa-6">
        <div class="d-flex justify-space-between align-center mb-4">
          <h3 class="text-h6 font-weight-bold">編輯個人資料</h3>
          <v-btn
            icon="mdi-close"
            variant="text"
            @click="dialog = false"
          ></v-btn>
        </div>

        <v-form v-model="formValid">
          <v-row dense>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="editForm.username"
                label="暱稱"
                variant="outlined"
                density="comfortable"
                :rules="[rules.required]"
              ></v-text-field>
            </v-col>
            <!-- Email先不能改 <v-col cols="12" sm="6">
                        <v-text-field
                          v-model="editForm.email"
                          label="Email"
                          variant="outlined"
                          density="comfortable"
                          :rules="[rules.required, rules.email]"
                        ></v-text-field>
                      </v-col> -->
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="editForm.phone"
                label="手機"
                variant="outlined"
                density="comfortable"
                :rules="[rules.phone]"
                maxlength="10"
                counter
              ></v-text-field>
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="editForm.birthday"
                label="生日"
                type="date"
                variant="outlined"
                density="comfortable"
                :active="true"
              ></v-text-field>
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="editForm.address"
                label="地址"
                variant="outlined"
                density="comfortable"
              ></v-text-field>
            </v-col>
          </v-row>
        </v-form>

        <v-card-actions class="pa-0 mt-2">
          <v-spacer></v-spacer>
          <v-hover v-slot="{ isHovering, props }">
            <v-btn
              variant="flat"
              v-bind="props"
              class="text-body-1 font-weight-bold mb-4"
              :loading="isLoading"
              :color="isHovering ? '#FFFFFF' : '#145369'"
              :style="{
                color: isHovering ? '#145369 !important' : '#FFFFFF !important',
                border: '1px solid #145369',
              }"
              @click="dialog = false"
              >取消</v-btn
            ></v-hover
          >
          <v-hover v-slot="{ isHovering, props }">
            <v-btn
              v-bind="props"
              variant="flat"
              class="text-body-1 font-weight-bold mb-4"
              :loading="isLoading"
              :color="isHovering ? '#FFFFFF' : '#145369'"
              :style="{
                color: isHovering ? '#145369 !important' : '#FFFFFF !important',
                border: '1px solid #145369',
              }"
              :disabled="!formValid"
              @click="handleUpdate"
              >保存變更</v-btn
            ></v-hover
          >
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- <v-col cols="12" md="4">
          <h3 class="text-h6 font-weight-bold text-grey-darken-3 mb-4">
            最新通知
          </h3>

          <div class="d-flex flex-column">
            <v-card
              v-for="item in notifications"
              :key="item.id"
              class="rounded-lg mb-3"
              border
              flat
              hover
            >
              <div class="d-flex align-start pa-4">
                <v-avatar
                  :color="item.color"
                  variant="tonal"
                  class="mr-3"
                  rounded
                >
                  <v-icon :icon="item.icon"></v-icon>
                </v-avatar>
                <div class="flex-grow-1">
                  <div class="d-flex justify-space-between align-center mb-1">
                    <span class="text-subtitle-2 font-weight-bold">{{
                      item.title
                    }}</span>
                    <span class="text-caption text-grey">{{ item.time }}</span>
                  </div>
                  <div class="text-body-2 text-grey-darken-1">
                    {{ item.subtitle }}
                  </div>
                </div>
              </div>
            </v-card>
          </div>
        </v-col> -->

    <v-row>
      <v-col cols="12" class="mt-5">
        <v-card class="rounded-xl overflow-hidden" border flat>
          <nav
            class="text-center py-3"
            style="font-size: 24px; background-color: #86b0bd"
          >
            <router-link :to="{ name: 'UserMission' }">任務</router-link> |
            <router-link :to="{ name: 'UserCollections' }">討論區</router-link>
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
