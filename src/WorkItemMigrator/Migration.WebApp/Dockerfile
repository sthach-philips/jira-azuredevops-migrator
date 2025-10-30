FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Migration.WebApp/Migration.WebApp.csproj", "Migration.WebApp/"]
COPY ["Migration.Common/Migration.Common.csproj", "Migration.Common/"]
COPY ["Migration.WIContract/Migration.WIContract.csproj", "Migration.WIContract/"]
COPY ["JiraExport/JiraExport.csproj", "JiraExport/"]
COPY ["WorkItemImport/WorkItemImport.csproj", "WorkItemImport/"]
COPY ["Migration.Common.Log/Migration.Common.Log.csproj", "Migration.Common.Log/"]

RUN dotnet restore "Migration.WebApp/Migration.WebApp.csproj"
COPY . .
WORKDIR "/src/Migration.WebApp"
RUN dotnet build "Migration.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Migration.WebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Migration.WebApp.dll"]