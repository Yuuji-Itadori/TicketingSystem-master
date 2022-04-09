// Barlow, Ethan; Peachey, Benjamin; Blood, Joel; Abdi Ibrahim, Mubarak. (2021) SOFTWARE PROJECTS Stage 2, (SEM2 AF-2020/1) 55-407815-AF-20201 [Unpublished assignment]. Sheffield Hallam University

using System;
using System.Text;
using System.Security.Cryptography;


namespace TicketingSystem.Data
{
    public static class Crypto
    {
        public static string Encrypt(string content)
        {
            if (content.Equals(string.Empty))
                return string.Empty;

            byte[] source = Encoding.UTF8.GetBytes(content);
            byte[] hashBytes;

            using (var hash = MD5.Create())
                hashBytes = hash.ComputeHash(source);

            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
    }
}