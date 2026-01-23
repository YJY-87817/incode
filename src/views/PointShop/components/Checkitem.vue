<script setup>

</script>

<template>
  <div class="col-12">
    <div class="card shadow-sm border-0 rounded-4">
      <div class="card-header bg-white border-bottom-0 pt-4 px-4">
        <h4 class="fw-bold text-dark mb-0">
          <i class="bi bi-cart3 me-2 text-primary"></i>購物清單
        </h4>
      </div>

      <div class="card-body px-4">
        <div class="d-flex flex-column gap-3">

          <div class="d-flex align-items-center p-3 border rounded-3 bg-light-hover transition-all">
            <img :src="`https://localhost:7190/img/product/NoImage.png`"
              class="rounded flex-shrink-0 object-fit-cover border" style="width: 80px; height: 80px;" />

            <div class="flex-grow-1 ms-3">
              <h6 class="fw-bold mb-1 text-dark">測試商品名稱</h6>
              <div class="d-flex align-items-center gap-3">
              </div>
            </div>
            <div>
              <button class="btn btn-outline-success">+</button>
              1
              <button class="btn btn-outline-success">-</button>
            </div>
            <div class="text-end">
              <button class="btn btn-outline-danger btn-sm border-0 hover-bg-danger">
                <i class="bi bi-trash3 me-1"></i>刪除
              </button>
            </div>
          </div>

          <div class="d-flex align-items-center p-3 border rounded-3 bg-light-hover transition-all">
            <img :src="`https://localhost:7190/img/product/NoImage.png`"
              class="rounded flex-shrink-0 object-fit-cover border" style="width: 80px; height: 80px;" />
            <div class="flex-grow-1 ms-3">
              <h6 class="fw-bold mb-1 text-dark">另一個測試商品</h6>
              <div class="d-flex align-items-center gap-3">
                <small class="text-secondary"><i class="bi bi-box-seam me-1"></i>數量：2</small>
                <small class="text-primary fw-medium"><i class="bi bi-coin me-1"></i>800 點</small>
              </div>
            </div>
            <div class="text-end">
              <button class="btn btn-outline-danger btn-sm border-0">
                <i class="bi bi-trash3 me-1"></i>刪除
              </button>
            </div>
          </div>

        </div>
      </div>

      <div class="card-footer bg-white border-top-0 pb-4 px-4 text-end">
        <hr class="text-muted opacity-25">
        <div class="d-flex justify-content-end align-items-baseline gap-2">
          <span class="text-secondary small">共 2 項商品，總計：</span>
          <span class="fs-4 fw-bold text-primary">1,000 P</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 簡單的微互動效果 */
.transition-all {
  transition: all 0.2s ease-in-out;
}

.bg-light-hover:hover {
  background-color: #f8f9fa;
  border-color: #dee2e6 !important;
  transform: translateY(-2px);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.object-fit-cover {
  object-fit: cover;
}
</style>