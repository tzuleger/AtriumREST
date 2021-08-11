using System;
using System.Collections.Generic;

using ThreeRiversTech.Zuleger.Atrium.API.Objects;

namespace ThreeRiversTech.Zuleger.Atrium.API.Example
{
    /// <summary>
    /// In-depth example on how to use the Atrium API.
    /// </summary>
    class Example
    {
        public static void Main(String[] args)
        {
            // Variables for making sure updates/inserts occur.
            bool userInsertedOrUpdated = false;
            bool cardInsertedOrUpdated = false;

            AtriumController.MaxAttempts = 3; // Attempt to establish connection 3 times.
            AtriumController.DelayBetweenAttempts = 12; // Wait 12 seconds between each attempt to establish a connection.

            // Establish connection (connection is to CDVI public A22 board)
            AtriumController cnn = new AtriumController("admin", "admin", "http://69.70.57.94/");
            
            // Print the Request Text to Atrium SDK and Response Text from Atrium SDK for debugging purposes.
            // Updates after every SDK transaction (e.g. InsertCard, InsertUser, UpdateCard, UpdateUser, etc.)
            Console.WriteLine($"Request Text:\n{cnn.RequestText}\nResponse Text:\n{cnn.ResponseText}");

            // The public board (at the time of developing this example) only has 9 Users, so for best optimization, an increment of 10 is useful.
            List<User> users = cnn.GetAllUsers(increment: 10);
            // By default, the increment is 100.
            List<Card> cards = cnn.GetAllCards();
            Console.WriteLine($"{cnn.RequestText}\n{cnn.ResponseText}");

            // Using Atrium SDK Demo App, I obtained the Object ID for "Door 1 ONLY", "Area A", and "Warehouse Access Level"
            const int access_level_door_1_only = 0;
            const int access_level_area_a = 9;
            const int access_level_warehouse = 16;

            // Create a new User that is set to expire 1 year and 7 days from now.
            User newUser = new User
            {
                FirstName = "John",
                LastName = "Doe",
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(1).AddDays(7),
                ObjectGuid = cnn.GenerateGuid,
                // static AtriumController.ACCESS_LEVELS() can have up to 5 arguments, allowing up to 5 different levels of access for a User
                AccessLevelObjectIds = AtriumController.ACCESS_LEVELS(
                    access_level_door_1_only, 
                    access_level_area_a, 
                    access_level_warehouse)
            };

            // Check if User exists.
            List<User> usersWithNameJohnDoe = users.FindAll(user => user.ToString() == newUser.ToString());
            if (usersWithNameJohnDoe.Count > 0)
            {
                // User exists.

                // User.Update(User) updates the caller User object's attributes to be the same as the argument User object.
                // All null attributes in the argument User object are ignored and are not copied to the caller User object.
                usersWithNameJohnDoe[0].Update(newUser);
                userInsertedOrUpdated = cnn.UpdateUser(usersWithNameJohnDoe[0]);

                // Just to keep continuity with newUser.
                newUser = usersWithNameJohnDoe[0];
            }
            else
            {
                // User does not exist.

                // Insert the User. The Object ID for this User is automatically updated in AtriumConnection.InsertUser.
                // Additionally, the ObjectID is returned as well.
                String newUserObjectID = cnn.InsertUser(newUser);

                // If the returned Object ID is null, then the insertion failed.
                userInsertedOrUpdated = newUserObjectID != null;
            }

            // We also want a card attached to this User.
            Card newCard = new Card
            {
                DisplayName = newUser.ToString() + " Card", // John Doe Card
                // static AtriumController.To26BitCardNumber takes a Family Number and a Member Number and does the correct bit manipulation to make it
                // the correct 26-bit integer.
                CardNumber = AtriumController.To26BitCardNumber(77, 4033),
                ActivationDate = newUser.ActivationDate,
                ExpirationDate = newUser.ExpirationDate,
                ObjectGuid = cnn.GenerateGuid,
                // Specify the related item so they are logically attached to eachother.
                EntityRelationshipId = newUser.ObjectId,
                EntityRelationshipGuid = newUser.ObjectGuid
            };

            // You can use the FindAll LINQ method to filter what cards you want.
            // Filter for all attached cards to a User
            List<Card> cardsAttachedToJohnDoe = cards.FindAll(card => card.EntityRelationshipId == newUser.ObjectId);

            // Filter for all cards that have the same card number (should be 0 or 1, if 1, then the card exists and cannot be inserted.
            Console.WriteLine(cards.Count);
            List<Card> cardsWithSameCardNumber = cards.FindAll(card =>
            {
                Console.WriteLine($"card: {card.CardNumber}, newCard: {newCard.CardNumber}");
                return card.CardNumber == newCard.CardNumber;
            });

            if(cardsWithSameCardNumber.Count > 0)
            {
                // Card already exists.

                cardsWithSameCardNumber[0].Update(newCard);
                cardInsertedOrUpdated = cnn.UpdateCard(cardsWithSameCardNumber[0]);
                newCard = cardsWithSameCardNumber[0];
            }
            else
            {
                // Card does not exist.

                // Insert the Card. Just like inserting a User, the Card's Object ID is attached upon insertion.
                String newCardObjectId = cnn.InsertCard(newCard);

                // Just like User, if the returned Object Id is null, then the insertion failed.
                cardInsertedOrUpdated = newCardObjectId == null;

                // Alternatively, with both User and Card, you can insert with just the fields, instead of creating a brand new Object every time.
                //cnn.InsertCard(
                //    newCard.DisplayName, 
                //    newCard.ObjectGuid, 
                //    newCard.EntityRelationshipGuid.Value, 
                //    newCard.EntityRelationshipId, 
                //    newCard.CardNumber,
                //    newCard.ActivationDate, 
                //    newCard.ExpirationDate);
            }
        }
    }
}
