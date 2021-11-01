using System;
using System.Security.Cryptography;
using System.Text;

namespace OCM.Core.Util
{
    public class SecurityHelper
    {
        public static string GetGravatarURLFromEmailAddress(string email)
        {
            if (email == null) email = "";
            return GetGravatarURLFromHash(GetMd5Hash(email.Trim().ToLower()));
        }

        public static string GetGravatarURLFromHash(string hash)
        {
            if (hash == null) hash = "";
            return "https://www.gravatar.com/avatar/" + hash + "?d=robohash";
        }

        public static string GetMd5Hash(string input)
        {
            if (input == null) return null;

            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public static string GetSHA256Hash(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            SHA256Managed hashFunction = new SHA256Managed();
            byte[] hash = hashFunction.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }
    }
}