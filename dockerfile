FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ONLY the project file first for efficient caching
COPY ["video-uploader-api/video-uploader-api/video-uploader-api.csproj", "video-uploader-api/"]
RUN dotnet restore "video-uploader-api/video-uploader-api.csproj"

# Copy the remaining source code from the correct path
COPY video-uploader-api/video-uploader-api/ ./video-uploader-api/
RUN dotnet build "video-uploader-api/video-uploader-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "video-uploader-api/video-uploader-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "video-uploader-api.dll"]