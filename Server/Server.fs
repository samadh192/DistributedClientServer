open System
open System.Net
open System.Net.Sockets
open System.Text


let add result = 
    Console.WriteLine("Add numbers")
    let mutable sum = 0
    for i = 2 to Array.length result - 1 do
        let temp = result[i]|> int 
        sum <- sum +  temp
    Console.WriteLine(sum)

let mult result = 
    Console.WriteLine("Multiply numbers")
    let mutable mul = 1
    for i = 2 to Array.length result - 1 do
        let temp = result[i]|> int 
        mul <- mul *  temp
    Console.WriteLine(mul)

let subtract result = 
    Console.WriteLine("Subtract numbers")
    let mutable sub = 0
    for i = 2 to Array.length result - 1 do
        let temp = result[i]|> int 
        sub <- sub -  temp
    Console.WriteLine(sub)

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
                    let result = receivedMessage.Split ' '
                    
                    let res =   match result[0] with
                                | "add" -> add result
                                | "multiply" -> mult result
                                | "subtract" -> subtract result
                                | _ -> Console.WriteLine("Anything")

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