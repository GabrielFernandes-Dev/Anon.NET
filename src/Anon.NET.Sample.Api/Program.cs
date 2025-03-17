using Anon.NET.Middleware;
using Anon.NET.SqlInterception.EntityFramework;
using Anon.NET.Core.Extensions;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<SampleDbContext>((sp, options) => {
    var interceptor = sp.GetRequiredService<AnonDbCommandInterceptor>();

    // Configura o SQLite com o arquivo no diretório atual
    options.UseSqlite("Data Source=sample.db")
           .UseAnonSqlInterception(interceptor); // Integra o interceptor SQL
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Garante que o banco de dados existe e aplica as migrações
    //using (var scope = app.Services.CreateScope())
    //{
    //    var dbContext = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    //    dbContext.Database.EnsureCreated();
    //}
}

app.UseAnonymization();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", (HttpContext context) => {
    return Results.Text("Anon.NET API funcionando!");
});

// Rota para testar a interceptação de SQL
app.MapGet("/test-db", async (SampleDbContext dbContext) => {
    // Executa uma query simples para testar o interceptor
    var users = await dbContext.Users.ToListAsync();
    return Results.Ok(new { Message = "Query executada com sucesso", UserCount = users.Count });
});
app.Run();

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configura a entidade User
        modelBuilder.Entity<User>().HasKey(u => u.Id);

        // Dados iniciais para teste
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "João", Email = "joao@example.com" },
            new User { Id = 2, Name = "Maria", Email = "maria@example.com" },
            new User { Id = 3, Name = "Pedro", Email = "pedro@example.com" }
        );
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
