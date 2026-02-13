# CONTRIBUTING.md

歡迎貢獻本專案。請在提 PR 前遵守以下指南，確保程式碼品質與團隊一致性。

## 一般流程
1. Fork 並在本地建立分支，分支命名格式：`feat/<短描述>`、`fix/<短描述>`、`chore/<描述>`。
2. 確認本地能通過測試與 `dotnet build`。
3. 發 PR 到 `dev_qiu`（或指定目標分支），附上變更說明與測試步驟。

## 程式碼風格
- 專案包含 `.editorconfig`，請使用該設定（4 個空白縮排、CRLF、UTF-8）。
- 私有 readonly 欄位請使用下劃線前綴，例如 `_context`。
- 類別與方法使用 PascalCase，局部變數使用 camelCase。
- 使用 async/await 與非同步 EF Core 查詢（`.ToListAsync()`、`.FirstOrDefaultAsync()`）。

## 單元測試
- 任何行為改動應新增或更新對應的單元測試（xUnit / MSTest 或團隊指定框架）。
- CI 檢查需通過所有測試才可合併。

## 安全與機密資訊
- 切勿將 API keys 或秘密資訊提交到 Git。請使用 __User Secrets__（開發）或安全的 CI/CD 密鑰管理（部署）。
- 本專案已使用 User Secrets（請參考 `secrets.json` 範例），如需新增第三方憑證請通知維運。

## 資料庫與遷移
- 若新增或修改 EF Core model，請新增 migration 並在 PR 說明中提供遷移指示。
- 生產環境遷移需由部署人員在 CI/CD pipeline 中執行。

## 日誌與錯誤處理
- 使用統一日誌（建議注入 ILogger<T>）記錄重要錯誤與交易失敗，勿輸出敏感資訊。

## 參考工具
- 格式化：`dotnet format`。
- 靜態分析：建議整合 Roslyn 分析器或 SonarQube。

感謝你的貢獻！
