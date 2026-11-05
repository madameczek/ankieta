# polls-api – ASP.NET Core API (D21). Build context: src/polls
#   docker build -f deploy/polls-api.Dockerfile -t polls-api src/polls

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached while only code changes): solution-level props + the API project
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY polls.Api/polls.Api.csproj polls.Api/
RUN dotnet restore polls.Api/polls.Api.csproj

COPY polls.Api/ polls.Api/
RUN dotnet publish polls.Api/polls.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Non-root user of the base image; Kestrel listens on 8080 (ASPNETCORE_HTTP_PORTS default)
USER $APP_UID
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "polls.Api.dll"]
