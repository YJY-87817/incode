<script setup>
  import { ref, watch } from "vue";
  import axios from "axios";
  import Swal from "sweetalert2";

  // 會員中心「修改密碼」：需要目前的密碼；成功後其他裝置的登入會被登出，目前的裝置保持登入
  const model = defineModel({ type: Boolean, default: false });

  const MIN_LENGTH = 6; // 與後端 PasswordRules 相同
  const MAX_LENGTH = 100;

  const form = ref({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const formValid = ref(false);
  const formRef = ref(null);
  const isSubmitting = ref(false);
  // 錯誤訊息顯示在對話框內（SweetAlert 會被 v-dialog 的遮罩蓋住，無法點擊）
  const errorMessage = ref("");
  const showPasswords = ref(false);

  const rules = {
    required: (v) => !!v || "此欄位為必填",
    length: (v) => (v && v.length >= MIN_LENGTH && v.length <= MAX_LENGTH) || `密碼需為 ${MIN_LENGTH}～${MAX_LENGTH} 碼`,
    differs: (v) => v !== form.value.currentPassword || "新密碼不能與目前的密碼相同",
    matches: (v) => v === form.value.newPassword || "兩次輸入的密碼不一致",
  };

  // 每次打開都清空
  watch(model, (open) => {
    if (open) {
      form.value = { currentPassword: "", newPassword: "", confirmPassword: "" };
      showPasswords.value = false;
      errorMessage.value = "";
      formRef.value?.resetValidation();
    }
  });

  const errorText = (error) => {
    const data = error.response?.data;
    if (data?.message) return data.message;
    const first = data?.errors && Object.values(data.errors).flat()[0]; // ASP.NET 模型驗證錯誤
    return first || "請稍後再試";
  };

  const submit = async () => {
    if (!formValid.value) return;
    isSubmitting.value = true;
    errorMessage.value = "";
    try {
      const res = await axios.post("/AuthApi/change-password", {
        currentPassword: form.value.currentPassword,
        newPassword: form.value.newPassword,
      });
      model.value = false;
      Swal.fire({
        icon: "success",
        title: "密碼已更新",
        text: res.data?.message || "其他裝置的登入已登出",
        confirmButtonColor: "#145369",
      });
    } catch (error) {
      errorMessage.value = errorText(error);
    } finally {
      isSubmitting.value = false;
    }
  };
</script>

<template>
  <v-dialog v-model="model" max-width="480">
    <v-card rounded="xl" class="pa-6">
      <div class="d-flex justify-space-between align-center mb-4">
        <h3 class="text-h6 font-weight-bold text-primary">修改密碼</h3>
        <v-btn icon="mdi-close" variant="text" @click="model = false"></v-btn>
      </div>

      <v-form ref="formRef" v-model="formValid" @submit.prevent="submit">
        <v-text-field v-model="form.currentPassword" label="目前的密碼" :rules="[rules.required]"
          :type="showPasswords ? 'text' : 'password'" :maxlength="MAX_LENGTH" autocomplete="current-password"
          prepend-inner-icon="mdi-lock-outline" variant="outlined" density="comfortable" class="mb-2"></v-text-field>
        <v-text-field v-model="form.newPassword" label="新密碼" :rules="[rules.required, rules.length, rules.differs]"
          :type="showPasswords ? 'text' : 'password'" :maxlength="MAX_LENGTH" autocomplete="new-password"
          prepend-inner-icon="mdi-lock-check-outline" variant="outlined" density="comfortable"
          class="mb-2"></v-text-field>
        <v-text-field v-model="form.confirmPassword" label="確認新密碼" :rules="[rules.required, rules.matches]"
          :type="showPasswords ? 'text' : 'password'" :maxlength="MAX_LENGTH" autocomplete="new-password"
          prepend-inner-icon="mdi-lock-check-outline" :append-inner-icon="showPasswords ? 'mdi-eye-off' : 'mdi-eye'"
          @click:append-inner="showPasswords = !showPasswords" variant="outlined" density="comfortable"></v-text-field>

        <p class="text-caption text-grey mt-2 mb-4">修改後，其他裝置上的登入會被登出。</p>

        <v-alert v-if="errorMessage" type="error" variant="tonal" density="compact" :icon="false" class="mb-4">
          {{ errorMessage }}
        </v-alert>

        <v-card-actions class="pa-0">
          <v-spacer></v-spacer>
          <v-btn variant="text" class="text-body-1 font-weight-bold mr-2" color="grey-darken-1"
            @click="model = false">取消</v-btn>
          <v-btn type="submit" variant="flat" class="text-body-1 font-weight-bold px-6" color="#145369"
            :loading="isSubmitting" :disabled="!formValid">更新密碼</v-btn>
        </v-card-actions>
      </v-form>
    </v-card>
  </v-dialog>
</template>
