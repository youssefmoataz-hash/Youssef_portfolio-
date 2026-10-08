FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY portfolio.csproj ./
RUN dotnet restore portfolio.csproj
COPY . ./
RUN dotnet publish portfolio.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
ENV PORTFOLIO_DATA_DIR=/tmp/portfolio-data
EXPOSE 10000
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "portfolio.dll"]
