FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Agent.Dividend.Service/Tgs.Agent.Dividend.Service.csproj", "Tgs.Agent.Dividend.Service/"]
COPY ["Tgs.Agent.Dividend.Core/Tgs.Agent.Dividend.Core.csproj", "Tgs.Agent.Dividend.Core/"]
COPY ["Tgs.Agent.Dividend.Domain/Tgs.Agent.Dividend.Domain.csproj", "Tgs.Agent.Dividend.Domain/"]
COPY ["Tgs.Agent.Dividend.Repository/Tgs.Agent.Dividend.Repository.csproj", "Tgs.Agent.Dividend.Repository/"]
RUN dotnet restore "./Tgs.Agent.Dividend.Service/./Tgs.Agent.Dividend.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Agent.Dividend.Service"
RUN dotnet build "./Tgs.Agent.Dividend.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Agent.Dividend.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Agent.Dividend.Service.dll"]