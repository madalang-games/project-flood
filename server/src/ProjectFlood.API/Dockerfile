FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY server/src/ ./src/
COPY server/generated/ ./generated/
COPY shared/contracts/ ../shared/contracts/
RUN dotnet publish src/ProjectFlood.API/ProjectFlood.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ProjectFlood.API.dll"]
