FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Api/JobRag.Api.csproj", "Api/"]
COPY ["src/Application/JobRag.Application.csproj", "Application/"]
COPY ["src/Domain/JobRag.Domain.csproj", "Domain/"]
COPY ["src/Infrastructure/JobRag.Infrastructure.csproj", "Infrastructure/"]
COPY ["NuGet.Config", "."]
RUN dotnet restore "Api/JobRag.Api.csproj"
COPY src/ .
WORKDIR "/src/Api"
RUN dotnet build "JobRag.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobRag.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobRag.Api.dll"]
