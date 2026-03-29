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
                        CreateAccount();
                        break;
                    case "2":
                        DeleteAccount();
                        break;
                    case "3":
                        UpdateAccount();
                        break;
                    case "4":
                        SearchAccount();
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
        static void CreateAccount()
        {
            Console.WriteLine("\n--- Create New Account ---");
            
            Console.Write("Login: ");
            string login = Console.ReadLine();

            Console.Write("Pin Code: ");
            string pin = Console.ReadLine();

            // Validate the pin is exactly 5 characters AND is a number
            if (pin.Length != 5 || !int.TryParse(pin, out _))
            {
                Console.WriteLine("Error: PIN must be an integer of exactly 5 digits.");
                return;
            }

            Console.Write("Holders Name: ");
            string name = Console.ReadLine();

            Console.Write("Starting Balance: ");
            string balanceInput = Console.ReadLine();
            
            // Validate the balance is a valid number
            if (!decimal.TryParse(balanceInput, out decimal balance) || balance < 0)
            {
                Console.WriteLine("Error: Invalid balance amount.");
                return;
            }

            Console.Write("Status (Active/Disabled): ");
            string status = Console.ReadLine();
            
            // Validate status spelling
            if (status != "Active" && status != "Disabled")
            {
                Console.WriteLine("Error: Status must be exactly 'Active' or 'Disabled'.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // Insert the new account into the database
                    string insertQuery = @"
                        INSERT INTO Accounts (Login, PinCode, Role, HolderName, Balance, Status) 
                        VALUES (@login, @pin, 'Customer', @name, @balance, @status)";
                        
                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@pin", pin);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@balance", balance);
                        cmd.Parameters.AddWithValue("@status", status);
                        
                        cmd.ExecuteNonQuery();
                        
                        // Grab the auto-generated Account ID to show on the receipt
                        long newId = cmd.LastInsertedId;
                        
                        Console.WriteLine($"\nAccount Successfully Created – the account number assigned is: {newId}");
                    }
                }
            }
            catch (Exception ex)
            {
                // This will catch things like trying to use a Login name that already exists
                Console.WriteLine($"\nDatabase Error: {ex.Message}");
            }
        }
        static void DeleteAccount()
        {
            Console.Write("\nEnter the account number to which you want to delete: ");
            string idInput = Console.ReadLine();

            if (!int.TryParse(idInput, out int accountId))
            {
                Console.WriteLine("Error: Invalid account number format.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. Look up the account to get the Holder's Name
                    // We also ensure we only delete Customers, not other Administrators
                    string findQuery = "SELECT HolderName FROM Accounts WHERE AccountID = @id AND Role = 'Customer'";
                    string holderName = null;

                    using (MySqlCommand findCmd = new MySqlCommand(findQuery, conn))
                    {
                        findCmd.Parameters.AddWithValue("@id", accountId);
                        object result = findCmd.ExecuteScalar();
                        
                        if (result != null)
                        {
                            holderName = result.ToString();
                        }
                    }

                    // If result is null, the account doesn't exist
                    if (holderName == null)
                    {
                        Console.WriteLine("Error: Customer account not found.");
                        return;
                    }

                    // 2. Ask for confirmation 
                    Console.WriteLine($"You wish to delete the account held by {holderName}.");
                    Console.Write("If this information is correct, please re-enter the account number: ");
                    string confirmInput = Console.ReadLine();

                    if (confirmInput != idInput)
                    {
                        Console.WriteLine("Account numbers do not match. Deletion cancelled.");
                        return;
                    }

                    // 3. Actually delete the account from the database
                    string deleteQuery = "DELETE FROM Accounts WHERE AccountID = @id";
                    using (MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", accountId);
                        deleteCmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("Account Deleted Successfully"); // 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
            }
        }
        static void UpdateAccount()
        {
            Console.Write("\nEnter the Account Number: "); // [cite: 63]
            string idInput = Console.ReadLine();

            if (!int.TryParse(idInput, out int accountId))
            {
                Console.WriteLine("Error: Invalid account number.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // 1. Fetch the current account data
                    string fetchQuery = "SELECT HolderName, Balance, Status, Login, PinCode FROM Accounts WHERE AccountID = @id AND Role = 'Customer'";
                    
                    string currentHolder = "", currentStatus = "", currentLogin = "", currentPin = "";
                    decimal currentBalance = 0;

                    using (MySqlCommand fetchCmd = new MySqlCommand(fetchQuery, conn))
                    {
                        fetchCmd.Parameters.AddWithValue("@id", accountId);
                        using (MySqlDataReader reader = fetchCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                Console.WriteLine("Error: Customer account not found.");
                                return;
                            }

                            // Store the current values
                            currentHolder = reader.GetString("HolderName");
                            currentBalance = reader.GetDecimal("Balance");
                            currentStatus = reader.GetString("Status");
                            currentLogin = reader.GetString("Login");
                            currentPin = reader.GetString("PinCode");
                        } // Reader closes here so we can run an UPDATE command next
                    }

                    Console.WriteLine($"\nAccount # {accountId}"); // [cite: 65]
                    Console.WriteLine("(Press Enter to keep the current value, or type a new one to update it)");

                    // 2. Prompt for new values one by one
                    Console.Write($"Holder ({currentHolder}): "); // [cite: 66]
                    string newHolder = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newHolder)) newHolder = currentHolder;

                    Console.Write($"Balance ({currentBalance}): "); // [cite: 67]
                    string balanceInput = Console.ReadLine();
                    decimal newBalance = currentBalance;
                    if (!string.IsNullOrWhiteSpace(balanceInput)) decimal.TryParse(balanceInput, out newBalance);

                    Console.Write($"Status ({currentStatus}): "); // [cite: 68]
                    string newStatus = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newStatus)) newStatus = currentStatus;

                    Console.Write($"Login ({currentLogin}): "); // [cite: 69]
                    string newLogin = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newLogin)) newLogin = currentLogin;

                    Console.Write($"Pin Code ({currentPin}): "); // [cite: 70]
                    string newPin = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newPin)) newPin = currentPin;

                    // 3. Save the changes back to the database
                    string updateQuery = @"
                        UPDATE Accounts 
                        SET HolderName = @name, Balance = @balance, Status = @status, Login = @login, PinCode = @pin 
                        WHERE AccountID = @id";
                        
                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@name", newHolder);
                        updateCmd.Parameters.AddWithValue("@balance", newBalance);
                        updateCmd.Parameters.AddWithValue("@status", newStatus);
                        updateCmd.Parameters.AddWithValue("@login", newLogin);
                        updateCmd.Parameters.AddWithValue("@pin", newPin);
                        updateCmd.Parameters.AddWithValue("@id", accountId);
                        
                        updateCmd.ExecuteNonQuery();
                    }
                    
                    Console.WriteLine("\nAccount Updated Successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
            }
        }
        static void SearchAccount()
        {
            Console.Write("\nEnter Account number: "); // [cite: 74]
            string idInput = Console.ReadLine();

            if (!int.TryParse(idInput, out int accountId))
            {
                Console.WriteLine("Error: Invalid account number.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // Fetch the account data. We ensure we only search for Customers.
                    string fetchQuery = "SELECT HolderName, Balance, Status, Login, PinCode FROM Accounts WHERE AccountID = @id AND Role = 'Customer'";
                    
                    using (MySqlCommand fetchCmd = new MySqlCommand(fetchQuery, conn))
                    {
                        fetchCmd.Parameters.AddWithValue("@id", accountId);
                        using (MySqlDataReader reader = fetchCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                Console.WriteLine("Error: Customer account not found.");
                                return;
                            }

                            // Print the exact format requested by the requirements
                            Console.WriteLine("\nThe account information is:"); // [cite: 75]
                            Console.WriteLine($"Account # {accountId}"); // [cite: 76]
                            Console.WriteLine($"Holder: {reader.GetString("HolderName")}"); // [cite: 77]
                            Console.WriteLine($"Balance: {reader.GetDecimal("Balance"):N0}"); // [cite: 78]
                            Console.WriteLine($"Status: {reader.GetString("Status")}"); // [cite: 79]
                            Console.WriteLine($"Login: {reader.GetString("Login")}"); // [cite: 80]
                            Console.WriteLine($"Pin Code: {reader.GetString("PinCode")}"); // [cite: 81]
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
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