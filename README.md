OAuth 2.0架構使用到Google
請先到Google API Consolegp 申請憑證
https://console.developers.google.com/
再回到PC的CMD執行
dotnet user-secrets set "Authentication:Google:ClientId" "你的ClientID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "你的ClientSecret"
