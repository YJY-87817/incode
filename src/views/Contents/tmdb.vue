<script setup>
import { ref } from "vue";
import downloadPosters from "@/api/tmdbapi";
import NavbarDefault from "@/examples/navbars/NavbarDefault.vue";

const posters = ref([]);
const isLoading = ref(false);
const error = ref(null);

const downloadPoster = async () => {
  try {
    isLoading.value = true;
    error.value = null;
    posters.value = [];

    const results = await downloadPosters();
    posters.value = results;

    console.log("Posters downloaded:", results);
  } catch (err) {
    console.error("Error downloading posters:", err);
    error.value = "下載海報時發生錯誤，請稍後再試。";
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <NavbarDefault></NavbarDefault>
  <div class="container">
    <button
      class="btn btn-primary w-100 w-md-auto mb-4"
      @click="downloadPoster"
      :disabled="isLoading"
      type="button"
    >
      <span
        v-if="isLoading"
        class="spinner-border spinner-border-sm"
        role="status"
        aria-hidden="true"
      ></span>
      <span v-if="isLoading">下載中...</span>
      <span v-else>下載TMDB海報</span>
    </button>

    <!-- Error message -->
    <div v-if="error" class="alert alert-danger mt-3">
      {{ error }}
    </div>

    <!-- Loading indicator -->
    <div v-if="isLoading && !posters.length" class="text-center mt-4">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <p class="mt-2">正在下載海報...</p>
    </div>

    <!-- Posters display -->
    <div v-if="posters.length > 0" class="row mt-4">
      <div
        v-for="(poster, index) in posters"
        :key="index"
        class="col-md-4 mb-4"
      >
        <div class="card">
          <img
            :src="poster.posterData"
            :alt="`${poster.title} - ${poster.movieTitle} poster`"
            class="card-img-top"
            style="height: 450px; object-fit: cover"
          />
          <div class="card-body">
            <h5 class="card-title">{{ poster.movieTitle }}</h5>
            <p class="card-text">搜索關鍵字: {{ poster.title }}</p>
            <a
              :href="poster.posterUrl"
              target="_blank"
              class="btn btn-sm btn-outline-primary"
            >
              查看原始圖片
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.card {
  transition: transform 0.3s ease;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.card:hover {
  transform: translateY(-5px);
  box-shadow: 0 6px 12px rgba(0, 0, 0, 0.15);
}

.card-img-top {
  border-radius: 0.5rem 0.5rem 0 0 !important;
}

.spinner-border-sm {
  margin-right: 0.5rem;
}
</style>
