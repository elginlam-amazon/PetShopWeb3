namespace PetShopWeb.Tests;

public class AccountControllerTests : IClassFixture<PetShopWebApplicationFactory>
{
    private readonly PetShopWebApplicationFactory _factory;

    public AccountControllerTests(PetShopWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginPage_ReturnsOk()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPage_ReturnsOk()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessDeniedPage_ReturnsOk()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/AccessDenied");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UsersManager_Unauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/UsersManager");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RolesManager_Unauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/RolesManager");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        // Each authenticated test gets its own client so cookies don't bleed between tests
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        // GET the login page to obtain the CSRF token
        var loginPage = await client.GetAsync("/Account/Login");
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var csrfToken = AntiForgeryHelper.Extract(loginHtml);
        Assert.NotNull(csrfToken);

        var formValues = new Dictionary<string, string>
        {
            ["Email"] = "admin@admin.com",
            ["Password"] = "Admin123",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = csrfToken!
        };

        var postResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(formValues));
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        // RedirectToAction("HomePage", "Home") generates "/" for the default route
        var location = postResponse.Headers.Location?.ToString() ?? string.Empty;
        Assert.True(
            location == "/" ||
            location.Contains("Home", StringComparison.OrdinalIgnoreCase),
            $"Expected redirect to home but got: {location}");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsLoginPage()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        var loginPage = await client.GetAsync("/Account/Login");
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var csrfToken = AntiForgeryHelper.Extract(loginHtml);
        Assert.NotNull(csrfToken);

        var formValues = new Dictionary<string, string>
        {
            ["Email"] = "admin@admin.com",
            ["Password"] = "WrongPassword!",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = csrfToken!
        };

        var postResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(formValues));
        // Invalid credentials return the login form (200) with an error
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        var html = await postResponse.Content.ReadAsStringAsync();
        Assert.Contains("incorrect", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IsEmailInUse_ExistingEmail_ReturnsFalseJson()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Account/IsEmailInUse?email=admin@admin.com");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        // Existing email returns a string message (truthy-false for client-side validation)
        Assert.DoesNotContain("true", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IsEmailInUse_FreshEmail_ReturnsTrueJson()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Account/IsEmailInUse?email=nobody@example.com");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(string.Equals("true", json.Trim(), StringComparison.OrdinalIgnoreCase),
            $"Expected 'true' but got: {json.Trim()}");
    }
}
