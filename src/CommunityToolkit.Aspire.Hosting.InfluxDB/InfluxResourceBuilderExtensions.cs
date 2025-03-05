using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.InfluxDB;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding InfluxDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class InfluxResourceBuilderExtensions
{
    // Internal port is always 8086.
    private const int DefaultContainerPort = 8086;
    
    private const string UserNameEnvVarName = "DOCKER_INFLUXDB_INIT_USERNAME";
    private const string PasswordEnvVarName = "DOCKER_INFLUXDB_INIT_PASSWORD";
    private const string AdminTokenEnvVarName = "DOCKER_INFLUXDB_INIT_ADMIN_TOKEN";
    private const string OrganizationEnvVarName = "DOCKER_INFLUXDB_INIT_ORG";
    private const string BucketEnvVarName = "DOCKER_INFLUXDB_INIT_BUCKET";
    private const string RetentionEnvVarName = "DOCKER_INFLUXDB_INIT_RETENTION";
    
    /// <summary>
    /// Adds a InfluxDB resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to which the resource is added.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="token">The parameter used to provide the operator token for the InfluxDB. If <see langword="null"/> a random token will be generated.</param>
    /// <param name="userName">The parameter used to provide the user name for the InfluxDB resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the administrator password for the InfluxDB resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="organization">The parameter used to provide the name of the initial organization for the InfluxDB resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="bucket">The parameter used to provide the name of the initial bucket (database) for the InfluxDB resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="retention">The parameter used to provide the duration to use as the initial bucket's retention period. If <see langword="null"/> a default value (0 - infinite; doesn't delete data) will be used.</param>
    /// <param name="port">The host port used when launching the container. If null a random port will be assigned.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for the InfluxDB server resource.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the InfluxDB resource is able to service requests.
    /// </para>
    /// This version of the package defaults to the <inheritdoc cref="InfluxContainerImageTags.Tag"/> tag of the <inheritdoc cref="InfluxContainerImageTags.Image"/> container image.
    /// </remarks>
    public static IResourceBuilder<InfluxServerResource> AddInfluxDB(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource>? token = null,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        IResourceBuilder<ParameterResource>? organization = null,
        IResourceBuilder<ParameterResource>? bucket = null,
        IResourceBuilder<ParameterResource>? retention = null,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var tokenParameter = token?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-token", special: false);
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var influxDBServer = new InfluxServerResource(name, userName?.Resource, passwordParameter, tokenParameter, organization?.Resource, bucket?.Resource, retention?.Resource);

        string? connectionString = null;
        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(influxDBServer, async (@event, ct) =>
        {
            connectionString = await influxDBServer.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString is null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{influxDBServer.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services
            .AddHealthChecks()
            .AddInfluxDb(
                sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"),
                name: healthCheckKey);

        return builder
            .AddResource(influxDBServer)
            .WithEndpoint(port: port, targetPort: DefaultContainerPort, scheme: InfluxServerResource.PrimaryEndpointName)
            .WithImage(InfluxContainerImageTags.Image, InfluxContainerImageTags.Tag)
            .WithImageRegistry(InfluxContainerImageTags.Registry)
            .WithEnvironment("DOCKER_INFLUXDB_INIT_MODE", "setup")
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[UserNameEnvVarName] = influxDBServer.UserNameReference;
                context.EnvironmentVariables[PasswordEnvVarName] = influxDBServer.PasswordParameter;
                context.EnvironmentVariables[AdminTokenEnvVarName] = influxDBServer.TokenParameter;
                context.EnvironmentVariables[OrganizationEnvVarName] = influxDBServer.OrganizationReference;
                context.EnvironmentVariables[BucketEnvVarName] = influxDBServer.BucketReference;
                context.EnvironmentVariables[RetentionEnvVarName] = influxDBServer.RetentionReference;
            })
            .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a named volume for the data directory to a InfluxDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder for the InfluxDB server.</param>
    /// <param name="name">Optional name for the volume. Defaults to a generated name if not provided.</param>
    /// <param name="isReadOnly">Indicates whether the volume should be read-only. Defaults to false.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for the InfluxDB server resource.</returns>
    public static IResourceBuilder<InfluxServerResource> WithDataVolume(this IResourceBuilder<InfluxServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "influxdb2-data"), "/var/lib/influxdb2", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data directory to a InfluxDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder for the InfluxDB server.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">Indicates whether the bind mount should be read-only. Defaults to false.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for the InfluxDB server resource.</returns>
    public static IResourceBuilder<InfluxServerResource> WithDataBindMount(this IResourceBuilder<InfluxServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/var/lib/influxdb2", isReadOnly);
    }

    /// <summary>
    /// Adds a named volume for the configuration directory to a InfluxDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder for the InfluxDB server.</param>
    /// <param name="name">Optional name for the volume. Defaults to a generated name if not provided.</param>
    /// <param name="isReadOnly">Indicates whether the volume should be read-only. Defaults to false.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for the InfluxDB server resource.</returns>
    public static IResourceBuilder<InfluxServerResource> WithConfigVolume(this IResourceBuilder<InfluxServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "influxdb2-config"), "/etc/influxdb2", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the configuration directory to a InfluxDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder for the InfluxDB server.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">Indicates whether the bind mount should be read-only. Defaults to false.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for the InfluxDB server resource.</returns>
    public static IResourceBuilder<InfluxServerResource> WithConfigBindMount(this IResourceBuilder<InfluxServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/etc/influxdb2", isReadOnly);
    }
}
