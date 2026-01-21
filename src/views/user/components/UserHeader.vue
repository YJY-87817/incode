<script setup>
defineProps({
  posts: {
    type: Number,
    default: 0,
  },
  followers: {
    type: Number, // 建議後端傳數字，前端自己轉 k/m 格式，或者直接傳字串
    default: 0,
  },
  following: {
    type: Number,
    default: 0,
  },
});
</script>
<template>
  <div class="row mb-4">
    <div class="col-auto">
      <span class="h6 me-1">{{ following }}</span>
    </div>
    <div class="col-auto">
      <span class="h6 me-1">{{ followers }}</span>
    </div>
    <div class="col-auto">
      <span class="h6 me-1">{{ post }}</span>
    </div>
  </div>
</template>
