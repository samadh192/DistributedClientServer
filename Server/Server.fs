open System
open System.Net
open System.Net.Sockets
open System.Collections.Generic
open System.Text
open System.Threading

// Creating cancellation token to indicate server to exit
let cancellationTokenSource = new CancellationTokenSource()

// List to store connected clients
let clients = new List<TcpClient>()

// Map to store relationship between actual client endpoint and client ID
let ClientMap = new Dictionary<EndPoint, int>()
let nextClientNumber = ref 1

// Function to return clientNumber from actual client endpoint
let getClientNumber endPoint = 
    if ClientMap.ContainsKey(endPoint) then
        ClientMap.[endPoint]
    else
        let clientNumber = !nextClientNumber
        ClientMap.Add(endPoint, clientNumber)
        nextClientNumber := clientNumber + 1
        clientNumber

// Function to broadcast -5(exit code) to all clients after terminate
let broadcastMessage (message: string) =
    let messageBytes = Encoding.ASCII.GetBytes(message)
    let disconnectedClients = ref []

    lock clients (fun () ->
        // For each connected client send -5 to it
        for client in clients do
            try
                let stream = client.GetStream()
                stream.Write(messageBytes, 0, messageBytes.Length)
                Console.WriteLine("Responding to client {0} with result: -5", getClientNumber client.Client.RemoteEndPoint)
            with
            | :? System.IO.IOException ->
                disconnectedClients := client :: !disconnectedClients
            | :? System.ObjectDisposedException ->
                ignore()

    )

    // Remove disconnected clients
    for disconnectedClient in !disconnectedClients do
        lock clients (fun () -> clients.Remove(disconnectedClient)) |> ignore

// Function to perform addition
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
    
// Function to perform multiplication
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

// Function to perform subtraction
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

// Function to service client
let handleClient (client : TcpClient) =
    async {
        // Retrieve clientID
        let clientID = client.Client.RemoteEndPoint

        // Establish stream to communicate with client
        let stream = client.GetStream()

        // Store client in 
        lock clients (fun () -> clients.Add(client))

        // Send Hello! to client when it connects
        let responseBytes = Encoding.ASCII.GetBytes("Hello!")
        do! Async.AwaitTask (stream.WriteAsync(responseBytes, 0, responseBytes.Length))

        // Create buffer to store messages received from client
        let bufferLength = 256
        let buffer = Array.zeroCreate<byte> bufferLength

        let mutable continueCommunication = true
        while continueCommunication do
            try
                // Read messages from client
                let! bytesRead = Async.AwaitTask (stream.ReadAsync(buffer, 0, buffer.Length))

                // If message is empty stop communication
                if bytesRead = 0 then
                    continueCommunication <- false
                else
                    // Display received message from client
                    let receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead)
                    Console.WriteLine("Received: {0}",receivedMessage)
                    let mutable res = 0

                    // If received message is terminate then stop communication and send 
                    // -5(exit code) to all connected clients so that they all exit
                    if receivedMessage.Trim().ToLower() = "terminate" then
                        res <- -5
                        continueCommunication <- false
                        broadcastMessage "-5"

                        // Indicate server to exit
                        cancellationTokenSource.Cancel() 
                        Environment.Exit(0)

                    // For other messages process them according to the operation type
                    else
                        let result = receivedMessage.Split ' '
                        res <-   match result[0] with
                                    | "add" -> add result
                                    | "multiply" -> mult result
                                    | "subtract" -> subtract result
                                    | "bye" -> -5
                                    | _ -> -1

                    // Send response back to client
                    let responseMessage = res|> string
                    Console.WriteLine("Responding to Client {0} with result: {1}", getClientNumber clientID, responseMessage)
                    let responseBytes = Encoding.ASCII.GetBytes(responseMessage)
                    do! Async.AwaitTask (stream.WriteAsync(responseBytes, 0, responseBytes.Length))
            with
            | :? System.IO.IOException -> continueCommunication <- false

        // Close client connection after processing
        client.Close()
    }


let startServer () =

    // Define server IP and port
    let serverIP = IPAddress.Parse("127.0.0.1")
    let serverPort = 12345

    // Start listening to clients
    let listener = new TcpListener(serverIP, serverPort)
    listener.Start()

    Console.WriteLine("Server is running and listening on port {0}", serverPort)

    // Recursive function to service threads concurrently by starting asynchronous
    // task for every client that joins
    let rec acceptClients () =
        async {
            try
                let! client = Async.AwaitTask (listener.AcceptTcpClientAsync())
                async {
                    let! _ = Async.StartChild (handleClient client)
                    return ()
                } |> Async.Start
                return! acceptClients ()
            with
            | :? OperationCanceledException ->
                // Handle server termination
                Console.WriteLine("Server terminated.")
                listener.Stop()
        }
    // Run acceptClients function asynchronously
    Async.RunSynchronously (acceptClients ())

startServer()