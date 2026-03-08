# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia a solução e todos os projetos.
COPY ["NotificationsAPI.sln", "."]
COPY ["src/NotificationsAPI.Api/NotificationsAPI.Api.csproj", "src/NotificationsAPI.Api/"]
COPY ["src/NotificationsAPI.Application/NotificationsAPI.Application.csproj", "src/NotificationsAPI.Application/"]
COPY ["src/NotificationsAPI.Data/NotificationsAPI.Data.csproj", "src/NotificationsAPI.Data/"]
COPY ["src/NotificationsAPI.Domain/NotificationsAPI.Domain.csproj", "src/NotificationsAPI.Domain/"]
COPY ["src/NotificationsAPI.IoC/NotificationsAPI.IoC.csproj", "src/NotificationsAPI.IoC/"]
COPY ["src/NotificationsAPI.Messaging/NotificationsAPI.Messaging.csproj", "src/NotificationsAPI.Messaging/"]


# restaura as dependencias
RUN dotnet restore "NotificationsAPI.sln"

#copia o restante do codigo.
COPY . .

#publica o projeto principal
RUN dotnet publish "src/NotificationsAPI.Api/NotificationsAPI.Api.csproj" -c Release -o /app/publish


#img final.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationsAPI.Api.dll"]