using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlite("Data Source=orders.db"));

var app = builder.Build();

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


app.MapGet("/api/orders", async (OrderContext db) => {
    var kitchenOrders = await db.Orders.Where(o => o.IsDone == false).ToListAsync();
    return Results.Ok(kitchenOrders);
});


app.MapDelete("/api/orders/{id}", async (int id, OrderContext db) => {
    var order = await db.Orders.FindAsync(id);
    if (order == null) return Results.NotFound();

    order.IsDone = true; 
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم إنجاز الأوردر ونقله للأرشيف التاريخي! ✅" });
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
}