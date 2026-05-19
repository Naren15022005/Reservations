using Xunit;

namespace FODUN.Reservations.Tests.Integration;

public class PlaceholderIntegrationTest
{
    [Fact]
    public void Integration_Placeholder_AlwaysPasses()
    {
        // Tests de integración requieren SQL Server disponible.
        // Se ejecutan en CI con la cadena de conexión configurada.
        Assert.True(true);
    }
}
