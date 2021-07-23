using System;
using System.Linq;

namespace ThreeRiversTech.Zuleger.Atrium.API
{
    class Program
    {
        private static Random _random = new Random();

        public static void Main(String[] args)
        {
            var atriumConnection = new AtriumConnection("testuser1", "testuser1", "http://69.70.57.94:8083/");

            var userId = GenerateRandomId();
            var cardId = GenerateRandomId();
            atriumConnection.InsertUser("John", "Doe", userId, DateTime.Now.AddDays(-13), DateTime.Now.AddYears(2));
            //atriumConnection.InsertCard("John Doe Card", cardId, userId, 0x7fffff, DateTime.Now.AddDays(-13), DateTime.Now.AddYears(2));
            var users = atriumConnection.GetUsersByName("John", "Doe");

            foreach(var user in users)
            {
                Console.WriteLine("User: " + user);
            }
        }

        private static Guid GenerateRandomId()
        {
            byte[] buf = new byte[16];
            _random.NextBytes(buf);
            String result = String.Concat(buf.Select(x => x.ToString("X2")).ToArray());

            String s1 = result.Substring(0, 8);
            String s2 = result.Substring(7, 4);
            String s3 = result.Substring(11, 4);
            String s4 = result.Substring(15, 4);
            String s5 = result.Substring(19, 12);

            return Guid.Parse($"{s1}-{s2}-{s3}-{s4}-{s5}");
        }
    }
}
