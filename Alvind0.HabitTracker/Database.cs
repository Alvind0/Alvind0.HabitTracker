﻿using System.Globalization;
using Microsoft.Data.Sqlite;
using Spectre.Console;

namespace Alvind0.HabitTracker;

internal class Database
{
    private const string ConnectionString = @"Data Source=Habit-Tracker.db";
    internal record WalkingRecord(int Id, string Habit, DateTime Date, int Quantity, string Unit);
    internal record HabitTypes(string Habit, string Unit);

    internal static void CreateDatabase()
    {
        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
        {
            using (SqliteCommand tableCmd = connection.CreateCommand())
            {
                connection.Open();

                // TODO: Seed data automatically on DB creation
                tableCmd.CommandText =
                    @"CREATE TABLE IF NOT EXISTS Habits(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Habit TEXT,
                    Date TEXT,
                    Quantity INTEGER,
                    Unit TEXT
                    )";
                tableCmd.ExecuteNonQuery();

                tableCmd.CommandText =
                    @"CREATE TABLE IF NOT EXISTS HabitTypes(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Habit TEXT,
                    Unit TEXT
                    )";
                tableCmd.ExecuteNonQuery();
            }
        }
    }

    internal static void AddRecord()
    {
        string habit;
        GetHabits();
        while (true)
        { 
            habit = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter which habit to add a record to insert 0 to go back to Main Menu:\n"));
            if (habit == "0")
            {
                Console.Clear();
                return;
            }
            else if (!CheckIfHabitExists(habit))
            {
                Console.Clear();
                Console.WriteLine("Habit does not exist.");
                GetHabits();
                continue;
            }
            break;
        }

        var date = GetDate("\nEnter the date (format - dd-MM-yy) or insert 0 to go back to Main Menu:\n");
        if (date == null)
        {
            Console.Clear();
            return;
        }

        var quantity = GetNumber("\nEnter quantity or enter 0 to go back to Main Menu:\n");
        if (quantity == 0)
        {
            Console.Clear();
            return;
        }

        Console.Clear();

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var tableCmd = connection.CreateCommand();

            tableCmd.CommandText = @"
INSERT INTO Habits(habit, date, quantity) VALUES(@habit, @date, @quantity);
UPDATE Habits SET Unit = (
SELECT HabitTypes.Unit FROM HabitTypes WHERE HabitTypes.Habit = Habits.Habit);";

            Console.Clear();
            tableCmd.Parameters.AddWithValue("@habit", habit);
            tableCmd.Parameters.AddWithValue("@date", date);
            tableCmd.Parameters.AddWithValue("@quantity", quantity);
            tableCmd.ExecuteNonQuery();
        }
        Console.Clear();
        Console.WriteLine("Action completed successfully..");
    }

    internal static void DeleteRecord()
    {
        Console.Clear();
        int id = 0;
        while (true)
        {
            GetRecords();
            id = GetNumber("Please type the id of the record you want to delete or insert 0 to Go Back to Main Menu:\n");
            if (id == 0)
            {
                Console.Clear();
                return;
            }
            if (CheckIfIdExists(id)) break;
            else
            {
                Console.Clear();
                Console.WriteLine("Id does not exist.");
            }
        }

        using (var connection = new SqliteConnection(ConnectionString))
        {
            using (var tableCmd = connection.CreateCommand())
            {
                connection.Open();

                tableCmd.CommandText = @$"DELETE FROM Habits WHERE Id = @id";
                tableCmd.Parameters.AddWithValue("@id", id);
                tableCmd.ExecuteNonQuery();
            }
        }
        Console.Clear();
        Console.WriteLine("Action completed successfully..");
    }

    internal static void UpdateRecord()
    {
        Console.Clear();
        int id = 0;
        while (true)
        {
            GetRecords();
            id = GetNumber("Please type the id of the record you want to update or insert 0 to Go Back to Main Menu:\n");
            if (id == 0)
            {
                Console.Clear();
                return;
            }
            if (CheckIfIdExists(id)) break;
            else
            {
                Console.Clear();
                Console.WriteLine("Id does not exist.");
            }
        }
        string habit;
        bool updateHabit = AnsiConsole.Confirm("Update habit type?");
        if (updateHabit)
        {
            habit = AnsiConsole.Ask<string>("Enter the habit or input 0 to go back to main menu");
            if (habit == "0")
            {
                Console.Clear();
                return;
            }
        }

        string date = "";
        bool updateDate = AnsiConsole.Confirm("Update date?");
        if (updateDate)
        {
            date = GetDate("\nEnter the date (format : dd-mm-yy) or insert 0 to Go Back to Main Menu:\n");
            if (date == null) return;
        }

        int quantity = 0;
        bool updateQuantity = AnsiConsole.Confirm("Update quantity?");
        if (updateQuantity)
        {
            quantity = GetNumber("\nPlease enter number of meters walked (no decimals or negatives allowed) or enter 0 to go back to Main Menu.");
        }


        Console.Clear();

        string query = "";

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var tableCmd = connection.CreateCommand();

            int flags = (updateHabit ? 1 : 0)
                      | (updateDate ? 2 : 0)
                      | (updateQuantity ? 4 : 0);

            switch (flags)
            {
                case 0:
                    Console.WriteLine("No updates needed.");
                    return;
                case 1:
                    query = "UPDATE Habits SET habit = @habit WHERE Id = @id";
                    break;
                case 2:
                    query = "UPDATE Habits SET Date = @date WHERE Id = @id";
                    break;
                case 3:
                    query = "UPDATE Habits SET habit = @habit, Date = @date WHERE Id = @id";
                    break;
                case 4:
                    query = "UPDATE Habits SET Quantity = @quantity WHERE Id = @id";
                    break;
                case 5:
                    query = "UPDATE Habits SET habit = @habit, Quantity = @quantity WHERE Id = @id";
                    break;
                case 6:
                    query = "UPDATE Habits SET Date = @date, Quantity = @quantity WHERE Id = @id";
                    break;
                case 7:
                    query = "UPDATE Habits SET habit = @habit, Date = @date, Quantity = @quantity WHERE Id = @id";
                    break;
            }

            tableCmd.CommandText = query;
            tableCmd.Parameters.AddWithValue("@date", date);
            tableCmd.Parameters.AddWithValue("@quantity", quantity);
            tableCmd.Parameters.AddWithValue("@id", id);
            tableCmd.ExecuteNonQuery();
        }

        Console.Clear();
        Console.WriteLine("Action completed successfully..");
    }



    internal static void AddHabit()
    {
        string habit, unit;

        while (true)
        {
            GetHabits();
            habit = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter the habit or input 0 to go back to main menu"));
            if (habit == "0") return;

            if (CheckIfHabitExists(habit))
            {
                Console.Clear();
                Console.WriteLine("Habit already exists.");
                continue;
            }

            unit = AnsiConsole.Ask<string>("Enter the unit of measurement or input 0 to go back to main menu\n");
            if (unit == "0") return;
            else break;
        }

        using (SqliteConnection connection = new(ConnectionString))
        {
            using (SqliteCommand tableCmd = connection.CreateCommand())
            {
                connection.Open();
                tableCmd.CommandText = $"INSERT INTO HabitTypes(Habit, Unit) VALUES (@habit, @unit)";

                tableCmd.Parameters.AddWithValue("@habit", habit);
                tableCmd.Parameters.AddWithValue("@unit", unit);
                tableCmd.ExecuteNonQuery();
            }
        }
    }

    internal static void DeleteHabit()
    {
        string habit;

        while (true)
        {
            GetHabits();
            habit = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter the habit or input 0 to go back to main menu"));
            if (habit == "0") return;

            if (!CheckIfHabitExists(habit))
            {
                Console.Clear();
                Console.WriteLine("Habit does not exist");
                continue;
            }
            break;
        }

        var isSure = AnsiConsole.Confirm($"Are you sure? This will delete all entries of [italic]{habit}[/]");
        if (!isSure)
        {
            Console.Clear();
            return;
        }

        using (SqliteConnection connection = new(ConnectionString))
        {
            using (SqliteCommand tableCmd = connection.CreateCommand())
            {
                connection.Open();
                tableCmd.CommandText = @"
DELETE FROM HabitTypes WHERE Habit = @habit;
DELETE FROM Habits WHERE Habit = @habit";

                tableCmd.ExecuteNonQuery();
            }
        }

        Console.Clear();
        Console.WriteLine("Action completed successfully..");
    }

    internal static bool CheckIfIdExists(int id)
    {
        using (SqliteConnection connection = new(ConnectionString))
        {
            connection.Open();
            using (SqliteCommand tableCmd = connection.CreateCommand())
            {
                tableCmd.CommandText = $"SELECT count(1) FROM walkingHabit WHERE Id = @id";
                tableCmd.Parameters.AddWithValue("@id", id);
                return Convert.ToInt64(tableCmd.ExecuteScalar()) == 1 ? true : false;
            }

        }
    }

    internal static bool CheckIfHabitExists(string habit)
    {
        using (SqliteConnection connection = new(ConnectionString))
        {
            using (SqliteCommand tableCmd = connection.CreateCommand())
            {
                connection.Open();
                tableCmd.CommandText = $"SELECT 1 FROM HabitTypes WHERE Habit = @habit";
                tableCmd.Parameters.AddWithValue("@habit", habit);
                return Convert.ToInt64(tableCmd.ExecuteScalar()) == 1 ? true : false;
            }
        }
    }

    internal static void ViewHabits(List<HabitTypes> habits)
    {
        if (habits.Count == 0)
        {
            Console.WriteLine("No habits yet!");
            return;
        }

        var table = new Table();
        table.Border = TableBorder.Minimal;
        table.AddColumn(new TableColumn("[bold]Habits[/]"));
        table.AddColumn(new TableColumn("[bold]Unit[/]"));

        foreach (var habit in habits)
        {
            table.AddRow(habit.Habit, habit.Unit);
        }
        AnsiConsole.Write(table);
    }

    internal static void GetHabits()
    {
        List<HabitTypes> habitTypes = new();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            using (SqliteCommand tableCmd = connection.CreateCommand())
            {
                tableCmd.CommandText = "SELECT Habit, Unit FROM HabitTypes";

                using (SqliteDataReader reader = tableCmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())

                            habitTypes.Add(
                                new HabitTypes(
                                    reader.GetString(0),
                                    reader.GetString(1)
                                ));


                    }
                    else Console.WriteLine("No rows found.");
                }

            }

            ViewHabits(habitTypes);
        }
    }

        

    internal static void ViewRecords(List<WalkingRecord> records)
    {
        var table = new Table();
        table.Border = TableBorder.Square;
        table.AddColumn(new TableColumn("[bold]Id[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Habit[/]"));
        table.AddColumn(new TableColumn("[bold]Date[/]"));
        table.AddColumn(new TableColumn("[bold]Amount[/]"));
        table.AddColumn(new TableColumn("[bold]Unit[/]"));

        foreach (var record in records)
        {
            table.AddRow(record.Id.ToString(), record.Habit, record.Date.ToShortDateString(), record.Quantity.ToString("N0"), record.Unit);
        }
        AnsiConsole.Write(table);

    }

    internal static void GetRecords(bool isFromMenu = false)
    {
        List<WalkingRecord> records = new List<WalkingRecord>();

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            var tableCmd = connection.CreateCommand();
            tableCmd.CommandText = $"SELECT * FROM Habits";

            using (SqliteDataReader reader = tableCmd.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        try
                        {
                            records.Add(
                                new WalkingRecord(
                                    reader.GetInt32(0),
                                    reader.GetString(1),
                                    DateTime.ParseExact(reader.GetString(2), "dd-MM-yy", new CultureInfo("en-US")),
                                    reader.GetInt32(3),
                                    reader.GetString(4)
                                ));
                        }
                        catch (FormatException ex)
                        {
                            Console.WriteLine($"Error parsing data: {ex.Message}. Skipping this record.");
                        }
                    }
                }
                else Console.WriteLine("No rows found.");
            }
        }
        if (isFromMenu) Console.Clear();
        ViewRecords(records);
    }

    internal static string GetDate(string message)
    {
        Console.WriteLine(message);

        string? dateInput = Console.ReadLine();

        if (dateInput == "0") return String.Empty;

        while (!DateTime.TryParseExact(dateInput, "dd-MM-yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            Console.Clear();
            Console.WriteLine("Invalid date (format dd-MM-yy). Please try again.\n");
            dateInput = Console.ReadLine();
        }

        return dateInput;
    }

    private static int GetNumber(string message)
    {
        Console.WriteLine(message);

        string? numberInput = Console.ReadLine();
        int output;
        while (!int.TryParse(numberInput, out output) || Convert.ToInt32(numberInput) < 0)
        {
            Console.Clear();
            Console.WriteLine("Invalid number (positive integers only). Please try again.\n");
            numberInput = Console.ReadLine();
        }
        return output;
    }


}
