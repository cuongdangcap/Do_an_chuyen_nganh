FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api ./apps/api
WORKDIR /src/apps/api
RUN dotnet restore AdmissionsAiSystem.slnx
RUN dotnet publish src/Admissions.Api/Admissions.Api/Admissions.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Admissions.Api.dll"]
