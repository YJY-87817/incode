<script setup>
// example components
import DefaultInfoCard from "../../../examples/cards/infoCards/DefaultInfoCard.vue";
import CenteredBlogCard from "../../../examples/cards/blogCards/CenteredBlogCard.vue";
import UserCenteredBlogCard from "../../../examples/cards/blogCards/UserCenteredBlogCard.vue";
</script>
<template>
  <section class="py-7">
    <div class="container">
      <div class="row align-items-center">
        <div class="col-lg-6">
          <div class="row justify-content-start">
            <DefaultInfoCard
              color="info"
              icon="public"
              title="我的文章"
              description="We get insulted by others, lose trust for those We get back
                  freezes"
            />
            <DefaultInfoCard
              color="info"
              icon="payments"
              title="我的回覆"
              description="We get insulted by others, lose trust for those We get back
                  freezes"
            />
          </div>
          <div class="row justify-content-start mt-4">
            <DefaultInfoCard
              color="info"
              icon="apps"
              title="我的收藏"
              description="We get insulted by others, lose trust for those We get back
                  freezes"
            />
            <DefaultInfoCard
              color="info"
              icon="3p"
              title="我的點數"
              description="We get insulted by others, lose trust for those We get back
                  freezes"
            />
          </div>
        </div>
        <div class="col-lg-4 ms-auto mt-lg-0 mt-6">
          <UserCenteredBlogCard
            image="https://images.unsplash.com/photo-1544717302-de2939b7ef71?ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&ixlib=rb-1.2.1&auto=format&fit=crop&w=1950&q=80"
            title="會員帳號"
            description="會員資料"
          />
        </div>
      </div>
    </div>
  </section>
</template>
