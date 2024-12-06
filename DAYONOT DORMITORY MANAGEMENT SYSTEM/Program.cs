using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Diagnostics;
using static DORMITORY_MANAGEMENT_SYSTEM.RoomManagement;
using System.Runtime.CompilerServices;
using System.Globalization;
using Spectre.Console;

namespace DORMITORY_MANAGEMENT_SYSTEM
{
    public abstract class SetUP
    {
        public string DormitoryName { get; set; }
        public string DormitoryAddress { get; set; }
        public int Floors { get; set; }
        public int RoomsOnEachFloor { get; set; }
        public abstract void DisplayRoomsDetails();
    }
    public class Rooms : SetUP
    {
        public Rooms(string dormitoryName, string dormitoryAddress, int floors, int roomsOnEachFloor)
        {
            DormitoryAddress = dormitoryAddress;
            DormitoryName = dormitoryName;
            Floors = floors;
            RoomsOnEachFloor = roomsOnEachFloor;
        }
        public override void DisplayRoomsDetails()
        {
            AnsiConsole.MarkupLine("\n\t[gold1]Dormitory Information:[/]");
            AnsiConsole.MarkupLine("\n\t\t[Red]{0}[/]", DormitoryName);  
            AnsiConsole.MarkupLine("\t\t[Red]{0}[/]", DormitoryAddress); 
            AnsiConsole.MarkupLine("\t\t[Red]{0} Floors[/]", Floors);   
            AnsiConsole.MarkupLine("\t\t[Red]{0} Rooms per Floor[/]", RoomsOnEachFloor); 
        }
    }
    public abstract class Person
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public DateTime Birthday { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        protected Person(string userId, string firstName, string lastName, string address, DateTime birthday, string email, string phoneNumber)
        {
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Address = address;
            Birthday = birthday;
            Email = email;
            PhoneNumber = phoneNumber;
        }
    }
    public class Dormer : Person
    {
        public string RoomNo { get; set; }
        public double Payment { get; set; }
        public double RemainingBalance { get; set; }
        public DateTime EntryDate { get; set; }
        private RoomManagement _roomManagement;
        public Dormer(RoomManagement roomManagement, string userID, string firstName, string lastName, string address, DateTime birthday, string email, string phoneNumber, string roomNo, double payment, DateTime entryDate)
            : base(userID, firstName, lastName, address, birthday, email, phoneNumber)
        {
            _roomManagement = roomManagement;
            RoomNo = roomNo;
            Payment = payment;
            EntryDate = entryDate;
        }
        public string PaymentStatus
        {
            get
            {
                if (RemainingBalance <= 0)
                    return "Paid";

                DateTime nextDueDate = _roomManagement.CalculateNextDueDate(EntryDate);
                string dueDateFormatted = nextDueDate.ToString("MM/dd/yyyy");

                return DateTime.Now > nextDueDate
                    ? $"Late (Due Date(MM/DD/YYYY): {dueDateFormatted})"
                    : $"Due (Due Date(MM/DD/YYYY): {dueDateFormatted})";
            }
        }
    }
    public class RoomManagement
    {
        public List<Dormer> Dormers { get; set; } = new List<Dormer>();
        public Dictionary<string, string> RoomStatus { get; set; }
        public List<Payment> payments;
        public RoomManagement()
        {
            payments = new List<Payment>();
        }
        public RoomManagement(Dictionary<string, string> roomStatus)
        {
            RoomStatus = roomStatus;
            payments = new List<Payment>();
        }
        public class Payment
        {
            public string RoomNumber { get; set; }
            public double Amount { get; set; }
            public string Month { get; set; }
            public bool IsPaid { get; set; }
            public DateTime DueDate { get; set; }
            public string Status
            {
                get
                {
                    if (IsPaid) return "Paid";
                    return DateTime.Now <= DueDate ? "Due" : "Late";
                }
            }

            public Payment(string roomNumber, double amount, bool isPaid, string month, DateTime dueDate)
            {
                RoomNumber = roomNumber;
                Amount = amount;
                Month = month;
                IsPaid = isPaid; // By default, payment is not paid when created
                DueDate = dueDate;
            }
        }
        public void SearchDormer() // Method that allows the user to search for a dormer based on their assigned room number or name
        {
            Console.Clear();
            if (Dormers.Count == 0)
            {
                Console.WriteLine("No dormers in the system. Please add dormers first.");
                return;
            }
            var searchOption = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[gold1]\nSearch Dormer by:[/]")
                    .PageSize(5)
                    .AddChoices(new[]
                    {
                        "1. Room Number",
                        "2. Dormer's Name",
                    })
                    .HighlightStyle(new Style(foreground: Color.Red)));
            switch (searchOption)
            {
                case "1. Room Number":
                    SearchByRoomNo();
                    break;
                case "2. Dormer's Name":
                    SearchByName();
                    break;
                default:
                    break;
            }
        }
        private void SearchByRoomNo()
        {
            Console.Write("Enter Room Number: ");
            string searchRoomNo = Console.ReadLine();

            var searchedDormers = Dormers.Where(d => d.RoomNo.Equals(searchRoomNo, StringComparison.OrdinalIgnoreCase)).ToList();
            if (searchedDormers.Any())
            {
                DisplayDormersTable(searchedDormers);
            }
            else
            {
                Console.WriteLine("No dormer found with the Room Number: {0}", searchRoomNo);
            }
        }
        private void SearchByName()
        {
            Console.Write("Enter Dormer's Name: ");
            string searchName = Console.ReadLine();

            var searchedDormers = Dormers.Where(d =>
                d.FirstName.Contains(searchName, StringComparison.OrdinalIgnoreCase) ||
                d.LastName.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (searchedDormers.Any())
            {
                DisplayDormersTable(searchedDormers);
            }
            else
            {
                Console.WriteLine("No dormer found with the Name: {0}", searchName);
            }
        }
        public void UpdatePaymentByRoomNo(string roomNo, double paymentAmount, Rooms rooms, string dormersFilePath)
        {
            var dormer = Dormers.FirstOrDefault(d => d.RoomNo == roomNo);
            if (dormer != null)
            {
                dormer.RemainingBalance -= paymentAmount;
                if (dormer.RemainingBalance < 0) dormer.RemainingBalance = 0; // Prevent negative balance
                dormer.Payment = dormer.RemainingBalance;
                Program.SaveDormersToFile(Dormers, dormersFilePath);
                Console.WriteLine("Payment updated successfully!");
                DisplayReceipt(dormer, rooms, paymentAmount);
                Console.WriteLine("Remaining Balance for Room {0}: {1:F2}", roomNo, dormer.RemainingBalance);
                Console.WriteLine($"This is payment: " + dormer.Payment);
            }
            else
            {
                Console.WriteLine("No dormer found with Room Number: {0}", roomNo);
            }
        }
        public void UpdatePaymentForNextMonth(string roomNo, double amount, string dormersFilePath)
        {
            string nextMonth = GetNextMonth(); // Get the name of the next month
            var dormer = Dormers.FirstOrDefault(d => d.RoomNo == roomNo);

            DateTime nextDueDate = CalculateNextDueDate(dormer.EntryDate);
            bool paymentUpdated = false;
            var payment = payments.FirstOrDefault(p => p.RoomNumber.Equals(roomNo, StringComparison.OrdinalIgnoreCase) && p.Month == nextMonth);
            if (payment != null)
            {
                payment.Amount = amount; // Update the payment amount
                payment.DueDate = nextDueDate;
                payment.IsPaid = false; // Reset payment status if not already paid
                paymentUpdated = true;
                Console.WriteLine($"Payment for room {roomNo} for {nextMonth} updated to {amount} with a due date of {nextDueDate.ToShortDateString()}.");
            }
            else
            {
                // If the payment record doesn't exist, create a new payment entry
                payments.Add(new Payment(roomNo, amount, false, nextMonth, nextDueDate));
                Console.WriteLine($"New payment record created for room {roomNo} for {nextMonth} with amount {amount}.");
            }
            //var dormer = Dormers.FirstOrDefault(d => d.RoomNo == roomNo);
            if (!paymentUpdated)
            {
                dormer.RemainingBalance += amount; // Add to the remaining balance if payment is not paid
                dormer.Payment=dormer.RemainingBalance;
                dormer.EntryDate = nextDueDate;
                Console.WriteLine($"Remaining balance for Room {roomNo} updated to {dormer.RemainingBalance:F2}.");
            }
            else
            {
                //payments.Add(new Payment(roomNo, amount, false, nextMonth, nextDueDate));
                Console.WriteLine($"Remaining balance for Room {roomNo} remains unchanged at {dormer.RemainingBalance:F2}.");
            }
            Program.SaveDormersToFile(Dormers, dormersFilePath);
        }
        private string GetNextMonth()
        {
            // Get the current month and return the next month as a string
            DateTime now = DateTime.Now;
            return now.AddMonths(1).ToString("MMMM");
        }
        public DateTime CalculateNextDueDate(DateTime entryDate)
        {
            //DateTime currentDueDate = entryDate.AddMonths((DateTime.Now.Year - entryDate.Year) * 12 + DateTime.Now.Month - entryDate.Month);
            return entryDate.AddMonths(1);
        }
        public void DisplayReceipt(Dormer dormer, Rooms rooms, double amountPaid)
        {
            Console.Clear();
            Console.WriteLine("\t\t--- Receipt ---\n");
            AnsiConsole.MarkupLine($"[Red]Name of the Dormitory[/]: {rooms.DormitoryName}");
            AnsiConsole.MarkupLine($"[Red]Address of the Dormitory[/]: {rooms.DormitoryAddress}");
            AnsiConsole.MarkupLine($"[Red]Date(DD/MM/YYYY)[/]: {DateTime.Now.ToShortDateString()}\n");

            AnsiConsole.MarkupLine($"[Red]Name of the Dormer[/]: {dormer.FirstName} {dormer.LastName}");
            AnsiConsole.MarkupLine($"[Red]Address of the Dormer[/]: {dormer.Address}\n");

            //Console.WriteLine($"Month Payment: {GetNextMonth()}");
            //AnsiConsole.MarkupLine($"[Red]Payment to Pay[/]: {dormer.Payment:F2}");
            AnsiConsole.MarkupLine($"[Red]Amount Paid[/]: {amountPaid:F2}");
            AnsiConsole.MarkupLine($"[Red]Remaining Balance[/]: {dormer.RemainingBalance:F2}\n");

            AnsiConsole.MarkupLine("[Red]Thank you for your payment![/]");
            Console.WriteLine("\n----------------------------------------");
        }
        public void DisplayRoomTable() // New method to display room details in a table format
        {
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[Red]Room Number[/]");
            table.AddColumn("[Red]User-ID[/]");
            table.AddColumn("[Red]First Name[/]");
            table.AddColumn("[Red]Last Name[/]");
            table.AddColumn("[Red]Address[/]");
            table.AddColumn("[Red]Birthday[/]");
            table.AddColumn("[Red]Email[/]");
            table.AddColumn("[Red]Phone Number[/]");
            table.AddColumn("[Red]Room Status[/]");

            foreach (var room in RoomStatus)
            {
                var dormer = Dormers.FirstOrDefault(d => d.RoomNo == room.Key);

                if (dormer != null)
                {
                    table.AddRow(
                        room.Key,
                        dormer.UserId,
                        dormer.FirstName,
                        dormer.LastName,
                        dormer.Address,
                        dormer.Birthday.ToShortDateString(),
                        dormer.Email,
                        dormer.PhoneNumber,
                        room.Value
                    );
                }
                else
                {
                    table.AddRow(
                        room.Key,
                        "-",
                        "-",
                        "-",
                        "-",
                        "-",
                        "-",
                        "-",
                        room.Value
                    );
                }
            }
            AnsiConsole.Clear();
            AnsiConsole.Write(table);
        }
        public void DisplayDormersTable(List<Dormer> dormers) // Method that displays details in a structured, table-like format.
        {
            var table = new Table();

            // Add columns with specific widths
            table.AddColumn("[Red]ID Number[/]");
            table.AddColumn("[Red]First Name[/]");
            table.AddColumn("[Red]Last Name[/]");
            table.AddColumn("[Red]Address[/]");
            table.AddColumn("[Red]Birthday[/]");
            table.AddColumn("[Red]Email[/]");
            table.AddColumn("[Red]PhoneNumber[/]");
            table.AddColumn("[Red]Room No[/]");
            table.AddColumn("[Red]Remaining Balance[/]");

            // Add rows dynamically from dormers
            foreach (var dormer in dormers)
            {
                table.AddRow(
                    dormer.UserId,
                    dormer.FirstName,
                    dormer.LastName,
                    dormer.Address,
                    dormer.Birthday.ToShortDateString(),
                    dormer.Email,
                    dormer.PhoneNumber,
                    dormer.RoomNo,
                    dormer.Payment.ToString("F2")
                );
            }

            AnsiConsole.Clear();
            AnsiConsole.Write(table);
        }
        public void DisplayPaymentTable()
        {
            var table = new Table();

            
            table.AddColumn("[Red]Room No[/]");
            table.AddColumn("[Red]ID Number[/]");
            table.AddColumn("[Red]First Name[/]");
            table.AddColumn("[Red]Last Name[/]");
            table.AddColumn("[Red]Remaining Balance[/]");
            table.AddColumn("[Red]Payment Status[/]");

            
            foreach (var dormer in Dormers)
            {
                table.AddRow(
                    dormer.RoomNo,
                    dormer.UserId,
                    dormer.FirstName,
                    dormer.LastName,
                    dormer.RemainingBalance.ToString("F2"),
                    dormer.PaymentStatus
                );
            }

            AnsiConsole.Clear();
            AnsiConsole.Write(table);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            // Specify the file path for the dormitory setup data
            string setupFilePath = "setup.txt";
            string roomDataFilePath = "roomStatus.txt";
            string dormerDataFilePath = "dormers.txt";
            string paymentsDataFilePath = "paymentStatus.txt";

            var roomStatus = LoadRoomData(roomDataFilePath);
            RoomManagement roomManagement = new RoomManagement(roomStatus)
            {
                //Dormers = dormers
            };
            //var roomManagement = new RoomManagement(roomStatus);
            List<Dormer> dormers = new List<Dormer>();//List of dormers
            LoadDormersFromFile(dormerDataFilePath, roomManagement);

            SaveRoomData(roomDataFilePath, roomStatus);
            SaveDormersToFile(roomManagement.Dormers, dormerDataFilePath);

            AnsiConsole.MarkupLine("\n\t[gold1]Welcome to Dormitory Management System[/]");
            string dormName, dormAddress;
            int floors, roomsPerFloor;
            if (File.Exists(setupFilePath) && new FileInfo(setupFilePath).Length > 0)
            {
                LoadExistingSetup(setupFilePath, out dormName, out dormAddress, out floors, out roomsPerFloor);
            }
            else
            {
                Console.Write("Enter Dormitory Name: ");
                dormName = Console.ReadLine();

                Console.Write("Enter Dormitory Address: ");
                dormAddress = Console.ReadLine();

                Console.Write("Enter Number of Floors: ");
                while (!int.TryParse(Console.ReadLine(), out floors) || floors < 0)
                {
                    Console.Write("Invalid input. Enter number of Floors: ");
                }
                Console.Write("Enter Number of Rooms per Floor: ");
                while (!int.TryParse(Console.ReadLine(), out roomsPerFloor) || roomsPerFloor < 0)
                {
                    Console.Write("Invalid input. Enter number of Rooms per Floor: ");
                }
                SaveSetupDetails(setupFilePath, dormName, dormAddress, floors, roomsPerFloor);
                Console.WriteLine("Dormitory setup completed!");
            }

            Rooms rooms = new Rooms(dormName, dormAddress, floors, roomsPerFloor);
            rooms.DisplayRoomsDetails();

            int totalRooms = floors * roomsPerFloor;
            int availableRooms = totalRooms;

            if (roomStatus.Count == 0)
            {
                for (int i = 1; i <= floors; i++)
                {
                    for (int j = 1; j <= roomsPerFloor; j++)
                    {
                        string roomNumber = i.ToString() + j.ToString("00");
                        roomStatus[roomNumber] = "Vacant";
                    }
                }
                SaveRoomData(roomDataFilePath, roomStatus);
            }
            bool running = true;
            while (running)
            {
                Console.Clear();
                rooms.DisplayRoomsDetails();
                var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[gold1]\nDashboard: Choose an option[/]")
                    .PageSize(5)
                    .AddChoices(new[]
                    {
                        "1. Manage Rooms",
                        "2. Manage Dormers",
                        "3. Manage Payments",
                        "4. Reset",
                        "5. Exit the System"
                    })
                    .HighlightStyle(new Style(foreground: Color.Red)));
                switch (choice)
                {
                    case "1. Manage Rooms":
                        ManageRooms(roomStatus, ref availableRooms, roomManagement, roomDataFilePath, dormerDataFilePath);
                        break;
                    case "2. Manage Dormers":
                        ManageDormers(roomManagement, dormerDataFilePath);
                        break;
                    case "3. Manage Payments":
                        ManagePayments(roomManagement, rooms, paymentsDataFilePath);
                        break;
                    case "4. Reset":
                        ResetDormitorySetup(setupFilePath, roomDataFilePath, dormerDataFilePath, paymentsDataFilePath);
                        running = false;
                        break;
                    case "5. Exit the System":
                        Console.WriteLine("Exiting System...");
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice, please try again.");
                        break;
                }
            }
        }
        static void LoadExistingSetup(string filePath, out string dormName, out string dormAddress, out int floors, out int roomsPerFloor)
        {
            dormName = string.Empty;
            dormAddress = string.Empty;
            floors = 0;
            roomsPerFloor = 0;
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    dormName = reader.ReadLine();
                    dormAddress = reader.ReadLine();
                    floors = int.Parse(reader.ReadLine());
                    roomsPerFloor = int.Parse(reader.ReadLine());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured");
            }
        }
        static void SaveSetupDetails(string filePath, string dormName, string dormAddress, int floors, int roomsPerFloor)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(dormName);
                    writer.WriteLine(dormAddress);
                    writer.WriteLine(floors);
                    writer.WriteLine(roomsPerFloor);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured");
            }
        }
        public static void ResetDormitorySetup(string setupFilePath, string roomDataFilePath, string dormerDataFilePath, string paymentsDataFilePath)
        {
            Console.Write("Do you want to reset the dormitory setup? (y/n): ");
            char resetChoice = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (resetChoice == 'y' || resetChoice == 'Y')
            {
                // Delete files if they exist
                if (File.Exists(setupFilePath)) File.Delete(setupFilePath);
                if (File.Exists(roomDataFilePath)) File.Delete(roomDataFilePath);
                if (File.Exists(dormerDataFilePath)) File.Delete(dormerDataFilePath);
                if (File.Exists(paymentsDataFilePath)) File.Delete(paymentsDataFilePath);

                // Recreate empty files
                File.Create(setupFilePath).Dispose();
                File.Create(roomDataFilePath).Dispose();
                File.Create(dormerDataFilePath).Dispose();
                File.Create(paymentsDataFilePath).Dispose();

                Console.WriteLine("Dormitory setup has been reset.");
            }
        }
        // Method to load existing setup data from the file (optional implementation)
        static void ManageRooms(Dictionary<string, string> roomStatus, ref int availableRooms, RoomManagement roomManagement, string roomDataFilePath, string dormersDataFilePath)
        //Method that allows the user to assign rooms to dormers and upate the room availability status.
        {
            Console.Clear();
            bool managingRooms = true;
            while (managingRooms)
            {
                var roomChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[gold1]\nManage Rooms[/]")
                    .PageSize(3)
                    .AddChoices(new[]
                    {
                        "1. View All Rooms",
                        "2. Delete Room Data",
                        "3. Return to Main Menu",
                    })
                    .HighlightStyle(new Style(foreground: Color.Red)));
                switch (roomChoice)
                {
                    case "1. View All Rooms":
                        roomManagement.DisplayRoomTable();
                        break;
                    case "2. Delete Room Data":
                        Console.Write("Enter Room Number to vacate (e.g., 101): ");
                        string roomNumber = Console.ReadLine();

                        if (roomStatus.ContainsKey(roomNumber) && roomStatus[roomNumber] == "Occupied")
                        {
                            roomStatus[roomNumber] = "Vacant";
                            availableRooms++;
                            Console.WriteLine("Room {0} vacated successfully!", roomNumber);
                            SaveRoomData(roomDataFilePath, roomStatus); // Save data immediately after change
                            RemoveDormerFromFile(dormersDataFilePath, roomNumber, roomManagement);
                        }
                        else
                        {
                            Console.WriteLine("Room {0} is either vacant or does not exist.", roomNumber);
                        }
                        break;
                    case "3. Return to Main Menu":
                        managingRooms = false;
                        Console.WriteLine("Returning to main menu...");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Returning to main menu.");
                        break;
                }
            }
        }
        //static void LoadDormersFromFile(string dormerDataFilePath, Dictionary<string, string> roomStatus, List<Dormer> dormers)
        static Dictionary<string, string> LoadRoomData(string filePath)
        {
            var roomStatus = new Dictionary<string, string>();

            if (File.Exists(filePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split(',');
                            if (parts.Length == 2)
                            {
                                roomStatus[parts[0]] = parts[1];
                            }
                        }
                    }
                    //Console.WriteLine("Room data loaded successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load the file");
                }
            }
            else
            {
                Console.WriteLine("Room data file not found. Starting with empty data.");
            }

            return roomStatus;
        }
        static void SaveRoomData(string filePath, Dictionary<string, string> roomStatus)
        {
            //Console.WriteLine($"Saving room data to {filePath}...");
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    foreach (var room in roomStatus)
                    {
                        writer.WriteLine($"{room.Key},{room.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error had occured");
            }
        }
        static void RemoveDormerFromFile(string dormersFilePath, string roomNo, RoomManagement roomManagement)
        {
            var linesToKeep = new List<string>();

            // Read and filter lines using StreamReader
            using (StreamReader reader = new StreamReader(dormersFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith(roomNo + ",")) // Keep lines that don't match the roomNo
                    {
                        linesToKeep.Add(line);
                    }
                }
            }

            // Write the filtered lines back to the file using StreamWriter
            using (StreamWriter writer = new StreamWriter(dormersFilePath))
            {
                foreach (string line in linesToKeep)
                {
                    writer.WriteLine(line);
                }
            }
            var dormerToRemove = roomManagement.Dormers.FirstOrDefault(d => d.RoomNo == roomNo);
            if (dormerToRemove != null)
            {
                roomManagement.Dormers.Remove(dormerToRemove);
            }
            Console.WriteLine("Dormer data associated with Room {0} has been removed.", roomNo);
        }
        static void LoadDormersFromFile(string filePath, RoomManagement roomManagement)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split(',');
                            if (parts.Length == 10)
                            {
                                string roomNo = parts[0];
                                string userId = parts[1];
                                string firstName = parts[2];
                                string lastName = parts[3];
                                string address = parts[4];
                                DateTime birthday = DateTime.ParseExact(parts[5], "MM/dd/yyyy", CultureInfo.InvariantCulture);
                                string email = parts[6];
                                string phoneNumber = parts[7];
                                double payment = double.Parse(parts[8]);
                                DateTime entryDate = DateTime.ParseExact(parts[9], "MM/dd/yyyy", CultureInfo.InvariantCulture);

                                var dormer = new Dormer(roomManagement, userId, firstName, lastName, address, birthday, email, phoneNumber, roomNo, payment, entryDate)
                                {
                                    RemainingBalance = payment
                                };
                                roomManagement.Dormers.Add(dormer);
                                roomManagement.RoomStatus[roomNo] = "Occupied"; // Set room status to occupied
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read the file: {ex.Message}");
                }
                //Console.WriteLine("Dormer data loaded successfully.");
            }
            else
            {
                Console.WriteLine("Dormer data file not found.");
            }
        }
        public static void SaveDormersToFile(List<Dormer> dormers, string dormersFilePath)
        {
            using (StreamWriter writer = new StreamWriter(dormersFilePath))
            {
                foreach (var dormer in dormers)
                {
                    writer.WriteLine($"{dormer.RoomNo},{dormer.UserId},{dormer.FirstName},{dormer.LastName},{dormer.Address},{dormer.Birthday:MM/dd/yyyy},{dormer.Email},{dormer.PhoneNumber},{dormer.Payment},{dormer.EntryDate:MM/dd/yyyy}");
                }
            }
        }
        static int CalculateAvailableRooms(Dictionary<string, string> roomStatus)
        {
            int availableRooms = 0;
            foreach (var status in roomStatus.Values)
            {
                if (status == "Vacant")
                {
                    availableRooms++;
                }
            }
            return availableRooms;
        }
        static void ManageDormers(RoomManagement roomManagement, string dormersDataFilePath)
        {
            Console.Clear();
            bool managingDormers = true;
            while (managingDormers)
            {
                var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[gold1]\nManage Dormers[/]")
                    .PageSize(3)
                    .AddChoices(new[]
                    {
                        "1. View All Dormer",
                        "2. Add Dormer",
                        "3. Search Dormer",
                        "4. Update Dormer Information",
                        "5. Return to Main Menu",
                    })
                    .HighlightStyle(new Style(foreground: Color.Red)));
                switch (choice)
                {
                    case "1. View All Dormer":
                        roomManagement.DisplayRoomTable();
                        break;
                    case "2. Add Dormer":
                        Console.Clear();
                        Console.WriteLine("Add Dormer Information");

                        Console.Write("Enter ID number: ");
                        string userID = Console.ReadLine();

                        Console.Write("Enter First Name: ");
                        string firstName = Console.ReadLine();

                        Console.Write("Enter Last Name: ");
                        string lastName = Console.ReadLine();

                        Console.Write("Enter Address: ");
                        string address = Console.ReadLine();

                        Console.Write("Enter Birthday (MM/DD/YYYY): ");
                        DateTime birthday;
                        while (!DateTime.TryParseExact(Console.ReadLine(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out birthday))
                        {
                            Console.Write("Invalid date format. Enter Birthday (MM/DD/YYYY): ");
                        }

                        Console.Write("Enter Email: ");
                        string email = Console.ReadLine();

                        Console.Write("Enter Phone Number: ");
                        string phoneNumber = Console.ReadLine();

                        Console.Write("Enter RoomNo (e.g., 101): ");
                        string roomNo = Console.ReadLine();
                        if (roomManagement.RoomStatus.ContainsKey(roomNo) && roomManagement.RoomStatus[roomNo] == "Vacant")
                        {
                            roomManagement.RoomStatus[roomNo] = "Occupied";  // Update room status to occupied
                            Console.WriteLine("Room {0} assigned successfully!", roomNo);
                        }
                        else
                        {
                            Console.WriteLine("Room {0} is either occupied or does not exist.", roomNo);
                            break;
                        }

                        Console.Write("Enter Starting Amount: ");
                        double payment;
                        while (!double.TryParse(Console.ReadLine(), out payment) || payment < 0)
                        {
                            Console.Write("Invalid input. Enter Payment Amount: ");
                        }

                        Console.Write("Enter Entry Date (MM/DD/YYYY): ");
                        DateTime entryDate;
                        while (!DateTime.TryParseExact(Console.ReadLine(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out entryDate))
                        {
                            Console.Write("Invalid date format. Enter Entry Date (MM/DD/YYYY): ");
                        }
                        // Create a new Dormer instance and add to the list
                        Dormer newDormer = new Dormer(roomManagement, userID, firstName, lastName, address, birthday, email, phoneNumber, roomNo, payment, entryDate);
                        newDormer.RemainingBalance = newDormer.Payment;
                        roomManagement.Dormers.Add(newDormer);

                        Console.WriteLine("Dormer added successfully!");
                        SaveDormersToFile(roomManagement.Dormers, dormersDataFilePath);
                        //roomManagement.AddDormer();
                        break;
                    case "3. Search Dormer":
                        roomManagement.SearchDormer();
                        break;
                    case "4. Update Dormer Information":
                        Console.Write("Enter the Room Number of the dormer to update (e.g., 101): ");
                        string roomNumber = Console.ReadLine();

                        // Find the dormer by Room No
                        Dormer dormerToUpdate = roomManagement.Dormers.FirstOrDefault(d => d.RoomNo == roomNumber);
                        if (dormerToUpdate != null)
                        {
                            Console.WriteLine("Dormer found. Enter new details or press Enter to keep current values.");

                            // Update dormer details
                            Console.Write("First Name (current: " + dormerToUpdate.FirstName + "): ");
                            firstName = Console.ReadLine();
                            if (!string.IsNullOrEmpty(firstName))
                            {
                                dormerToUpdate.FirstName = firstName;
                            }

                            Console.Write("Last Name (current: " + dormerToUpdate.LastName + "): ");
                            lastName = Console.ReadLine();
                            if (!string.IsNullOrEmpty(lastName))
                            {
                                dormerToUpdate.LastName = lastName;
                            }
                            Console.Write("Address (current: " + dormerToUpdate.Address + "): ");
                            address = Console.ReadLine();
                            if (!string.IsNullOrEmpty(address))
                            {
                                dormerToUpdate.Address = address;
                            }

                            Console.Write("Email (current: " + dormerToUpdate.Email + "): ");
                            email = Console.ReadLine();
                            if (!string.IsNullOrEmpty(email))
                            {
                                dormerToUpdate.Email = email;
                            }
                            Console.Write("Phone Number (current: " + dormerToUpdate.PhoneNumber + "): ");
                            phoneNumber = Console.ReadLine();
                            if (!string.IsNullOrEmpty(phoneNumber))
                            {
                                dormerToUpdate.PhoneNumber = phoneNumber;
                            }
                            Console.WriteLine("Dormer information updated successfully.");
                        }
                        else
                        {
                            Console.WriteLine("No dormer found with Room No: " + roomNumber);
                        }
                        SaveDormersToFile(roomManagement.Dormers, dormersDataFilePath);
                        break;
                    case "5. Return to Main Menu":
                        managingDormers = false; // Exit the loop to return to the main menu
                        Console.WriteLine("Returning to main menu...");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Returning to main menu.");
                        break;
                }
            }
        }
        static void ManagePayments(RoomManagement roomManagement, Rooms rooms, string paymentsDataFilePath)
        {
            Console.Clear();
            bool managingPayments = true;
            LoadPaymentsFromFile(paymentsDataFilePath, roomManagement);

            while (managingPayments)
            {
                var paymentChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[gold1]\nManage Payments[/]")
                    .PageSize(3)
                    .AddChoices(new[]
                    {
                        "1. View Payments",
                        "2. Update Payment for This Month",
                        "3. Update Payment for Next Month",
                        "4. Return to Main Menu",
                    })
                    .HighlightStyle(new Style(foreground: Color.Red)));
                string dormersFilePath = "dormers.txt";

                switch (paymentChoice)
                {
                    case "1. View Payments":
                        roomManagement.DisplayPaymentTable();
                        break;
                    case "2. Update Payment for This Month":
                        Console.Write("Enter Room Number: ");
                        string roomNoThisMonth = Console.ReadLine();
                        Console.Write("Enter Payment Amount: ");
                        double amountThisMonth;
                        while (!double.TryParse(Console.ReadLine(), out amountThisMonth) || amountThisMonth < 0)
                        {
                            Console.Write("Invalid input. Enter Payment Amount: ");
                        }
                        roomManagement.UpdatePaymentByRoomNo(roomNoThisMonth, amountThisMonth, rooms, dormersFilePath);
                        SavePaymentsToFile(paymentsDataFilePath, roomManagement);
                        break;
                    case "3. Update Payment for Next Month":
                        Console.Write("Enter Room Number: ");
                        string roomNoNextMonth = Console.ReadLine();
                        Console.Write("Enter Payment Amount for Next Month: ");
                        double amountNextMonth;
                        while (!double.TryParse(Console.ReadLine(), out amountNextMonth) || amountNextMonth < 0)
                        {
                            Console.Write("Invalid input. Enter Payment Amount: ");
                        }
                        roomManagement.UpdatePaymentForNextMonth(roomNoNextMonth, amountNextMonth, dormersFilePath);
                        SavePaymentsToFile(paymentsDataFilePath, roomManagement);
                        break;
                    case "4. Return to Main Menu":
                        managingPayments = false;
                        Console.WriteLine("Returning to main menu...");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Returning to main menu.");
                        break;
                }
            }
        }
        static void LoadPaymentsFromFile(string paymentsDataFilePath, RoomManagement roomManagement)
        {
            if (File.Exists(paymentsDataFilePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(paymentsDataFilePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split(",");
                            if (parts.Length == 5)
                            {
                                string roomNumber = parts[0];
                                double amount = double.Parse(parts[1]);
                                string month = parts[2];
                                bool isPaid = bool.Parse(parts[3]);
                                DateTime dueDate = DateTime.Parse(parts[4]);

                                roomManagement.payments.Add(new RoomManagement.Payment(roomNumber, amount, isPaid, month, dueDate));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An erroer has occured" + ex.Message);
                }
            }
        }
        public static void SavePaymentsToFile(string paymentsDataFilePath, RoomManagement roomManagement)
        {
            List<string> lines = new List<string>();
            try
            {
                using (StreamWriter writer = new StreamWriter(paymentsDataFilePath))
                //using (StreamWriter writer = new StreamWriter(paymentDataFilePath))
                {
                    foreach (var payment in roomManagement.payments)
                    {
                        lines.Add($"{payment.RoomNumber},{payment.Amount},{payment.Month},{payment.IsPaid},{payment.DueDate}");
                    }
                    //File.WriteAllLines(paymentDataFilePath, lines);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Faield to write to the file");
            }
        }
    }
}
