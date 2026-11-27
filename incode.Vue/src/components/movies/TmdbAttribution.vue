<script setup>
// TMDB 使用條款：使用 API 的頁面要標示資料來源（官方 logo + 聲明文字）
import tmdbLogo from "@/assets/img/tmdb-logo.svg";
</script>

<template>
  <div class="tmdb-attribution d-flex align-items-center flex-wrap gap-2">
    <a
      href="https://www.themoviedb.org/"
      target="_blank"
      rel="noopener noreferrer"
      class="d-inline-flex"
    >
      <img :src="tmdbLogo" alt="The Movie Database (TMDB)" class="tmdb-logo" />
    </a>
    <span class="text-xs text-secondary">
      This product uses the TMDB API but is not endorsed or certified by TMDB.
    </span>
  </div>
</template>

<style scoped>
.tmdb-logo {
  height: 12px;
  width: auto;
  display: block;
}
</style>
