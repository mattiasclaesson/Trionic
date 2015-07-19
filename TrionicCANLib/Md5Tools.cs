using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace TrionicCANLib
{
    public class Md5Tools
    {
        public static string GetMd5Hash(MD5 md5Hash, byte[] input)
        {

            // Convert the input string to a byte array and compute the hash. 
            byte[] hash = md5Hash.ComputeHash(input);
            return BuildHashString(hash);
        }

        // Verify a hash against a string. 
        public static bool VerifyMd5Hash(MD5 md5Hash, byte[] input, string hash)
        {
            // Hash the input. 
            string hashOfInput = GetMd5Hash(md5Hash, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void WriteMd5HashFromByteBuffer(string filename, byte[] buf)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                WriteMd5File(filename, GetMd5Hash(md5Hash, buf));
            }
        }

        public static void WriteMd5Hash(MD5 md5Hash, string filename)
        {
            md5Hash.TransformFinalBlock(new byte[0], 0, 0);
            WriteMd5File(filename, BuildHashString(md5Hash.Hash));
        }

        private static void WriteMd5File(string filename, string hash)
        {
            string md5filename = Path.GetDirectoryName(filename);
            md5filename = Path.Combine(md5filename, Path.GetFileNameWithoutExtension(filename) + ".md5");
            File.WriteAllText(md5filename, hash);
        }

        private static string BuildHashString(byte[] hash)
        {
            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < hash.Length; i++)
            {
                sBuilder.Append(hash[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }
    }
}
