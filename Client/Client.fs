open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

// Server IP Address
let serverIpAddress = IPAddress.Parse("127.0.0.1")

// Server Port Number
let serverPort = 12345


// Function to connect to the server using IP address and port number
let connectToServer () =
    let client = new TcpClient()
    client.Connect(serverIpAddress, serverPort)
    client

// Function to send messages from client to server
let sendToServer (client: TcpClient) (cancellationToken: CancellationToken) =
    async {
        // Establish stream to communicate with server
        let stream = client.GetStream()
        // Cancellation token is used to determine if sendToServer thread must continue
        while not cancellationToken.IsCancellationRequested do
            // Sleep for 500 milliseconds to ensure prompt is printed after server response
            Async.Sleep(500) |> Async.RunSynchronously
            Console.Write("\nSending command: ")

            // Wait for user input
            let userInput = Console.ReadLine()
            let inputBytes = Encoding.ASCII.GetBytes(userInput)

            // Forward user input to server
            stream.Write(inputBytes, 0, inputBytes.Length)
    }


// Function to receive messages from server
let receiveFromServer (client: TcpClient) (cancellationToken: CancellationTokenSource)=
    async {
        // Establish stream to communicate with server
        let stream = client.GetStream()

        // Create buffer to store received messages from server
        let bufferLength = 256
        let buffer = Array.zeroCreate<byte> bufferLength

        // continueCommunication flag will be true as long as we dont encounter terminate
        let mutable continueCommunication = true
        while continueCommunication do
            // Reach from server
            let bytesRead = stream.Read(buffer, 0, buffer.Length)
            let receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead)
            let mutable serverResponse = ""

            // Match received message to server response 
            serverResponse <- match receivedMessage with
                                | "-1" -> "incorrect operation command"
                                | "-2" -> "number of inputs is less than two"
                                | "-3" -> "number of inputs is more than four"
                                | "-4" -> "one or more of the inputs contain(s) non-number(s)"
                                | "-5" -> "exit"
                                | "Hello!" -> "Hello!"
                                | _     -> receivedMessage
            // Display server response
            Console.WriteLine("\nServer Response: {0}", serverResponse)

            // If code -5 is received then the client must close
            if receivedMessage = (-5).ToString() then
                continueCommunication <- false
                // Indicate to sendToServer that it needs stop executing
                cancellationToken.Cancel()
                Environment.Exit(0)
    }

[<EntryPoint>]
let main argv =

    // Create client object
    let client = connectToServer()

    // Create cancellation token to allowe commincation between sendToServer
    // and receiveFromServer threads
    let cts = new CancellationTokenSource()

    // Create asynchronous tasks to run sendToServer and receiveFromServer threads
    let sendTask = async { do! sendToServer client cts.Token }
    let receiveTask = async { do! receiveFromServer client cts}

    let tasks = [sendTask; receiveTask]

    try
        // Parallely run sendToServer and receiveFromServer threads
        Async.RunSynchronously (Async.Parallel tasks) |> ignore
    with
        | :? OperationCanceledException ->
            Console.WriteLine("Send task canceled.")
        | ex ->
            Console.WriteLine($"An unexpected error occurred: {ex.Message}")
    
    client.Close()

    0 // Return an integer exit code
