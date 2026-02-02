<script setup>
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import { onMounted, onUnmounted } from "vue";

const body = document.getElementsByTagName("body")[0];
onMounted(() => {
  body.classList.add("bg-gray-100");
});
onUnmounted(() => {
  body.classList.remove("bg-gray-100");
});
</script>

<template>
  <div class="bg-white">
    <DefaultNavbar light />

    <!-- 首頁主要內容 -->
    <div class="page-header min-vh-80" style="background-color: #f8f9fa">
      <div class="container">
        <div class="row">
          <div class="col-lg-8 mx-auto text-center mt-8 mb-5">
            <h1 class="display-3 font-weight-bold mb-4 text-dark">
              <i class="fas fa-hat-cowboy-side me-2"></i>
              歡迎來到 incode影庫
            </h1>
            <p class="lead mb-5 text-dark">
              探索、分享、交流 - 您的影像創作平台
            </p>

            <div class="row mt-5 g-4">
              <div class="col-md-4">
                <div class="card shadow-lg h-100">
                  <div class="card-body text-center p-4">
                    <div
                      class="icon icon-shape bg-gradient-primary shadow mx-auto mb-3"
                    >
                      <i
                        class="material-icons opacity-10"
                        style="font-size: 2rem"
                        >dashboard</i
                      >
                    </div>
                    <h5 class="mt-3 mb-2">任務大廳</h5>
                    <p class="text-sm mb-4">完成任務獲取點數</p>
                    <router-link
                      :to="{ name: 'task' }"
                      class="btn btn-primary btn-sm w-75"
                      >前往</router-link
                    >
                  </div>
                </div>
              </div>

              <div class="col-md-4">
                <div class="card shadow-lg h-100">
                  <div class="card-body text-center p-4">
                    <div
                      class="icon icon-shape bg-gradient-success shadow mx-auto mb-3"
                    >
                      <i
                        class="material-icons opacity-10"
                        style="font-size: 2rem"
                        >shopping_cart</i
                      >
                    </div>
                    <h5 class="mt-3 mb-2">點數商城</h5>
                    <p class="text-sm mb-4">使用點數兌換商品</p>
                    <router-link
                      :to="{ name: 'PointShop' }"
                      class="btn btn-success btn-sm w-75"
                      >前往</router-link
                    >
                  </div>
                </div>
              </div>

              <div class="col-md-4">
                <div class="card shadow-lg h-100">
                  <div class="card-body text-center p-4">
                    <div
                      class="icon icon-shape bg-gradient-info shadow mx-auto mb-3"
                    >
                      <i
                        class="material-icons opacity-10"
                        style="font-size: 2rem"
                        >forum</i
                      >
                    </div>
                    <h5 class="mt-3 mb-2">討論區</h5>
                    <p class="text-sm mb-4">與大家交流分享</p>
                    <router-link
                      :to="{ name: 'forum' }"
                      class="btn btn-info btn-sm w-75"
                      >前往</router-link
                    >
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <DefaultFooter />
  </div>
</template>

<style scoped>
.page-header {
  min-height: 80vh;
  display: flex;
  align-items: center;
}

.icon-shape {
  width: 60px;
  height: 60px;
  border-radius: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
