# STAGE 1: Build React (Vite)
FROM node:18 AS frontend-build
WORKDIR /app-frontend
# 1. Copy package files from your specific frontend folder
COPY ./frontend-react/package*.json ./
RUN npm install

# 2. Copy the rest of the frontend code
COPY ./frontend-react ./

# 3. Build (Vite creates a 'dist' folder, not 'build')
RUN npm run build

# STAGE 2: Build .NET Backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

# 4. Copy the CSPROJ file, keeping the 'backend' folder structure
COPY ["backend/OddOneOut.csproj", "backend/"]

# 5. Restore dependencies inside that specific folder
RUN dotnet restore "backend/OddOneOut.csproj"

# 6. Copy EVERYTHING from the root (includes backend/ and frontend-react/)
COPY . .

# 7. Build and Publish
RUN dotnet build "backend/OddOneOut.csproj" -c Release -o /app/build
RUN dotnet publish "backend/OddOneOut.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 3: Final Production Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# 8. Copy the compiled .NET app
COPY --from=backend-build /app/publish .

# 9. Copy the React files.
# CRITICAL FIX for Vite: We copy from 'dist', not 'build'
COPY --from=frontend-build /app-frontend/dist ./wwwroot

ENTRYPOINT ["dotnet", "OddOneOut.dll"]