<script setup>
// example component
import userHorizontalTeamCard from "@/examples/cards/teamCards/userHorizontalTeamCard.vue";

import { ref } from "vue";
const reviews = ref([
  {
    id: 1,
    username: "王小明",
    avatarImage: "@/assets/img/team-3.jpg",
    detail: "這家餐廳的義大利麵非常好吃！",
  },
  {
    id: 2,
    username: "李知恩",
    avatarImage: "@/assets/img/team-2.jpg",
    detail: "環境整潔，服務態度親切。",
  },
]);
</script>

<template>
  <section class="pb-5 position-relative bg-gradient-dark mx-n3">
    <div class="container">
      <div class="row">
        <div class="col-md-8 text-start mb-5 mt-5">
          <h3 class="text-white z-index-1 position-relative">社群相關</h3>
          <p class="text-white opacity-8 mb-0">
            There’s nothing I really wanted to do in life that I wasn’t able to
            get good at. That’s my skill.
          </p>
        </div>
      </div>
      <div class="row mt-4">
        <div class="col-lg-6 col-12">
          <userHorizontalTeamCard
            class="mt-4 z-index-2"
            v-for="item in reviews"
            :key="item.id"
            :username="item.username"
            :image="item.avatarImage"
            :detail="item.detail"
          />
        </div>
      </div>
    </div>
  </section>
</template>
