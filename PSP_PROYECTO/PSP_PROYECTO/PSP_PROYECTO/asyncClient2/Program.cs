using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

public class Reservation
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int NumberOfGuests { get; set; }
    public string? SpecialRequests { get; set; }
    public int TableNumber { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}

public class ReservationNotification
{
    public string Type { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
}

public class RestaurantClient
{
    private const string API_BASE_URL = "http://localhost:5021/api/Reservations";
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task StartClientAsync()
    {
        try
        {
            await DisplayMenuAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client] Error: {ex.Message}");
        }
    }

    private async Task DisplayMenuAsync()
    {
        bool exit = false;

        while (!exit)
        {
            Console.WriteLine("\n===== Restaurant Reservation System =====");
            Console.WriteLine("1. View all reservations");
            Console.WriteLine("2. View available tables");
            Console.WriteLine("3. Make a reservation");
            Console.WriteLine("4. View reservation details");
            Console.WriteLine("5. Cancel a reservation");
            Console.WriteLine("6. Exit");
            Console.Write("\nEnter option: ");

            string option = Console.ReadLine() ?? "";

            switch (option)
            {
                case "1":
                    await ViewAllReservationsAsync();
                    break;
                case "2":
                    await ViewAvailableTablesAsync();
                    break;
                case "3":
                    await MakeReservationAsync();
                    break;
                case "4":
                    await ViewReservationDetailsAsync();
                    break;
                case "5":
                    await CancelReservationAsync();
                    break;
                case "6":
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Invalid option, please try again.");
                    break;
            }
        }
    }

    private async Task ViewAllReservationsAsync()
    {
        try
        {
            Console.WriteLine("\nFetching all reservations...");
            var reservations = await _httpClient.GetFromJsonAsync<List<Reservation>>(API_BASE_URL);

            if (reservations == null || reservations.Count == 0)
            {
                Console.WriteLine("No reservations found.");
                return;
            }

            Console.WriteLine("\n--- All Reservations ---");
            foreach (var reservation in reservations.OrderBy(r => r.ReservationDateTime))
            {
                Console.WriteLine($"ID: {reservation.Id} | {reservation.CustomerName} | Table {reservation.TableNumber} | {reservation.ReservationDateTime:g} | {reservation.NumberOfGuests} guests");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching reservations: {ex.Message}");
        }
    }

    private async Task ViewAvailableTablesAsync()
    {
        try
        {
            Console.Write("Enter date (YYYY-MM-DD): ");
            string dateInput = Console.ReadLine() ?? DateTime.Today.ToString("yyyy-MM-dd");

            if (!DateTime.TryParse(dateInput, out DateTime date))
            {
                Console.WriteLine("Invalid date format. Using today's date.");
                date = DateTime.Today;
            }

            string url = $"{API_BASE_URL}/Available/{date:yyyy-MM-dd}";
            var availableTables = await _httpClient.GetFromJsonAsync<List<int>>(url);

            if (availableTables == null || availableTables.Count == 0)
            {
                Console.WriteLine("No available tables for the selected date.");
                return;
            }

            Console.WriteLine($"\n--- Available Tables for {date:yyyy-MM-dd} ---");
            foreach (var table in availableTables.OrderBy(t => t))
            {
                Console.WriteLine($"Table {table}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching available tables: {ex.Message}");
        }
    }

    private async Task MakeReservationAsync()
    {
        try
        {
            var reservation = new Reservation();

            Console.Write("Customer name: ");
            reservation.CustomerName = Console.ReadLine() ?? "";

            Console.Write("Phone number: ");
            reservation.PhoneNumber = Console.ReadLine() ?? "";

            Console.Write("Date (YYYY-MM-DD): ");
            string dateInput = Console.ReadLine() ?? "";

            Console.Write("Time (HH:MM): ");
            string timeInput = Console.ReadLine() ?? "";

            string dateTimeString = $"{dateInput} {timeInput}";
            if (!DateTime.TryParse(dateTimeString, out DateTime reservationDateTime))
            {
                Console.WriteLine("Invalid date/time format.");
                return;
            }

            reservation.ReservationDateTime = reservationDateTime;

            Console.Write("Number of guests: ");
            if (!int.TryParse(Console.ReadLine(), out int guests) || guests < 1 || guests > 20)
            {
                Console.WriteLine("Invalid number of guests (1-20).");
                return;
            }
            reservation.NumberOfGuests = guests;

            Console.Write("Table number: ");
            if (!int.TryParse(Console.ReadLine(), out int table) || table < 1 || table > 20)
            {
                Console.WriteLine("Invalid table number (1-20).");
                return;
            }
            reservation.TableNumber = table;

            Console.Write("Special requests (optional): ");
            reservation.SpecialRequests = Console.ReadLine();

            reservation.IsConfirmed = true;

            var response = await _httpClient.PostAsJsonAsync(API_BASE_URL, reservation);

            if (response.IsSuccessStatusCode)
            {
                var createdReservation = await response.Content.ReadFromJsonAsync<Reservation>();
                Console.WriteLine($"\nReservation made successfully! ID: {createdReservation?.Id}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to make reservation: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error making reservation: {ex.Message}");
        }
    }

    private async Task ViewReservationDetailsAsync()
    {
        try
        {
            Console.Write("Enter reservation ID: ");
            if (!long.TryParse(Console.ReadLine(), out long id))
            {
                Console.WriteLine("Invalid ID format.");
                return;
            }

            var reservation = await _httpClient.GetFromJsonAsync<Reservation>($"{API_BASE_URL}/{id}");

            if (reservation == null)
            {
                Console.WriteLine("Reservation not found.");
                return;
            }

            Console.WriteLine("\n--- Reservation Details ---");
            Console.WriteLine($"ID: {reservation.Id}");
            Console.WriteLine($"Customer: {reservation.CustomerName}");
            Console.WriteLine($"Phone: {reservation.PhoneNumber}");
            Console.WriteLine($"Date/Time: {reservation.ReservationDateTime:g}");
            Console.WriteLine($"Table: {reservation.TableNumber}");
            Console.WriteLine($"Guests: {reservation.NumberOfGuests}");
            Console.WriteLine($"Special Requests: {reservation.SpecialRequests ?? "None"}");
            Console.WriteLine($"Status: {(reservation.IsConfirmed ? "Confirmed" : "Pending")}");
            Console.WriteLine($"Created: {reservation.CreatedAt:g}");
            if (reservation.LastModifiedAt.HasValue)
            {
                Console.WriteLine($"Last Modified: {reservation.LastModifiedAt:g}");
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            Console.WriteLine("Reservation not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching reservation details: {ex.Message}");
        }
    }

    private async Task CancelReservationAsync()
    {
        try
        {
            Console.Write("Enter reservation ID to cancel: ");
            if (!long.TryParse(Console.ReadLine(), out long id))
            {
                Console.WriteLine("Invalid ID format.");
                return;
            }

            var response = await _httpClient.DeleteAsync($"{API_BASE_URL}/{id}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Reservation cancelled successfully!");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("Reservation not found.");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to cancel reservation: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cancelling reservation: {ex.Message}");
        }
    }
}

class Program
{
    public static async Task Main(string[] args)
    {
        var client = new RestaurantClient();
        await client.StartClientAsync();
    }
}
