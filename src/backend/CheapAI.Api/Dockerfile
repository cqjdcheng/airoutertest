FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CheapAI.slnx Directory.Build.props global.json ./
COPY src/backend/CheapAI.Api/CheapAI.Api.csproj src/backend/CheapAI.Api/
COPY src/backend/CheapAI.Application/CheapAI.Application.csproj src/backend/CheapAI.Application/
COPY src/backend/CheapAI.Domain/CheapAI.Domain.csproj src/backend/CheapAI.Domain/
COPY src/backend/CheapAI.Infrastructure/CheapAI.Infrastructure.csproj src/backend/CheapAI.Infrastructure/
RUN dotnet restore src/backend/CheapAI.Api/CheapAI.Api.csproj

COPY src/backend src/backend
RUN dotnet publish src/backend/CheapAI.Api/CheapAI.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CheapAI.Api.dll"]
