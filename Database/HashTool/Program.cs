using System.Security.Cryptography;
using System.Text;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: HashTool <password>");
    return 1;
}

Console.WriteLine(Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(args[0]))));
return 0;
