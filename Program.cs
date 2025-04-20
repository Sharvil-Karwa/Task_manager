using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class TaskItem
{
    public string Description { get; set; }
    public DateTime Deadline { get; set; }
    public bool IsCompleted { get; set; } = false;
    public bool Reminded1Hr { get; set; } = false;
    public bool Reminded5Min { get; set; } = false;
    public bool FinalAlert { get; set; } = false;
}

class Program
{
    static List<TaskItem> tasks = new List<TaskItem>();
    static void Main(string[] args)
    {
        // Start the background reminder thread
        Thread reminderThread = new Thread(CheckReminders);
        reminderThread.Start();

        // Main user interaction loop
        while (true)
        {
            Console.WriteLine("\n==== Task Manager ====");
            Console.WriteLine("1. Add Task");
            Console.WriteLine("2. View Tasks");
            Console.WriteLine("3. Update Task");
            Console.WriteLine("4. Delete Task");
            Console.WriteLine("5. Exit");
            Console.Write("Choose an option: ");
            string input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    AddTask();
                    break;
                case "2":
                    ViewTasks();
                    break;
                case "3":
                    UpdateTask();
                    break;
                case "4":
                    DeleteTask();
                    break;
                case "5":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid option. Try again.");
                    break;
            }
        }
    }

    static void AddTask()
    {
        Console.Write("Enter task description: ");
        string desc = Console.ReadLine();

        Console.Write("Enter deadline (yyyy-MM-dd HH:mm): ");
        if (DateTime.TryParse(Console.ReadLine(), out DateTime deadline))
        {
            tasks.Add(new TaskItem { Description = desc, Deadline = deadline });
            Console.WriteLine("Task added successfully.");
        }
        else
        {
            Console.WriteLine("Invalid date format.");
        }
    }

    static void ViewTasks()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine("No tasks yet.");
            return;
        }

        for (int i = 0; i < tasks.Count; i++)
        {
            var t = tasks[i];
            Console.WriteLine($"{i + 1}. {t.Description} | Deadline: {t.Deadline} | Status: {(t.IsCompleted ? "✅ Completed" : "❌ Not Completed")}");
        }
    }

    static void UpdateTask()
    {
        ViewTasks();
        Console.Write("Enter task number to update: ");
        if (int.TryParse(Console.ReadLine(), out int index) && index >= 1 && index <= tasks.Count)
        {
            TaskItem t = tasks[index - 1];

            Console.Write("Enter new description (leave blank to keep current): ");
            string desc = Console.ReadLine();
            if (!string.IsNullOrEmpty(desc))
                t.Description = desc;

            Console.Write("Enter new deadline (yyyy-MM-dd HH:mm) or leave blank: ");
            string dateInput = Console.ReadLine();
            if (!string.IsNullOrEmpty(dateInput) && DateTime.TryParse(dateInput, out DateTime newDeadline))
                t.Deadline = newDeadline;

            Console.Write("Mark as completed? (y/n): ");
            string complete = Console.ReadLine();
            t.IsCompleted = complete.Trim().ToLower() == "y";

            Console.WriteLine("Task updated.");
        }
        else
        {
            Console.WriteLine("Invalid task number.");
        }
    }

    static void DeleteTask()
    {
        ViewTasks();
        Console.Write("Enter task number to delete: ");
        if (int.TryParse(Console.ReadLine(), out int index) && index >= 1 && index <= tasks.Count)
        {
            tasks.RemoveAt(index - 1);
            Console.WriteLine("Task deleted.");
        }
        else
        {
            Console.WriteLine("Invalid task number.");
        }
    }

    static void CheckReminders()
    {
        while (true)
        {
            foreach (var t in tasks)
            {
                if (t.IsCompleted) continue;

                TimeSpan timeLeft = t.Deadline - DateTime.Now;

                if (!t.Reminded1Hr && timeLeft.TotalMinutes <= 60 && timeLeft.TotalMinutes > 55)
                {
                    Console.WriteLine($"\n🔔 Reminder: Task '{t.Description}' is due in 1 hour.");
                    t.Reminded1Hr = true;
                }
                else if (!t.Reminded5Min && timeLeft.TotalMinutes <= 5 && timeLeft.TotalMinutes > 0)
                {
                    Console.WriteLine($"\n🔔 Reminder: Task '{t.Description}' is due in 5 minutes.");
                    t.Reminded5Min = true;
                }
                else if (!t.FinalAlert && timeLeft.TotalSeconds <= 0)
                {
                    Console.WriteLine($"\n⏰ ALERT: Task '{t.Description}' was NOT completed on time!");
                    t.FinalAlert = true;
                }
            }

            Thread.Sleep(30000); // Check every 30 seconds
        }
    }
}
