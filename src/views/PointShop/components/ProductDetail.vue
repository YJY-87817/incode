<script setup>

</script>

<template>
    <div class="card shadow-sm border border-0 rounded-3 overflow-hidden">
        <div class="card-body p-4 p-lg-5">
            <div class="row g-4 g-lg-5">
                <div class="col-12 col-md-6">
                    <div class="d-flex align-items-center justify-content-center border border-translucent rounded-3 mb-3 bg-light"
                        style="aspect-ratio: 1/1;">
                        <img class="img-fluid p-3" src="https://placehold.co/600x600" alt="商品圖片" />
                    </div>
                </div>

                <div class="col-12 col-md-6 d-flex flex-column h-100">
                    <div class="d-flex justify-content-end mb-2">
                        <button class="btn btn-link text-info p-0" aria-label="Close">
                            <i class="bi bi-x-lg fs-5"></i>
                        </button>
                    </div>

                    <div>
                        <span class="badge rounded-pill bg-primary bg-opacity-10 mb-2">商品類型</span>
                        <h2 class="card-title fw-bold text-body-highlight mb-3">星戰黑武士T-shirt</h2>
                    </div>

                    <div class="mt-auto">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <span class="text-primary fw-bold fs-4">
                                2000 <small>P</small>
                            </span>
                            <span class="badge rounded-pill bg-light text-dark border">
                                庫存: 200
                            </span>
                        </div>

                        <div class="input-group mb-3 shadow-sm rounded-pill overflow-hidden border">
                            <button class="btn btn-light border-0 px-3 transition m-0" type="button">
                                <i class=" bi bi-dash-lg"></i>
                            </button>
                            <input type="number" class="form-control border-0 text-center bg-white fw-bold" value="1"
                                min="1">
                            <button class="btn btn-light border-0 px-3 transition m-0" type="button">
                                <i class="bi bi-plus-lg"></i>
                            </button>
                        </div>

                        <button class="btn btn-lg btn-outline-primary w-100 fw-bold transition rounded-pill shadow-sm">
                            <i class="fas fa-shopping-cart me-2"></i>加入購物車
                        </button>
                    </div>
                </div>
            </div>

            <hr class="opacity-10">

            <div class="row">
                <div class="col-12">
                    <h5 class="mb-3 fw-bold d-flex align-items-center">
                        <span class="far fa-file-alt me-2 text-primary"></span>商品詳情
                    </h5>
                    <div class="text-body-secondary lh-lg">
                        <p>星際大戰經典黑武士圖樣純棉短袖T恤，適合各種休閒場合穿搭。</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>