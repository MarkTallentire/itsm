FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Itsm.Agent/Itsm.Agent.csproj", "Itsm.Agent/"]
RUN dotnet restore "Itsm.Agent/Itsm.Agent.csproj"
COPY . .
WORKDIR "/src/Itsm.Agent"
RUN dotnet build "./Itsm.Agent.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Itsm.Agent.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Itsm.Agent.dll"]
