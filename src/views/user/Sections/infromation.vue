<script setup>
defineProps({
  id: {
    type: Number,
    required: true,
  },
  username: {
    type: String,
    required: true,
  },
  avatarImage: {
    type: String,
    required: true,
  },
  detail: {
    type: String,
    required: false,
  },
  role: {
    type: String,
    default: "使用者",
  },
  roleColor: {
    // 可刪掉
    type: String,
    default: "success",
  },
});
</script>
<template>
  <div class="card card-profile">
    <div class="row">
      <div class="col-lg-4 col-md-6 col-12 mt-n5">
        <div class="p-3 pe-md-0">
          <img
            class="w-100 border-radius-md shadow-lg"
            :src="image"
            :alt="username"
          />
        </div>
      </div>
      <div class="col-lg-8 col-md-6 col-12 my-auto">
        <div class="card-body ps-lg-0">
          <h5 class="mb-0">{{ username }}</h5>
          <h6 :class="`text-${roleColor}`">{{ role }}</h6>
          <p class="mb-0">
            {{ detail }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
