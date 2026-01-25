<template>
  <div class="mb-4">
    <input
      type="text"
      class="form-control"
      placeholder="搜尋主題…"
      v-model="search"
      @input="onInput"
    />
  </div>
</template>

<script setup>
import { ref, watch } from "vue";

const emit = defineEmits(["search"]);

const search = ref("");

let debounceTimer = null;
function onInput() {
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    emit("search", search.value.trim());
  }, 300);
}

// In case the parent wants to reset the search programmatically
watch(search, (newVal) => {
  if (!newVal) {
    emit("search", "");
  }
});
</script>

<style scoped>
input.form-control {
  max-width: 400px;
  border-color: #d1d3d4;
}

input.form-control:focus {
  border-color: #86b0bd;
  box-shadow: 0 0 0 0.2rem rgba(134, 176, 189, 0.25);
}
</style>
