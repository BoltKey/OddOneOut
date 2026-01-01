# STAGE 1: Build the React Frontend
# Use Node.js image to build the frontend
FROM node:18 AS frontend-build
WORKDIR /app-frontend
COPY ./ClientApp/package*.json ./
# ^ Adjust './ClientApp' to your actual React folder name
RUN npm install
COPY ./ClientApp ./
RUN npm run build

# STAGE 2: Build the .NET Backend
# Use the .NET SDK image to build the backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY ["backend/OddOneOut.csproj", "./"]
RUN dotnet restore "backend/OddOneOut.csproj"
COPY . .
# Build the .NET app
RUN dotnet build "backend/OddOneOut.csproj" -c Release -o /app/build
RUN dotnet publish "backend/OddOneOut.csproj" -c Release -o /app/publish /p:UseAppHost=false
# STAGE 3: Final Production Image
# Use the lightweight ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy the compiled .NET app from Stage 2
COPY --from=backend-build /app/publish .

# Copy the compiled React files from Stage 1 into the .NET wwwroot
# Adjust 'build' to 'dist' if you use Vite instead of Create-React-App
COPY --from=frontend-build /app-frontend/build ./wwwroot

ENTRYPOINT ["dotnet", "OddOneOut.dll"]