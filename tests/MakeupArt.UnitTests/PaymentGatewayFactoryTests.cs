using MakeupArt.Application.Common;
using MakeupArt.Domain.Common;
using MakeupArt.Infrastructure.Options;
using MakeupArt.Infrastructure.Payments;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MakeupArt.UnitTests;

public sealed class PaymentGatewayFactoryTests
{
    [Fact]
    public void ProductionDoesNotExposeOrResolveMockPayments()
    {
        var factory = CreateFactory(Environments.Production);

        Assert.DoesNotContain("Mock", factory.AvailableProviders);
        Assert.Throws<ApiException>(() => factory.Resolve("Mock"));
        Assert.Throws<ApiException>(() => factory.Resolve(null));
    }

    [Fact]
    public void DevelopmentKeepsMockPaymentsAvailableForPreviewAndE2E()
    {
        var factory = CreateFactory(Environments.Development);

        Assert.Contains("Mock", factory.AvailableProviders);
        Assert.Equal(PaymentProviderType.Mock, factory.Resolve(null).Provider);
    }

    private static PaymentGatewayFactory CreateFactory(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return new PaymentGatewayFactory(
            [new MockPaymentGateway(), new UnavailablePaymentGateway(PaymentProviderType.Stripe), new UnavailablePaymentGateway(PaymentProviderType.PayPal)],
            Options.Create(new PaymentOptions { DefaultProvider = "Mock" }),
            Options.Create(new RazorpayOptions()),
            environment);
    }
}
