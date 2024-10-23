FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Booking/Bookings.Presentation/Bookings.Presentation.csproj", "Booking/Bookings.Presentation/"]
RUN dotnet restore "./Booking/Bookings.Presentation/Bookings.Presentation.csproj"
COPY . .
WORKDIR "/src/Booking/Bookings.Presentation"
RUN dotnet build "./Bookings.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Bookings.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bookings.Presentation.dll"]