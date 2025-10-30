OAuth 2.0架構使用到Google，請先到Google API Consolegp 申請憑證<br>
https://console.developers.google.com/<br>
### 再回到PC的CMD設定 Google OAuth 憑證
```bash
# 設定 Google 的 Client ID
dotnet user-secrets set "Authentication:Google:ClientId" "你的ClientID"
```
```bash
# 設定 Google 的 Client Secret
dotnet user-secrets set "Authentication:Google:ClientSecret" "你的ClientSecret"
```
⚠ 資料庫為SQL Server Management Studio 21，版本：21.5.14 ⚠<br>
⚠ Visual Studio Community2022，版本：17.14.13 ⚠<br>
專案分工：<br>
QJH<br>
Database、登入介面、用戶專區<br>
jasper<br>
首頁、搜尋頁面、播放清單<br>
