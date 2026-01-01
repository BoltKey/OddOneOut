# STAGE 1: Build React
FROM node:18 AS frontend-build
WORKDIR /app-frontend
# IMPORTANT: Ensure this matches your actual frontend folder name (e.g., 'ClientApp' or 'frontend')
COPY ./frontend-react/package*.json ./
RUN npm install
COPY ./frontend-react ./
RUN npm run build

# STAGE 2: Build .NET
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

# --- THE FIX IS HERE ---
# We copy the csproj file specifically to a 'backend' folder inside Docker
COPY ["backend/OddOneOut.csproj", "backend/"]

# Now we restore using that same path
RUN dotnet restore "backend/OddOneOut.csproj"

# Now copy EVERYTHING from your computer's root to the Docker's root
COPY . .

# Build using the path inside Docker
RUN dotnet build "backend/OddOneOut.csproj" -c Release -o /app/build
RUN dotnet publish "backend/OddOneOut.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=backend-build /app/publish .
COPY --from=frontend-build /app-frontend/build ./wwwroot
ENTRYPOINT ["dotnet", "OddOneOut.dll"]