using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using ThreeRiversTech.Zuleger.Atrium.REST.Objects;

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
            cnn.BatchSize = 10; // You can set the batch size here so it's static for all batch size needed functions.

            Console.WriteLine($"Connecting to {address} as \"{username}\" with password \"{password}\"");
            // Information on the Controller and the Connection itself is available too!
            Console.WriteLine($"Successfully connected. Device Info: {cnn.ProductName}, {cnn.ProductLabel} (v{cnn.ProductVersion}) - SN: {cnn.SerialNumber}");
            Console.WriteLine($"Credentials - SID: {cnn.SessionID}, Key: {cnn.SessionKey}");
            try
            {
                TestAsync(cnn); // asynchronous example
                Test(cnn); // synchronous example
                cnn.Close();
            }
            catch(Exception e)
            {
                // for debugging purposes, the Request and Response Texts (decrypted and encrypted versions) are available at any time.
                Console.WriteLine($"Request:\n{cnn.RequestText}\nResponse:\n{cnn.ResponseText}");
                Console.WriteLine($"Encrypted Request:\n{cnn.EncryptedRequest}\nEncrypted Response:\n{cnn.EncryptedResponse}");
                cnn.Close();
                throw e;
            }

        }

        /// <summary>
        /// Used as an example to show how synchronous programming works on an Atrium Controller using the REST API.
        /// </summary>
        /// <param name="cnn">Connection to an AtriumController.</param>
        private static void Test(AtriumController cnn)
        {
            // Grab all Users from the controller in batches of 5. If a batchSize is not specified, then the static BatchSize is used (set above)
            List<User> users = cnn.GetAll<User>(batchSize: 5);
            // If GetAll was called again with no batchSize, then the batchSize of 10 (static BatchSize set above) is used.

            // Grab all Cards from the controller starting from index 0 to index 25.
            List<Card> cards = cnn.GetAllByIndex<Card>(0, 25);

            // Create a new User
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

            // Create a new Card.
            // Note: To26BitCardNumber transforms a "FamilyNumber (10 bits): MemberNumber (16 bits)" card format number to its Hexadecimal Counterpart.
            Card c = new Card
            {
                DisplayName = u.ToString() + " CARD",
                CardNumberLo = AtriumController.To26BitCardNumber(r.Next(0, 1023), r.Next(0, 65535)),
                ActivationDate = u.ActivationDate,
                ExpirationDate = u.ExpirationDate
            };

            // Desired variables
            string 
                userId = null, 
                cardId = null;

            bool 
                didUserUpdate = false,
                didCardUpdate = false;

            // If a user does not exist with the same First Name and Last Name
            if(!users.Any(user => user.ToString() == u.ToString()))
            {
                // Insert the User.
                userId = cnn.Insert(u);
            }
            else
            {
                // Update the user. (assuming that there is only one user with that First and Last Name.)
                // If more than one, then only the first one to be retrieved is updated.
                User existingUser = users.Where(user => user.ToString() == u.ToString()).First();
                existingUser.ActivationDate = u.ActivationDate;
                existingUser.ExpirationDate = u.ExpirationDate;
                existingUser.AccessLevelObjectIds = u.AccessLevelObjectIds;
                didUserUpdate = cnn.Update(existingUser);
                u = existingUser; // Reassign to the existing version since u is used to check if a Card exists that is attached to the User.
            }

            // If a card does not already exist that is attached the User.
            if(!cards.Any(card => card.ObjectGuid == u.EntityRelationshipGuid))
            {
                // Insert the Card attached to that User.
                cardId = cnn.Insert(c);
            }
            else
            {
                // Update the card. (assuming that there is only one card attached to this user)
                // If more than one, then only the first one to be retrieved is updated.
                Card existingCard = cards.Where(card => card.ObjectGuid == u.EntityRelationshipGuid).First();
                existingCard.ActivationDate = c.ActivationDate;
                existingCard.ExpirationDate = c.ExpirationDate;
                existingCard.DisplayName = c.DisplayName;
                didCardUpdate = cnn.Update(existingCard);
                c = existingCard; // Reassign to the existing version since c could be used later in this function.
            }
        }
        
        /// <summary>
        /// Used as an example to show how Asynchronous programming works on an Atrium Controller using the REST API.
        /// </summary>
        /// <param name="cnn">Connection to an AtriumController.</param>
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
                updateUserTask = cnn.UpdateAsync(existingUser);
                u = existingUser; // Reassign to the existing version since u is used to check if a Card exists that is attached to the User.
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
                c = existingCard; // Reassign to the existing version since c could be used later in this function.
            }

            // Desired variables.
            string 
                userId = null, 
                cardId = null;
            bool 
                didUserUpdate = false, 
                didCardUpdate = false;

            // Await our applicable tasks.

            // User first, since the Card is dependent on User being in Controller.
            if(insertUserTask != null) userId = await insertUserTask;
            if(updateUserTask != null) didUserUpdate = await updateUserTask;
            // Card following.
            if(insertCardTask != null) cardId = await insertCardTask;
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
