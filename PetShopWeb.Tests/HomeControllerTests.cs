namespace PetShopWeb.Tests;

public class HomeControllerTests : IClassFixture<PetShopWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HomeControllerTests(PetShopWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task HomePage_ReturnsOk()
    {
        var response = await _client.GetAsync("/Home/HomePage");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RootPath_RedirectsToHomePage()
    {
        var response = await _client.GetAsync("/");
        // Default route redirects or serves the home page
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.MovedPermanently,
            $"Unexpected status: {response.StatusCode}");
    }
}
