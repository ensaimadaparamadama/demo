using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskService>();

var app = builder.Build();
app.UseStaticFiles();

// GET all tasks
app.MapGet("/api/tasks", (TaskService svc) => Results.Ok(svc.GetAll()));

// POST add task
app.MapPost("/api/tasks", (TaskService svc, [FromBody] TaskInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.BadRequest("Task title cannot be empty.");

    var task = svc.Add(input.Title);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

// PATCH toggle completed
app.MapPatch("/api/tasks/{id:int}/toggle", (TaskService svc, int id) =>
{
    var task = svc.Toggle(id);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

// DELETE task
app.MapDelete("/api/tasks/{id:int}", (TaskService svc, int id) =>
{
    var removed = svc.Delete(id);
    return removed ? Results.NoContent() : Results.NotFound();
});

// Serve index.html at root
app.MapGet("/", async (HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/html";
    await ctx.Response.SendFileAsync("wwwroot/index.html");
});

app.Run();

// ─── Models ───────────────────────────────────────────────────────────────────

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record TaskInput(string Title);

// ─── Service ──────────────────────────────────────────────────────────────────

public class TaskService
{
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;

    public List<TaskItem> GetAll() => _tasks.ToList();

    public TaskItem Add(string title)
    {
        var task = new TaskItem
        {
            Id = _nextId++,
            Title = title.Trim(),
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };
        _tasks.Add(task);
        return task;
    }

    public TaskItem? Toggle(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null) return null;
        task.IsCompleted = !task.IsCompleted;
        return task;
    }

    public bool Delete(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null) return false;
        _tasks.Remove(task);
        return true;
    }
}
