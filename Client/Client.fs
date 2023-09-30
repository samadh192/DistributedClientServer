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

    // Receive and print data from the server
    let bufferLength = 256
    let buffer = Array.zeroCreate<byte> bufferLength
    let bytesRead = stream.Read(buffer, 0, buffer.Length)

    // Convert the received bytes to a string
    let message = Encoding.ASCII.GetString(buffer, 0, bytesRead)
    Console.WriteLine("Received from server: {0}", message)

    client.Close()

connectToServer()