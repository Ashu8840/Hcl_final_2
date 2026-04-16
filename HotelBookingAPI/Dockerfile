# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN set -eux; \
		if [ -f "./HotelBookingAPI.csproj" ]; then \
			PROJECT="./HotelBookingAPI.csproj"; \
		elif [ -f "./HotelBookingAPI/HotelBookingAPI.csproj" ]; then \
			PROJECT="./HotelBookingAPI/HotelBookingAPI.csproj"; \
		else \
			echo "HotelBookingAPI.csproj not found in Docker build context."; \
			ls -la; \
			exit 1; \
		fi; \
		dotnet restore "$PROJECT"; \
		dotnet publish "$PROJECT" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Render provides PORT at runtime; default to 8080 for local docker runs.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet HotelBookingAPI.dll --urls=http://0.0.0.0:${PORT:-8080}"]
