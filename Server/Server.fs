open System
open System.Net
open System.Net.Sockets
open System.Text

let startServer () =
    let serverIP = IPAddress.Parse("127.0.0.1")
    let serverPort = 12345

    let clientListener = new TcpListener(serverIP, serverPort)
    clientListener.Start()

    Console.WriteLine("Server listening on {0}:{1}", serverIP, serverPort)

    while true do
        try
            let client = clientListener.AcceptTcpClient()
            let clientID = client.Client.RemoteEndPoint
            Console.WriteLine("Client connected: {0}", clientID)

            let stream = client.GetStream()

            // Handle client communication in a loop
            let mutable continueCommunication = true
            while continueCommunication do
                // Receive data from the client
                let bufferLength = 256
                let buffer = Array.zeroCreate<byte> bufferLength

                try
                    let bytesRead = stream.Read(buffer, 0, buffer.Length)

                    // Check if the client has disconnected
                    if bytesRead = 0 then
                        continueCommunication <- false
                    else
                        // Convert the received bytes to a string
                        let receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead)
                        Console.WriteLine("Received from client: {0}", receivedMessage)

                        // Check if the client wants to exit
                        if receivedMessage.Trim().ToLower() = "bye" then
                            continueCommunication <- false
                        else
                            // Respond to the client by appending '#' to the received message
                            let responseMessage = receivedMessage + " from the server"
                            let responseBytes = Encoding.ASCII.GetBytes(responseMessage)
                            stream.Write(responseBytes, 0, responseBytes.Length)
                with
                | :? System.IO.IOException -> // Handle IOException when client disconnects
                    continueCommunication <- false

            // Close the client connection
            client.Close()
            Console.WriteLine("Client disconnected: {0}", clientID)

        with
        | ex -> Console.WriteLine("Error: {0}", ex.Message)

startServer()