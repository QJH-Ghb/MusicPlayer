OAuth 2.0架構使用到Google，請先到Google API Consolegp 申請憑證<br>
https://console.developers.google.com/<br>
再回到PC的CMD執行<br>
dotnet user-secrets set "Authentication:Google:ClientId" "你的ClientID"<br>
dotnet user-secrets set "Authentication:Google:ClientSecret" "你的ClientSecret"<br>
專案負責部分：<br>
QJH<br>
Database、登入介面、用戶專區<br>
jasper<br>
首頁、搜尋頁面、播放清單<br>
