# STAGE 1: Build React (Vite)
FROM node:20-alpine AS frontend-build
WORKDIR /app-frontend
COPY ./frontend-react/package*.json ./
RUN npm install
COPY ./frontend-react ./
RUN npm run build

# STAGE 2: Build .NET Backend
# FIX 1: Change '8.0' to '10.0' to match your project version
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

COPY ["backend/OddOneOut.csproj", "backend/"]
RUN dotnet restore "backend/OddOneOut.csproj"
COPY . .

# Build and Publish
RUN dotnet build "backend/OddOneOut.csproj" -c Release -o /app/build
RUN dotnet publish "backend/OddOneOut.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 3: Final Production Image
# FIX 2: Change '8.0' to '10.0' here too
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=backend-build /app/publish .
# Ensuring we copy from 'dist' (since you are on Vite)
COPY --from=frontend-build /app-frontend/dist ./wwwroot

ENTRYPOINT ["dotnet", "OddOneOut.dll"]