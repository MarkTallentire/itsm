using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

var ip = IPAddress.Parse("192.168.1.133");
var endpoint = new IPEndPoint(ip, 161);
var community = new OctetString("public");

var oids = new Dictionary<string, string>
{
    ["sysDescr"]        = "1.3.6.1.2.1.1.1.0",
    ["hrDeviceDescr"]   = "1.3.6.1.2.1.25.3.2.1.3.1",
    ["prtGeneralSerial"]= "1.3.6.1.2.1.43.5.1.1.17.1",
    ["prtPageCount"]    = "1.3.6.1.2.1.43.10.2.1.4.1.1",
    ["hrDeviceType"]    = "1.3.6.1.2.1.25.3.2.1.2.1",
    ["hrPrinterStatus"] = "1.3.6.1.2.1.25.3.5.1.1.1",
};

foreach (var (name, oidStr) in oids)
{
    try
    {
        var oid = new ObjectIdentifier(oidStr);
        Console.Write($"{name,-20} ");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var result = await Messenger.GetAsync(
            VersionCode.V2,
            endpoint,
            community,
            [new Variable(oid)]).WaitAsync(cts.Token);

        if (result.Count > 0)
        {
            var data = result[0].Data;
            var typeName = data.GetType().Name;
            Console.WriteLine($"[{typeName}] {data}");

            if (data is Integer32 intVal)
                Console.WriteLine($"  -> Int32 value: {intVal.ToInt32()}");
            else if (data is Counter32 cntVal)
                Console.WriteLine($"  -> Counter32 value: {cntVal.ToUInt32()}");
        }
        else
        {
            Console.WriteLine("(no result)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
    }
}

// Test what GetSubnetHosts would produce for this network
Console.WriteLine("\n--- Subnet host check ---");
var addr = IPAddress.Parse("192.168.1.14");
var mask = IPAddress.Parse("255.255.255.0");
var addrBytes = addr.GetAddressBytes();
var maskBytes = mask.GetAddressBytes();
var networkBytes = new byte[4];
var broadcastBytes = new byte[4];
for (var i = 0; i < 4; i++)
{
    networkBytes[i] = (byte)(addrBytes[i] & maskBytes[i]);
    broadcastBytes[i] = (byte)(addrBytes[i] | ~maskBytes[i]);
}
var networkInt = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0);
var broadcastInt = BitConverter.ToUInt32(broadcastBytes.Reverse().ToArray(), 0);
Console.WriteLine($"Network:   {new IPAddress(networkBytes)}");
Console.WriteLine($"Broadcast: {new IPAddress(broadcastBytes)}");
Console.WriteLine($"Host range: {networkInt + 1} to {broadcastInt - 1} ({broadcastInt - networkInt - 1} hosts)");

// Check that 192.168.1.133 is in the range
var targetInt = BitConverter.ToUInt32(ip.GetAddressBytes().Reverse().ToArray(), 0);
Console.WriteLine($"Target {ip} int: {targetInt}, in range: {targetInt > networkInt && targetInt < broadcastInt}");
