using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace ConsoleApplicationMD5test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const string fileName = @"C:\cert.asc";

            if (!File.Exists(fileName)) // Блядь, нахуй все эти выебовны? Какой даун решил, что пропускать только byte[] в проверку SHA256 это крутая идея?
            {
                return;
            }
            string str1 = ""; // GetChecksum(fileName);

            var fileStream = new FileStream(fileName, FileMode.OpenOrCreate,
                FileAccess.Read);
            string str2 = GetChecksumBuffered(fileStream);

            Console.ReadLine();
        }

        private static string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        private static string GetChecksumBuffered(Stream stream)
        {
            using (var bufferedStream = new BufferedStream(stream, 1024 * 32))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}