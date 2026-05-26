using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlite("Data Source=orders.db"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGitHubPages",
        policy =>
        {
            policy.WithOrigins("https://abdelrhmanmgahedmgd-web.github.io")                 
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();
app.UseCors("AllowGitHubPages");
app.UseCors(policy => policy.AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowAnyOrigin());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
    db.Database.EnsureCreated();
}

app.MapPost("/api/orders", async (Order newOrder, OrderContext db) => {
    newOrder.OrderDate = DateTime.Now;
    newOrder.IsDone = false; 
    
    db.Orders.Add(newOrder);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { message = "تم استقبال أوردر البرجر بنجاح! 🍔", orderId = newOrder.Id });
});


app.MapGet("/api/orders", async (OrderContext db) =>
{
    var activeOrders = await db.Orders.Where(o => o.IsDone == false).ToListAsync();
    return Results.Ok(activeOrders);
});

app.MapPut("/api/orders/{id}/ready", async (int id, OrderContext db) =>{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();
    
    order.IsReady = true;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم تجهيز الأوردر ونقله للكاشير ✔" });
});

app.MapDelete("/api/orders/{id}", async (int id, OrderContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    order.IsDone = true;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم تسليم الأوردر ونقله لأرشيف التاريخي 📦" });
});

app.MapGet("/api/vault", async (OrderContext db) => {
    var allOrders = await db.Orders.OrderByDescending(o => o.OrderDate).ToListAsync();
    var totalEarnings = await db.Orders.SumAsync(o => o.Price);

    return Results.Ok(new { 
        totalEarnings = totalEarnings,
        orders = allOrders
    });
});

app.Run();

public class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options) { }
    public DbSet<Order> Orders { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public string Phone { get; set; }
    public string ItemName { get; set; }
    public double Price { get; set; }
    public DateTime OrderDate { get; set; }
    public bool IsDone { get; set; } 
    public bool IsReady { get; set; } ;
}

