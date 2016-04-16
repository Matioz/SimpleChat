using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public static class SocketExtensions
{
    public static bool IsConnected(this Socket socket)
    {
        try
        {
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch (Exception) { return false; }
    }
}
class Server
{
    private static void SendMessage(Socket socket, string message)
    {
        socket.Send(Encoding.ASCII.GetBytes(message), SocketFlags.None);
    }
    
    private static void SenderToClient(int index)
    {
        string name = "User " + (index + 1).ToString();

        Console.WriteLine("I'm sending to " + name);
        SendMessage(clientSockets[index], " SERVER: You have been connected as " + name + ".");
        for (int i = 0; i < ClientsLimit; i++)
        {
            if (i != index && clientSockets[i] != null && clientSockets[i].IsConnected())
            {
                Console.WriteLine("Setting message to User {0}", i + 1);
                lock (clientBuffors[i])
                {
                    clientBuffors[i] +="SERVER: " +name + " has been connected";
                }
            }
        }
        while (clientSockets[index].IsConnected())
        {
            lock (clientBuffors[index])
            {
                if (clientBuffors[index].Length > 1)
                {
                    Console.WriteLine("I'm sending {0} to {1}", clientBuffors[index], name);
                    SendMessage(clientSockets[index], clientBuffors[index]);
                    clientBuffors[index] = " ";
                }
            }
        }
        
        Console.WriteLine("Client {0} has been disconnected");
        lock (locker)
        {
            clientSockets[index].Close();
            clientSockets[index] = null;
            clientsNum--;
        }
        for (int i = 0; i < ClientsLimit; i++)
        {
            if (i != index && clientSockets[i] != null && clientSockets[i].IsConnected())
            {
                Console.WriteLine("Setting message to User {0}", i + 1);
                lock (clientBuffors[i])
                {
                    clientBuffors[i] += "SERVER: " + name + " has been disconnected";
                }
            }
        }
        return;
    }
    private static void ReceiverFromClient(int index)
    {

        string name = "User " + (index + 1).ToString();
        Console.WriteLine("I'm receiving from " + name);
        byte[] bytes = new byte[1024];
        int size;
        while (clientSockets[index].IsConnected())
        {
            if ((size = clientSockets[index].Receive(bytes, SocketFlags.None)) > 0)
            {
                if (bytes[0] == 0)
                {
                    return;
                }
                string message = name + ": " + Encoding.ASCII.GetString(bytes, 0, size);
                Console.WriteLine("I've received {0} from {1}", message, name);
                   
                for (int i = 0; i < ClientsLimit; i++)
                {
                    if (i != index && clientSockets[i] != null && clientSockets[i].IsConnected())
                    {
                        Console.WriteLine("Setting message to User {0}", i + 1);
                        lock (clientBuffors[i])
                        {
                            clientBuffors[i] += message;
                        }
                    }
                }
            }
        }
    }
    private const int ClientsLimit = 3;
    static Object locker = new Object();
    static Thread[] clientThreads = new Thread[ClientsLimit];
    static Socket[] clientSockets = new Socket[ClientsLimit];
    static string[] clientBuffors = new string[ClientsLimit];
    private static int clientsNum = 0;
    public static void StartServer()
    {
        try
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            Socket server = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            Socket newClient;
            try
            {
                server.Bind(remoteEP);
                server.Listen(ClientsLimit);
                do
                {
                    while (true)
                    {
                        if (clientsNum < ClientsLimit)
                        {
                            newClient = server.Accept();
                            lock (locker)
                            {
                                for (int i = 0; i < ClientsLimit; i++)
                                    if (clientSockets[i] == null)
                                    {
                                        clientSockets[i] = newClient;
                                        clientsNum++;
                                        clientThreads[i] = new Thread(() => SenderToClient(i));
                                        clientThreads[i].Start();
                                        new Thread(() => ReceiverFromClient(i)).Start();
                                        break;
                                    }
                            }
                            Console.WriteLine("{0} clients connected", clientsNum);
                        }
                        else
                            break;
                    }

                } while (true);
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    static void Main()
    {
        for (int i = 0; i < ClientsLimit; i++)
        {
            clientBuffors[i] = " ";
        }
        StartServer();

    }
}
