1.取得憑證<br>
OAuth 2.0架構使用到Google，請先到Google API Consolegp 申請憑證<br>
https://console.developers.google.com/<br>
2.建立資料<br>
使用SSMS21執行 資料庫.sql 建立資料<br>
至MusicPlayer資料夾底下更改appsettings.json資料庫帳號設定<br>
```
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MusicPlayer;User Id=(ID);Password=(Password);Trusted_Connection=False;MultipleActiveResultSets=true;Encrypt=False;"
```
以及model底下，DBmanager.cs檔案中的資料庫設定<br>
```
private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Database=MusicPlayer;User ID=QJhdatabase;Password=123456789;Trusted_Connection=True"
```
3.設定憑證<br>
至MusicPlayer資料夾底下更改appsettings.json
```
"ClientId": "YOUR_GOOGLE_CLIENT_ID",
"ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
```
或者
到PC的CMD執行<br>
```
dotnet user-secrets set "Authentication:Google:ClientId" "你的ClientID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "你的ClientSecret"
```
=======
專案負責部分：<br>
QJH<br>
Database、登入介面、用戶專區<br>
jasper<br>
首頁、搜尋頁面、播放清單<br>
