<template>
  <h1>我的討論區</h1>
  <p>掛Api</p>
  <hr class="horizontal dark my-4" />
  <nav class="mt-5 text-center" style="font-size: 24px">
    <router-link :to="{ name: 'UserPost' }">發文</router-link> |
    <router-link :to="{ name: 'UserSave' }">收藏</router-link> |
    <router-link :to="{ name: 'UserLike' }">點讚</router-link>
  </nav>

  <div class="nested-content">
    <router-view></router-view>
  </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref, computed } from "vue";
import { useRouter } from "vue-router";
</script>
