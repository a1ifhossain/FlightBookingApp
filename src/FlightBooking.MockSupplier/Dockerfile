FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/FlightBooking.MockSupplier/ src/FlightBooking.MockSupplier/
RUN dotnet publish src/FlightBooking.MockSupplier -c Release -o /publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FlightBooking.MockSupplier.dll"]
