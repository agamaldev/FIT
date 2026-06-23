using FitApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitApi.Tests;

public class EmailWiringTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EmailWiringTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void IEmailSender_resolves_to_the_factory_CapturingEmailSender_instance()
    {
        // Building a client forces the host to start (and applies the Task 2 startup migration).
        _ = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var resolved = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        Assert.IsType<CapturingEmailSender>(resolved);
        Assert.Same(_factory.Email, resolved);
    }
}
