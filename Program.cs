using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using System.Timers;

[Serializable]
public class TaskItem
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public DateTime Deadline { get; set; }
    public bool IsCompleted { get; set; }

    [XmlIgnore] public bool Reminded1Hr { get; set; }
    [XmlIgnore] public bool Reminded5Min { get; set; }
    [XmlIgnore] public bool RemindedDue { get; set; }
}

class Program
{
    static List<TaskItem> tasks = new();
    static string filePath = "tasks.xml";
    static System.Timers.Timer reminderTimer;
    static int nextId = 1;

    static void Main()
    {
        LoadTasks();
        StartReminderTimer();

        while (true)
        {
            Console.WriteLine("\n=== Task Manager ===");
            Console.WriteLine("1. Add Task");
            Console.WriteLine("2. View Tasks");
            Console.WriteLine("3. Update Task");
            Console.WriteLine("4. Delete Task");
            Console.WriteLine("5. Exit");
            Console.Write("Choose: ");

            switch (Console.ReadLine())
            {
                case "1": AddTask(); break;
                case "2": ViewTasks(); break;
                case "3": UpdateTask(); break;
                case "4": DeleteTask(); break;
                case "5": SaveTasks(); return;
                default: Console.WriteLine("Invalid choice."); break;
            }
        }
    }

    static void LoadTasks()
    {
        if (File.Exists(filePath))
        {
            var serializer = new XmlSerializer(typeof(List<TaskItem>));
            using var reader = new StreamReader(filePath);
            tasks = (List<TaskItem>)serializer.Deserialize(reader);
            if (tasks.Any())
                nextId = tasks.Max(t => t.Id) + 1;
        }
    }

    static void SaveTasks()
    {
        var serializer = new XmlSerializer(typeof(List<TaskItem>));
        using var writer = new StreamWriter(filePath);
        serializer.Serialize(writer, tasks);
    }

    static void AddTask()
    {
        Console.Write("Description: ");
        var desc = Console.ReadLine() ?? "";

        Console.Write("Deadline (yyyy-MM-dd HH:mm): ");
        if (DateTime.TryParse(Console.ReadLine(), out var deadline) && deadline > DateTime.Now)
        {
            var task = new TaskItem
            {
                Id = nextId++,
                Description = desc,
                Deadline = deadline,
                IsCompleted = false
            };
            tasks.Add(task);
            SaveTasks();
            Console.WriteLine("Task added.");
        }
        else Console.WriteLine("Invalid or past deadline.");
    }

    static void ViewTasks()
    {
        if (!tasks.Any()) Console.WriteLine("No tasks yet.");
        else
        {
            foreach (var t in tasks)
                Console.WriteLine($"{t.Id}. {t.Description} | Due: {t.Deadline} | {(t.IsCompleted ? "Done" : "Pending")}");
        }
    }

    static void UpdateTask()
    {
        ViewTasks();
        Console.Write("Task ID to update: ");
        if (int.TryParse(Console.ReadLine(), out int id))
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) { Console.WriteLine("Task not found."); return; }

            Console.Write("New description (blank to skip): ");
            var desc = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(desc)) task.Description = desc;

            Console.Write("New deadline (yyyy-MM-dd HH:mm): ");
            if (DateTime.TryParse(Console.ReadLine(), out var newDeadline) && newDeadline > DateTime.Now)
                task.Deadline = newDeadline;

            Console.Write("Mark as completed? (y/n): ");
            task.IsCompleted = Console.ReadLine()?.ToLower() == "y";

            SaveTasks();
            Console.WriteLine("Task updated.");
        }
        else Console.WriteLine("Invalid ID.");
    }

    static void DeleteTask()
    {
        ViewTasks();
        Console.Write("Task ID to delete: ");
        if (int.TryParse(Console.ReadLine(), out int id))
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                tasks.Remove(task);
                SaveTasks();
                Console.WriteLine("Task deleted.");
            }
            else Console.WriteLine("Task not found.");
        }
        else Console.WriteLine("Invalid ID.");
    }

    static void StartReminderTimer()
    {
        reminderTimer = new System.Timers.Timer(30000); // 30 sec
        reminderTimer.Elapsed += async (sender, e) =>
        {
            foreach (var task in tasks.Where(t => !t.IsCompleted))
            {
                var left = task.Deadline - DateTime.Now;

                if (left.TotalMinutes <= 60 && left.TotalMinutes > 55 && !task.Reminded1Hr)
                {
                    Console.WriteLine($"\nReminder: Task '{task.Description}' due in 1 hour.");
                    task.Reminded1Hr = true;
                }

                if (left.TotalMinutes <= 5 && left.TotalMinutes > 0 && !task.Reminded5Min)
                {
                    Console.WriteLine($"\nAlmost Due: '{task.Description}' in 5 mins.");
                    task.Reminded5Min = true;
                }

                if (left.TotalSeconds <= 0 && !task.RemindedDue)
                {
                    Console.WriteLine($"\nOVERDUE: '{task.Description}' is overdue!");
                    task.RemindedDue = true;

                    Console.Write($"Snooze '{task.Description}'? Enter new deadline (yyyy-MM-dd HH:mm) or wait 15s for auto-snooze: ");
                    string? input = null;

                    var inputThread = new Thread(() => { input = Console.ReadLine(); });
                    inputThread.Start();

                    var success = inputThread.Join(15000); // wait max 15s
                    if (success && DateTime.TryParse(input, out var newDeadline) && newDeadline > DateTime.Now)
                    {
                        task.Deadline = newDeadline;
                        task.Reminded1Hr = false;
                        task.Reminded5Min = false;
                        task.RemindedDue = false;
                        Console.WriteLine("Snoozed.");
                    }
                    else
                    {
                        task.Deadline = DateTime.Now.AddHours(1);
                        task.Reminded1Hr = false;
                        task.Reminded5Min = false;
                        task.RemindedDue = false;
                        Console.WriteLine("Auto-snoozed to 1 hour later.");
                    }
                }
            }

            SaveTasks();
        };

        reminderTimer.AutoReset = true;
        reminderTimer.Start();
    }
}
