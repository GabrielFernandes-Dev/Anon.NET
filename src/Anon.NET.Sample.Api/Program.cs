using Anon.NET.Anonimization;
using Anon.NET.Core.Extensions;
using Anon.NET.Middleware;
using Anon.NET.Sample.Api.Data;
using Anon.NET.SqlInterception.EntityFramework;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddAnonymization(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAnonDashboard();
builder.Services.AddAnonymization();

string connectionString = builder.Configuration.GetConnectionString("ConnectionString:Anon.NET")!;
var conStrBuilder = new MySqlConnectionStringBuilder(connectionString);
builder.Services.AddDbContext<SampleDbContext>((sp, options) => {
    var interceptor = sp.GetRequiredService<AnonDbCommandInterceptor>();
    var anonInterceptor = sp.GetRequiredService<AnonymizationInterceptor>();

    options.UseMySQL(conStrBuilder.ConnectionString)
           .UseAnonSqlInterception(interceptor)
           .UseAnonymization(anonInterceptor);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAnonDashboard();

app.UseAnonymization();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
