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
                }
            }
            AtriumController cnn = null;
            Console.WriteLine($"Connecting to {address} as \"{username}\" with password \"{password}\"");
            cnn = new AtriumController(username, password, address);
            cnn.FragmentSize = 10;
            Console.WriteLine($"Successfully connected. Device Info: {cnn.ProductName}, {cnn.ProductLabel} (v{cnn.ProductVersion}) - SN: {cnn.SerialNumber}");
            Console.WriteLine($"Credentials - SID: {cnn.SessionID}, Key: {cnn.SessionKey}");
            var key = "4DBB6AB4E90D38A3FBCCB2CCEC8E5F1B";
            var ct = "D6CF78324D1574A95FA9AE682CB052FD837AE48831C05F790303ABBAAF146455E89BF2262244EA223F0C78377435F58215C821102DA5A184860B00FDD1D52D673FB13F17925329D85CE4FB1711F73C92F6184092216D4B5957897A67731E80F4EF1BBCBA1C2B70EACAC9D04CDD8156B5192476CF210FEF195AE73BF5F011E4E401C6A36BD22ABF3A8FADE837F20055616C38A93891BF68BAD11180657DA8E6C904AA89275FCAAA78A170E204D0D5D27246F7ED9CDD06CFA60DADC60A6AFC97F4431142BE2A9EBCAA6A05EC2EFEF6FB11DC76D9072EBD8BAAA92FB37D4EF2CD35788D219966EC8064C2FB8CA855D1F16938D5F79AC4E7D49195EB85D61C7B31AE84B5385D2097BA18C248DA0E52FE12C36915AD14CC2C9824F336C3DC8D3C3A6FA5B590F6A32A7299AF95E7AED7D4C1040D5559F2C2A9829F06F1BE80C6CB8874F1F8D74EED7D91A52A42567E09B61983942CDCA4243EC6A2E92429E2ED2443CDF12CD9C4075007C34F736AB88FD0637ED461EBCAA7F16632A395981C46C3E97C34E05AB306AF14C0A2D824C861C5C2F6A4E4A4939317FA185495D16135DDE49484412CBF7A8640FC6F343B50140F2F4C8E019BFDB9CDBC45EBB07F6E2558875753444ECE5B6A038F2E184BA62090AF73B7428E936146C654010BC43E729E03C4E7587B85C53374000B4F09EEB553FA47C3CA3BCCD5CE5F0A4A249BCC74128ADF03CD34DFE4C7D88DC0EA1724D4F680C323A7C1F5B53884E73249FA1A96AD9B2A4DAFF66354AB0BC605614FDD1D2364BAEA1CC00C510C0ACEA1E77EAF04178DEA6936EFBA1C91CCC9A4979B8287552AAFD160CA12D70F72608EAF383AF77E43F3301EDE6EC1ED1255C0D257E2F1BD96AD5CB92DDDA565F01D33B88924859495CA16D2819E51DFD31AABD3BDD04F4A1F5F3FB264EC29EFFF8512BE2ED0120E59F2D3D33621BB3DD112ED19A2A1FD6BAD59D1DF07E286A29CEC4FFA972039D4B553B8F36B064D055A924119FB0032246A6D92DDFF84320499226BAD262492471BC9AF6AE8CAF3C3B96C2033AA4E4E3BE85A73F6DD3F97BEE19ABAD7E82ADC1CCA3BEF195AE73BF5F0";
            Console.WriteLine($"{RC4.Decrypt(key, ct)}");
                Test(cnn);
            try
            {
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
            List<User> users = cnn.GetAll<User>();

            // GetAllByIndex<AtriumObjectType>([sIdx=0, eIdx=100]) retrieves all objects of specified generic type using Index retrieval. The indices can be set via the passed arguments "sIdx" for Start Index and "eIdx" for End Index.
            List<Card> cards = cnn.GetAllByIndex<Card>(startIndex:0, endIndex:25);
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
