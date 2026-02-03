<script setup>
// 套版範例
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";

</script>

<template>
    <DefaultNavbar light />

    <div class="container-fluid min-vh-100 d-flex flex-column justify-content-center align-items-center">

        <!-- 404 Error Text -->
        <div class="text-center">
            <div class="error mx-auto" data-text="404">404</div>
            <p class="lead text-gray-800 mb-5">你走錯路了</p>
            <p class="text-gray-500 mb-3">看來你走到了錯誤的影廳...</p>
            <router-link to="/" class="btn btn-custom-primary">回首頁</router-link>
        </div>

    </div>
    <DefaultFooter />

</template>

<style scoped>
.error {
    color: #5a5c69;
    font-size: 7rem;
    position: relative;
    line-height: 1;
    width: 12.5rem;
    font-weight: 800;
}

.error:before {
    content: attr(data-text);
    position: absolute;
    left: -2px;
    text-shadow: 1px 0 #4e73df;
    top: 0;
    color: #5a5c69;
    background: #fff;
    overflow: hidden;
    clip: rect(0, 900px, 0, 0);
    animation: noise-anim-2 3s infinite linear alternate-reverse;
}

.error:after {
    content: attr(data-text);
    position: absolute;
    left: 2px;
    text-shadow: -1px 0 #e74a3b;
    top: 0;
    color: #5a5c69;
    background: #fff;
    overflow: hidden;
    clip: rect(0, 900px, 0, 0);
    animation: noise-anim 2s infinite linear alternate-reverse;
}

@keyframes noise-anim {
    0% {
        clip: rect(6px, 9999px, 86px, 0);
    }

    10% {
        clip: rect(98px, 9999px, 3px, 0);
    }

    20% {
        clip: rect(32px, 9999px, 60px, 0);
    }

    30% {
        clip: rect(51px, 9999px, 7px, 0);
    }

    40% {
        clip: rect(39px, 9999px, 70px, 0);
    }

    50% {
        clip: rect(96px, 9999px, 5px, 0);
    }

    60% {
        clip: rect(21px, 9999px, 79px, 0);
    }

    70% {
        clip: rect(4px, 9999px, 69px, 0);
    }

    80% {
        clip: rect(68px, 9999px, 54px, 0);
    }

    90% {
        clip: rect(79px, 9999px, 38px, 0);
    }

    100% {
        clip: rect(33px, 9999px, 63px, 0);
    }
}

@keyframes noise-anim-2 {
    0% {
        clip: rect(60px, 9999px, 25px, 0);
    }

    10% {
        clip: rect(43px, 9999px, 45px, 0);
    }

    20% {
        clip: rect(95px, 9999px, 7px, 0);
    }

    30% {
        clip: rect(36px, 9999px, 20px, 0);
    }

    40% {
        clip: rect(3px, 9999px, 84px, 0);
    }

    50% {
        clip: rect(38px, 9999px, 8px, 0);
    }

    60% {
        clip: rect(22px, 9999px, 96px, 0);
    }

    70% {
        clip: rect(6px, 9999px, 95px, 0);
    }

    80% {
        clip: rect(82px, 9999px, 3px, 0);
    }

    90% {
        clip: rect(10px, 9999px, 99px, 0);
    }

    100% {
        clip: rect(27px, 9999px, 19px, 0);
    }
}

.text-gray-800 {
    color: #5a5c69 !important;
}

.text-gray-500 {
    color: #858796 !important;
}

.btn-custom-primary {
    color: #145369;
    background-color: transparent;
    border: 1px solid #145369 !important;
}

.btn-custom-primary:hover {
    color: #fff;
    background-color: #145369;
    border-color: #145369;
}
</style>