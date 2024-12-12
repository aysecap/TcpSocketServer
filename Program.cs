using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    private static readonly List<TcpClient> clients = new List<TcpClient>();

    static void Main(string[] args)
    {
        Console.WriteLine("Starting TCP Server...");

        // Start the server on a separate thread so that the main thread can accept user input
        Thread serverThread = new Thread(StartServer);
        serverThread.Start();

        // Keep the main thread running until user presses Enter to stop the server
        Console.WriteLine("Press Enter to stop the server.");
        Console.ReadLine();
    }

    static async void StartServer()
    {
        const int port = 3000;

        var hostName = Dns.GetHostName();
        IPHostEntry localhost = await Dns.GetHostEntryAsync(hostName);
        // This is the IP address of the local machine
        IPAddress localIpAddress = localhost.AddressList[0];
        // Create a TcpListener to listen for incoming TCP connections
        TcpListener tcpListener = new TcpListener(localIpAddress, port);
        tcpListener.Start();

        Console.WriteLine($"Server is listening on {localIpAddress}:{port}...");

        try
        {
            while (true)
            {
                // Accept an incoming client connection
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                clients.Add(tcpClient);
                Console.WriteLine($"New Client Connected!! Total clients: {clients.Count}");

                // Handle the client connection on a separate thread
                Thread clientThread = new Thread(() => HandleClient(tcpClient));
                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async void HandleClient(TcpClient tcpClient)
    {
        NetworkStream networkStream = tcpClient.GetStream();
        byte[] buffer = new byte[1024];

        int bytesRead;

        try
        {
            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received from client: {clientMessage}");

                // Send a response back to the client
                string serverResponse = $"You: {clientMessage}";
                byte[] responseBytes = Encoding.UTF8.GetBytes(serverResponse);
                networkStream.Write(responseBytes, 0, responseBytes.Length);

                await BroadcastMessage(clientMessage, tcpClient);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while handling client: {ex.Message}");
        }
        finally
        {
            // Close the connection to the client
            tcpClient.Close();
            clients.Remove(tcpClient);
            Console.WriteLine("A Client Disconnected. Remaining clients: " + clients.Count);
        }
    }
    private static async Task BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        // Send message to all connected clients except the sender
        foreach (var client in clients)
        {
            if (client != sender)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error broadcasting message: {ex.Message}");
                }
            }
        }
    }
}
