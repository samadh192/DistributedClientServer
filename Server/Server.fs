open System
open System.Net
open System.Net.Sockets
open System.Text

let handleClient (client : TcpClient) =
    async {
        let clientID = client.Client.RemoteEndPoint
        Console.WriteLine("Client connected: {0}", clientID)

        let stream = client.GetStream()
        let bufferLength = 256
        let buffer = Array.zeroCreate<byte> bufferLength

        let mutable continueCommunication = true
        while continueCommunication do
            try
                let! bytesRead = Async.AwaitTask (stream.ReadAsync(buffer, 0, buffer.Length))

                if bytesRead = 0 then
                    continueCommunication <- false
                else
                    let receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead)
                    Console.WriteLine("Received from client {0}: {1}", clientID, receivedMessage)

                    if receivedMessage.Trim().ToLower() = "bye" then
                        continueCommunication <- false
                    else
                        let responseMessage = receivedMessage + " from the server"
                        let responseBytes = Encoding.ASCII.GetBytes(responseMessage)
                        do! Async.AwaitTask (stream.WriteAsync(responseBytes, 0, responseBytes.Length))
            with
            | :? System.IO.IOException -> continueCommunication <- false

        client.Close()
        Console.WriteLine("Client disconnected: {0}", clientID)
    }

let startServer () =
    let serverIP = IPAddress.Parse("127.0.0.1")
    let serverPort = 12345

    let listener = new TcpListener(serverIP, serverPort)
    listener.Start()

    Console.WriteLine("Server listening on {0}:{1}", serverIP, serverPort)

    let rec acceptClients () =
        async {
            let! client = Async.AwaitTask (listener.AcceptTcpClientAsync())
            async {
                let! _ = Async.StartChild (handleClient client)
                return ()
            } |> Async.Start
            return! acceptClients ()
        }

    Async.RunSynchronously (acceptClients ())

startServer()