namespace PetShopWeb.Tests;

public class CatalogControllerTests : IClassFixture<PetShopWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CatalogControllerTests(PetShopWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task ShowCatalog_AllAnimals_ReturnsOk()
    {
        var response = await _client.GetAsync("/Catalog/ShowCatalog/0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShowCatalog_WithMammalCategory_ReturnsOk()
    {
        var response = await _client.GetAsync("/Catalog/ShowCatalog/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShowCatalog_WithUnknownCategory_ReturnsOk()
    {
        // Category id 999 does not exist — repository falls back to returning all animals
        var response = await _client.GetAsync("/Catalog/ShowCatalog/999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShowCatalog_AllAnimals_ResponseContainsAnimalData()
    {
        var response = await _client.GetAsync("/Catalog/ShowCatalog/0");
        var html = await response.Content.ReadAsStringAsync();
        // Seed data includes "Dog" and "Cat" (mammals in category 1)
        Assert.Contains("Dog", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cat", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShowCatalog_MammalFilter_ContainsMammalsAndExcludesFish()
    {
        var response = await _client.GetAsync("/Catalog/ShowCatalog/1");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dog", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cat", html, StringComparison.OrdinalIgnoreCase);
        // Birds/fish/reptiles/amphibians should not appear in mammals-only view
        Assert.DoesNotContain("White Shark", html, StringComparison.OrdinalIgnoreCase);
    }
}
