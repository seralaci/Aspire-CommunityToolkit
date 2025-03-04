# CommunityToolkit.Aspire.Hosting.InfluxDB library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure an [InfluxDB](https://github.com/influxdata/influxdb) resource.

## Getting Started

### Install the package

In your AppHost project, install the .NET Aspire InfluxDB Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package CommunityToolkit.Aspire.Hosting.InfluxDB
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add an InfluxDB resource and consume the connection using the following methods:

```csharp
var influxDb = builder.AddInfluxDB("influxdb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(influxDb);
```

## Additional documentation

<!-- TODO: Update the link once it is created -->
https://learn.microsoft.com/dotnet/aspire/community-toolkit/hosting-influxdb

## Feedback & contributing

https://github.com/CommunityToolkit/Aspire

_*InfluxDB is a trademark registered by InfluxData, which is not affiliated with, and does not endorse, this library._