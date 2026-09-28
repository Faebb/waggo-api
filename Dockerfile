# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached layer) — copy only project/props files
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Waggo.Domain/Waggo.Domain.csproj src/Waggo.Domain/
COPY src/Waggo.Application/Waggo.Application.csproj src/Waggo.Application/
COPY src/Waggo.Infrastructure/Waggo.Infrastructure.csproj src/Waggo.Infrastructure/
COPY src/Waggo.Api/Waggo.Api.csproj src/Waggo.Api/
RUN dotnet restore src/Waggo.Api/Waggo.Api.csproj

COPY src/ src/
RUN dotnet publish src/Waggo.Api/Waggo.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Non-root user shipped with the official .NET images
USER $APP_UID
ENTRYPOINT ["dotnet", "Waggo.Api.dll"]
