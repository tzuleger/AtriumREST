using System;
using System.Collections.Generic;

namespace ThreeRiversTech.Zuleger.Atrium.API
{
    class Program
    {
        public static void Main(String[] args)
        {
            // Set maximum number of attempts and delay between attempts to connect to Atrium Controller. (Optional, done by default to 10 attempts/10 seconds per attempt
            AtriumController.MaxAttempts = 10;
            AtriumController.DelayBetweenAttempts = 30;

            // Connect to Atrium Controller on A22K Public Demo by CDVI under username "admin" and password "admin"
            AtriumController controller = new AtriumController("admin", "admin", "http://69.70.57.94:8083/");

            // After object creation, the connection will have properties reflecting information on the controller connected to.
            Console.WriteLine($"Connected to {controller.ProductName}, {controller.ProductLabel} v{controller.ProductVersion} (SN: {controller.SerialNumber})");

            Guid userId = controller.GenerateGuid;
            Guid cardId = controller.GenerateGuid;

            // Insert User
            String userOID = controller.InsertUser(
                "Test User", // First Name
                "via Atrium API", // Last Name
                userId, // User GUID
                DateTime.Now.AddDays(-13), // Activation Date
                DateTime.Now.AddYears(2) // Expiration Date
            );

            // See generated XML by fetching RequestText and ResponseText parameters.
            Console.WriteLine($"Inserted user. Request Text:\n{controller.RequestText}\n\nResponse Text:\n{controller.ResponseText}");

            // Insert Card attached to respective inserted User
            String cardOID = controller.InsertCard(
                "TEST USER CARD", // DisplayName
                cardId, // Card GUID
                userId, // User GUID
                userOID, // User Object ID
                43022, // Card Number (nothing significant about 43022)
                DateTime.Now.AddDays(-13), // Activation Date
                DateTime.Now.AddYears(2) // Expiration Date
            );

            Console.WriteLine($"Inserted card for user. Request Text:\n{controller.RequestText}\n\nResponse Text:\n{controller.ResponseText}");

            // Grab all Users with the First Name of "Test User" and Last Name of "via Atrium API".
            Dictionary<String, String> user = controller.GetUserByName("Test User", "via Atrium API");

            DateTime date = DateTime.Now.AddDays(24);
            bool cardUpdated = false;
            bool userUpdated = false;

            // Since we are confident there is only one user with the first name"Test User" and lastname "via Atrium API", then update the zeroth index of users.
            userUpdated = controller.UpdateUser(user["objectID"], "TEST USER", "VIA ATRIUM API", date, date.AddYears(1).AddDays(7));
            if (userUpdated) // If the user is successfully updated.
            {
                // Then let's do the same to the User's respective card.
                Dictionary<String, String> card = controller.GetCardByUserID(user["userID"]);
                cardUpdated = controller.UpdateCard(card["objectID"], "Test User via Atrium API Card", date, date.AddYears(1));
            }

            // Finally, print to the User (of the program) the results if the User and Card has been updated successfully.
            Console.WriteLine($"User updated: {userUpdated}, Card updated: {cardUpdated}.");
        }
    }
}
