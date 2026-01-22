<script setup>
defineProps({
  order: { type: Object, required: true },
});
</script>

<template>
  <div class="accordion-item mb-3 border shadow-sm">
    <h2 class="accordion-header">
      <button
        class="accordion-button collapsed"
        type="button"
        data-bs-toggle="collapse"
        :data-bs-target="'#order-' + order.id"
      >
        <div class="container-fluid">
          <div class="row w-100 align-items-center">
            <div class="col-6 col-md-3">
              <strong>#{{ order.id }}</strong>
            </div>
            <div class="col-6 col-md-3 text-muted small">{{ order.date }}</div>
            <div class="col-6 col-md-3 text-primary fw-bold">
              ${{ order.total.toLocaleString() }}
            </div>
            <div class="col-6 col-md-3">
              <span :class="['badge', order.statusClass]">{{
                order.status
              }}</span>
            </div>
          </div>
        </div>
      </button>
    </h2>

    <div
      :id="'order-' + order.id"
      class="accordion-collapse collapse"
      data-bs-parent="#orderAccordion"
    >
      <div class="accordion-body bg-white">
        <div class="row g-4">
          <div class="col-md-7">
            <h6 class="fw-bold border-bottom pb-2">商品明細</h6>
            <div
              v-for="(item, index) in order.items"
              :key="index"
              class="d-flex align-items-center mb-3"
            >
              <img :src="item.img" class="product-img me-3" :alt="item.name" />
              <div class="flex-grow-1">
                <div class="d-flex justify-content-between">
                  <span>{{ item.name }}</span>
                  <span class="fw-bold"
                    >${{ item.price.toLocaleString() }}</span
                  >
                </div>
                <small class="text-muted">數量: {{ item.qty }}</small>
              </div>
            </div>
          </div>

          <div class="col-md-5">
            <h6 class="fw-bold border-bottom pb-2">配送資訊</h6>
            <p class="small mb-1 text-muted">
              收件人：{{ order.recipient.name }}
            </p>
            <p class="small mb-1 text-muted">
              電話：{{ order.recipient.phone }}
            </p>
            <p class="small mb-3 text-muted">
              地址：{{ order.recipient.address }}
            </p>
          </div>
        </div>

        <div class="mt-3 text-end">
          <button class="btn btn-outline-primary btn-sm me-2">再次購買</button>
          <button class="btn btn-outline-secondary btn-sm">聯繫客服</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.product-img {
  width: 60px;
  height: 60px;
  object-fit: cover;
  border-radius: 5px;
}
</style>
