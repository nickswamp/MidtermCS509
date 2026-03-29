using System;
using MySql.Data.MySqlClient;

namespace ATMConsoleApp
{
    class Program
    {
        // IMPORTANT: Change "YourPasswordHere" to the root password you created!
        static string connectionString = "Server=localhost;Database=ATM_System;Uid=root;Pwd=password;";
        static int currentAccountId = -1;
        static string currentUserRole = "";

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Welcome to the ATM System ===");
                
                if (Login())
                {
                    if (currentUserRole == "Customer")
                    {
                        CustomerMenu();
                    }
                    else if (currentUserRole == "Administrator")
                    {
                        AdminMenu();
                    }
                }
                else
                {
                    Console.WriteLine("\nInvalid Login or PIN. Press any key to try again.");
                    Console.ReadKey();
                }
            }
        }

        static bool Login()
        {
            Console.Write("Enter login: ");
            string login = Console.ReadLine();
            
            Console.Write("Enter Pin code: ");
            string pin = Console.ReadLine();

            if (pin.Length != 5)
            {
                Console.WriteLine("Error: PIN must be exactly 5 digits.");
                return false;
            }

            try
            {
                // This connects to the database and checks if the user exists
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT AccountID, Role FROM Accounts WHERE Login = @login AND PinCode = @pin";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@pin", pin);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentAccountId = reader.GetInt32("AccountID");
                                currentUserRole = reader.GetString("Role");
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nDatabase Error: {ex.Message}");
            }
            return false;
        }

        static void CustomerMenu()
        {
            bool loggedIn = true;
            while (loggedIn)
            {
                Console.Clear();
                Console.WriteLine("=== Customer Menu ===");
                Console.WriteLine("1----Withdraw Cash");
                Console.WriteLine("3----Deposit Cash");
                Console.WriteLine("4----Display Balance");
                Console.WriteLine("5----Exit");
                Console.Write("Selection: ");
                
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("Withdrawal feature coming next...");
                        break;
                    case "3":
                        Console.WriteLine("Deposit feature coming soon...");
                        break;
                    case "4":
                        DisplayBalance();
                        break;
                    case "5":
                        loggedIn = false;
                        currentAccountId = -1;
                        currentUserRole = "";
                        break;
                    default:
                        Console.WriteLine("Invalid selection.");
                        break;
                }
                if (loggedIn)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static void AdminMenu()
        {
            bool loggedIn = true;
            while (loggedIn)
            {
                Console.Clear();
                Console.WriteLine("=== Administrator Menu ===");
                Console.WriteLine("1----Create New Account");
                Console.WriteLine("2----Delete Existing Account");
                Console.WriteLine("3----Update Account Information");
                Console.WriteLine("4----Search for Account");
                Console.WriteLine("6----Exit");
                Console.Write("Selection: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                        Console.WriteLine("Admin features coming soon...");
                        break;
                    case "6":
                        loggedIn = false;
                        currentAccountId = -1;
                        currentUserRole = "";
                        break;
                    default:
                        Console.WriteLine("Invalid selection.");
                        break;
                }
                if (loggedIn)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static void DisplayBalance()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Balance FROM Accounts WHERE AccountID = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", currentAccountId);
                        decimal balance = Convert.ToDecimal(cmd.ExecuteScalar());
                        
                        Console.WriteLine($"\nAccount #{currentAccountId}");
                        Console.WriteLine($"Date: {DateTime.Now:MM/dd/yyyy}");
                        Console.WriteLine($"Balance: {balance:N0}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
            }
        }
    }
}