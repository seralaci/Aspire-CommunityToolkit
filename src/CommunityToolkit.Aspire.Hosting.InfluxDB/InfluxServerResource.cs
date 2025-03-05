using Aspire.Hosting.ApplicationModel;

namespace CommunityToolkit.Aspire.Hosting.InfluxDB;

/// <summary>
/// Represents a resource for InfluxDB container.
/// </summary>
public class InfluxServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";
    private const string DefaultUserName = "username";
    private const string DefaultOrganization = "org";
    private const string DefaultBucket = "bucket";
    private const string DefaultRetention = "0";
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InfluxServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the InfluxDB server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the InfluxDB server password.</param>
    /// <param name="token">The parameter used to provide the operator token for the InfluxDB. If <see langword="null"/> a random token will be generated.</param>
    /// <param name="organization">The parameter used to provide the name of the initial organization for the InfluxDB resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="bucket">The parameter used to provide the name of the initial bucket (database) for the InfluxDB resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="retention">The parameter used to provide the duration to use as the initial bucket's retention period. If <see langword="null"/> a default value (0 - infinite; doesn't delete data) will be used.</param>
    public InfluxServerResource(
        string name,
        ParameterResource? userName,
        ParameterResource password,
        ParameterResource token,
        ParameterResource? organization,
        ParameterResource? bucket,
        ParameterResource? retention) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);

        PrimaryEndpoint = new EndpointReference(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;
        TokenParameter = token;
        OrganizationParameter = organization;
        BucketParameter = bucket;
        RetentionParameter = retention;
    }
    
    /// <summary>
    /// Gets the primary endpoint for the InfluxDB server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// Gets or sets the parameter that contains the InfluxDB server user name.
    /// </summary>
    public ParameterResource? UserNameParameter { get; set; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");
    
    /// <summary>
    /// Gets or sets the parameter that contains the InfluxDB server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; set; } 
    
    /// <summary>
    /// Gets or sets the parameter that contains the InfluxDB server admin token.
    /// </summary>
    public ParameterResource TokenParameter { get; set; }

    /// <summary>
    /// Gets or sets the parameter that contains the InfluxDB server initial organization.
    /// </summary>
    public ParameterResource? OrganizationParameter { get; set; }

    internal ReferenceExpression OrganizationReference => OrganizationParameter is not null
        ? ReferenceExpression.Create($"{OrganizationParameter}")
        : ReferenceExpression.Create($"{DefaultOrganization}");

    /// <summary>
    /// Gets or sets the parameter that contains the InfluxDB server initial bucket (database).
    /// </summary>
    public ParameterResource? BucketParameter { get; set; } 

    internal ReferenceExpression BucketReference => BucketParameter is not null
        ? ReferenceExpression.Create($"{BucketParameter}")
        : ReferenceExpression.Create($"{DefaultBucket}");
    
    /// <summary>
    /// Gets or sets the parameter that contains the InfluxDB server retention.
    /// </summary>
    public ParameterResource? RetentionParameter { get; set; } 

    internal ReferenceExpression RetentionReference => RetentionParameter is not null
        ? ReferenceExpression.Create($"{RetentionParameter}")
        : ReferenceExpression.Create($"{DefaultRetention}");

    /// <summary>
    /// Gets the connection string expression for the InfluxDB server,
    /// in the following format: "{scheme}://{host}:{port}?token={token}").
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{PrimaryEndpoint.Scheme}://{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}?token={TokenParameter}");
}
