<script setup>
import {ref} from "vue";

//導覽列
import NavbarDefault from "../../examples/navbars/NavbarDefault.vue";
//footer
import FooterDefault from "../../examples/footers/FooterDefault.vue";
//背景圖
// import image from "@/assets/img/examples/color1.jpg";


</script>
<template>
    <NavbarDefault dark="" transparent=""/>
    
    <div class="bg-gray-200 min-vh-100">
        <header class="position--relative">
            
        </header>
    </div>

    <FooterDefault transparent=""/>
</template>