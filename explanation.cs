using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using System.Timers;

// TaskItem class represents a task with all relevant properties.
[Serializable]  // This attribute makes the class serializable so it can be saved to or loaded from a file (tasks.xml).
public class TaskItem
{
    public int Id { get; set; }  // Unique identifier for the task.
    public string Description { get; set; } = "";  // Description of the task.
    public DateTime Deadline { get; set; }  // Deadline for the task.
    public bool IsCompleted { get; set; }  // Whether the task is marked as completed.

    // These are not serialized in the XML (XmlIgnore attribute). They are used for reminder logic.
    [XmlIgnore] public bool Reminded1Hr { get; set; }
    [XmlIgnore] public bool Reminded5Min { get; set; }
    [XmlIgnore] public bool RemindedDue { get; set; }
}

class Program
{
    // List of tasks. This holds all the task objects.
    static List<TaskItem> tasks = new();
    // Path to the file where tasks are saved.
    static string filePath = "tasks.xml";
    // A timer for sending reminders about tasks.
    static System.Timers.Timer reminderTimer;
    // The next ID for a new task.
    static int nextId = 1;

    static void Main()
    {
        // Load tasks from the file when the program starts.
        LoadTasks();
        // Start the timer for reminders.
        StartReminderTimer();

        // Main loop for interacting with the user.
        while (true)
        {
            // Show the menu for task management.
            Console.WriteLine("\n=== Task Manager ===");
            Console.WriteLine("1. Add Task");
            Console.WriteLine("2. View Tasks");
            Console.WriteLine("3. Update Task");
            Console.WriteLine("4. Delete Task");
            Console.WriteLine("5. Exit");
            Console.Write("Choose: ");

            // Handle the user's choice based on their input.
            switch (Console.ReadLine())
            {
                case "1": AddTask(); break;  // Add a new task.
                case "2": ViewTasks(); break;  // View the list of tasks.
                case "3": UpdateTask(); break;  // Update an existing task.
                case "4": DeleteTask(); break;  // Delete a task.
                case "5": SaveTasks(); return;  // Exit the program and save tasks.
                default: Console.WriteLine("Invalid choice."); break;  // If the input is invalid, notify the user.
            }
        }
    }

    // Loads tasks from the XML file if it exists. If not, it initializes an empty list.
    static void LoadTasks()
    {
        if (File.Exists(filePath))
        {
            var serializer = new XmlSerializer(typeof(List<TaskItem>));
            using var reader = new StreamReader(filePath);
            tasks = (List<TaskItem>)serializer.Deserialize(reader);  // Deserialize the list of tasks from the XML file.
            if (tasks.Any())  // If there are any tasks, set the next ID.
                nextId = tasks.Max(t => t.Id) + 1;  // Get the highest ID and increment it for the next task.
        }
    }

    // Saves the current list of tasks to an XML file.
    static void SaveTasks()
    {
        var serializer = new XmlSerializer(typeof(List<TaskItem>));
        using var writer = new StreamWriter(filePath);
        serializer.Serialize(writer, tasks);  // Serialize and write the tasks to the file.
    }

    // Allows the user to add a new task.
    static void AddTask()
    {
        // Ask the user for a description.
        Console.Write("Description: ");
        var desc = Console.ReadLine() ?? "";  // Get the task description.

        // Ask the user for a deadline.
        Console.Write("Deadline (yyyy-MM-dd HH:mm): ");
        // Parse the deadline to ensure it's a valid DateTime.
        if (DateTime.TryParse(Console.ReadLine(), out var deadline) && deadline > DateTime.Now)
        {
            // Create a new TaskItem object with the provided description and deadline.
            var task = new TaskItem
            {
                Id = nextId++,  // Assign the next available ID.
                Description = desc,  // Set the description.
                Deadline = deadline,  // Set the deadline.
                IsCompleted = false  // Mark the task as not completed.
            };
            // Add the task to the list and save the tasks to the file.
            tasks.Add(task);
            SaveTasks();
            Console.WriteLine("Task added.");
        }
        else
        {
            Console.WriteLine("Invalid or past deadline.");  // If the deadline is invalid or in the past, show an error.
        }
    }

    // Allows the user to view all tasks.
    static void ViewTasks()
    {
        // If there are no tasks, show a message.
        if (!tasks.Any())
        {
            Console.WriteLine("No tasks yet.");
        }
        else
        {
            // Print each task's ID, description, deadline, and completion status.
            foreach (var t in tasks)
                Console.WriteLine($"{t.Id}. {t.Description} | Due: {t.Deadline} | {(t.IsCompleted ? "Done" : "Pending")}");
        }
    }

    // Allows the user to update an existing task.
    static void UpdateTask()
    {
        // First, show all tasks to the user.
        ViewTasks();
        Console.Write("Task ID to update: ");
        if (int.TryParse(Console.ReadLine(), out int id))
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);  // Find the task by ID.
            if (task == null)
            {
                Console.WriteLine("Task not found.");  // If the task doesn't exist, notify the user.
                return;
            }

            // Ask for a new description (or leave it blank to keep the old one).
            Console.Write("New description (blank to skip): ");
            var desc = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(desc)) task.Description = desc;  // If the description is not empty, update it.

            // Ask for a new deadline.
            Console.Write("New deadline (yyyy-MM-dd HH:mm): ");
            if (DateTime.TryParse(Console.ReadLine(), out var newDeadline) && newDeadline > DateTime.Now)
                task.Deadline = newDeadline;  // Update the deadline if it's valid.

            // Ask if the task is completed.
            Console.Write("Mark as completed? (y/n): ");
            task.IsCompleted = Console.ReadLine()?.ToLower() == "y";  // Set the task's completion status.

            SaveTasks();  // Save the updated task list.
            Console.WriteLine("Task updated.");
        }
        else
        {
            Console.WriteLine("Invalid ID.");  // If the input is not a valid number, notify the user.
        }
    }

    // Allows the user to delete a task by its ID.
    static void DeleteTask()
    {
        // Display the tasks and ask for the ID of the task to delete.
        ViewTasks();
        Console.Write("Task ID to delete: ");
        if (int.TryParse(Console.ReadLine(), out int id))
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);  // Find the task by ID.
            if (task != null)
            {
                tasks.Remove(task);  // Remove the task from the list.
                SaveTasks();  // Save the updated task list.
                Console.WriteLine("Task deleted.");
            }
            else
            {
                Console.WriteLine("Task not found.");  // If the task doesn't exist, show an error message.
            }
        }
        else
        {
            Console.WriteLine("Invalid ID.");  // If the input is not a valid number, notify the user.
        }
    }

    // Starts a timer to periodically check for task reminders.
    static void StartReminderTimer()
    {
        // Create a timer that fires every 30 seconds.
        reminderTimer = new System.Timers.Timer(30000); // 30 seconds
        reminderTimer.Elapsed += async (sender, e) =>
        {
            foreach (var task in tasks.Where(t => !t.IsCompleted))  // Loop through tasks that are not completed.
            {
                var left = task.Deadline - DateTime.Now;  // Calculate the remaining time until the deadline.

                // If the task is due in 1 hour and hasn't been reminded yet, remind the user.
                if (left.TotalMinutes <= 60 && left.TotalMinutes > 55 && !task.Reminded1Hr)
                {
                    Console.WriteLine($"\nReminder: Task '{task.Description}' due in 1 hour.");
                    task.Reminded1Hr = true;  // Mark as reminded for 1-hour warning.
                }

                // If the task is due in 5 minutes and hasn't been reminded yet, remind the user.
                if (left.TotalMinutes <= 5 && left.TotalMinutes > 0 && !task.Reminded5Min)
                {
                    Console.WriteLine($"\nAlmost Due: '{task.Description}' in 5 mins.");
                    task.Reminded5Min = true;  // Mark as reminded for 5-minute warning.
                }

                // If the task is overdue and hasn't been reminded yet, show an overdue warning.
                if (left.TotalSeconds <= 0 && !task.RemindedDue)
                {
                    Console.WriteLine($"\nOVERDUE: '{task.Description}' is overdue!");
                    task.RemindedDue = true;  // Mark as reminded for overdue warning.

                    // Allow the user to snooze the task by providing a new deadline.
                    Console.Write($"Snooze '{task.Description}'? Enter new deadline (yyyy-MM-dd HH:mm) or wait 15s for auto-snooze: ");
                    string? input = null;

                    var inputThread = new Thread(() => { input = Console.ReadLine(); });
                    inputThread.Start();  // Start a thread to allow the user to input within 15 seconds.

                    var success = inputThread.Join(15000); // Wait for input for 15 seconds.
                    if (success && !string.IsNullOrEmpty(input))
                    {
                        // If the user provides a new deadline, update it.
                        if (DateTime.TryParse(input, out var newDeadline))
                        {
                            task.Deadline = newDeadline;
                            task.RemindedDue = false;  // Reset the overdue reminder flag.
                            Console.WriteLine($"'{task.Description}' is snoozed until {newDeadline}.");
                        }
                        else
                        {
                            Console.WriteLine("Invalid deadline format.");
                        }
                    }
                    else
                    {
                        // Auto-snooze to 15 minutes later if no input.
                        task.Deadline = DateTime.Now.AddMinutes(15);
                        task.RemindedDue = false;
                        Console.WriteLine($"'{task.Description}' is auto-snoozed to {task.Deadline}.");
                    }
                }

                SaveTasks();  // Save tasks after any updates.
            }
        };

        reminderTimer.Start();  // Start the timer.
    }
}
