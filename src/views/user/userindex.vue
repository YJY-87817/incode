<script setup>
  import { onMounted, onUnmounted, ref, shallowRef } from "vue";
  import { useRouter } from "vue-router";
  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import DefaultFooter from "@/examples/footers/FooterCentered.vue";
  import { useUserStore } from "@/stores/user/user";
  import { useAuthStore } from "@/stores/user/auth";
  import { storeToRefs } from "pinia";
  import axios from "axios";

  const userStore = useUserStore();
  const authStore = useAuthStore();
  const router = useRouter();
  const { userProfile } = storeToRefs(userStore);

  const dialog = shallowRef(false);
  const isLoading = ref(false);
  const stats = ref({});

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
        draggable: true
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

  // Hooks
  onMounted(() => {
    const body = document.getElementsByTagName("body")[0];
    body.classList.add("bg-grey-lighten-5"); // 全局背景色
    userStore.fetchUserProfile();
  });

  onUnmounted(() => {
    const body = document.getElementsByTagName("body")[0];
    body.classList.remove("bg-grey-lighten-5");
  });
</script>

<template>
  <DefaultNavbar light />
  <div class="container">
    <v-container class="mt-5 position-relative" style="min-height: 85vh; z-index: 1">
      <div class="text-center mb-3">
        <h1 class="text-h3 font-weight-bold text-grey-darken-4">會員中心</h1>
      </div>

      <v-row>
        <v-col cols="12">
          <h3 class="text-h6 font-weight-bold text-grey-darken-3 mb-4">
            我的資料
          </h3>
          <v-card class="rounded-xl overflow-hidden" elevation="0" border>
            <div class="d-flex align-center pa-6" style="background-color: #86b0bd">
              <div style="position: relative" class="mb-0 mr-6">
                <v-avatar size="120" class="border border-4 border-white elevation-2">
                  <v-img :src="userStore.displayAvatar" alt="User Avatar" cover></v-img>
                  <v-btn icon="mdi-camera" size="x-small" color="grey-darken-3" variant="flat"
                    style="position: absolute; bottom: 0; right: 0"></v-btn>
                </v-avatar>
              </div>
              <div class="d-flex flex-column justify-center w-100">
                <h2 class="text-h5 font-weight-bold text-grey-darken-4 mb-2">
                  {{ userStore.userProfile.username }}
                </h2>
                <div class="d-flex flex-wrap align-center gap-4 mt-2">
                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3">{{ userStore.userProfile.articleCount
                      }}</span>
                    <span class="text-caption text-white ml-1">文章</span>
                  </div>

                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3">{{ userStore.userProfile.missionCount
                      }}</span>
                    <span class="text-caption text-white ml-1">任務</span>
                  </div>

                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3">{{ userStore.userProfile.points }}</span>
                    <span class="text-caption text-white ml-1">點數</span>
                  </div>

                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3">{{ userStore.userProfile.followers
                      }}</span>
                    <span class="text-caption text-white ml-1">關注</span>
                  </div>

                  <div class="text-center">
                    <span class="text-h10 font-weight-bold text-grey-darken-3">{{ userStore.userProfile.following
                      }}</span>
                    <span class="text-caption text-white ml-1">粉絲</span>
                  </div>
                </div>
              </div>
            </div>

            <v-row class="text-body-2 mt-2 pa-4">
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3">mdi-email-outline</v-icon>
                <span class="text-grey-darken-1">Email：</span>
                <span class="font-weight-medium ml-1">{{
                  userStore.userProfile.email
                  }}</span>
              </v-col>
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3">mdi-phone-outline</v-icon>
                <span class="text-grey-darken-1">手機：</span>
                <span class="font-weight-medium ml-1">{{
                  userStore.userProfile.phone || "未設定"
                  }}</span>
              </v-col>
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3">mdi-map-marker-outline</v-icon>
                <span class="text-grey-darken-1">地址：</span>
                <span class="font-weight-medium ml-1">{{
                  userStore.userProfile.address || "未設定"
                  }}</span>
              </v-col>
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3">mdi-cake-variant-outline</v-icon>
                <span class="text-grey-darken-1">生日：</span>
                <span class="font-weight-medium ml-1">{{
                  userStore.userProfile.birthday || "未設定"
                  }}</span>
              </v-col>
            </v-row>
            <div class="mt-2 mb-2 d-flex justify-content-end">
              <v-btn prepend-icon="mdi-pencil-outline" variant="flat" color="#145369" class="text-white" rounded="pill"
                size="small" @click="openEditDialog">
                編輯資料
              </v-btn>
              <v-dialog v-model="dialog" max-width="600">
                <v-card rounded="xl" class="pa-6">
                  <div class="d-flex justify-space-between align-center mb-4">
                    <h3 class="text-h6 font-weight-bold">編輯個人資料</h3>
                    <v-btn icon="mdi-close" variant="text" @click="dialog = false"></v-btn>
                  </div>

                  <v-form v-model="formValid">
                    <v-row dense>
                      <v-col cols="12" sm="6">
                        <v-text-field v-model="editForm.username" label="暱稱" variant="outlined" density="comfortable"
                          :rules="[rules.required]"></v-text-field>
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
                        <v-text-field v-model="editForm.phone" label="手機" variant="outlined" density="comfortable"
                          :rules="[rules.phone]" maxlength="10" counter></v-text-field>
                      </v-col>
                      <v-col cols="12" sm="6">
                        <v-text-field v-model="editForm.birthday" label="生日" type="date" variant="outlined"
                          density="comfortable" :active="true"></v-text-field>
                      </v-col>
                      <v-col cols="12" sm="6">
                        <v-text-field v-model="editForm.address" label="地址" variant="outlined"
                          density="comfortable"></v-text-field>
                      </v-col>
                    </v-row>
                  </v-form>

                  <v-card-actions class="pa-0 mt-4">
                    <v-spacer></v-spacer>
                    <v-hover v-slot="{ isHovering, props }">
                      <v-btn variant="flat" v-bind="props" class="text-body-1 font-weight-bold mb-4"
                        :loading="isLoading" :color="isHovering ? '#FFFFFF' : '#145369'" :style="{
                          color: isHovering
                            ? '#145369 !important'
                            : '#FFFFFF !important',
                          border: '1px solid #145369',
                        }" @click="dialog = false">取消</v-btn></v-hover>
                    <v-hover v-slot="{ isHovering, props }">
                      <v-btn v-bind="props" variant="flat" class="text-body-1 font-weight-bold mb-4"
                        :loading="isLoading" :color="isHovering ? '#FFFFFF' : '#145369'" :style="{
                          color: isHovering
                            ? '#145369 !important'
                            : '#FFFFFF !important',
                          border: '1px solid #145369',
                        }" :disabled="!formValid" @click="handleUpdate">保存變更</v-btn></v-hover>
                  </v-card-actions>
                </v-card>
              </v-dialog>
            </div>
          </v-card>
        </v-col>

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
      </v-row>
      <v-row>
        <v-col cols="12">
          <v-card class="rounded-xl overflow-hidden" border flat>
            <nav class="text-center py-3" style="font-size: 24px; background-color: #86b0bd">
              <router-link :to="{ name: 'UserMission' }">任務</router-link> |
              <router-link :to="{ name: 'UserCollections' }">討論區</router-link>
            </nav>

            <div class="nested-content">
              <router-view></router-view>
            </div>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </div>
  <DefaultFooter />
</template>