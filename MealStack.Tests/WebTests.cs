using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace MealStack.Tests
{
    public class WebTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        
        public WebTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }
        
        [Fact]
        public async Task HomePage_Works()
        {
            var response = await _client.GetAsync("/");
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("MealStack");
        }
        
        [Fact]
        public async Task RecipesPage_Works()
        {
            var response = await _client.GetAsync("/Recipe");
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        
        [Fact]
        public async Task CreateRecipe_RequiresLogin()
        {
            var response = await _client.GetAsync("/Recipe/Create");
            
            // Should redirect to login
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        }
    }
}