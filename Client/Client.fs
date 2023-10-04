open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

let serverIpAddress = IPAddress.Parse("127.0.0.1") // Replace with the server's IP address
let serverPort = 12345 // Replace with the server's port number

let connectToServer () =
    let client = new TcpClient()
    client.Connect(serverIpAddress, serverPort)
    client

let sendToServer (client: TcpClient) (cancellationToken: CancellationToken) =
    async {
        let stream = client.GetStream()
        while not cancellationToken.IsCancellationRequested do
            // Console.Write("Enter a message to send to the server (or 'bye' to exit): ")
            let userInput = Console.ReadLine()
            let inputBytes = Encoding.ASCII.GetBytes(userInput)
            stream.Write(inputBytes, 0, inputBytes.Length)
    }

let receiveFromServer (client: TcpClient) (cancellationToken: CancellationTokenSource)=
    async {
        let stream = client.GetStream()
        let bufferLength = 256
        let buffer = Array.zeroCreate<byte> bufferLength
        let mutable continueCommunication = true
        while continueCommunication do
            let bytesRead = stream.Read(buffer, 0, buffer.Length)
            let receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead)
            Console.WriteLine("Received from server: {0}", receivedMessage)
            if receivedMessage = (-5).ToString() then
                continueCommunication <- false
                cancellationToken.Cancel()
    }

[<EntryPoint>]
let main argv =
    let client = connectToServer()
    let cts = new CancellationTokenSource()
    
    let sendTask = async { do! sendToServer client cts.Token }
    let receiveTask = async { do! receiveFromServer client cts}

    let tasks = [sendTask; receiveTask]

    try
        Async.RunSynchronously (Async.Parallel tasks) |> ignore
    with
        | :? OperationCanceledException ->
            Console.WriteLine("Send task canceled.")
        | ex ->
            Console.WriteLine($"An unexpected error occurred: {ex.Message}")
    
    client.Close()

    0 // Return an integer exit code
