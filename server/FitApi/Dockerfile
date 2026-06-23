# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore using only the project file first (better layer caching)
COPY server/FitApi/FitApi.csproj server/FitApi/
RUN dotnet restore server/FitApi/FitApi.csproj

# Copy the rest of the API source and publish
COPY server/FitApi/ server/FitApi/
RUN dotnet publish server/FitApi/FitApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Published API
COPY --from=build /app/publish ./

# Static frontend served from /app/wwwroot (Program.cs falls back to wwwroot when Frontend:WebRoot is unset)
COPY *.html ./wwwroot/
COPY assets/ ./wwwroot/assets/
COPY html/ ./wwwroot/html/

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FitApi.dll"]
