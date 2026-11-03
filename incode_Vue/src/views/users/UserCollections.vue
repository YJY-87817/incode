<template>
  <div>
    <div class="mt-4 px-4 mb-4">
      <h3>我的討論區</h3>
    </div>

    <nav
      class="text-center py-3 w-100"
      style="font-size: 24px; background-color: #86b0bd"
    >
      <router-link :to="{ name: 'UserPost' }">發文</router-link> |
      <router-link :to="{ name: 'UserSave' }">收藏</router-link> |
      <router-link :to="{ name: 'UserLike' }">點讚</router-link>
    </nav>

    <div class="nested-content">
      <router-view></router-view>
    </div>
  </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref, computed } from "vue";
import { useRouter } from "vue-router";
</script>
