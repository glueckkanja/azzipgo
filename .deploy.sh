cd "AzZipGo/bin/Release"
FILE=$(ls -t azzipgo.*.nupkg | head -1)
dotnet nuget push "$FILE" -s "https://api.nuget.org/v3/index.json" -k "$NUGET_API_KEY"
