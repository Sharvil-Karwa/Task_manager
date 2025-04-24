using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;
using Dapper;
using Npgsql;
using System.Data;

class TaskItem
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public DateTime Deadline { get; set; }
    public bool IsCompleted { get; set; }
}

class Program
{
    static string connectionString = "";

    static async Task Main(string[] args)
    {
        // Load .env file and get DB connection string
        DotNetEnv.Env.Load();
        connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") 
                           ?? throw new Exception("DB_CONNECTION not found.");

        // Start reminder thread
        _ = StartReminderThread();

        while (true)
        {
            Console.WriteLine("\n=== Task Manager (PostgreSQL) ===");
            Console.WriteLine("1. Add Task");
            Console.WriteLine("2. View Tasks");
            Console.WriteLine("3. Update Task");
            Console.WriteLine("4. Delete Task");
            Console.WriteLine("5. Exit");
            Console.Write("Choose: ");

            switch (Console.ReadLine())
            {
                case "1": await AddTask(); break;
                case "2": await ViewTasks(); break;
                case "3": await UpdateTask(); break;
                case "4": await DeleteTask(); break;
                case "5": return;
                default: Console.WriteLine("❌ Invalid choice."); break;
            }
        }
    }

    static IDbConnection DbConnection => new NpgsqlConnection(connectionString);

    static async Task AddTask()
    {
        Console.Write("Description: ");
        var desc = Console.ReadLine() ?? "";

        Console.Write("Deadline (yyyy-MM-dd HH:mm): ");
        if (DateTime.TryParse(Console.ReadLine(), out var deadline))
        {
            var sql = "INSERT INTO Tasks (Description, Deadline, IsCompleted) VALUES (@desc, @deadline, false)";
            using var db = DbConnection;
            await db.ExecuteAsync(sql, new { desc, deadline });
            Console.WriteLine("✅ Task added.");
        }
        else Console.WriteLine("❌ Invalid date format.");
    }

    static async Task ViewTasks()
    {
        using var db = DbConnection;
        var tasks = (await db.QueryAsync<TaskItem>("SELECT * FROM Tasks ORDER BY Deadline")).ToList();

        if (!tasks.Any()) Console.WriteLine("No tasks yet.");
        else
        {
            foreach (var t in tasks)
                Console.WriteLine($"{t.Id}. {t.Description} | Due: {t.Deadline} | {(t.IsCompleted ? "✅ Done" : "❌ Pending")}");
        }
    }

    static async Task UpdateTask()
    {
        await ViewTasks();
        Console.Write("Task ID to update: ");
        if (int.TryParse(Console.ReadLine(), out int id))
        {
            using var db = DbConnection;
            var task = await db.QueryFirstOrDefaultAsync<TaskItem>("SELECT * FROM Tasks WHERE Id = @id", new { id });
            if (task == null) { Console.WriteLine("❌ Task not found."); return; }

            Console.Write("New description (blank to skip): ");
            var desc = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(desc)) task.Description = desc;

            Console.Write("New deadline (yyyy-MM-dd HH:mm): ");
            if (DateTime.TryParse(Console.ReadLine(), out var newDeadline))
                task.Deadline = newDeadline;

            Console.Write("Mark as completed? (y/n): ");
            task.IsCompleted = Console.ReadLine()?.ToLower() == "y";

            await db.ExecuteAsync("UPDATE Tasks SET Description=@Description, Deadline=@Deadline, IsCompleted=@IsCompleted WHERE Id=@Id", task);
            Console.WriteLine("✅ Task updated.");
        }
        else Console.WriteLine("❌ Invalid ID.");
    }

    static async Task DeleteTask()
    {
        await ViewTasks();
        Console.Write("Task ID to delete: ");
        if (int.TryParse(Console.ReadLine(), out int id))
        {
            using var db = DbConnection;
            await db.ExecuteAsync("DELETE FROM Tasks WHERE Id = @id", new { id });
            Console.WriteLine("🗑️ Task deleted.");
        }
        else Console.WriteLine("❌ Invalid ID.");
    }

    static Task StartReminderThread()
    {
        return Task.Run(async () =>
        {
            var alerted = new HashSet<(int, string)>();
            while (true)
            {
                using var db = DbConnection;
                var tasks = (await db.QueryAsync<TaskItem>("SELECT * FROM Tasks WHERE IsCompleted = false")).ToList();

                foreach (var t in tasks)
                {
                    var left = t.Deadline - DateTime.Now;

                    if (left.TotalMinutes <= 60 && left.TotalMinutes > 55 && !alerted.Contains((t.Id, "1hr")))
                    {
                        Console.WriteLine($"\n🔔 Reminder: Task '{t.Description}' due in 1 hour.");
                        alerted.Add((t.Id, "1hr"));
                    }

                    if (left.TotalMinutes <= 5 && left.TotalMinutes > 0 && !alerted.Contains((t.Id, "5min")))
                    {
                        Console.WriteLine($"\n⚠️ Almost Due: '{t.Description}' in 5 mins.");
                        alerted.Add((t.Id, "5min"));
                    }

                    if (left.TotalSeconds <= 0 && !alerted.Contains((t.Id, "due")))
                    {
                        Console.WriteLine($"\n⏰ OVERDUE: '{t.Description}' is overdue!");
                        alerted.Add((t.Id, "due"));
                    }
                }

                await Task.Delay(30000); // 30 seconds
            }
        });
    }
}
