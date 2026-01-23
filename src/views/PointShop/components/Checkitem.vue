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

            <div class="d-flex align-items-center flex-grow-1">
              <img :src="`https://localhost:7190/img/product/NoImage.png`"
                class="rounded flex-shrink-0 object-fit-cover border" style="width: 65px; height: 65px;" />
              <div class="ms-3">
                <h6 class="fw-bold mb-0 text-dark">另一個測試商品</h6>
              </div>
            </div>

            <div class="d-flex align-items-center gap-4 px-3">
              <div class="text-center">
                <small class="text-secondary d-block">數量</small>
                <span class="fw-medium">2</span>
              </div>
              <div class="text-end" style="min-width: 80px;">
                <small class="text-secondary d-block">小計</small>
                <span class="text-primary fw-bold">800 點</span>
              </div>
            </div>

            <div class="ms-3 border-start ps-3 py-1">
              <div class="d-flex align-items-center gap-2">
                <button class="btn btn-sm btn-outline-secondary" style="font-size: 0.875rem;">−</button>
                <button class="btn btn-sm btn-outline-secondary" style="font-size: 0.875rem;">+</button>
                <button class="btn btn-sm btn-outline-danger ms-1" style="font-size: 0.875rem;">
                  <i class="bi bi-trash" style="font-size: 0.875rem;"></i>
                </button>
              </div>
            </div>

          </div>

          <div class="d-flex align-items-center p-3 border rounded-3 bg-light-hover transition-all">

            <div class="d-flex align-items-center flex-grow-1">
              <img :src="`https://localhost:7190/img/product/NoImage.png`"
                class="rounded flex-shrink-0 object-fit-cover border" style="width: 65px; height: 65px;" />
              <div class="ms-3">
                <h6 class="fw-bold mb-0 text-dark">另一個測試商品</h6>
              </div>
            </div>

            <div class="d-flex align-items-center gap-4 px-3">
              <div class="text-center">
                <small class="text-secondary d-block">數量</small>
                <span class="fw-medium">2</span>
              </div>
              <div class="text-end" style="min-width: 80px;">
                <small class="text-secondary d-block">小計</small>
                <span class="text-primary fw-bold">800 點</span>
              </div>
            </div>

            <div class="ms-3 border-start ps-3 py-1">
              <div class="d-flex align-items-center gap-2">
                <button class="btn btn-sm btn-outline-secondary" style="font-size: 0.875rem;">−</button>
                <button class="btn btn-sm btn-outline-secondary" style="font-size: 0.875rem;">+</button>
                <button class="btn btn-sm btn-outline-danger ms-1" style="font-size: 0.875rem;">
                  <i class="bi bi-trash" style="font-size: 0.875rem;"></i>
                </button>
              </div>
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