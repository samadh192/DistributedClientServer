open System
open System.Net
open System.Net.Sockets
open System.Text

let startServer () =
    let ipAddress = IPAddress.Parse("127.0.0.1") // Replace with your desired IP address
    let port = 12345 // Replace with your desired port number

    let listener = new TcpListener(ipAddress, port)
    listener.Start()

    Console.WriteLine("Server listening on {0}:{1}", ipAddress, port)

    while true do
        try
            let client = listener.AcceptTcpClient()
            Console.WriteLine("Client connected: {0}", client.Client.RemoteEndPoint)

            // Send "Hello" to the client
            let helloMessage = "Hello"
            let helloBytes = Encoding.ASCII.GetBytes(helloMessage)
            let stream = client.GetStream()
            stream.Write(helloBytes, 0, helloBytes.Length)

            // Handle client communication (e.g., reading/writing data)
            // You can create a separate function or use async workflows here

            client.Close()
        with
        | ex -> Console.WriteLine("Error: {0}", ex.Message)

startServer()