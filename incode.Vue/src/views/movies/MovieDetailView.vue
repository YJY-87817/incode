<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import DefaultFooter from "@/components/layout/FooterCentered.vue";
import TmdbAttribution from "@/components/movies/TmdbAttribution.vue";
import { getMovie } from "@/api/movieApi.js";
import {
  formatReleaseDate,
  formatRuntime,
  tmdbImage,
  youtubeEmbedUrl,
} from "@/utils/movie.js";
import { getAvatarUrl } from "@/utils/avatar.js";

const route = useRoute();
const movie = ref(null);
const loading = ref(true);
// "notFound" | "error" | ""
const failure = ref("");

const load = async () => {
  loading.value = true;
  failure.value = "";
  movie.value = null;
  try {
    const res = await getMovie(route.params.id);
    movie.value = res?.data ?? null;
    if (!movie.value) failure.value = "notFound";
  } catch (error) {
    failure.value = error?.response?.status === 404 ? "notFound" : "error";
  } finally {
    loading.value = false;
  }
};

onMounted(load);
watch(() => route.params.id, load);

const backdropStyle = computed(() =>
  movie.value?.backdropPath
    ? {
        backgroundImage: `linear-gradient(rgba(0,0,0,.55), rgba(0,0,0,.75)), url(${tmdbImage(movie.value.backdropPath, "w1280")})`,
      }
    : {},
);
const trailerUrl = computed(() =>
  youtubeEmbedUrl(movie.value?.trailerYoutubeKey),
);
const infoLine = computed(() =>
  [
    formatReleaseDate(movie.value?.releaseDate),
    formatRuntime(movie.value?.runtime),
  ]
    .filter(Boolean)
    .join("　·　"),
);
</script>

<template>
  <DefaultNavbar light />

  <main class="movie-page">
    <div v-if="loading" class="container state-box text-center text-secondary">
      載入中…
    </div>

    <div v-else-if="failure" class="container state-box text-center">
      <h1 class="h3 text-dark mb-3">
        {{ failure === "notFound" ? "找不到這部電影" : "電影資訊暫時無法取得" }}
      </h1>
      <p class="text-secondary mb-4" v-if="failure === 'error'">請稍後再試。</p>
      <router-link to="/" class="btn btn-primary">回首頁</router-link>
    </div>

    <template v-else-if="movie">
      <section class="movie-hero" :style="backdropStyle">
        <div class="container py-6">
          <div class="row g-4 align-items-center">
            <div class="col-md-4 col-lg-3 text-center">
              <img
                :src="tmdbImage(movie.posterPath, 'w500')"
                :alt="`${movie.title} 海報`"
                class="hero-poster"
              />
            </div>
            <div class="col-md-8 col-lg-9 text-white">
              <h1 class="display-6 text-white mb-1">{{ movie.title }}</h1>
              <p
                v-if="
                  movie.originalTitle && movie.originalTitle !== movie.title
                "
                class="text-white opacity-8 mb-2"
              >
                {{ movie.originalTitle }}
              </p>
              <p class="mb-2" v-if="infoLine">{{ infoLine }}</p>
              <p class="mb-2" v-if="movie.genres?.length">
                <span
                  v-for="genre in movie.genres"
                  :key="genre"
                  class="badge bg-white text-dark me-1"
                  >{{ genre }}</span
                >
              </p>
              <p class="mb-3" v-if="movie.voteAverage > 0">
                <v-icon size="small" color="amber">mdi-star</v-icon>
                {{ movie.voteAverage.toFixed(1) }} / 10
              </p>
              <p class="mb-2" v-if="movie.directors?.length">
                <strong>導演：</strong>{{ movie.directors.join("、") }}
              </p>
              <h2 class="h5 text-white mt-4">劇情簡介</h2>
              <p class="mb-0 overview">
                {{ movie.overview || "目前沒有繁體中文簡介。" }}
              </p>
            </div>
          </div>
        </div>
      </section>

      <div class="container py-5">
        <section v-if="movie.cast?.length" class="mb-5">
          <h2 class="h5 text-dark mb-3">主要演員</h2>
          <div class="row g-3">
            <div
              v-for="person in movie.cast"
              :key="`${person.name}-${person.character}`"
              class="col-6 col-sm-4 col-md-3 col-lg-2"
            >
              <div class="text-center">
                <img
                  :src="
                    person.profilePath
                      ? tmdbImage(person.profilePath, 'w185')
                      : getAvatarUrl(person.name)
                  "
                  :alt="person.name"
                  loading="lazy"
                  class="cast-photo"
                />
                <div class="fw-semibold text-dark mt-2 text-sm">
                  {{ person.name }}
                </div>
                <div class="text-xs text-secondary" v-if="person.character">
                  {{ person.character }}
                </div>
              </div>
            </div>
          </div>
        </section>

        <section v-if="trailerUrl" class="mb-5">
          <h2 class="h5 text-dark mb-3">預告片</h2>
          <div class="ratio ratio-16x9 trailer">
            <iframe
              :src="trailerUrl"
              :title="`${movie.title} 預告片`"
              loading="lazy"
              allow="
                accelerometer;
                encrypted-media;
                gyroscope;
                picture-in-picture;
                fullscreen;
              "
              referrerpolicy="strict-origin-when-cross-origin"
            />
          </div>
        </section>

        <div
          class="d-flex justify-content-between align-items-center flex-wrap gap-2"
        >
          <router-link to="/" class="btn btn-outline-primary btn-sm">
            <v-icon size="small">mdi-arrow-left</v-icon> 回首頁
          </router-link>
          <TmdbAttribution />
        </div>
      </div>
    </template>
  </main>

  <DefaultFooter />
</template>

<style scoped>
.movie-page {
  min-height: 70vh;
}
.state-box {
  padding: 8rem 0 4rem;
}
.movie-hero {
  padding-top: 4.5rem; /* 導覽列高度 */
  background-color: #212529;
  background-size: cover;
  background-position: center;
}
.hero-poster {
  width: 100%;
  max-width: 280px;
  border-radius: 0.75rem;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45);
}
.overview {
  white-space: pre-line;
  line-height: 1.7;
}
.cast-photo {
  width: 100%;
  max-width: 120px;
  aspect-ratio: 2 / 3;
  object-fit: cover;
  border-radius: 0.5rem;
  background: #e9ecef;
}
.trailer {
  border-radius: 0.75rem;
  overflow: hidden;
  max-width: 900px;
}
.trailer iframe {
  border: 0;
}
</style>
