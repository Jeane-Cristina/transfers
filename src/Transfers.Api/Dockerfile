FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/Transfers.Domain/Transfers.Domain.csproj src/Transfers.Domain/
COPY src/Transfers.Application/Transfers.Application.csproj src/Transfers.Application/
COPY src/Transfers.Infrastructure/Transfers.Infrastructure.csproj src/Transfers.Infrastructure/
COPY src/Transfers.Api/Transfers.Api.csproj src/Transfers.Api/
RUN dotnet restore src/Transfers.Api/Transfers.Api.csproj

COPY src/ src/
RUN dotnet publish src/Transfers.Api/Transfers.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Transfers.Api.dll"]
