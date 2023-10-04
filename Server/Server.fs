open System
open System.Net
open System.Net.Sockets
open System.Collections.Generic
open System.Text

let clients = new List<TcpClient>()

let broadcastMessage (message: string) =
    let messageBytes = Encoding.ASCII.GetBytes(message)
    let disconnectedClients = ref []

    lock clients (fun () ->
        for client in clients do
            try
                let stream = client.GetStream()
                stream.Write(messageBytes, 0, messageBytes.Length)
            with
            | :? System.IO.IOException ->
                disconnectedClients := client :: !disconnectedClients
    )

    // Remove disconnected clients
    for disconnectedClient in !disconnectedClients do
        lock clients (fun () -> clients.Remove(disconnectedClient)) |> ignore

let add result = 
    let mutable continueProcessing = true
    if Array.length result < 3 then 
        -2
    elif  Array.length result > 5 then
        -3
    else
        let mutable sum = 0
        for i = 1 to Array.length result - 1 do
            if continueProcessing then
                try
                    let temp = result[i]|> int 
                    sum <- sum +  temp
                with
                | :? System.FormatException as ex -> 
                    continueProcessing <- false
                    sum <- -4
        sum
    

let mult result = 
    let mutable continueProcessing = true
    if Array.length result < 3 then 
        -2
    elif  Array.length result > 5 then
        -3
    else
        let mutable product = 1
        for i = 1 to Array.length result - 1 do
            if continueProcessing then
                try
                    let temp = result[i]|> int 
                    product <- product *  temp
                with
                | :? System.FormatException as ex ->
                    continueProcessing <- false
                    product <- -4
        product

let subtract result = 
    let mutable continueProcessing = true
    if Array.length result < 3 then 
        -2
    elif  Array.length result > 5 then
        -3
    else
        let mutable sub = 0
        for i = 1 to Array.length result - 1 do
            if continueProcessing then
                try
                    let temp = result[i]|> int 
                    if i = 1 then 
                        sub <- temp
                    else 
                        sub <- sub -  temp 
                with
                | :? System.FormatException as ex -> 
                    continueProcessing <- false
                    sub <- -4
        sub

let handleClient (client : TcpClient) =
    async {
        let clientID = client.Client.RemoteEndPoint
        Console.WriteLine("Client connected: {0}", clientID)
        lock clients (fun () -> clients.Add(client))


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
                    let mutable res = 0
                    if receivedMessage.Trim().ToLower() = "terminate" then
                        res <- -5
                        continueCommunication <- false
                        broadcastMessage "-5"
                    else 
                        let result = receivedMessage.Split ' '
                        
                        res <-   match result[0] with
                                    | "add" -> add result
                                    | "multiply" -> mult result
                                    | "subtract" -> subtract result
                                    | "bye" -> -5
                                    | _ -> -1

                    let responseMessage = res|> string
                    Console.WriteLine("Responding to Client {0} with result: {1}", clientID, responseMessage)
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