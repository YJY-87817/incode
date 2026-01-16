<script setup>
    import { ref, onMounted } from "vue";
    import axios from "axios";

    // 設定 API 路徑 (請確認您的後端 Port 正確)
    const API_BASE_URL = "https://localhost:7190/api";

    const statusMsg = ref("等待測試...");
    const rawData = ref("");

    // 取得 Axios 設定 (含 Cookie)
    const getAxiosConfig = () => {
        return {
            withCredentials: true,
            headers: { "Content-Type": "application/json" }
        };
    };

    const testConnection = async () => {
        statusMsg.value = "正在連線...";
        rawData.value = "";

        try {
            const response = await axios.get(
                `${API_BASE_URL}/Social/following`,
                getAxiosConfig()
            );

            statusMsg.value = "✅ 連線成功！(200 OK)";
            rawData.value = JSON.stringify(response.data, null, 2);

            if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE')) {
                statusMsg.value = "⚠️ 警告：連線成功但收到 HTML。後端可能回傳了 302 重導向 (請檢查 Program.cs)。";
            }

        } catch (error) {
            console.error(error);
            if (error.response) {
                statusMsg.value = `❌ 伺服器錯誤: ${error.response.status} ${error.response.statusText}`;
                rawData.value = JSON.stringify(error.response.data, null, 2);

                if (error.response.status === 401) {
                    statusMsg.value += "\n(原因：Cookie 未被接受或未登入)";
                }
            } else {
                statusMsg.value = `❌ 網絡錯誤 (CORS 或 伺服器未開啟): ${error.message}`;
            }
        }
    };

    onMounted(() => {
        testConnection();
    });
</script>

<template>
    <div style="padding: 50px; font-family: sans-serif;">
        <h1>Cookie 連線測試 (精簡版)</h1>
        <hr />

        <div>
            <h3>狀態：</h3>
            <pre style="background: #f0f0f0; padding: 10px; color: blue; font-weight: bold;">{{ statusMsg }}</pre>
        </div>

        <div>
            <h3>原始資料：</h3>
            <pre style="background: #333; color: #fff; padding: 10px; border-radius: 5px;">{{ rawData }}</pre>
        </div>

        <button @click="testConnection" style="padding: 10px 20px; font-size: 16px; cursor: pointer;">
            重新測試
        </button>
    </div>
</template>