<script setup>
    import { useAlertStore } from "@/stores/alert";
    import { storeToRefs } from "pinia";

    const alertStore = useAlertStore();
    const { show, type, title, text, timeout, color } = storeToRefs(alertStore);
</script>

<template>
    <v-snackbar v-model="show" :timeout="timeout" location="top" color="transparent" elevation="0" variant="text">
        <v-alert :type="type" :title="title" :text="text" :color="color" closable class="shadow-3" border="start"
            elevation="4"></v-alert>
    </v-snackbar>
</template>

<style scoped>
    /* 讓 snackbar 背景透明，只顯示 alert 本體 */
    :deep(.v-snackbar__content) {
        padding: 0;
    }
</style>