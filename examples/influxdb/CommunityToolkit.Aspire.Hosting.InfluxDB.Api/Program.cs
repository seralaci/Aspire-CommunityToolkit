var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();

app.MapGet("/ping", () => Results.Text("Ping"));

app.MapDefaultEndpoints();
app.Run();