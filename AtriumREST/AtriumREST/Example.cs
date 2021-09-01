using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using ThreeRiversTech.Zuleger.Atrium.REST.Objects;
using ThreeRiversTech.Zuleger.Atrium.REST.Security;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Example
{
    /// <summary>
    /// In-depth example on how to use the Atrium API.
    /// </summary>
    class Example
    {
        private static Random r = new Random();
        private static int increment = 100;

        const int access_level_door_1_only = 0;
        const int access_level_area_a = 9;
        const int access_level_warehouse = 16;

        public static void Main(String[] args)
        {
            AtriumController.Timeout = 20;
            String username = "admin";
            String password = "admin";
            String address = "http://69.70.57.94/";
            if(args.Length > 0)
            {
                for(int i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];
                    if(arg == "-u")
                    {
                        username = args[++i];
                    }
                    else if(arg == "-p")
                    {
                        password = args[++i];
                    }
                    else if(arg == "-a")
                    {
                        address = args[++i];
                    }
                    else if(arg == "-hrg")
                    {
                        username = "admin";
                        password = "Holmen2019";
                        address = "http://192.168.1.218:2000/";
                        break;
                    }
                    else if(arg == "-pub")
                    {
                        username = "admin";
                        password = "admin";
                        address = "http://69.70.57.94/";
                        break;
                    }
                    else if(arg == "-inc")
                    {
                        increment = Int32.Parse(args[++i]);
                    }
                }
            }
            AtriumController cnn = null;
            Console.WriteLine($"Connecting to {address} as \"{username}\" with password \"{password}\"");
            cnn = new AtriumController(username, password, address);
            Console.WriteLine($"Successfully connected. Device Info: {cnn.ProductName}, {cnn.ProductLabel} (v{cnn.ProductVersion}) - SN: {cnn.SerialNumber}");
            Console.WriteLine($"Credentials - SID: {cnn.SessionID}, Key: {cnn.SessionKey}");
            try
            {
                //String key = "F52478D27ADAF3810F473230BB2013BF";
                //String ct = "AB803CED37D37E15865DB51C6223C9DAD3A084AAB37E330893DD237EF326CDE8C5A7CF1B44B1D256C29BEA966832F511D7C0848B927BA1BF89AA2E56629671C53A33655435A36A9A3682ED682CC879F7F1CC42DCE5BDC9010987B43E7F3D19569D6B2C0E78059F5889CBE915D489EE939FD60567BF3A3A952DC8391E8CF9F2F235F4985675EE1797D50757B3C464498515EAE8EBDC9E647626F2D64E6346C928318E83CDA03EBBFA4F6D5FF03498DE9F1ADE3D656C0F30991CC954A9C33B7A7F61D8269EA8C56EAAD49A1C37856182926C17B4724A8EAF6F6778D2BDB3686D740A20360151F76989CFB7DE59B86B2BAE45DA45CA48932C4213B0C96368925CFAAFEB0C8CAFFE30ED9EEB5F9D2364DB2F35B65B6AE3BAE453FC7FFDFF6DB294AF4E707CBDB4D1F56102FDD08E8561F1B700DCBEF250FDAA1981A10AA5DC2010A769D3DF301FE0855D406946984BAB453B161363071ACC486253478043AA6687793FC3F1A04F054EF41744DFBF6C5C7D102365C487C594F900C78DAFCF5B7D64D71428E0072FEEC4C7F380BDFA463B03953B4F8FE8E83B07F032DD2AFE8614BBB080FA44248A7BA5C2827BD24199FF123219A7012D4411BAB7DE1D71E1DFFCB69178D4382D2404A37E9E5AA6D5C1D53A3F9A42B1F8511DE3A09C0C0E74B1632CB5340D63B339702242EC19F7200210B86A996FAE11BF7B275FAC9F223F7F4C216DA0FC33EB65ADD5FD1D0AD3DB91A14EACAA283B4CF6E85F2BDAE69CB108B9E242AF1C97145853F157CC2E0CEB111D9A427F35F1FA317FFFAD75E8498BA9AC327A2E6212AE9D94641D3F78C6DC1B7A72C3F18445ED7293555562AF1259BEFD58BAF62F4A6F8FA74C81029D3E";
                //Console.WriteLine($"\nCheckSum = {RC4.CheckSum(RC4.Decrypt(key, ct))}");
                //Console.WriteLine($"Decrypted:\n{RC4.Decrypt(key, ct)}\n\n");
                Test(cnn);
                cnn.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine($"Request:\n{cnn.RequestText}\nResponse:\n{cnn.ResponseText}");
                Console.WriteLine($"Encrypted Request:\n{cnn.EncryptedRequest}\nEncrypted Response:\n{cnn.EncryptedResponse}");
                cnn.Close();
                throw e;
            }
        }

        private static void Test(AtriumController cnn)
        {
            increment = 5000;
            List<User> users = cnn.GetAll<User>(0, increment);
            List<Card> cards = cnn.GetAll<Card>(0, increment);

            foreach(var o in users)
            {
                Console.WriteLine(o.Jsonify());
            }
            foreach(var o in cards)
            {
                Console.WriteLine(o.Jsonify());
            }

            User u = new User
            {
                FirstName = "JANE",
                LastName = "DOE",
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(3).AddDays(7),
                AccessLevelObjectIds = AtriumController.ACCESS_LEVELS(
                    access_level_door_1_only,
                    access_level_area_a,
                    access_level_warehouse)
            };
            Card c = new Card
            {
                DisplayName = "JANE DOE CARD",
                CardNumberLo = AtriumController.To26BitCardNumber(r.Next(0, 100), r.Next(0, 8000)),
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(3).AddDays(7),
            };

            Func<User, bool> userPred = (user => user.FirstName == u.FirstName && user.LastName == u.LastName);
            // Update the card if the FirstName and LastName are equal.
            if (users.Any(userPred))
            {
                User existingUser = users.Where(userPred).First();
                existingUser.ActivationDate = u.ActivationDate;
                existingUser.ExpirationDate = u.ExpirationDate;
                existingUser.AccessLevelObjectIds = u.AccessLevelObjectIds;
                u = existingUser;

                Console.WriteLine($"User exists. Updating {existingUser} (Object ID: {existingUser.ObjectId}, Object GUID: {existingUser.ObjectGuid}).");
                if (cnn.Update(existingUser))
                {
                    Console.WriteLine($"User successfully updated. (Object ID: {existingUser.ObjectId}, Object GUID: {existingUser.ObjectGuid})");
                    c.EntityRelationshipId = existingUser.ObjectId;
                    c.EntityRelationshipGuid = existingUser.ObjectGuid;
                }
                else
                {
                    Console.WriteLine($"User update unsuccessful.\nRequest Text:\n{cnn.RequestText}\nResponse Text:\n{cnn.ResponseText}\n{u.Jsonify()}");
                }
            }
            else
            {
                Console.WriteLine($"User does not exist. Inserting {u}.");
                cnn.Insert(u);
                if (u.ObjectId != null)
                {
                    Console.WriteLine($"User successfully inserted. Object ID: {u.ObjectId}");
                    c.EntityRelationshipId = u.ObjectId;
                    c.EntityRelationshipGuid = u.ObjectGuid;
                }
                else
                {
                    Console.WriteLine($"User insert unsuccessful.\nRequest Text:\n{cnn.RequestText}\nResponse Text:\n{cnn.ResponseText}\n{u.Jsonify()}");
                }
            }

            Func<Card, bool> cardPred = (card => card.EntityRelationshipGuid == u.ObjectGuid);
            bool checkForUpdate = true;
            // Update all cards where the Card and User are related.
            if (cards.Any(cardPred) && checkForUpdate)
            {
                foreach (Card existingCard in cards.Where(cardPred))
                {
                    Console.WriteLine($"Card exists. Updating {existingCard} (Object ID: {existingCard.ObjectId}).");

                    existingCard.ActivationDate = c.ActivationDate;
                    existingCard.ExpirationDate = c.ExpirationDate;
                    existingCard.DisplayName = c.DisplayName;

                    if (cnn.Update(existingCard))
                    {
                        Console.WriteLine($"Card successfully updated. Object ID: {existingCard.ObjectId}");
                    }
                    else
                    {
                        Console.WriteLine($"Card update unsuccessful.\nRequest Text:\n{cnn.RequestText}\nResponse Text:\n{cnn.ResponseText}\n{c.Jsonify()}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Card does not exist. Object ID. Inserting {c}");
                cnn.Insert(c);
                if (c.ObjectId != null)
                {
                    Console.WriteLine($"Card successfully inserted. Object ID: {c.ObjectId}");
                }
                else
                {
                    Console.WriteLine($"Card insert unsuccessful.\nRequest Text:\n{cnn.RequestText}\nResponse Text:\n{cnn.ResponseText}\n{c.Jsonify()}");
                }
            }
        }
    }
}
