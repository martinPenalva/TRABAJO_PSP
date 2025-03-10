using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.Services
{
    public class AppointmentNotification
    {
        public string Type { get; set; } = "notification";
        public string Action { get; set; } = "";
        public object Data { get; set; } = null!;
    }

    public class AppointmentNotificationService
    {
        private static readonly ConcurrentDictionary<string, TcpClient> _connectedClients 
            = new ConcurrentDictionary<string, TcpClient>();
        
        private static TcpListener _listener;
        private const int PORT = 11000;
        private static bool _isRunning = false;

        public static void StartServer()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            Task.Run(async () => await StartServerAsync());
        }

        private static async Task StartServerAsync()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, PORT);
                _listener.Start();
                Console.WriteLine($"Notification server started. Listening on port {PORT}.");

                while (true)
                {
                    // Accept a client connection asynchronously
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    
                    // Generate a new GUID for each connection
                    string clientId = Guid.NewGuid().ToString();

                    Console.WriteLine($"Client connected. Assigned ID: {clientId}");

                    // Add the client to our connected clients
                    _connectedClients[clientId] = client;

                    // Handle the new client in a separate task, passing the unique ID
                    _ = Task.Run(() => HandleClientAsync(client, clientId));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationServer] Exception: {ex}");
                _isRunning = false;
            }
        }

        private static async Task HandleClientAsync(TcpClient client, string clientId)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Just keep the connection alive, no need to handle incoming messages
                    // in this notification service
                    byte[] buffer = new byte[1024];
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) != 0)
                    {
                        // We don't actually need to do anything with client messages
                        // This is just to detect when clients disconnect
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationServer] Exception handling client {clientId}: {ex}");
            }
            finally
            {
                // Clean up
                _connectedClients.TryRemove(clientId, out _);
                client.Close();
                Console.WriteLine($"Client {clientId} disconnected.");
            }
        }

        public static async Task NotifyAppointmentCreatedAsync(object appointmentData)
        {
            await NotifyAllClientsAsync("created", appointmentData);
        }

        public static async Task NotifyAppointmentUpdatedAsync(object appointmentData)
        {
            await NotifyAllClientsAsync("updated", appointmentData);
        }

        public static async Task NotifyAppointmentDeletedAsync(object appointmentData)
        {
            await NotifyAllClientsAsync("deleted", appointmentData);
        }

        private static async Task NotifyAllClientsAsync(string action, object data)
        {
            var notification = new AppointmentNotification
            {
                Action = action,
                Data = data
            };

            string json = JsonSerializer.Serialize(notification) + "\n";
            byte[] messageBytes = Encoding.UTF8.GetBytes(json);

            var deadClients = new List<string>();

            foreach (var clientPair in _connectedClients)
            {
                try
                {
                    if (clientPair.Value.Connected)
                    {
                        NetworkStream stream = clientPair.Value.GetStream();
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    }
                    else
                    {
                        deadClients.Add(clientPair.Key);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending to client {clientPair.Key}: {ex.Message}");
                    deadClients.Add(clientPair.Key);
                }
            }

            // Clean up any dead clients
            foreach (var clientId in deadClients)
            {
                _connectedClients.TryRemove(clientId, out var client);
                client?.Close();
            }
        }
    }
} 