<script setup>
// 引入 Vue 核心功能：ref(響應式變數), 生命周期鉤子, nextTick(DOM更新後執行), watch(監聽器)
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
// 引入 axios 用於發送 HTTP 請求 (API)
import axios from 'axios';
// 引入 Microsoft SignalR 套件，用於即時通訊
import * as signalR from "@microsoft/signalr";
// 引入 Pinia Store，用於全域管理聊天訊息狀態
import { useChatStore } from '@/stores/chatmessagestores'; 
// 引入 AI 頭像的 GIF 圖片資源
import aiAvatarGif from '@/assets/gif/1.gif';

// 初始化 Chat Store 實例
const chatStore = useChatStore(); // 建立Pinia Store實例，用於存取/修改全域聊天訊息狀態（跨元件共享）
// 定義響應式變數：控制聊天視窗是否開啟 (true: 開啟, false: 關閉)
const isOpen = ref(false); // 布林型響應式變數，預設false（關閉），綁定到模板控制聊天視窗顯示/隱藏
// 定義響應式變數：綁定使用者輸入框的文字內容
const userInput = ref(''); // 字串型響應式變數，預設空字串，雙向綁定輸入框的文字內容
// 定義響應式變數：綁定訊息顯示區域的 DOM 元素 (用於控制捲軸)
const messagesContainer = ref(null); // 物件型響應式變數，綁定訊息容器DOM元素，用於後續控制捲軸位置
// 定義響應式變數：綁定隱藏的檔案上傳輸入框 DOM 元素
const fileInput = ref(null); // 物件型響應式變數，綁定隱藏的<input type="file">元素，用於觸發檔案選擇
// 定義響應式變數：標示是否正在發送訊息中 (用於防止重複發送或顯示 Loading)
const isSending = ref(false); // 布林型響應式變數，預設false，標記發送狀態（防止重複提交/顯示加載動畫）

// 定義響應式變數：儲存 SignalR 的連線物件
const connection = ref(null); // 物件型響應式變數，儲存SignalR Hub連線實例，管理即時通訊連線
// 定義響應式變數：標示 SignalR 是否正在連線中 (避免重複連線)
const isConnecting = ref(false); // 布林型響應式變數，預設false，標記連線狀態（避免並發重複連線）

// 預覽與檔案狀態
// 定義響應式變數：儲存圖片預覽的 URL (Base64 或 Blob URL)
const previewImage = ref(null); // 字串型響應式變數，儲存圖片預覽的Blob URL，用於前端預覽選擇的圖片
// 定義響應式變數：儲存使用者目前選擇的實體檔案物件
const selectedFile = ref(null); // 物件型響應式變數，儲存使用者選擇的File物件，用於後續上傳

// 時間格式化函式
// 參數 dateInput: 傳入的時間字串或物件
const formatTime = (dateInput) => { // 定義時間格式化函式，將時間轉為台灣格式的「時:分」字串
  // 如果有傳入時間就轉換，沒傳入就用當下時間
  const date = dateInput ? new Date(dateInput) : new Date(); // 建立Date物件，優先使用傳入值，否則取當前時間
  // 回傳格式化後的時間字串 (例如 "14:30")，設定為 24 小時制
  return date.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false }); // 格式化為24小時制的「HH:MM」格式
};

// 切換視窗函式
const toggleChat = () => { // 定義切換聊天視窗的函式，綁定到浮動按鈕點擊事件
  // 將 isOpen 狀態反轉 (開變關，關變開)
  isOpen.value = !isOpen.value; // 反轉isOpen值，切換視窗顯示/隱藏
  // 如果變成開啟狀態，則執行捲動到底部
  if (isOpen.value) scrollToBottom(); // 視窗開啟時，自動捲動到最新訊息位置
};

// 捲動到底部函式
const scrollToBottom = async () => { // 非同步函式，將訊息容器捲動到最底部
  // 等待 Vue 下一次 DOM 更新循環結束 (確保新訊息已經渲染到畫面上)
  await nextTick(); // 等待Vue完成DOM更新，確保新訊息渲染後再執行捲動
  // 如果訊息容器存在
  if (messagesContainer.value) { // 檢查DOM元素是否存在，避免空值錯誤
    // 將容器的捲軸位置 (scrollTop) 設定為內容總高度 (scrollHeight)，即最底部
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight; // 設定捲軸頂部=內容總高度，實現捲動到底部
  }
};

// 監聽 Chat Store 中的訊息陣列長度
// 當訊息數量改變時，自動捲動到底部
watch(() => chatStore.messages.length, () => scrollToBottom()); // 監聽Store中訊息數量變化，有新增/刪除時自動捲動到底部

// SignalR 初始化函式
const initSignalR = async () => { // 非同步函式，建立與後端SignalR Hub的連線
  // 檢查是否已經連線或是正在連線中，如果是則直接返回，避免重複建立
  if (connection.value?.state === signalR.HubConnectionState.Connected || isConnecting.value) return; // 已連線/正在連線則返回，避免重複建立
  
  // 設定狀態為正在連線
  isConnecting.value = true; // 標記為正在連線，防止並發請求
  try {
    // 建立 SignalR 連線建構器
    const newConnection = new signalR.HubConnectionBuilder() // 建立連線建構器實例
      .withUrl("https://localhost:7190/chatHub") // 設定後端ChatHub的URL地址
      .withAutomaticReconnect() // 啟用自動重新連線（網路斷開後自動重連）
      .build(); // 建構SignalR連線實例

    // 監聽後端推播的 "ReceiveMessage" 事件
    // 參數 sender: 發送者名稱, message: 訊息內容
    newConnection.on("ReceiveMessage", (sender, message) => { // 註冊接收後端推播訊息的事件處理函式
        // 如果發送者不是 'User_Image' (避免自己傳的圖片重複顯示)
        if (sender !== 'User_Image') { // 過濾自己發送的圖片訊息，避免重複顯示
             // 這裡主要負責接收 AI 回覆或純文字推播
             // 目前邏輯交由 Store 或其他地方處理，此處為擴充點
        }
    });

    // 開始連線
    await newConnection.start(); // 非同步啟動SignalR連線
    // 將連線成功後的 Connection ID 存入 Store (發送 API 時需要帶入)
    chatStore.setConnectionId(newConnection.connectionId); // 將連線ID存入Store，供發送訊息時攜帶
    // 更新本地 connection 變數
    connection.value = newConnection; // 將成功建立的連線實例存入全域變數
  } catch (err) {
    // 錯誤處理：印出連線失敗訊息
    console.error("❌ SignalR error:", err); // 捕捉連線錯誤，列印錯誤資訊便於除錯
  } finally {
    // 無論成功失敗，解除「正在連線」的鎖定狀態
    isConnecting.value = false; // 解除連線鎖定，允許下次重新嘗試
  }
};

// ★★★ 新增：載入歷史紀錄函式 ★★★
const loadHistory = async () => { // 非同步函式，載入使用者的聊天歷史紀錄
    // 1. 嘗試從瀏覽器的 localStorage 取得上次儲存的 chatSessionId (對話識別碼)
    const savedSessionId = localStorage.getItem('chatSessionId'); // 從本地儲存取得SessionID，識別特定對話
    // 如果沒有舊紀錄 ID，就直接結束函式
    if (!savedSessionId) return; // 無儲存的SessionID則返回

    try {
        // 將取得的 ID 轉為整數並存入 Store
        chatStore.setSessionId(parseInt(savedSessionId)); // 轉換SessionID為整數，存入Store供API使用
        
        // 2. 呼叫後端 API 取得該 Session 的歷史訊息
        // 參數 savedSessionId: 用於查詢特定對話紀錄
        const res = await axios.get(`https://localhost:7190/api/ChatMessages/GetHistory/${savedSessionId}`); // 發送GET請求取得歷史訊息
        
        // 如果 API 回傳有資料且長度大於 0
        if (res.data && res.data.length > 0) { // 檢查回傳資料是否有效
            // 先清空目前的 Store 訊息 (避免重複堆疊)
            chatStore.messages = []; // 清空現有訊息，避免與歷史訊息重疊

            // 3. 遍歷回傳的資料，轉換格式並填入 Store
            res.data.forEach(msg => { // 遍歷每一筆歷史訊息
                chatStore.addMessage({ // 呼叫Store方法新增訊息
                    // 轉換角色名稱：後端 'assistant' -> 前端 'ai'，其他為 'user'
                    role: msg.role === 'assistant' ? 'ai' : 'user', // 轉換後端角色名為前端識別格式
                    // ★ 關鍵：將後端回傳的 body (可能是文字或 HTML 圖片標籤) 放入 text 欄位
                    text: msg.body, // 存入訊息內容（支援HTML）
                    // 設定訊息時間
                    time: msg.createdAt, // 存入後端記錄的建立時間
                    // 歷史紀錄中的圖片已包含在 HTML body 中，故 imageUrl 設為 null
                    imageUrl: null // 歷史圖片已內嵌在text中，imageUrl設為null
                });
            });
            // 載入完成後捲動到底部
            scrollToBottom(); // 歷史訊息載入後，捲動到最底部
        }
    } catch (error) {
        console.error("載入歷史紀錄失敗:", error); // 捕捉載入失敗錯誤，列印除錯資訊
    }
};

// 觸發檔案上傳函式：模擬點擊隱藏的 input[type="file"]
const triggerFileUpload = () => fileInput.value.click(); // 模擬點擊隱藏的檔案輸入框，開啟檔案選擇對話框

// 處理檔案選擇變更函式
// 參數 event: DOM 事件物件
const handleFileChange = (event) => { // 檔案選擇變更的處理函式，接收change事件
  // 取得使用者選擇的第一個檔案
  const file = event.target.files[0]; // 取得選擇的第一個File物件
  // 如果沒有檔案則返回
  if (!file) return; // 未選擇檔案則直接返回
  // 更新選取的檔案狀態
  selectedFile.value = file; // 將選擇的檔案存入響應式變數
  // 建立預覽用的 URL (blob:...)
  previewImage.value = URL.createObjectURL(file); // 建立Blob URL，用於前端預覽圖片
  // 清空 input 的值，確保下次選同一個檔案也能觸發 change 事件
  event.target.value = ''; // 清空檔案輸入框值，避免選相同檔案不觸發change
};

// 移除預覽圖片函式
const removePreview = () => { // 移除圖片預覽的函式，綁定到預覽圖關閉按鈕
  // 清空選取的檔案
  selectedFile.value = null; // 清空已選擇的檔案物件
  // 如果有預覽圖，清空預覽圖 URL
  if (previewImage.value) { // 檢查是否有預覽圖URL
    previewImage.value = null; // 清空預覽圖URL，隱藏預覽圖
  }
};

// 處理發送按鈕點擊函式
const handleSend = async () => { // 發送按鈕/Enter鍵的處理函式（非同步）
    // 判斷邏輯：如果目前有選取檔案 (圖片)
    if (selectedFile.value) { // 有選擇圖片檔案
        // 執行發送圖片與文字的函式
        await sendImageWithText(selectedFile.value, userInput.value); // 執行圖片+文字發送邏輯
    } 
    // 否則：如果只有文字內容 (去除空白後不為空)
    else if (userInput.value.trim()) { // 無圖片但有非空白文字
        // 執行發送純文字的函式
        await sendMessage(userInput.value); // 執行純文字發送邏輯
    }
};

// 發送圖片與文字函式
// 參數 file: 圖片檔案物件, text: 附帶的文字說明
const sendImageWithText = async (file, text) => { // 發送圖片+文字的非同步函式
  // 如果 SignalR 未連線，先執行連線
  if (!connection.value) { await initSignalR(); } // 未連線則先建立SignalR連線
  
  // 先在前端 Store 新增一筆「使用者發送中」的訊息 (立即顯示，提升體驗)
  chatStore.addMessage({ // 前端立即新增訊息，提升交互體驗
      role: 'user', // 標記為使用者訊息
      text: text, // 攜帶的文字說明
      imageUrl: previewImage.value, // 顯示圖片預覽URL
      time: new Date() // 設定發送時間為當前時間
  }); 

  // 暫存要發送的資料
  const fileToSend = file; // 暫存檔案物件，避免後續狀態變更影響
  const textToSend = text; // 暫存文字內容，避免後續狀態變更影響

  // 清除預覽狀態與輸入框
  removePreview(); // 移除圖片預覽
  userInput.value = ''; // 清空文字輸入框

  try {
    // 建立 FormData 物件，用於上傳檔案
    let formData = new FormData(); // 建立FormData物件，承載檔案和表單資料
    // 加入參數 SessionId (若無則送 0，代表新對話)
    formData.append("SessionId", chatStore.sessionId || 0); // 加入SessionID，0代表新對話
    // 加入參數 Image (檔案)
    formData.append("Image", fileToSend); // 加入圖片檔案
    // 加入參數 UserContent (使用者文字)
    formData.append("UserContent", textToSend); // 加入使用者文字內容
    // 加入參數 ConnectionId (用於 SignalR 推播)
    formData.append("ConnectionId", chatStore.connectionId); // 加入SignalR連線ID，供後端推播回覆

    // 設定發送狀態為 true (顯示 Loading)
    isSending.value = true; // 標記為發送中，顯示加載動畫
    // 發送 POST 請求到後端 UploadImage API
    const res = await axios.post('https://localhost:7190/api/ChatMessages/UploadImage', formData, {
        headers: { 'Content-Type': 'multipart/form-data' } // 設定請求頭為多媒體表單格式
    });

    // 如果後端回傳成功
    if (res.data.success) { // 檢查後端回傳的成功狀態
       // 檢查 SessionId 是否需要更新
       if (chatStore.sessionId === 0 || !chatStore.sessionId) { // 若當前為新對話
            // (註解備註) 如果後端有回傳新 SessionId 可在此更新
       }
       
       // 將後端 AI 的回覆加入 Store 顯示
       chatStore.addMessage({ role: 'ai', text: res.data.reply, time: new Date() }); // 將AI回覆加入Store顯示
    }
  } catch (error) {
    // 錯誤處理
    console.error(error); // 列印發送錯誤資訊
    chatStore.addMessage({ role: 'ai', text: "發送失敗。", time: new Date() }); // 前端顯示失敗提示
  } finally {
    // 解除發送狀態
    isSending.value = false; // 無論成功失敗，解除發送中狀態
  }
};

// 發送純文字函式
// 參數 text: 使用者輸入的文字
const sendMessage = async (text) => { // 發送純文字的非同步函式
  // 如果正在發送中，防止重複提交
  if (isSending.value) return; // 正在發送中則返回，防止重複提交

  // 前端先顯示使用者的訊息
  chatStore.addMessage({ role: 'user', text: text, time: new Date() }); // 前端立即新增使用者訊息
  // 清空輸入框
  userInput.value = ''; // 清空文字輸入框
  // 設定發送狀態
  isSending.value = true; // 標記為發送中

  // 確保 SignalR 連線
  if (!connection.value) { await initSignalR(); } // 未連線則先建立

  try {
    // 準備 API 請求的 Payload
    const payload = { // 建立API請求的資料物件
        SessionId: chatStore.sessionId || 0, // 當前SessionID，0代表新對話
        UserContent: text, // 使用者輸入的文字內容
        ConnectionId: chatStore.connectionId // SignalR連線ID，供後端推播回覆
    };
    // 發送 POST 請求到 SendMessage API
    const res = await axios.post('https://localhost:7190/api/ChatMessages/SendMessage', payload); // 發送POST請求到文字訊息API

    // 如果成功
    if (res.data.success) { // 檢查後端回傳的成功狀態
        // ★ 關鍵：如果是新對話拿到新的 SessionId，存入 localStorage
        if (res.data.sessionId) { // 後端回傳新SessionID（新對話）
            chatStore.setSessionId(res.data.sessionId); // 更新Store中的SessionID
            localStorage.setItem('chatSessionId', res.data.sessionId); // 存入本地儲存，用於下次載入歷史
        }
        // 將 AI 回覆加入 Store (res.data.data.body 對應後端 DTO 結構)
        chatStore.addMessage({ role: 'ai', text: res.data.data.body, time: new Date() }); // 將AI回覆加入Store顯示
    }
  } catch (error) {
    console.error(error); // 列印發送錯誤資訊
    chatStore.addMessage({ role: 'ai', text: "伺服器連線失敗。", time: new Date() }); // 前端顯示連線失敗提示
  } finally {
    isSending.value = false; // 解除發送中狀態
  }
};

// Vue 生命周期：當元件掛載完成時執行
onMounted(async () => { // 元件掛載到DOM後執行
    await loadHistory(); // 先載入歷史聊天紀錄
    await initSignalR(); // 再建立SignalR連線
});

// Vue 生命周期：當元件卸載 (離開頁面) 時執行
onUnmounted(() => { if (connection.value) connection.value.stop(); }); // 元件卸載時斷開SignalR連線，釋放資源
</script>

<template>
  <div class="chat-widget-wrapper"> <!-- 聊天元件外層容器，負責整體定位 -->
    <transition name="slide-up"> <!-- 過渡動畫元件，為視窗顯示/隱藏添加slide-up動畫 -->
      <div v-show="isOpen" class="custom-chat-window"> <!-- 聊天視窗主容器，v-show根據isOpen控制顯示/隱藏 -->
        
        <div class="chat-header"> <!-- 聊天視窗標題列 -->
            <div class="header-title">AI小幫手</div> <!-- 標題文字 -->
        </div>

        <div class="messages-area" ref="messagesContainer"> <!-- 訊息顯示區域，ref綁定到messagesContainer變數 -->
            <div class="message-row ai" v-if="chatStore.messages.length === 0"> <!-- 初始歡迎訊息，僅在無訊息時顯示 -->
                <div class="avatar">🤖</div> <!-- AI頭像 --> <div class="bubble-content"> <!-- 氣泡內容容器 -->
                    <div class="bubble"> <!-- 訊息氣泡 -->
                        <span>你好！我是AI小幫手，有什麼可以幫你的嗎？</span> <!-- 歡迎文字 -->
                    </div>
                    <div class="msg-time">{{ formatTime(new Date()) }}</div> <!-- 格式化顯示當前時間 -->
                </div>
            </div>

            <div 
              v-for="(msg, index) in chatStore.messages" 
              :key="index" 
              class="message-row" 
              :class="msg.role === 'user' ? 'user' : 'ai'" 
            >
                <div v-if="msg.role === 'ai'" class="avatar">🤖</div> <!-- AI訊息顯示頭像，使用者訊息不顯示 -->
                
                <div class="bubble-content"> <!-- 氣泡內容容器 -->
                    <div class="bubble"> <!-- 訊息氣泡 -->
                        <img v-if="msg.imageUrl" :src="msg.imageUrl" class="msg-image" @load="scrollToBottom" /> <!-- 顯示圖片（有imageUrl時），加載完成後捲動到底部 -->

                        <span v-if="msg.text" class="msg-text" v-html="msg.text"></span> <!-- 顯示文字（有text時），v-html支援HTML格式 -->
                    </div>
                    <div class="msg-time">{{ formatTime(msg.time) }}</div> <!-- 格式化顯示訊息時間 -->
                </div>
            </div>
            
            <div v-if="isSending" class="message-row ai"> <!-- 發送中loading動畫，isSending為true時顯示 -->
                <div class="avatar">🤖</div> <!-- AI頭像 -->
                <div class="bubble typing">● ● ●</div> <!-- 輸入中動畫文字 -->
            </div>
        </div>

        <div class="input-area"> <!-- 底部輸入區域容器 -->
            <div v-if="previewImage" class="preview-popup"> <!-- 圖片預覽彈窗，有previewImage時顯示 -->
                <img :src="previewImage" /> <!-- 預覽選擇的圖片 -->
                <button class="close-btn" @click="removePreview">×</button> <!-- 關閉預覽按鈕，點擊執行removePreview -->
            </div>

            <input type="file" ref="fileInput" @change="handleFileChange" accept=".jpg,.jpeg,.png,.gif" style="display: none" /> <!-- 隱藏的檔案輸入框，ref綁定fileInput，change事件執行handleFileChange，僅支援圖片格式 -->

            <div class="input-wrapper"> <!-- 文字輸入框包裹層 -->
                <input 
                    v-model="userInput" 
                    @keyup.enter="handleSend" 
                    placeholder="請輸入訊息..." 
                    type="text" 
                />
                
                <button class="icon-btn image-btn" @click="triggerFileUpload" title="上傳圖片"> <!-- 圖片上傳按鈕，點擊執行triggerFileUpload -->
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"> <!-- 圖片icon SVG -->
                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect> <!-- SVG矩形 -->
                        <circle cx="8.5" cy="8.5" r="1.5"></circle> <!-- SVG圓形 -->
                        <polyline points="21 15 16 10 5 21"></polyline> <!-- SVG折線 -->
                    </svg>
                </button>
            </div>

            <button class="send-btn" @click="handleSend" :disabled="!userInput.trim() && !selectedFile"> <!-- 發送按鈕，點擊執行handleSend，無文字/無圖片時禁用 -->
                <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="currentColor"> <!-- 發送icon SVG -->
                    <path d="m21.426 11.095-17-8A.999.999 0 0 0 3.03 4.242L4.969 12 3.03 19.758a.998.998 0 0 0 1.396 1.147l17-8a1 1 0 0 0 0-1.81zM5.481 18.197l.839-3.357L12 12 6.32 9.16l-.839-3.357L18.651 12l-13.17 6.197z"/> <!-- SVG路徑 -->
                </svg>
            </button>
        </div>

      </div>
    </transition>

    <button class="floating-btn" @click="toggleChat" :class="{ 'is-open': isOpen }"> <!-- 浮動按鈕，點擊執行toggleChat，isOpen時添加is-open樣式 -->
      <div class="icon-container"> <!-- 按鈕圖示容器 -->
        <img v-if="!isOpen" :src="aiAvatarGif" class="icon-chat-gif" alt="AI Helper" style="width: 40px; height: 40px;"/> <!-- 未開啟時顯示AI頭像GIF -->
        <svg v-else class="icon-close" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"> <!-- 開啟時顯示關閉icon -->
          <line x1="18" y1="6" x2="6" y2="18"></line> <!-- 關閉線1 -->
          <line x1="6" y1="6" x2="18" y2="18"></line> <!-- 關閉線2 -->
        </svg>
      </div>
    </button>
  </div>
</template>

<style scoped>
/* =========================================
   保留你原本所有的 CSS 設定，完全未改動
   ========================================= */

/* 自訂聊天視窗主體的樣式 */
.custom-chat-window {
    width: 420px; height: 520px; /* 設定視窗大小：寬420px，高520px */
    background: linear-gradient(90deg, rgb(239, 242, 247) 0%, 7.60286%, rgb(237, 240, 249) 15.2057%, 20.7513%, rgb(235, 239, 248) 26.297%, 27.6386%, rgb(235, 239, 248) 28.9803%, 38.2826%, rgb(231, 237, 249) 47.585%, 48.1216%, rgb(230, 236, 250) 48.6583%, 53.1306%, rgb(228, 236, 249) 57.6029%, 61.5385%, rgb(227, 234, 250) 65.4741%, 68.7835%, rgb(222, 234, 250) 72.093%, 75.7603%, rgb(219, 230, 248) 79.4275%, 82.8265%, rgb(216, 229, 248) 86.2254%, 87.8354%, rgb(213, 228, 249) 89.4454%, 91.8605%, rgb(210, 226, 249) 94.2755%, 95.4383%, rgb(209, 225, 248) 96.6011%, 98.3005%, rgb(208, 224, 247) 100%); /* 設定漸層背景色 */
    border-radius: 20px; border: none; /* 設定圓角20px，無邊框 */
    box-shadow: 0 12px 40px rgba(0, 0, 0, 0.15); /* 設定陰影效果：偏移0 12px，模糊40px，透明度15% */
    display: flex; flex-direction: column; overflow: hidden; /* 設定Flex佈局，垂直排列，隱藏溢出內容 */
    margin-bottom: 20px; font-family: 'Segoe UI', sans-serif; /* 設定底部外邊距20px，字體為Segoe UI */
    border: 1px solid transparent; /* 設定透明邊框（預留擴充） */
}

/* 聊天視窗標題列樣式 */
.chat-header {
    background: linear-gradient(135deg, #4398a5, #3a5f54); /* 設定標題列漸層背景色 */
    color: white; padding: 15px 17px; flex-shrink: 0; /* 文字白色，內邊距15px 17px，不縮放 */
    border: none; border-bottom: none; box-shadow: none; /* 無邊框、無底部邊框、無陰影 */
}
.header-title { font-size: 18px; font-weight: bold; } /* 標題文字大小18px，粗體 */

/* 訊息列表區域樣式 */
.messages-area {
    flex: 1; padding: 20px; overflow-y: auto; /* 自動填充剩餘空間，內邊距20px，垂直自動捲動 */
    display: flex; flex-direction: column; gap: 15px; background: transparent; /* Flex垂直排列，訊息間距15px，透明背景 */
}
/* 自訂捲軸樣式 (Webkit 瀏覽器) */
.messages-area::-webkit-scrollbar { width: 6px; } /* 捲軸寬度6px */
.messages-area::-webkit-scrollbar-track { background: transparent; } /* 捲軸軌道透明 */
.messages-area::-webkit-scrollbar-thumb { background-color: rgba(0, 0, 0, 0.1); border-radius: 10px; } /* 捲軸滑塊淺灰色，圓角10px */
.messages-area::-webkit-scrollbar-thumb:hover { background-color: rgba(0, 0, 0, 0.2); } /* 滑塊hover時加深顏色 */

/* 單條訊息列樣式 */
.message-row { display: flex; align-items: flex-end; gap: 8px; max-width: 100%; } /* Flex佈局，底部對齊，元素間距8px，最大寬度100% */

/* 訊息氣泡基本樣式 */
.bubble {
    padding: 10px 14px; /* 內邊距10px 14px */
    border-radius: 18px; /* 圓角18px */
    font-size: 14px; /* 字體大小14px */
    line-height: 1.5; /* 行高1.5 */
    word-break: break-word; /* 長單字自動換行 */
    display: flex;     /* Flex佈局 */
    flex-direction: column; /* 垂直排列 */
    gap: 8px;           /* 子元素間距8px */
}

/* AI 訊息的特定樣式 (靠左) */
.message-row.ai { align-self: flex-start; } /* AI訊息靠左對齊 */
/* AI 頭像樣式 */
.message-row.ai .avatar {  
    width: 32px; height: 32px; background: #90afe4; border-radius: 50%; /* 頭像大小32px，背景色#90afe4，圓形 */
    display: flex; align-items: center; justify-content: center; font-size: 18px; flex-shrink: 0; /* Flex居中，字體18px，不縮放 */
}
.message-row.ai .bubble {
    background-color: white; color: #333; border-bottom-left-radius: 4px; /* 白色背景，深灰色文字，左下角圓角4px（氣泡效果） */
}

/* 使用者訊息的特定樣式 (靠右) */
.message-row.user { align-self: flex-end; flex-direction: row-reverse; /* 使用者訊息靠右，元素反轉排列 */ }
.message-row.user .bubble {
    background: linear-gradient(135deg, #3578F6, #205AE6); /* 藍色漸層背景 */
    color: white; border-bottom-right-radius: 4px; /* 白色文字，右下角圓角4px（氣泡效果） */
}

/* 正在輸入中的動畫文字樣式 */
.typing { color: #888; letter-spacing: 2px; font-size: 12px; } /* 淺灰色文字，字間距2px，大小12px */

/* 底部輸入區域容器樣式 */
.input-area {
    padding: 15px; /* 內邊距15px */
    background: linear-gradient(90deg, rgb(239, 242, 247) 0%, 7.60286%, rgb(237, 240, 249) 15.2057%, 20.7513%, rgb(235, 239, 248) 26.297%, 27.6386%, rgb(235, 239, 248) 28.9803%, 38.2826%, rgb(231, 237, 249) 47.585%, 48.1216%, rgb(230, 236, 250) 48.6583%, 53.1306%, rgb(228, 236, 249) 57.6029%, 61.5385%, rgb(227, 234, 250) 65.4741%, 68.7835%, rgb(222, 234, 250) 72.093%, 75.7603%, rgb(219, 230, 248) 79.4275%, 82.8265%, rgb(216, 229, 248) 86.2254%, 87.8354%, rgb(213, 228, 249) 89.4454%, 91.8605%, rgb(210, 226, 249) 94.2755%, 95.4383%, rgb(209, 225, 248) 96.6011%, 98.3005%, rgb(208, 224, 247) 100%); /* 漸層背景色 */
    background-color: transparent; /* 透明背景（覆蓋預設） */
    border-top: none; /* 無頂部邊框 */
    display: flex; align-items: flex-end; gap: 10px; /* Flex佈局，底部對齊，元素間距10px */
    position: relative; /* 相對定位（供預覽彈窗定位） */
}
/* 輸入框包裹層 */
.input-wrapper { position: relative; flex: 1; display: flex; align-items: center; } /* 相對定位，自動填充空間，Flex居中 */
.input-wrapper input {
    width: 100%; padding: 12px 15px; padding-right: 45px; /* 寬度100%，內邊距12px 15px，右內邊距45px（預留圖片按鈕） */
    border-radius: 24px; border: 1px solid #e0e0e0; /* 圓角24px，淺灰色邊框 */
    font-size: 15px; outline: none; background: #f9f9f9; /* 字體15px，無外框，淺灰色背景 */
    transition: all 0.3s; position: relative; z-index: 1; /* 所有屬性0.3s過渡，相對定位，層級1 */
}
.input-wrapper input:focus {
    border-color: #3578F6; background: #fff; box-shadow: 0 0 0 2px rgba(53, 120, 246, 0.1); /* 焦點時藍色邊框，白色背景，淺藍色外影 */
}

/* 圖片上傳按鈕樣式 */
.image-btn {
    position: absolute; right: 8px; background: transparent; border: none; /* 絕對定位靠右8px，透明背景，無邊框 */
    color: #888; cursor: pointer; padding: 5px; border-radius: 50%; /* 淺灰色，滑鼠指針，內邊距5px，圓形 */
    display: flex; transition: color 0.3s; z-index: 10; /* Flex佈局，顏色0.3s過渡，層級10 */
}
.image-btn:hover { color: #3578F6; background: #eee; } /* hover時藍色文字，淺灰色背景 */
.image-btn svg { width: 20px; height: 20px; } /* SVG圖示大小20px */

/* 發送按鈕樣式 */
.send-btn {
    width: 46px; height: 50px; border-radius: 50%; /* 按鈕大小46x50px，圓形 */
    background: linear-gradient(135deg, #3578F6, #205AE6); /* 藍色漸層背景 */
    border: none; color: white; /* 無邊框，白色文字 */
    display: flex; align-items: center; justify-content: center; /* Flex居中 */
    cursor: pointer; flex-shrink: 0; /* 滑鼠指針，不縮放 */
    transition: transform 0.2s, box-shadow 0.2s; /* 變形/陰影0.2s過渡 */
    box-shadow: 0 4px 10px rgba(53, 120, 246, 0.3); z-index: 10; /* 藍色陰影，層級10 */
}
.send-btn:hover { transform: scale(1.05); } /* hover時放大1.05倍 */
.send-btn:active { transform: scale(0.95); } /* 點擊時縮小0.95倍 */
.send-btn:disabled { background: #ccc; cursor: not-allowed; box-shadow: none; } /* 禁用時灰色背景，禁止指針，無陰影 */
.send-btn svg { width: 20px; height: 20px; fill: white; margin-left: 2px; } /* SVG大小20px，白色填充，左邊距2px */

/* 懸浮按鈕外層定位 */
.chat-widget-wrapper {
    position: fixed; bottom: 30px; right: 30px; z-index: 9999; /* 固定定位，底部30px，右邊30px，最高層級 */
    display: flex; flex-direction: column; align-items: flex-end; /* Flex垂直排列，靠右對齊 */
}
/* 圓形懸浮按鈕本體 */
.floating-btn {
    width: 60px; height: 60px; border-radius: 50%; /* 按鈕大小60px，圓形 */
    background: transparent; border: none; cursor: pointer; /* 透明背景，無邊框，滑鼠指針 */
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.12); /* 陰影效果 */
    position: relative; overflow: hidden; transition: all 0.3s; /* 相對定位，隱藏溢出，所有屬性0.3s過渡 */
}
.floating-btn:hover { transform: scale(1.08); } /* hover時放大1.08倍 */
.floating-btn.is-open { transform: rotate(90deg); background: #333; } /* 開啟時旋轉90度，黑色背景 */
.icon-chat-gif { width: 100%; height: 100%; object-fit: cover; transform: scale(1.5); } /* GIF填滿按鈕，放大1.5倍 */
.icon-close { width: 24px; height: 24px; color: white; } /* 關閉icon大小24px，白色 */

/* 氣泡內容文字與圖片排版 */
.bubble-content { display: flex; flex-direction: column; max-width: 80%; } /* Flex垂直排列，最大寬度80% */
.msg-time { font-size: 11px; color: #999; margin-top: 4px; margin-left: 6px; } /* 時間文字11px，淺灰色，上邊距4px，左邊距6px */
.message-row.user .msg-time { text-align: right; margin-right: 6px; margin-left: 0; } /* 使用者訊息時間靠右，右邊距6px */
.msg-image { max-width: 100%; border-radius: 8px; display: block; } /* 圖片最大寬度100%，圓角8px，區塊顯示 */
.msg-image + .msg-text { margin-top: 4px; display: block; } /* 圖片後的文字上邊距4px，區塊顯示 */

/* 圖片預覽彈出窗樣式 */
.preview-popup {
    position: absolute; bottom: 75px; left: 20px; /* 絕對定位，底部75px，左邊20px */
    background: white; padding: 6px; border-radius: 12px; /* 白色背景，內邊距6px，圓角12px */
    box-shadow: 0 5px 15px rgba(0,0,0,0.15); z-index: 100; /* 陰影效果，層級100 */
    display: flex; align-items: start; animation: popIn 0.2s ease-out; /* Flex佈局，頂部對齊，popIn動畫0.2s */
}
.preview-popup img { height: 70px; width: auto; border-radius: 8px; display: block; } /* 預覽圖高度70px，自適應寬度，圓角8px */
.close-btn {
    position: absolute; top: -8px; right: -8px; /* 絕對定位，頂部-8px，右邊-8px（超出容器） */
    width: 22px; height: 22px; background: #ff5e57; /* 按鈕大小22px，紅色背景 */
    color: white; border: none; border-radius: 50%; /* 白色文字，無邊框，圓形 */
    font-size: 16px; line-height: 1; cursor: pointer; /* 字體16px，行高1，滑鼠指針 */
    box-shadow: 0 2px 5px rgba(0,0,0,0.2); /* 陰影效果 */
    display: flex; justify-content: center; align-items: center; /* Flex居中 */
}
@keyframes popIn { /* 預覽彈窗彈入動畫 */
    from { opacity: 0; transform: translateY(10px); } /* 初始狀態：透明，向下偏移10px */
    to { opacity: 1; transform: translateY(0); } /* 結束狀態：不透明，原位 */
}
</style>