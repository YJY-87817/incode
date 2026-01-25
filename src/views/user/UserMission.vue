<script setup>
  import { ref, computed } from "vue";

  // 搜尋關鍵字
  const searchQuery = ref("");

  // 模擬任務資料 (未來改為 API 獲取)
  const missions = ref([
    {
      id: 1,
      title: "完成新手引導教學",
      date: "2024-06-02 10:30",
      content: "閱讀完網站的使用規範，並完成第一次的個人資料設定，即可獲得新手獎勵點數。",
      status: "進行中", // 用於顯示不同顏色狀態 (可選)
      statusColor: "blue"
    },
    {
      id: 2,
      title: "參與每日簽到",
      date: "2024-06-01 08:00",
      content: "連續簽到 7 天可以獲得額外徽章，請記得每天登入會員中心點擊簽到按鈕。",
      status: "已完成",
      statusColor: "success"
    },
    {
      id: 3,
      title: "發布第一篇討論文章",
      date: "2024-05-28 15:45",
      content: "在「風景」或「人像」版塊發布您的第一篇攝影心得，字數需超過 50 字。",
      status: "已過期",
      statusColor: "grey"
    },
    {
      id: 4,
      title: "完善個人簡介",
      date: "2024-05-30 09:20",
      content: "前往設定頁面，填寫您的自我介紹與社群連結，讓更多人認識您。",
      status: "進行中",
      statusColor: "blue"
    },
    {
      id: 5,
      title: "邀請好友加入",
      date: "2024-06-03 14:00",
      content: "分享您的專屬邀請碼給朋友，雙方皆可獲得 100 積分。",
      status: "進行中",
      statusColor: "blue"
    }
  ]);

  // 搜尋過濾邏輯
  const filteredMissions = computed(() => {
    if (!searchQuery.value) return missions.value;
    const query = searchQuery.value.toLowerCase();
    return missions.value.filter(
      (item) =>
        item.title.toLowerCase().includes(query) ||
        item.content.toLowerCase().includes(query)
    );
  });
</script>

<template>
  <div class="mission-container ma-4">
    <div class="d-flex flex-column flex-sm-row justify-space-between align-center mb-4">
      <h3 class="mb-3 mb-sm-0">我的任務</h3>

      <div style="width: 100%; max-width: 300px;">
        <v-text-field v-model="searchQuery" density="comfortable" variant="outlined" label="搜尋任務" placeholder="輸入關鍵字..."
          prepend-inner-icon="mdi-magnify" single-line hide-details rounded="pill" color="primary"
          bg-color="white"></v-text-field>
      </div>
    </div>

    <v-container class="pa-0" fluid>
      <v-row>
        <v-col v-for="item in filteredMissions" :key="item.id" cols="12" md="6">
          <v-card class="mb-2 h-100 d-flex flex-column" elevation="0" border rounded="lg" hover>
            <v-card-item class="pb-0">
              <div class="d-flex justify-space-between align-start">
                <div class="text-h6 font-weight-bold text-primary text-truncate pr-2">
                  {{ item.title }}
                </div>
                <v-chip size="x-small" :color="item.statusColor" variant="tonal" class="font-weight-bold">
                  {{ item.status }}
                </v-chip>
              </div>

              <div class="text-caption text-grey mt-1 d-flex align-center">
                <v-icon icon="mdi-clock-outline" size="small" start class="mr-1"></v-icon>
                {{ item.date }}
              </div>
            </v-card-item>

            <v-card-text class="pt-3 text-body-1 text-medium-emphasis flex-grow-1">
              {{ item.content }}
            </v-card-text>

            <v-card-actions class="pt-0 px-4 pb-4">
              <v-spacer></v-spacer>
              <v-btn variant="text" color="primary" size="small" append-icon="mdi-arrow-right">
                查看詳情
              </v-btn>
            </v-card-actions>
          </v-card>
        </v-col>

        <v-col cols="12" v-if="filteredMissions.length === 0">
          <div class="text-center py-10 text-grey">
            <v-icon icon="mdi-clipboard-text-search-outline" size="60" class="mb-2"></v-icon>
            <p>找不到符合「{{ searchQuery }}」的任務</p>
          </div>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<style scoped>
  /* 針對標題做一些微調，讓它跟搜尋框對齊好看一點 */
  h3 {
    font-weight: 700;
    color: #344767;
    /* 延續您原本樣式的深藍灰色 */
  }
</style>