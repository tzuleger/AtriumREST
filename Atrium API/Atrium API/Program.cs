using System;
using System.Linq;

namespace ThreeRiversTech.Zuleger.Atrium.API
{
    class Program
    {
        private static Random _random = new Random();

        public static void Main(String[] args)
        {
            AtriumConnection.MaxAttempts = 10;
            AtriumConnection.DelayBetweenAttempts = 30;
            var atriumConnection = new AtriumConnection("admin", "admin", "http://69.70.57.94:8083/");

            var userId = GenerateRandomId();
            var cardId = GenerateRandomId();
            //var objectId = atriumConnection.InsertUser("Terry", "Crews", userId, DateTime.Now.AddDays(-13), DateTime.Now.AddYears(2));
            //Console.WriteLine("oid: " + objectId);
            //atriumConnection.InsertCard("Terry Crews Card", cardId, userId, objectId, 0x7fffff, DateTime.Now.AddDays(-13), DateTime.Now.AddYears(2));
            var users = atriumConnection.GetUsersByName("Terry", "Crews", endIdx: 50);
            Console.WriteLine(atriumConnection.ResponseText);
            foreach(var user in users)
            {
                foreach(var kvp in user)
                {
                    Console.WriteLine(kvp.Key + ": " + kvp.Value);
                }
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
