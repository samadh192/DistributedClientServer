open System
open System.Net
open System.Net.Sockets
open System.Text

let connectToServer () =
    let serverIpAddress = IPAddress.Parse("127.0.0.1") // Replace with the server's IP address
    let serverPort = 12345 // Replace with the server's port number

    let client = new TcpClient()
    client.Connect(serverIpAddress, serverPort)

    let stream = client.GetStream()

    // Create a loop to send messages to the server
    let mutable continueCommunication = true
    while continueCommunication do
        // Read user input from the console
        Console.Write("Enter a message to send to the server (or 'bye' to exit): ")
        let userInput = Console.ReadLine()

        // Convert the user input to bytes and send it to the server
        let inputBytes = Encoding.ASCII.GetBytes(userInput)
        stream.Write(inputBytes, 0, inputBytes.Length)

        // Receive and print the server's response
        let bufferLength = 256
        let buffer = Array.zeroCreate<byte> bufferLength
        let bytesRead = stream.Read(buffer, 0, buffer.Length)
        let receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead)
        Console.WriteLine("Received from server: {0}", receivedMessage)

        // Check if the user wants to exit
        if userInput.Trim().ToLower() = "bye" then
            continueCommunication <- false

    // Close the client connection
    client.Close()

connectToServer()