<script setup>

</script>

<template>
  <div class="card shadow-sm border-0 rounded-4">
    <div class="card-header bg-white border-bottom-0 pt-4 px-4">
      <h5 class="fw-bold text-dark mb-0">
        <i class="bi bi-truck me-2 text-primary"></i>寄送資訊
      </h5>
    </div>

    <div class="card-body px-4">
      <div class="mb-3">
        <label class="form-label text-secondary small fw-bold">收件人姓名</label>
        <input type="text" class="form-control" placeholder="請輸入真實姓名">
      </div>
      <div class="mb-3">
        <label class="form-label text-secondary small fw-bold">聯絡電話</label>
        <input type="tel" class="form-control" placeholder="09xx-xxx-xxx">
      </div>
      <div class="mb-3">
        <label class="form-label text-secondary small fw-bold">寄送地址</label>
        <input type="text" class="form-control" placeholder="請輸入詳細地址">
      </div>
      <div class="mb-3">
        <label class="form-label text-secondary small fw-bold">備註事項</label>
        <textarea class="form-control" rows="3" placeholder="如有特殊需求請填寫"></textarea>
      </div>
    </div>

    <div class="card-footer bg-white border-top-0 pb-4 px-4">
      <button class="btn w-100 py-2 fw-bold shadow-sm" style="background-color: #145369; color: white;">
        <i class="bi bi-check2-circle me-1"></i> 確認結帳
      </button>
    </div>
  </div>
</template>
