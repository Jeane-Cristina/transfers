FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/Transfers.Domain/Transfers.Domain.csproj src/Transfers.Domain/
COPY src/Transfers.Application/Transfers.Application.csproj src/Transfers.Application/
COPY src/Transfers.Infrastructure/Transfers.Infrastructure.csproj src/Transfers.Infrastructure/
COPY src/Transfers.Worker/Transfers.Worker.csproj src/Transfers.Worker/
RUN dotnet restore src/Transfers.Worker/Transfers.Worker.csproj

COPY src/ src/
RUN dotnet publish src/Transfers.Worker/Transfers.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Transfers.Worker.dll"]
