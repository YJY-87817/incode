<script setup>
  import { onMounted } from "vue";

  import { useAuthStore } from "@/stores/user/auth";
  const authStore = useAuthStore();
  const handleRegister = async () => {
    await authStore.register();
  };

  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import Header from "@/examples/Header.vue";
  import UserInput from "@/views/user/components/UserInput.vue";
  import UserSwitch from "@/views/user/components/UserSwitch.vue";
  import UserButton from "@/views/user/components/UserButton.vue";
  import setMaterialInput from "@/assets/js/material-input";

  onMounted(() => {
    setMaterialInput();
  });
</script>

<template>
  <v-img class="mx-auto my-6" max-width="228" src="" alt="網站logo">網站logo</v-img>
  <v-card title="註冊成為會員" class="mx-auto pa-12 pb-8" elevation="8" max-width="448" rounded="lg">
    <v-form>
      <v-col class="mt-2"> <!--cols="12" md="4" sm="6" -->
        <v-text-field label="帳號" placeholder="Email"></v-text-field>
        <v-text-field label="暱稱" placeholder="Name" variant="solo"></v-text-field>
        <v-text-field label="生日" placeholder="Birthday" variant="solo"></v-text-field>
        <v-text-field label="密碼" placeholder="Password" variant="filled"></v-text-field>
        <v-text-field label="確認密碼" placeholder="ComparePassword" variant="filled"></v-text-field>
        <v-text-field label="自我介紹" placeholder="Introduce" variant="plain"></v-text-field>
      </v-col>
      <v-btn class="mb-2" color="blue" size="large" variant="tonal" block>
        註冊 <RouterLink v-if="authStore.isLoggedIn" to="/UserIndex" class="btn btn-sm bg-gradient-success mb-0">
        </RouterLink>
      </v-btn>
      <!-- 其他樣式
      <v-col cols="12" md="4" sm="6">
        <v-text-field label="Underlined" placeholder="Placeholder" variant="underlined"></v-text-field>
      </v-col>
      <v-col cols="12" md="4" sm="6">
        <v-text-field label="Outlined" placeholder="Placeholder" variant="outlined"></v-text-field>
      </v-col> -->
    </v-form>
  </v-card>
</template>