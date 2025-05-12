using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business
{
    public static class Utility
    {
        private static readonly IConfigurationRoot _configuration;
        static Utility()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set path to the appsettings.json file
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        private static DataTable ConvertToDataTable<T>(List<T> data)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (var item in data)
            {
                var values = new object[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    values[i] = properties[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }

            return dataTable;
        }


        public static bool ValidatePassport(IFormFile passportData)
        {
            int size = Convert.ToInt32(_configuration["AppConstants:PassportMaxSize"]);
            return passportData.Length < 1024 * size;
        }


        public static string Encrypt(string plainText, string password="Securityr&d1", string salt="HEFRA")
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            using (Aes aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(cs))
                        {
                            writer.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string encryptedText, string password = "Securityr&d1", string salt = "HEFRA")
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            byte[] cipherBytes = Convert.FromBase64String(encryptedText);
            using (Aes aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (MemoryStream ms = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }


        public static DateTime StartOfDay(DateTime startDate)
        {
            DateTime date = startDate.Date;
            return date;
        }

        public static DateTime EndOfDay(DateTime endDate)
        {
            DateTime date = endDate.Date;
            date = date.AddHours(23).AddMinutes(59).AddSeconds(59);
            return date;
        }

    }
}
