using System.Globalization;
using Npgsql;
using Shouldly;

namespace EduPlatform.Api.IntegrationTests;

/// <summary>
/// Verifies the database contract the rest of the platform is built on, against a real
/// PostgreSQL server. These tests need a running Docker daemon; Testcontainers starts and
/// disposes the server itself, so no manual setup is involved.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DatabaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Server_accepts_connections_and_reports_its_version()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.ConnectionString);

        var version = await Scalar<string>(dataSource, "SELECT version();");

        version.ShouldNotBeNull();
        version.ShouldContain("PostgreSQL 17");
    }

    [Fact]
    public async Task Pgvector_extension_can_be_enabled_and_used()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.ConnectionString);

        await Execute(dataSource, "CREATE EXTENSION IF NOT EXISTS vector;");
        await Execute(dataSource, "CREATE TABLE IF NOT EXISTS vector_probe (id int primary key, embedding vector(3));");
        await Execute(dataSource, "INSERT INTO vector_probe VALUES (1, '[1,2,3]') ON CONFLICT DO NOTHING;");

        // Cosine distance between a vector and itself is zero. This is the operator the
        // Phase 8 retrieval search will rely on, so it is worth proving it is present.
        var distance = await Scalar<object>(
            dataSource, "SELECT embedding <=> '[1,2,3]' FROM vector_probe WHERE id = 1;");

        Convert.ToDouble(distance, CultureInfo.InvariantCulture).ShouldBe(0d, 1e-6);
    }

    [Fact]
    public async Task Text_search_extensions_are_available()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.ConnectionString);

        await Execute(dataSource, "CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await Execute(dataSource, "CREATE EXTENSION IF NOT EXISTS unaccent;");

        // Trigram similarity is what will drive "did you mean" lookups over subject and lesson titles.
        var similarity = await Scalar<float>(dataSource, "SELECT similarity('matema', 'matematika');");

        similarity.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public async Task Schemas_isolate_modules_from_one_another()
    {
        await using var dataSource = NpgsqlDataSource.Create(postgres.ConnectionString);

        // Each module owns a schema, so two modules may hold a same-named table without colliding.
        await Execute(dataSource, "CREATE SCHEMA IF NOT EXISTS probe_identity;");
        await Execute(dataSource, "CREATE SCHEMA IF NOT EXISTS probe_gradebook;");
        await Execute(dataSource, "CREATE TABLE IF NOT EXISTS probe_identity.audit_log (id int primary key);");
        await Execute(dataSource, "CREATE TABLE IF NOT EXISTS probe_gradebook.audit_log (id int primary key);");

        var count = await Scalar<long>(
            dataSource,
            """
            SELECT count(*) FROM information_schema.tables
            WHERE table_name = 'audit_log' AND table_schema LIKE 'probe_%';
            """);

        count.ShouldBe(2);
    }

    private static async Task Execute(NpgsqlDataSource dataSource, string sql)
    {
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T?> Scalar<T>(NpgsqlDataSource dataSource, string sql)
    {
        await using var command = dataSource.CreateCommand(sql);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)result;
    }
}
