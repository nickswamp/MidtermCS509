using System;
using MySql.Data.MySqlClient;

namespace ATMConsoleApp
{
    class Program
    {
        static string connectionString = "Server=localhost;Database=ATM_System;Uid=root;Pwd=password;";
        static int currentAccountId = -1;
        static string currentUserRole = "";

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Welcome to the ATM System");
                
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
                Console.WriteLine("Customer Menu");
                Console.WriteLine("1 - Withdraw Cash");
                Console.WriteLine("3 - Deposit Cash");
                Console.WriteLine("4 - Display Balance");
                Console.WriteLine("5 - Exit");
                Console.Write("Selection: ");
                
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        WithdrawCash();
                        break;
                    case "3":
                        DepositCash();
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
        static void WithdrawCash()
        {
            Console.Write("\nEnter the withdrawal amount: ");
            string input = Console.ReadLine();

            // Validate the input is a number and is greater than 0
            if (!decimal.TryParse(input, out decimal amount) || amount <= 0)
            {
                Console.WriteLine("Error: Please enter a valid positive number.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // 1. Check if they have enough money first
                    string checkQuery = "SELECT Balance FROM Accounts WHERE AccountID = @id";
                    decimal currentBalance = 0;
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", currentAccountId);
                        currentBalance = Convert.ToDecimal(checkCmd.ExecuteScalar());
                    }

                    if (amount > currentBalance)
                    {
                        Console.WriteLine("Error: Insufficient funds for this withdrawal.");
                        return;
                    }

                    // 2. Deduct the money from the database
                    string updateQuery = "UPDATE Accounts SET Balance = Balance - @amount WHERE AccountID = @id";
                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@amount", amount);
                        updateCmd.Parameters.AddWithValue("@id", currentAccountId);
                        updateCmd.ExecuteNonQuery();
                    }

                    // 3. Print the required receipt format
                    Console.WriteLine("Cash Successfully Withdrawn");
                    Console.WriteLine($"Account #{currentAccountId}");
                    Console.WriteLine($"Date: {DateTime.Now:MM/dd/yyyy}");
                    Console.WriteLine($"Withdrawn: {amount}");
                    Console.WriteLine($"Balance: {currentBalance - amount:N0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
            }
        }
        static void DepositCash()
        {
            Console.Write("\nEnter the cash amount to deposit: ");
            string input = Console.ReadLine();

            // Validate the input is a positive number
            if (!decimal.TryParse(input, out decimal amount) || amount <= 0)
            {
                Console.WriteLine("Error: Please enter a valid positive number.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // 1. Add the money to the database
                    string updateQuery = "UPDATE Accounts SET Balance = Balance + @amount WHERE AccountID = @id";
                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@amount", amount);
                        updateCmd.Parameters.AddWithValue("@id", currentAccountId);
                        updateCmd.ExecuteNonQuery();
                    }

                    // 2. Fetch the new updated balance to print on the receipt
                    string balanceQuery = "SELECT Balance FROM Accounts WHERE AccountID = @id";
                    decimal newBalance = 0;
                    using (MySqlCommand balanceCmd = new MySqlCommand(balanceQuery, conn))
                    {
                        balanceCmd.Parameters.AddWithValue("@id", currentAccountId);
                        newBalance = Convert.ToDecimal(balanceCmd.ExecuteScalar());
                    }

                    // 3. Print the exact receipt format requested
                    Console.WriteLine("Cash Deposited Successfully.");
                    Console.WriteLine($"Account #{currentAccountId}");
                    Console.WriteLine($"Date: {DateTime.Now:MM/dd/yyyy}");
                    Console.WriteLine($"Deposited: {amount}");
                    Console.WriteLine($"Balance: {newBalance:N0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
            }
        }
    }
}