# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached layer) — copy only project/props files
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY Waggo.Domain/Waggo.Domain.csproj Waggo.Domain/
COPY Waggo.Application/Waggo.Application.csproj Waggo.Application/
COPY Waggo.Infrastructure/Waggo.Infrastructure.csproj Waggo.Infrastructure/
COPY Waggo.Api/Waggo.Api.csproj Waggo.Api/
RUN dotnet restore Waggo.Api/Waggo.Api.csproj

# Only the production layers (test projects are not copied into the image)
COPY Waggo.Domain/ Waggo.Domain/
COPY Waggo.Application/ Waggo.Application/
COPY Waggo.Infrastructure/ Waggo.Infrastructure/
COPY Waggo.Api/ Waggo.Api/
RUN dotnet publish Waggo.Api/Waggo.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Non-root user shipped with the official .NET images
USER $APP_UID
ENTRYPOINT ["dotnet", "Waggo.Api.dll"]
