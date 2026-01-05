<script setup>
    import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
    import DefaultFooter from "@/examples/footers/FooterDefault.vue";

    import { reactive } from 'vue';

    const user = reactive({
        Account: '123@gmaol.com',
        Username: '小明',
        userId: '2781305',
        avatar: 'https://api.dicebear.com/7.x/lorelei/svg?seed=Alex',
        regDate: '2014-11-19',
        follower: 0,
        followee: 0,
        discussion: 47,
        level: 15,
        currentExp: 2500,
        //nextLevelExp: 3000,
        points: 1850,
    });

</script>

<template>
    <div class="max-w-6xl mx-auto p-6 bg-slate-50 min-h-screen">
        <div
            class="bg-white rounded-2xl p-6 shadow-sm border border-slate-100 flex flex-col md:flex-row items-center gap-6 mb-6">
            <h1 class="text-3xl font-bold text-center">{{ user.Username }}</h1>
            <img :src="user.avatar" class="w-10 h-10 object-cover grayscale brightness-105" alt="圖片" />
            <div class="flex-1 text-center md:text-left">
                <div class="flex flex-wrap justify-center md:justify-start gap-x-4 gap-y-1 text-sm text-slate-400">

                    <span>帳號：{{ user.Account }}</span>
                    <span>會員編號：{{ user.userId }}</span>
                </div>
                <button
                    class="bg-emerald-600 hover:bg-emerald-700 text-white px-5 py-1.5 rounded-md text-sm font-medium transition-all">
                    查看我的檔案
                </button>
            </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
                <div class="row">
                    <div class="column-6">
                        <h2 class="text-xl font-bold text-slate-800 mb-4">社群相關</h2>
                        <div class="space-y-4">
                            <div
                                class="flex justify-between items-center p-3 hover:bg-slate-50 rounded-lg cursor-pointer transition-colors group">
                                <span class="text-emerald-600 font-medium">追蹤:{{ user.followee }}</span>
                                <span class="text-slate-300 group-hover:text-emerald-600">→</span>
                            </div>
                            <div
                                class="flex justify-between items-center p-3 hover:bg-slate-50 rounded-lg cursor-pointer transition-colors group">
                                <span class="text-emerald-600 font-medium">粉絲:{{ user.follower }}</span>
                                <span class="text-slate-300 group-hover:text-emerald-600">→</span>
                            </div>
                        </div>
                    </div>
                    <div class="column-6">
                        <h2 class="text-xl font-bold text-slate-800 mb-4">會員相關</h2>
                        <div class="flex flex-col items-center text-center">
                            <span class="text-xs text-slate-400 font-medium mb-1">會員等級</span>
                            <p class="text-2xl font-bold text-slate-700">LV. {{ user.level }}</p>
                        </div>

                        <div class="flex flex-col items-center text-center">
                            <span class="text-xs text-slate-400 font-medium mb-1">經驗值</span>
                            <p class="text-2xl font-bold text-slate-700">{{ user.currentExp }} </p>
                            <!-- 百分比進度： -->
                        </div>
                        <div class="flex flex-col items-center text-center">
                            <span class="text-xs text-slate-400 font-medium mb-1">點數噗滿</span>
                            <p class="text-2xl font-bold text-slate-700">{{ user.points }} <span
                                    class="text-sm font-normal text-slate-400">點</span></p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
            <h2 class="text-xl font-bold text-slate-800 mb-4">我的討論</h2>
            <div class="flex flex-col items-center text-center">
                <span class="text-xs text-slate-400 font-medium mb-1">討論參與度</span>
                <p class="text-2xl font-bold text-slate-700">{{ user.discussion }} <span
                        class="text-sm font-normal text-slate-400">篇</span></p>
            </div>
            <div class="flex flex-col items-center text-center">
                <span class="text-xs text-slate-400 font-medium mb-1">文章被收藏</span>
                <p class="text-2xl font-bold text-slate-700">{{ user.favoritesCount }} <span
                        class="text-sm font-normal text-slate-400">次</span></p>
            </div>

            <div class="space-y-4">
                <div
                    class="flex justify-between items-center p-3 hover:bg-slate-50 rounded-lg cursor-pointer transition-colors group">
                    <span class="text-emerald-600 font-medium">我的文章(2)</span>
                    <span class="text-slate-300 group-hover:text-emerald-600">→</span>
                </div>
                <div
                    class="flex justify-between items-center p-3 hover:bg-slate-50 rounded-lg cursor-pointer transition-colors group">
                    <span class="text-emerald-600 font-medium">我的留言(58)</span>
                    <span class="text-slate-300 group-hover:text-emerald-600">→</span>
                </div>
            </div>
        </div>
    </div>

    <div class="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
        <h2 class="text-xl font-bold text-slate-800 mb-4">任務</h2>
        <div class="space-y-4">
            <div
                class="flex justify-between items-center p-3 hover:bg-slate-50 rounded-lg cursor-pointer transition-colors group">
                <span class="text-slate-600 font-medium">接受的任務(20)</span>
                <span class="text-slate-300 group-hover:text-emerald-600">→</span>
            </div>
            <div
                class="flex justify-between items-center p-3 hover:bg-slate-50 rounded-lg cursor-pointer transition-colors group">
                <span class="text-slate-600 font-medium">發布的任務(0)</span>
                <span class="text-slate-300 group-hover:text-emerald-600">→</span>
            </div>
        </div>
    </div>
</template>