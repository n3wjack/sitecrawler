param ($version="1.0")

Write-Host "`n*** Building packages. ***`n" -ForegroundColor Cyan

cd Crawler
dotnet publish -c Release 
dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release --self-contained true -p:PublishTrimmed=true

cd ..
Compress-Archive Crawler\bin\Release\net6.0\publish\*.* Crawler-$version.zip
Compress-Archive Crawler\bin\Release\net6.0\win-x64\publish\Crawler.exe Crawler-$version-windowsx64-self-contained.zip

ls *.zip
