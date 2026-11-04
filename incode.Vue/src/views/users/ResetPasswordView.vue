<script setup>
    import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
    import { ref, onMounted } from "vue";
    import { useRoute, useRouter } from "vue-router";
    import Swal from "sweetalert2";
    import { useAuthStore } from "@/stores/users/auth";

    const route = useRoute();
    const router = useRouter();
    const authStore = useAuthStore();
    const isLoading = ref(false);
    const formValid = ref(false);

    // 密碼顯示控制
    const showPassword = ref(false);
    const showConfirmPassword = ref(false);

    // 表單資料
    const formData = ref({
        email: "",
        token: "",
        newPassword: "",
        confirmPassword: ""
    });

    // 驗證規則 (與註冊頁面保持一致)
    const passwordRules = [
        (v) => !!v || "密碼為必填欄位",
        (v) => (v && v.length >= 6) || "密碼長度需至少 6 個字元",
    ];

    const confirmPasswordRules = [
        (v) => !!v || "確認密碼為必填欄位",
        (v) => v === formData.value.newPassword || "兩次輸入的密碼不一致",
    ];

    // 從網址抓取 Token 和 Email
    onMounted(() => {
        const { token, email } = route.query;

        // 如果網址沒有 token，代表連結無效，踢回首頁或登入頁
        if (!token || !email) {
            Swal.fire({
                icon: 'error',
                title: '連結無效',
                text: '重設密碼連結已失效或不完整，請重新申請。'
            }).then(() => {
                router.push('/auth');
            });
            return;
        }

        formData.value.token = token;
        formData.value.email = email;
    });

    const handleResetPassword = async () => {
        if (!formValid.value) return;

        isLoading.value = true;
        try {
            const success = await authStore.resetPassword({
                email: formData.value.email,
                token: formData.value.token,
                newPassword: formData.value.newPassword
            });

            if (success) {
                Swal.fire({
                    title: "密碼重設成功!",
                    text: "請使用新密碼重新登入",
                    icon: "success",
                    confirmButtonText: "前往登入",
                    confirmButtonColor: "#145369"
                }).then(() => {
                    // 成功後轉跳回登入頁
                    router.push('/auth');
                });
            }
        } catch (error) {
            const errorMsg = error.response?.data?.message || "重設失敗，連結可能已過期";
            Swal.fire({
                icon: "error",
                title: "重設失敗",
                text: errorMsg,
                confirmButtonColor: "#d33"
            });
        } finally {
            isLoading.value = false;
        }
    };
</script>

<template>
    <DefaultNavbar light />

    <v-container fluid class="fill-height bg-grey-lighten-5 mt-4">
        <div class="d-flex flex-column align-center w-100">

            <div class="text-center mb-4">
                <h1 class="text-h5 font-weight-bold text-grey-darken-2">
                    帳戶安全
                </h1>
            </div>

            <v-card class="mx-auto pa-8 pa-md-10" elevation="0" max-width="450" width="100%" rounded="xl" border>
                <div class="text-center mb-6">
                    <h2 class="text-h5 font-weight-bold mb-2 text-primary">
                        設定新密碼
                    </h2>
                    <p class="text-caption text-grey">
                        請為您的帳號設定一個新的密碼
                    </p>
                </div>

                <v-form ref="formRef" v-model="formValid" @submit.prevent="handleResetPassword">
                    <div class="mb-4">
                        <v-text-field v-model="formData.email" label="帳號 (Email)" variant="outlined"
                            density="comfortable" color="primary" prepend-inner-icon="mdi-email-outline" readonly
                            disabled bg-color="grey-lighten-4" hint="此為您申請重設密碼的帳號" persistent-hint></v-text-field>
                    </div>

                    <div class="mb-4">
                        <v-text-field v-model="formData.newPassword" label="新密碼" placeholder="請輸入新密碼"
                            :rules="passwordRules" :type="showPassword ? 'text' : 'password'"
                            :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
                            @click:append-inner="showPassword = !showPassword" prepend-inner-icon="mdi-lock-outline"
                            variant="outlined" density="comfortable" color="primary"
                            autocomplete="new-password"></v-text-field>
                    </div>

                    <div class="mb-6">
                        <v-text-field v-model="formData.confirmPassword" label="確認新密碼" placeholder="請再次輸入新密碼"
                            :rules="confirmPasswordRules" :type="showConfirmPassword ? 'text' : 'password'"
                            :append-inner-icon="showConfirmPassword ? 'mdi-eye-off' : 'mdi-eye'"
                            @click:append-inner="showConfirmPassword = !showConfirmPassword"
                            prepend-inner-icon="mdi-lock-check-outline" variant="outlined" density="comfortable"
                            color="primary" autocomplete="new-password"></v-text-field>
                    </div>

                    <v-hover v-slot="{ isHovering, props }">
                        <v-btn v-bind="props" type="submit" size="large" block rounded="lg" variant="flat"
                            class="text-body-1 font-weight-bold mb-4" :loading="authStore.isLoading"
                            :disabled="!formValid" :color="isHovering ? '#FFFFFF' : '#145369'" :style="{
                                color: isHovering ? '#145369 !important' : '#FFFFFF !important',
                                border: '1px solid #145369',
                            }">
                            確認修改
                        </v-btn>
                    </v-hover>

                    <div class="text-center">
                        <v-btn variant="text" size="small" color="grey-darken-1" prepend-icon="mdi-arrow-left"
                            to="/auth">
                            取消並返回登入
                        </v-btn>
                    </div>
                </v-form>
            </v-card>

            <div class="mt-4 text-caption text-grey-lighten-1">
                &copy; 2024 Incode Inc.
            </div>
        </div>
    </v-container>
</template>

<style scoped>
    :deep(.v-label) {
        font-size: 0.875rem;
        color: #666;
    }
</style>