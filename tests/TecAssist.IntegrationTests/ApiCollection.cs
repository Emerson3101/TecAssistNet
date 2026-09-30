using TecAssist.IntegrationTests.Infrastructure;

namespace TecAssist.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
