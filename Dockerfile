FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /app

COPY . .

RUN dotnet restore MongoBackupService.csproj

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine AS runtime
WORKDIR /app
COPY --from=build /app/out ./

RUN apk add --update mongodb-tools

ENTRYPOINT ["dotnet", "MongoBackupService.dll"]