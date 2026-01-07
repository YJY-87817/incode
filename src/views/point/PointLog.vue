<script setup>
import {ref} from "vue";

//導覽列
import NavbarDefault from "../../examples/navbars/NavbarDefault.vue";
//footer
import FooterDefault from "../../examples/footers/FooterDefault.vue";
//背景圖
import DefaultBackgroundCard from "@/examples/cards/backgroundCards/DefaultBackgroundCard.vue";
import image from "@/assets/img/examples/color1.jpg";


</script>
<template>
    <NavbarDefault dark="" transparent=""/>
    <DefaultBackgroundCard :image="image" label="Website visitors" title="The Best Productivity Apps on Market" description="As we live, our hearts turn colder. Cause pain is what we go
        through as we become older. We get insulted by others, lose
        trust for those others."
 />
    <FooterDefault transparent=""/>
    
</template>