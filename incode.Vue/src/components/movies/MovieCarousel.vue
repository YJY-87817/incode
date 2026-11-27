<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { getRecentMovies } from "@/api/movieApi.js";
import { formatReleaseDate, releaseLabel, tmdbImage } from "@/utils/movie.js";
import TmdbAttribution from "@/components/movies/TmdbAttribution.vue";

const movies = ref([]);
const loading = ref(true);
const paused = ref(false);
const scroller = ref(null);

const visible = computed(() => loading.value || movies.value.length > 0);
const today = new Date();

const AUTO_SCROLL_MS = 4000;
let timer = null;
let reducedMotion = false;

// 後端 / TMDB 失敗時不顯示整個區塊，不影響首頁其他內容
const load = async () => {
  try {
    const res = await getRecentMovies();
    movies.value = Array.isArray(res?.data) ? res.data : [];
  } catch {
    movies.value = [];
  } finally {
    loading.value = false;
  }
};

// 每次往右捲動約 7 成的可視寬度；捲到底就回到最前面
const next = () => {
  if (paused.value || document.hidden || movies.value.length < 2) return;
  const container = scroller.value?.$el?.querySelector(
    ".v-slide-group__container",
  );
  if (!container) return;
  const max = container.scrollWidth - container.clientWidth;
  if (max <= 0) return;
  if (container.scrollLeft >= max - 4) {
    container.scrollTo({ left: 0, behavior: "smooth" });
  } else {
    container.scrollBy({
      left: container.clientWidth * 0.7,
      behavior: "smooth",
    });
  }
};

onMounted(() => {
  reducedMotion =
    window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
  load();
  if (!reducedMotion) timer = window.setInterval(next, AUTO_SCROLL_MS);
});

onBeforeUnmount(() => {
  if (timer) window.clearInterval(timer);
});
</script>

<template>
  <section
    v-if="visible"
    class="movie-carousel text-start"
    aria-label="近期上映電影"
  >
    <div class="d-flex align-items-baseline justify-content-between mb-2 px-1">
      <h2 class="h5 mb-0 text-dark">近期上映</h2>
      <span class="text-xs text-secondary">上個月至下個月的台灣上映電影</span>
    </div>

    <div v-if="loading" class="d-flex gap-3 overflow-hidden" aria-busy="true">
      <div v-for="n in 6" :key="n" class="movie-skeleton" />
    </div>

    <v-slide-group
      v-else
      ref="scroller"
      show-arrows
      class="movie-slide-group"
      @mouseenter="paused = true"
      @mouseleave="paused = false"
      @focusin="paused = true"
      @focusout="paused = false"
    >
      <v-slide-group-item v-for="movie in movies" :key="movie.id">
        <router-link
          :to="{ name: 'movie-detail', params: { id: movie.id } }"
          class="movie-slide"
          :title="movie.title"
        >
          <div class="poster-wrap">
            <img
              :src="tmdbImage(movie.posterPath, 'w342')"
              :alt="`${movie.title} 海報`"
              loading="lazy"
              class="poster"
            />
            <span
              v-if="releaseLabel(movie.releaseDate, today) === '即將上映'"
              class="badge bg-gradient-primary release-badge"
              >即將上映</span
            >
          </div>
          <div class="movie-title text-dark">{{ movie.title }}</div>
          <div class="text-xs text-secondary">
            {{ formatReleaseDate(movie.releaseDate) }}
          </div>
        </router-link>
      </v-slide-group-item>
    </v-slide-group>

    <div class="mt-2 px-1">
      <TmdbAttribution />
    </div>
  </section>
</template>

<style scoped>
.movie-slide {
  display: block;
  width: 150px;
  margin: 0 8px;
  text-decoration: none;
  flex: 0 0 auto;
}
.poster-wrap {
  position: relative;
  aspect-ratio: 2 / 3;
  border-radius: 0.75rem;
  overflow: hidden;
  background: #e9ecef;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.18);
  transition: transform 0.2s ease;
}
.movie-slide:hover .poster-wrap,
.movie-slide:focus-visible .poster-wrap {
  transform: translateY(-4px);
}
.poster {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.release-badge {
  position: absolute;
  top: 8px;
  left: 8px;
  font-size: 0.7rem;
}
.movie-title {
  margin-top: 0.5rem;
  font-size: 0.9rem;
  font-weight: 600;
  line-height: 1.3;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.movie-skeleton {
  width: 150px;
  aspect-ratio: 2 / 3;
  flex: 0 0 auto;
  border-radius: 0.75rem;
  background: linear-gradient(90deg, #e9ecef 25%, #f4f5f7 50%, #e9ecef 75%);
  background-size: 200% 100%;
  animation: movie-shimmer 1.4s infinite;
}
@keyframes movie-shimmer {
  to {
    background-position: -200% 0;
  }
}
@media (prefers-reduced-motion: reduce) {
  .movie-skeleton {
    animation: none;
  }
  .poster-wrap {
    transition: none;
  }
}
@media (max-width: 576px) {
  .movie-slide,
  .movie-skeleton {
    width: 120px;
  }
}
</style>
