# PetShop ASP.NET Core web application
<h3>ASP.NET Core 10 MVC web app using MSSQL, bootstrap, EF Core, identity and docker</h3>

<p align="center">
  <img width="600"  src="https://user-images.githubusercontent.com/110489710/209929509-3ed325ce-c8b4-40f4-a2f7-a2f7ad0ff727.png">
</p>

<h3>Docker instructions</h3>
pulling images to your local machine:

```javascript
  $ docker pull yarins2000/petshopapp:1.0
  $ docker pull yarins2000/petshopdb:1.0
```

running the containers of these images:

```javascript
  $ docker-compose up -d
```
then, open a browser and write in the URL: localhost:3000

Note: the published `yarins2000/petshopapp:1.0` image was built from the earlier .NET 6 version of this code.
To run the current .NET 10 code, build the app image from the repository root:

```javascript
  $ docker build -t yarins2000/petshopapp:1.0 .
```

<h3>Building from source</h3>
Requires the .NET 10 SDK.

```javascript
  $ dotnet build ./PetShopWeb/PetShopWeb.csproj
```

The app reads its SQL Server connection string from `ConnectionStrings:DefaultConnection` in `PetShopWeb/appsettings.json`
(it can be overridden with the `ConnectionStrings__DefaultConnection` environment variable).

<h3>About the web app</h3>
This is a pet shop web when you can view the animals, add comments and filter them by their category.
You can also register to the website and this provides you an access to add, edit and delete an animal.
If you are an admin, you can also edit the registrated users and add/edit/delete their roles(e.g admin).

for login as an admin, there is a default admin user:
email: admin@admin.com
password: Admin123
