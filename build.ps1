Write-Host "`n*** Building self-contained winx64 exectable. ***`n" -ForegroundColor Cyan

cd Crawler
dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release --self-contained true -p:PublishTrimmed=true

cd bin\Release\net6.0\win-x64\publish

ls *.exe
