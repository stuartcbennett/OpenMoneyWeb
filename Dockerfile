# Stage 1 — Build React client
FROM node:22-alpine AS client-build
WORKDIR /client
COPY OpenMoneyWeb.Client/package*.json ./
RUN npm ci
COPY OpenMoneyWeb.Client/ ./
RUN npm run build

# Stage 2 — Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
# Copy project files first so package restore is cached independently of source changes
COPY OpenMoneyWeb.sln ./
COPY OpenMoneyWeb.Core/OpenMoneyWeb.Core.csproj OpenMoneyWeb.Core/
COPY OpenMoneyWeb.Data/OpenMoneyWeb.Data.csproj OpenMoneyWeb.Data/
COPY OpenMoneyWeb.Api/OpenMoneyWeb.Api.csproj   OpenMoneyWeb.Api/
RUN dotnet restore
COPY OpenMoneyWeb.Core/ OpenMoneyWeb.Core/
COPY OpenMoneyWeb.Data/ OpenMoneyWeb.Data/
COPY OpenMoneyWeb.Api/  OpenMoneyWeb.Api/
RUN dotnet publish OpenMoneyWeb.Api/OpenMoneyWeb.Api.csproj \
    -c Release -o /app/publish --no-restore

# Stage 3 — Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api-build /app/publish ./
# React build output goes into wwwroot so ASP.NET Core serves it as static files
COPY --from=client-build /client/dist ./wwwroot
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "OpenMoneyWeb.Api.dll"]
