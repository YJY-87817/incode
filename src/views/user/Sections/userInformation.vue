<script setup>
// example components
import userDefaultInfoCard from "@/examples/cards/infoCards/DefaultInfoCard.vue";
</script>
<template>
  <section class="py-7">
    <div class="container">
      <div class="row align-items-center">
        <div class="col-lg-12">
          <div class="row justify-content-start">
            <userDefaultInfoCard
              color="info"
              icon="payments"
              title="我的回覆"
              description="We get insulted by others, lose trust for those We get back
                  freezes"
            />
            <userDefaultInfoCard
              color="info"
              icon="apps"
              title="我的收藏"
              description="We get insulted by others, lose trust for those We get back
                  freezes"
            />
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
