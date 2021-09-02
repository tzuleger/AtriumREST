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
            cnn.FragmentSize = 30;
            Console.WriteLine($"Connecting to {address} as \"{username}\" with password \"{password}\"");
            cnn = new AtriumController(username, password, address);
            Console.WriteLine($"Successfully connected. Device Info: {cnn.ProductName}, {cnn.ProductLabel} (v{cnn.ProductVersion}) - SN: {cnn.SerialNumber}");
            Console.WriteLine($"Credentials - SID: {cnn.SessionID}, Key: {cnn.SessionKey}");
            try
            {
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
            // Fragmentation retrieval is the proces of retrieving a set of data over multiple packets. The retrieval process stops once the last packet returns no data.
            // Pro: Grabs the almost exact number of records that actually exist.
            // Pro: No need to know underlying information about the layout of the controller's database.
            // Con: If a fragment occurs larger than the fragmentSize provided, then all data after that fragment is ignored.
            // Con: Uses multiple HTTP packet requests which can be bottlenecked by network speed.

            // Index retrieval is the process of retrieving a set of data over one packet specified by indices. The retrieval process stops after the first packet sent.
            // Pro: Grabs all data in one HTTP packet, keeping local network bandwidth limited.
            // Pro: Data is guaranteed to be the same every time, no matter how many fragments occur.
            // Con: Efficiency cost when indices are a larger range and the actual number of records in the controller are smaller.

            // GetAll<AtriumObjectType>([fragmentSize=100]) retrieves all objects of specified generic type using Fragmentation retrieval. The fragment size can be set via passed argument "fragmentSize" or setting the public Instance attribute "FragmentSize".
            List<User> users = cnn.GetAll<User>(fragmentSize:35);

            // GetAllByIndex<AtriumObjectType>([sIdx=0, eIdx=100]) retrieves all objects of specified generic type using Index retrieval. The indices can be set via the passed arguments "sIdx" for Start Index and "eIdx" for End Index.
            List<Card> cards = cnn.GetAllByIndex<Card>(sIdx:0, eIdx:35);
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
                    c = existingCard;

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

            // Known issues of cards being placed in the 60s, a Fragment size of 30 (set at start of program) could cause problems.
            cnn.FragmentSize = 100;
            Console.WriteLine($"Cleaning up Cards from this program...");
            var deletedCards = cnn.Delete<Card>(
                new Func<Card, bool> (theCard => theCard.EntityRelationshipGuid == u.ObjectGuid) );

            if (deletedCards.Count > 0)
            {
                Console.WriteLine($"Card cleanup successful!");
            }
            else
            {
                Console.WriteLine($"Card cleanup unsuccessful!\n{cnn.RequestText}\n{cnn.ResponseText}");
            }

            Console.WriteLine($"Cleaning up Users from this program...");
            var deletedUsers = cnn.Delete<User>(
                new Func<User, bool> (theUser => theUser.FirstName == u.FirstName && theUser.LastName == u.LastName) );

            if(deletedUsers.Count > 0)
            {
                Console.WriteLine($"User cleanup successful!");
            }
            else
            {
                Console.WriteLine($"User cleanup unsuccessful!\n{cnn.RequestText}\n{cnn.ResponseText}");
            }

        }
    }
}
