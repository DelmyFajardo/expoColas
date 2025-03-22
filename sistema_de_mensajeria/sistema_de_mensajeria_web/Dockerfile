FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["sistema_de_mensajeria_web.csproj", "./"]
RUN dotnet restore "sistema_de_mensajeria_web.csproj"
COPY . .
RUN dotnet build "sistema_de_mensajeria_web.csproj" -c Release -o /app/build
RUN dotnet publish "sistema_de_mensajeria_web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "sistema_de_mensajeria_web.dll"]