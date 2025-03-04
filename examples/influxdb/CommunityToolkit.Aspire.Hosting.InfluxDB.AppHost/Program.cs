using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<CommunityToolkit_Aspire_Hosting_InfluxDB_Api>("InfluxDBExample");

builder.Build().Run();