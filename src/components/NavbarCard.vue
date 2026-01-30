<script setup></script>

<template>
    <div class="card shadow-sm border-0 sticky-top mb-5 mt-2 accordion rounded" id="accordionNavbar">

        <button class="card-header d-flex justify-content-between align-items-center accordion-button rounded"
            style="background-color: #86B0BD" data-bs-toggle="collapse" data-bs-target="#collapseNavbar"
            aria-expanded="true" aria-controls="collapseNavbar">
            <h6 class="mb-0">網站導覽</h6>
        </button>

        <div class="accordion-collapse collapse show" id="collapseNavbar" aria-labelledby="headingOne"
            data-bs-parent="#accordionNavbar">
            <div class="card-body">
                <router-link :to="{ name: 'UserIndex' }">
                    <div class="mb-4 border-bottom pb-3">
                        <label class="form-label fw-bold">
                            <i class="bi bi-tag"></i> 會員中心
                        </label>
                    </div>
                </router-link>
                <router-link :to="{ name: 'forum' }">
                    <div class="mb-4 border-bottom pb-3">
                        <label class="form-label fw-bold">
                            <i class="bi bi-list"></i> 討論區
                        </label>
                    </div>
                </router-link>
                <router-link :to="{ name: 'mission' }">
                    <div class="mb-4 border-bottom pb-3">
                        <label class="form-label fw-bold">
                            <i class="bi bi-collection"></i> 任務大廳
                        </label>
                    </div>
                </router-link>
                <router-link :to="{ name: 'PointShop' }">
                    <div class="mb-2">
                        <label class="form-label fw-bold">
                            <i class="bi bi-box-seam"></i> 點數商城
                        </label>
                    </div>
                </router-link>
            </div>
        </div>
    </div>
</template>