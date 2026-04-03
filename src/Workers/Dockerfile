FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Workers/JobRag.Workers.csproj", "Workers/"]
COPY ["src/Application/JobRag.Application.csproj", "Application/"]
COPY ["src/Domain/JobRag.Domain.csproj", "Domain/"]
COPY ["src/Infrastructure/JobRag.Infrastructure.csproj", "Infrastructure/"]
COPY ["NuGet.Config", "."]
RUN dotnet restore "Workers/JobRag.Workers.csproj"
COPY src/ .
WORKDIR "/src/Workers"
RUN dotnet build "JobRag.Workers.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobRag.Workers.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobRag.Workers.dll"]
