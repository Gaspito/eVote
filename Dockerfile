FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5079

ENV ASPNETCORE_URLS=http://+:5079

# USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["VotingApp.Web.csproj", "./"]
RUN dotnet restore "VotingApp.Web.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "VotingApp.Web.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "VotingApp.Web.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VotingApp.Web.dll"]
