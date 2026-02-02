<script setup>
  import { computed } from "vue";
  import { useAlertStore } from "@/stores/alert";
  import { storeToRefs } from "pinia";

  const alertStore = useAlertStore();
  const { show, type, title, text, timeout, color } = storeToRefs(alertStore);

  const alertColor = computed(() => {
    if (color.value) return color.value; // 手動指定的優先

    if (type.value === "success") {
      return "#86b0bd"; // 你的自訂成功色
    }
    return undefined; // 其他類型維持 Vuetify 原廠設定 (紅、橘、藍)
  });
</script>

<template>
  <v-snackbar v-model="show" :timeout="timeout" location="top" color="transparent" elevation="0" variant="text">
    <v-alert :type="type" :title="title" :text="text" :color="alertcolor" closable class="shadow-3" border="start"
      elevation="4"></v-alert>
  </v-snackbar>
</template>

<style scoped>
  /* 讓 snackbar 背景透明，只顯示 alert 本體 */
  :deep(.v-snackbar__content) {
    padding: 0;
  }
</style>

<!-- 改掉 -->