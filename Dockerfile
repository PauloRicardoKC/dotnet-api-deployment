FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/MinimalApi.Api/MinimalApi.Api.csproj", "src/MinimalApi.Api/"]
COPY ["src/MinimalApi.Application/MinimalApi.Application.csproj", "src/MinimalApi.Application/"]
COPY ["src/MinimalApi.Domain/MinimalApi.Domain.csproj", "src/MinimalApi.Domain/"]
COPY ["src/MinimalApi.Infrastructure/MinimalApi.Infrastructure.csproj", "src/MinimalApi.Infrastructure/"]
RUN dotnet restore "src/MinimalApi.Api/MinimalApi.Api.csproj"
COPY . .
RUN dotnet publish "src/MinimalApi.Api/MinimalApi.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MinimalApi.Api.dll"]
