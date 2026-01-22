<script setup>
  import { onMounted, onUnmounted, ref, computed } from "vue";
  import { useRouter } from "vue-router";
  import axios from "axios";
  import Typed from "typed.js";
  // 引入 Store 以便取得 Token
  import { useAuthStore } from "@/stores/user/auth";
  const authStore = useAuthStore();

  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import DefaultFooter from "@/examples/footers/FooterDefault.vue";
  import bg0 from "@/assets/img/bg9.jpg";
  //修改資料彈出視窗
  import { shallowRef } from 'vue'
  const dialog = shallowRef(false);

  const router = useRouter();
  const API_BASE_URL = "https://localhost:7190/api";

  // const goToBasic = () => {
  //   router.push("/login");
  // };
  // const goToindex = () => {
  //   router.push("/");
  // };

  const isLoading = ref(false);

  //hooks
  onMounted(() => {
    const body = document.getElementsByTagName("body")[0];
    body.classList.add("about-us");
    body.classList.add("bg-gray-200");

    if (document.getElementById("typed")) {
      new Typed("#typed", {
        stringsElement: "#typed-strings",
        typeSpeed: 90,
        backSpeed: 90,
        backDelay: 200,
        startDelay: 500,
        loop: true,
      });
    }
  });

  onUnmounted(() => {
    const body = document.getElementsByTagName("body")[0];
    body.classList.remove("about-us");
    body.classList.remove("bg-gray-200");
  });
</script>
<template>
  <DefaultNavbar :action="{
      route: 'javascript:;',
      label: 'Buy Now',
      color: 'btn-white',
    }" transparent />
  <header class="bg-gradient-dark"></header>

  <div class="container user-index-page mt-5 pt-5" style="min-height: 80vh">
    <div class="row">
      <div class="col-12">
        <h1 class="mb-4 display-4 font-weight-bold text-center">會員中心</h1>

        <h3>通知</h3>
        <!-- 通知格式 可以篩選-->
        <v-row class="mt-3 align-center" justify="center" dense>
          <v-col cols="12" md="4">
            <v-card append-icon="mdi-check" class="mx-auto" prepend-icon="mdi-account" subtitle="傳送時間" title="系統">
              <v-card-text>提醒內容</v-card-text>
            </v-card>
          </v-col>
          <v-col cols="12" md="4">
            <v-card class="mx-auto" subtitle="prepend and append" title="任務">
              <template v-slot:prepend>
                <v-icon color="primary" icon="mdi-account"></v-icon>
              </template>
              <template v-slot:append>
                <v-icon color="success" icon="mdi-check"></v-icon>
              </template>
              <v-card-text>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do
                eiusmod.</v-card-text>
            </v-card>
          </v-col>
          <v-col cols="12" md="4">
            <v-card append-avatar="https://cdn.vuetifyjs.com/images/john.jpg" class="mx-auto"
              prepend-avatar="https://cdn.vuetifyjs.com/images/logos/v-alt.svg"
              subtitle="prepend-avatar and append-avatar" title="好友">
              <v-card-text>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do
                eiusmod.</v-card-text>
            </v-card>
          </v-col>
        </v-row>
        <h2 class="mt-5">我的資料</h2>
        <v-container max-width="912" class="mt-4">
          <v-row>
            <v-col cols="2" md="6">
              <v-card class="mx-auto" max-width="500">
                <v-card-item class="bg-cyan-darken-1">
                  <v-card-title>
                    <span class="text-h5">暱稱name</span>
                  </v-card-title>
                  <template v-slot:append>
                    <v-defaults-provider :defaults="{
            VBtn: {
              variant: 'text',
              density: 'comfortable',
            }
          }">
                      <!--目前用不到 <v-btn icon="mdi-chevron-left"></v-btn>
                      <v-btn icon="mdi-dots-vertical"></v-btn>
                       -->
                      <!--改資料 -->
                      <div class="pa-4 text-center">
                        <v-dialog v-model="dialog" max-width="600">
                          <template v-slot:activator="{ props: activatorProps }">
                            <v-btn class="text-none font-weight-regular" prepend-icon="mdi-pencil" text="編輯"
                              variant="tonal" v-bind="activatorProps"></v-btn>
                          </template>
                          <!-- 再多一個進設定 -->
                          <v-card prepend-icon="mdi-account" title="我的資料">
                            <v-card-text>
                              <v-row dense>
                                <v-col cols="12" md="4" sm="6">
                                  <v-text-field label="暱稱*" required></v-text-field>
                                </v-col>

                                <v-col cols="12" md="4" sm="6">
                                  <v-text-field hint="example of helper text only on focus" label="Email*"
                                    required></v-text-field>
                                </v-col>

                                <v-col cols="12" md="4" sm="6">
                                  <v-text-field hint="example of persistent helper text" label="手機"
                                    persistent-hint></v-text-field>
                                </v-col>

                                <v-col cols="12" md="4" sm="6">
                                  <v-text-field label="地址"></v-text-field>
                                </v-col>

                                <v-col cols="12" md="4" sm="6">
                                  <v-text-field label="密碼*" type="password" required></v-text-field>
                                </v-col>

                                <v-col cols="12" md="4" sm="6">
                                  <v-text-field label="確認密碼*" type="password" required></v-text-field>
                                </v-col>

                                <v-col cols="12" sm="6">
                                  <v-select :items="['0-17', '18-29', '30-54', '54+']" label="Age"></v-select>
                                </v-col>

                                <v-col cols="12" sm="6">
                                  <v-autocomplete
                                    :items="['Skiing', 'Ice hockey', 'Soccer', 'Basketball', 'Hockey', 'Reading', 'Writing', 'Coding', 'Basejump']"
                                    label="Interests" auto-select-first multiple></v-autocomplete>
                                </v-col>
                              </v-row>

                              <small class="text-caption text-medium-emphasis">*自我介紹</small>
                            </v-card-text>

                            <v-divider></v-divider>

                            <v-card-actions>
                              <v-spacer></v-spacer>

                              <v-btn text="關閉" variant="plain" @click="dialog = false"></v-btn>

                              <v-btn color="primary" text="保存" variant="tonal" @click="dialog = false"></v-btn>
                            </v-card-actions>
                          </v-card>
                        </v-dialog>
                      </div>
                    </v-defaults-provider>
                  </template>
                </v-card-item>
                <v-list>
                  <v-list-item append-icon="mdi-message-text" prepend-icon="mdi-email" title="Email帳號"></v-list-item>
                  <v-divider inset></v-divider>
                  <v-list-item prepend-icon="mdi-phone" title="手機"></v-list-item>
                  <v-divider inset></v-divider>
                  <v-list-item prepend-icon="mdi-map-marker" title="地址"></v-list-item>
                  <v-divider inset></v-divider>
                  <v-list-item prepend-icon="mdi-map-marker" title="生日"></v-list-item>
                </v-list>
              </v-card>
            </v-col>
            <v-col cols="6">
              <v-card class="mx-auto" style="position: relative;">
                <v-img height="300" src="https://picsum.photos/700?image=996" cover></v-img>
                <div class="d-flex align-center justify-space-around" style="margin-top: -30px">
                  <v-avatar size="100">
                    <v-img alt="John" src="https://cdn.vuetifyjs.com/images/john.jpg"></v-img>
                  </v-avatar>
                </div>
                <!-- 做換大頭貼 -->
                <v-btn icon="mdi-pencil" style="position: absolute; bottom: 10px; right: 10px;" variant="text"></v-btn>
              </v-card>
            </v-col>
            <h5>自我介紹</h5>
          </v-row>
        </v-container>
        <hr class="horizontal dark my-4" />
        <h2 class="mt-5">我的討論區</h2>
        <nav class="mt-5 text-center" style="font-size: 24px;">
          <router-link :to="{ name: 'UserPost' }">發文</router-link> |
          <router-link :to="{ name: 'UserSave' }">收藏</router-link> |
          <router-link :to="{ name: 'UserLike' }">點讚</router-link>
        </nav>


        <div class="nested-content">
          <router-view></router-view>
        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>
<style scoped>
  /* 讓被選中的路由連結變色 (Bootstrap 預設 router-link-active 可能需要微調) */
  .router-link-active {
    background-color: #fff;
    /* 或您想要的主題色 */
    color: #000;
    font-weight: bold;
    box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
    border-radius: 5px;
  }

  .nav-link {
    color: #344767;
    font-weight: 600;
    margin-right: 1rem;
    /* 按鈕之間的間距 */
  }
</style>