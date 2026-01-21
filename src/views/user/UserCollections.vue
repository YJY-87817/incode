<template>
  <h1>我的討論區</h1>

  <ul>
    <li>發文</li>
    <li>收藏</li>
    <li>點讚</li>
  </ul>
  <v-container max-width="912">
    <v-row>
      <v-col v-for="n in 4" :key="n" cols="12" md="3">
        <v-card class="mb-3" hover>
          <v-card-item>
            <v-card-title> 大濛 {{ n }} </v-card-title>
            <v-card-subtitle> 類型 </v-card-subtitle>
          </v-card-item>
          <v-card-text> 介紹 </v-card-text>
          <!-- 跳轉到該文章 <v-card-actions>
            <v-btn text color="primary">查看文章</v-btn>-->
        </v-card>
      </v-col>
    </v-row>
  </v-container>
  <!-- 如果文章被刪除
   <v-card
  class="mx-auto"
  max-width="344"
  subtitle="The card stays disabled"
  title="Disabled card"
  disabled
  link
></v-card> -->
</template>