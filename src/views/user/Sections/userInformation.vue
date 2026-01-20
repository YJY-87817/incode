<script setup>
// example components
import userDefaultInfoCard from "@/views/user/Sections/userDefaultInfoCard.vue";
</script>
<template>
  <section class="py-7">
    <div class="container">
      <div class="row align-items-center">
        <!-- <div class="col-lg-12"> -->
        <div class="row justify-content-start">
          <userDefaultInfoCard
            color="info"
            icon="payments"
            col="col-md-6"
            title="我的關注"
            description="isLoading ? '載入中...' : `您目前關注了 ${followingCount} 位使用者`"
          />
          <userDefaultInfoCard
            color="info"
            icon="apps"
            col="col-md-6"
            title="我的粉絲"
            description="We get insulted by others, lose trust for those We get back
                  freezes"
          />
        </div>
      </div>
    </div>
  </section>
</template>
