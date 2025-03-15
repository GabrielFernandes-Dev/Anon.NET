using Anon.NET;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseAnonymization();

app.MapGet("/", (HttpContext context) => {
    return Results.Text("Teste middleware");
});

app.Run();
