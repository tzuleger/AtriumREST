using System;
using System.Linq;
using System.Collections.Generic;

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
            // Variables for making sure updates/inserts occur.
            AtriumController cnn = null;
            var username = "admin";
            var password = "admin";
            var address = "http://69.70.57.94/";
            Console.WriteLine($"Connecting to {address} as \"{username}\" with password \"{password}\"");
            cnn = new AtriumController(username, password, address);
            Console.WriteLine($"Successfully connected. Device Info: {cnn.ProductName}, {cnn.ProductLabel} (v{cnn.ProductVersion}) - SN: {cnn.SerialNumber}");

            List<User> users = cnn.GetAll<User>();
            List<Card> cards = cnn.GetAll<Card>();

            User u = new User
            {
                FirstName = "John",
                LastName = "Doe",
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(3).AddDays(7),
                AccessLevelObjectIds = AtriumController.ACCESS_LEVELS(
                    access_level_door_1_only,
                    access_level_area_a,
                    access_level_warehouse)
            };
            Card c = new Card
            {
                DisplayName = "John Doe Card",
                CardNumberLo = AtriumController.To26BitCardNumber(r.Next(0, 100), r.Next(0, 8000)),
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(3).AddDays(7),
            };

            Func<User, bool> userPred = (user => user.FirstName == "Jerry" && user.LastName == u.LastName);
            // Update the card if the FirstName and LastName are equal.
            if(users.Any(userPred))
            {
                User existingUser = users.Where(userPred).First();
                existingUser.Copy(u, true);
                Console.WriteLine($"User exists. Updating {existingUser} (Object ID: {existingUser.ObjectId}).");
                if (cnn.Update(existingUser))
                {
                    Console.WriteLine($"User successfully updated. Object ID: {u.ObjectId}");
                    c.EntityRelationshipId = u.ObjectId;
                    c.EntityRelationshipGuid = u.ObjectGuid;
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
                if(u.ObjectId != null)
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
            if(cards.Any(cardPred) && checkForUpdate)
            {
                foreach(Card existingCard in cards.Where(cardPred))
                {
                    Console.WriteLine($"Card exists. Updating {existingCard} (Object ID: {existingCard.ObjectId}).");
                    existingCard.Copy(c, true, false);
                    if (cnn.Update(existingCard))
                    {
                        Console.WriteLine($"Card successfully updated. Object ID: {c.ObjectId}");
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

            cnn.Close();
        }
    }
}
