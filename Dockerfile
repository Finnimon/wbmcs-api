FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Wbmcs.Api/Wbmcs.Api.csproj Wbmcs.Api/
RUN dotnet restore Wbmcs.Api/Wbmcs.Api.csproj

COPY . .
RUN dotnet publish Wbmcs.Api/Wbmcs.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Wbmcs.Api.dll"]