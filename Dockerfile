FROM mcr.microsoft.com/dotnet/sdk:10.0 as build-env
WORKDIR /src
COPY PetShopWeb/PetShopWeb.csproj PetShopWeb/
RUN dotnet restore PetShopWeb/PetShopWeb.csproj
COPY . .
RUN dotnet publish PetShopWeb/PetShopWeb.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .
# .NET 8+ container images default to port 8080; pin 80 to keep the existing published port mapping.
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
EXPOSE 433
ENTRYPOINT ["dotnet", "PetShopWeb.dll"]
