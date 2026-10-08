using System.Net.Mail;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
var dataDirectory = Environment.GetEnvironmentVariable("PORTFOLIO_DATA_DIR")
    ?? Path.Combine(app.Environment.ContentRootPath, "data");
var messagesPath = Path.Combine(dataDirectory, "contact-messages.json");
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
var writeLock = new SemaphoreSlim(1, 1);

app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/projects", () => Results.Ok(new[]
{
    new Project("Business Operations Platform", "ADMIN DASHBOARD", "A unified workspace for operations and customer management, with role-based access and actionable reporting.", ["ASP.NET Core", "EF Core", "React"], "ops-dashboard.png"),
    new Project("Commerce Platform", "E-COMMERCE", "An end-to-end shopping experience covering product catalogues, inventory, orders, and secure payments.", [".NET Web API", "SQL Server", "Stripe"], "commerce-store.png"),
    new Project("Smart Booking System", "BOOKING PLATFORM", "A flexible booking system for managing availability, appointments, and customer notifications.", ["ASP.NET Core", "SignalR", "Azure"], "booking-calendar.png"),
    new Project("Team Workspace", "TASK MANAGEMENT", "A collaborative workspace for organizing tasks, tracking progress, and keeping teams aligned.", ["Clean Architecture", "React", "Docker"], "team-workspace.png")
}));

app.MapPost("/api/contact", async (ContactRequest request, HttpContext context) =>
{
    if (!string.IsNullOrWhiteSpace(request.Website))
        return Results.Accepted();

    var name = request.Name?.Trim() ?? "";
    var email = request.Email?.Trim() ?? "";
    var message = request.Message?.Trim() ?? "";
    var errors = new Dictionary<string, string[]>();

    if (name.Length is < 2 or > 100)
        errors["name"] = ["Enter a name between 2 and 100 characters."];
    if (email.Length is < 5 or > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
        errors["email"] = ["Enter a valid email address."];
    if (message.Length is < 10 or > 4000)
        errors["message"] = ["Enter a message between 10 and 4,000 characters."];

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var entry = new ContactMessage(Guid.NewGuid(), name, email, message, DateTimeOffset.UtcNow);

    await writeLock.WaitAsync(context.RequestAborted);
    try
    {
        Directory.CreateDirectory(dataDirectory);
        List<ContactMessage> messages = [];
        if (File.Exists(messagesPath))
        {
            await using var input = File.OpenRead(messagesPath);
            messages = await JsonSerializer.DeserializeAsync<List<ContactMessage>>(input, jsonOptions, context.RequestAborted) ?? [];
        }
        messages.Add(entry);
        var temporaryPath = messagesPath + ".tmp";
        await using (var output = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(output, messages, jsonOptions, context.RequestAborted);
        File.Move(temporaryPath, messagesPath, true);
    }
    finally
    {
        writeLock.Release();
    }

    return Results.Created("/api/contact", new { message = "Thanks, your message has been received." });
}).RequireRateLimiting("contact");

app.Run();

record Project(string Title, string Category, string Description, string[] Technologies, string Image);

record ContactRequest(string? Name, string? Email, string? Message, string? Website);

record ContactMessage(Guid Id, string Name, string Email, string Message, DateTimeOffset ReceivedAt);
