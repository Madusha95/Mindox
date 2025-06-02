using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerApp
{
    class ClientInfo
    {  // Class to hold information about a connected client
        public int Id { get; set; }
        public TcpClient TcpClient { get; set; }
        public NetworkStream Stream { get; set; }
        public string RemoteEndPoint { get; set; }
        public bool IsConnected { get; set; }
        public Thread ClientThread { get; set; }
    }

    class Program
    {
        private static Dictionary<int, ClientInfo> connectedClients = new Dictionary<int, ClientInfo>();
        private static int nextClientId = 1;
        private static readonly object clientsLock = new object();

        static void Main(string[] args)
        {
            // Start server in a separate thread
            Thread serverThread = new Thread(StartServer);
            serverThread.IsBackground = true;
            serverThread.Start();

            // Start console menu
            ShowMenu();
        }

        // Function to start the TCP server and listen for incoming client connections
        static void StartServer()
        {

            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Server started on port 5000, waiting for clients...");
            Console.WriteLine("========================================");

            while (true)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();

                    lock (clientsLock)
                    {
                        ClientInfo clientInfo = new ClientInfo
                        {
                            Id = nextClientId++,
                            TcpClient = client,
                            Stream = client.GetStream(),
                            RemoteEndPoint = client.Client.RemoteEndPoint.ToString(),
                            IsConnected = true
                        };

                        connectedClients[clientInfo.Id] = clientInfo;
                        Console.WriteLine($"\nNew client connected: ID {clientInfo.Id} - {clientInfo.RemoteEndPoint}");

                        // Start handling this client
                        Thread clientThread = new Thread(() => HandleClient(clientInfo));
                        clientInfo.ClientThread = clientThread;
                        clientThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        //Function to handle receiving messages from a connected client
        static void HandleClient(ClientInfo clientInfo)
        {
            byte[] buffer = new byte[1024];

            while (clientInfo.IsConnected)
            {
                try
                {
                    int bytesRead = clientInfo.Stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    string clientMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"\nMessage from Client {clientInfo.Id}: {clientMessage}");
                }
                catch
                {
                    break;
                }
            }

            // Cleanup when client disconnects
            DisconnectClient(clientInfo.Id);
        }

        // Function to display the main menu and handle user interactions
        static void ShowMenu()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("\n========================================");
                    Console.Write("To Search available clients: Yes or No? ");
                    string input = Console.ReadLine()?.Trim().ToLower();

                    if (input == "yes" || input == "y")
                    {
                        ShowAvailableClients();

                        if (GetConnectedClientsCount() > 0)
                        {
                            Console.Write("\nWhich client do you want to connect? Please enter number from list: ");
                            if (int.TryParse(Console.ReadLine(), out int clientId))
                            {
                                InteractWithClient(clientId);
                            }
                            else
                            {
                                Console.WriteLine("Invalid client ID entered.");
                            }
                        }
                    }
                    else if (input == "no" || input == "n")
                    {
                        Console.WriteLine("Continuing to wait for clients...");
                    }
                    else
                    {
                        Console.WriteLine("Please enter 'Yes' or 'No'.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in menu: {ex.Message}");
                }
            }
        }

        // Function to display all currently connected clients
        static void ShowAvailableClients()
        {
            lock (clientsLock)
            {
                Console.WriteLine("\n--- Available Clients ---");
                if (connectedClients.Count == 0)
                {
                    Console.WriteLine("No clients connected.");
                    return;
                }

                foreach (var client in connectedClients.Values)
                {
                    if (client.IsConnected)
                    {
                        Console.WriteLine($"{client.Id}. Client ID: {client.Id} - {client.RemoteEndPoint}");
                    }
                }
            }
        }

        // Function to get the count of currently connected clients
        static int GetConnectedClientsCount()
        {
            lock (clientsLock) // Lock the clients list to safely access shared data in multi-threaded environments
            {
                int count = 0;
                foreach (var client in connectedClients.Values)
                {
                    if (client.IsConnected) count++;
                }
                return count;
            }
        }


        // Function to interact with a connected client via the console
        static void InteractWithClient(int clientId)
        {
            ClientInfo clientInfo;

            lock (clientsLock)
            {
                if (!connectedClients.TryGetValue(clientId, out clientInfo) || !clientInfo.IsConnected)
                {
                    Console.WriteLine("Client not found or disconnected.");
                    return;
                }
            }

            Console.WriteLine($"\nConnected to Client {clientId} - {clientInfo.RemoteEndPoint}");
            Console.WriteLine("Available commands: 'ALL', 'Close', or type any message");

            // Continue interacting as long as the client is connected
            while (clientInfo.IsConnected)
            {
                try
                {
                    Console.Write($"Command for Client {clientId}: ");
                    string command = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(command))
                        continue;

                    if (command.ToLower() == "close")
                    {
                        Console.WriteLine($"Exiting interaction with Client {clientId}. Client remains connected.");
                        break;
                    }
                    else if (command.ToLower() == "status")
                    {
                        SendMessageToClient(clientInfo, "STATUS_REQUEST");
                        Console.WriteLine("Status request sent to client. Check for response above.");
                    }
                    else
                    {
                        SendMessageToClient(clientInfo, command);
                        Console.WriteLine($"Message sent to Client {clientId}: {command}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error interacting with client: {ex.Message}");
                    break;
                }
            }
        }

        // Function to send a message to a specific client
        static void SendMessageToClient(ClientInfo clientInfo, string message)
        {
            try
            {
                if (clientInfo.IsConnected && clientInfo.Stream != null)
                {
                    byte[] data = Encoding.ASCII.GetBytes(message); // Convert the message string to a byte array using ASCII encoding
                    clientInfo.Stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to Client {clientInfo.Id}: {ex.Message}");
                DisconnectClient(clientInfo.Id);
            }
        }

        // Function to disconnect a client based on their clientId
        static void DisconnectClient(int clientId)
        {
            // Locking the shared resource to ensure thread safety
            lock (clientsLock)
            {
                if (connectedClients.TryGetValue(clientId, out ClientInfo clientInfo))
                {
                    clientInfo.IsConnected = false;

                    try
                    {
                        clientInfo.Stream?.Close();
                        clientInfo.TcpClient?.Close();
                    }
                    catch { }

                    connectedClients.Remove(clientId);
                    Console.WriteLine($"\nClient {clientId} disconnected and removed from list.");
                }
            }
        }
    }
}