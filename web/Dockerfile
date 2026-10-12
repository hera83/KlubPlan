# web/Dockerfile — bygges med løsningens rod som context
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY web/web.csproj web/
RUN dotnet restore web/web.csproj
COPY . .
RUN dotnet publish web/web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/app_dbs /app/app_files && chown -R $APP_UID /app/app_dbs /app/app_files
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
ENV TZ=Europe/Copenhagen
EXPOSE 8080
ENTRYPOINT ["dotnet", "web.dll"]
