using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenMoneyWeb.Api.Dtos;

namespace OpenMoneyWeb.Api.Tests;

public class InvestmentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public InvestmentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/investments/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_InvalidInvestmentType_Returns400()
    {
        var dto = new CreateInvestmentDto("Apple Inc", "AAPL", "NotAValidType", 150m);
        var response = await _client.PostAsJsonAsync("/api/investments", dto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ValidInvestment_Returns201()
    {
        var dto = new CreateInvestmentDto("Apple Inc", "AAPL", "Stock", 150m);
        var response = await _client.PostAsJsonAsync("/api/investments", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var dto = new CreateInvestmentDto("Apple Inc", "AAPL", "Stock", 150m);
        var response = await _client.PutAsJsonAsync("/api/investments/99999", dto);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_InvalidInvestmentType_Returns400()
    {
        // Create a valid investment first
        var create = new CreateInvestmentDto("Apple Inc", "AAPL", "Stock", 150m);
        var created = await _client.PostAsJsonAsync("/api/investments", create);
        var inv = await created.Content.ReadFromJsonAsync<InvestmentDto>(JsonOpts);

        var update = new CreateInvestmentDto("Apple Inc", "AAPL", "BadType", 160m);
        var response = await _client.PutAsJsonAsync($"/api/investments/{inv!.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
