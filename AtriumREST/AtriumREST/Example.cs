using System;
using System.Linq;
using System.Collections.Generic;

using ThreeRiversTech.Zuleger.Atrium.REST.Objects;
using System.Threading.Tasks;

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
            String username = "admin";
            String password = "admin";
            String address = "http://69.70.57.94/";
            AtriumController cnn = new AtriumController(username, password, address); ;
            cnn.FragmentSize = 10;
            Console.WriteLine($"Connecting to {address} as \"{username}\" with password \"{password}\"");
            Console.WriteLine($"Successfully connected. Device Info: {cnn.ProductName}, {cnn.ProductLabel} (v{cnn.ProductVersion}) - SN: {cnn.SerialNumber}");
            Console.WriteLine($"Credentials - SID: {cnn.SessionID}, Key: {cnn.SessionKey}");
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

            // Alternatively, you can use Asynchronous methods.
            TestAsync(cnn);
        }

        private static void Test(AtriumController cnn)
        {
            // Fragmentation retrieval is the process of retrieving a set of data over multiple packets. The retrieval process stops once the last packet returns no data.
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
            List<Card> cards = cnn.GetAllByIndex<Card>(startIndex: 0, endIndex: 25);
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
                new Func<Card, bool>(theCard => theCard.EntityRelationshipGuid == u.ObjectGuid));

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
                new Func<User, bool>(theUser => theUser.FirstName == u.FirstName && theUser.LastName == u.LastName));

            if (deletedUsers.Count > 0)
            {
                Console.WriteLine($"User cleanup successful!");
            }
            else
            {
                Console.WriteLine($"User cleanup unsuccessful!\n{cnn.RequestText}\n{cnn.ResponseText}");
            }
        }
    
        private async static void TestAsync(AtriumController cnn)
        {
            // You may also provide a feedback function that is to be called on every retrieval. This applies to both Sync and Async GetAll methods.
            // The smaller the fragment size, the more often your feedback function is called.
            Console.Write($"Retrieving Users and Cards...");
            var usersTask = cnn.GetAllAsync<User>(fragmentSize: 2, feedback: () => Console.Write(".")); // If 20 users exist, then 10 '.' characters will print.
            var cardsTask = cnn.GetAllByIndexAsync<Card>(startIndex: 0, endIndex: 25);

            // Await our retrieval tasks.
            List<User> allUsers = await usersTask;
            List<Card> allCards0to25 = await cardsTask;

            // Create the potentially new User.
            User u = new User
            {
                FirstName="David",
                LastName="Spade",
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(3).AddDays(7),
                AccessLevelObjectIds = AtriumController.ACCESS_LEVELS(
                    access_level_door_1_only,
                    access_level_area_a,
                    access_level_warehouse)
            };

            // Create the potentially new Card for the User.
            Card c = new Card
            {
                DisplayName = u.ToString() + " Card", // First Name Last Name + Card
                CardNumberLo = AtriumController.To26BitCardNumber(r.Next(0, 100), r.Next(0, 8000)),
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(3).AddDays(7),
            };

            // Asynchronously insert or update the User and the User's attached card.
            Task<string> 
                insertUserTask = null, 
                insertCardTask = null;
            Task<bool> 
                updateUserTask = null, 
                updateCardTask = null;
            // If a user does not already exist with both first name and last name being equal.
            if(!allUsers.Any(user => user.FirstName == u.FirstName && user.LastName == u.LastName))
            {
                // Insert the user.
                insertUserTask = cnn.InsertAsync(u);
            }
            else
            {
                // Update the user. (assuming that there is only one user with that First and Last Name.)
                // If more than one, then only the first one to be retrieved is updated.
                User existingUser = allUsers.Where(user => user.FirstName == u.FirstName && user.LastName == u.LastName).First();
                existingUser.ActivationDate = u.ActivationDate;
                existingUser.ExpirationDate = u.ExpirationDate;
                existingUser.AccessLevelObjectIds = u.AccessLevelObjectIds;
                updateUserTask = cnn.UpdateAsync(u);
            }
            // If the user does not already have a card assigned to them 
            if (!allCards0to25.Any(card => card.ObjectGuid == u.EntityRelationshipGuid))
            {
                // Insert the card.
                insertCardTask = cnn.InsertAsync(c);
            }
            else
            {
                // Update the card. (assuming that there is only one card attached to this user)
                // If more than one, then only the first one to be retrieved is updated.
                Card existingCard = allCards0to25.Where(card => card.ObjectGuid == u.EntityRelationshipGuid).First();
                existingCard.ActivationDate = c.ActivationDate;
                existingCard.ExpirationDate = c.ExpirationDate;
                existingCard.DisplayName = c.DisplayName;
                updateCardTask = cnn.UpdateAsync(existingCard);
            }

            // Desired variables.
            string 
                userId = null, 
                cardId = null;
            bool 
                didUserUpdate = false, 
                didCardUpdate = false;

            // Await our applicable tasks.
            if(insertUserTask != null) userId = await insertUserTask;
            if(insertCardTask != null) cardId = await insertCardTask;
            if(updateUserTask != null) didUserUpdate = await updateUserTask;
            if(updateCardTask != null) didCardUpdate = await updateCardTask;

            // Do what we want with the variables.
            if(!didUserUpdate)
            {
                u.ObjectId = userId; // This is technically assigned in the "Insert" functions.
            }

            if(!didCardUpdate)
            {
                c.ObjectId = cardId; // This is technically assigned in the "Insert" functions.
            }

            // By the end of this function, either a User named "David Spade" will be inserted with the respective card
            // Or the User David Spade will be updated with their new attributes.
            // and/or the User David Spade will have a card inserted attached to them.
            // (dependent on above) or User David Spade will have their attached card updated with their new attributes.
        }
    }
}
