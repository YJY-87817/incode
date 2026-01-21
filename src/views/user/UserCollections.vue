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
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>