using Testcontainers.PostgreSql;

namespace EduPlatform.Api.IntegrationTests;

/// <summary>
/// Starts a real PostgreSQL server in a container for the duration of the test run.
/// <para>
/// The image is <c>pgvector/pgvector:pg17</c> — the same one <c>docker-compose.yml</c> uses — so the
/// tests exercise the extensions the application actually depends on, not a stripped-down stand-in.
/// The container is created once and shared by every test in the collection, because starting one
/// per test class would dominate the run time.
/// </para>
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("eduplatform_tests")
        .WithUsername("eduplatform")
        .WithPassword("test")
        .Build();

    /// <summary>Connection string pointing at the throwaway container, not at the developer's database.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// Binds <see cref="PostgresFixture"/> to a collection so a single container is reused
/// across every test class that declares <c>[Collection(PostgresCollection.Name)]</c>.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
