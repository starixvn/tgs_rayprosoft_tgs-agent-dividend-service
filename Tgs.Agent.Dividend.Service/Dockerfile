#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Tgs.Agent.Dividend.Service/Tgs.Agent.Dividend.Service.csproj", "Tgs.Agent.Dividend.Service/"]
RUN dotnet restore "./Tgs.Agent.Dividend.Service/./Tgs.Agent.Dividend.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Agent.Dividend.Service"
RUN dotnet build "./Tgs.Agent.Dividend.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Agent.Dividend.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Agent.Dividend.Service.dll"]