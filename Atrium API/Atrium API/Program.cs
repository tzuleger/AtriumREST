using System;
using System.Collections.Generic;

namespace ThreeRiversTech.Zuleger.Atrium.API
{
    class Program
    {
        public static void Main(String[] args)
        {
            // Set maximum number of attempts and delay between attempts to connect to Atrium Controller. (Optional, done by default to 10 attempts/10 seconds per attempt
            AtriumConnection.MaxAttempts = 10;
            AtriumConnection.DelayBetweenAttempts = 30;

            // Connect to Atrium Controller on A22K Public Demo by CDVI under username "admin" and password "admin"
            AtriumConnection atrium = new AtriumConnection("admin", "admin", "http://69.70.57.94:8083/");

            // After object creation, the connection will have properties reflecting information on the controller connected to.
            Console.WriteLine($"Connected to {atrium.ProductName}, {atrium.ProductLabel} v{atrium.ProductVersion} (SN: {atrium.SerialNumber})");

            Guid userId = atrium.GenerateGuid;
            Guid cardId = atrium.GenerateGuid;

            // Insert User
            String userOID = atrium.InsertUser(
                "Test User", // First Name
                "via Atrium API", // Last Name
                userId, // User GUID
                DateTime.Now.AddDays(-13), // Activation Date
                DateTime.Now.AddYears(2) // Expiration Date
            );

            // See generated XML by fetching RequestText and ResponseText parameters.
            Console.WriteLine($"Inserted user. Request Text:\n{atrium.RequestText}\n\nResponse Text:\n{atrium.ResponseText}");

            // Insert Card attached to respective inserted User
            String cardOID = atrium.InsertCard(
                "TEST USER CARD", // DisplayName
                cardId, // Card GUID
                userId, // User GUID
                userOID, // User Object ID
                43022, // Card Number (nothing significant about 43022)
                DateTime.Now.AddDays(-13), // Activation Date
                DateTime.Now.AddYears(2) // Expiration Date
            ); 

            // Grab all Users with the First Name of "Test User" and Last Name of "via Atrium API".
            List<Dictionary<String, String>> users = atrium.GetUsersByName("Test User", "via Atrium API");

            DateTime date = DateTime.Now.AddDays(24);
            bool cardUpdated = false;
            bool userUpdated = false;

            // Since we are confident there is only one user with the first name"Test User" and lastname "via Atrium API", then update the zeroth index of users.
            userUpdated = atrium.UpdateUser(users[0]["objectID"], "TEST USER", "VIA ATRIUM API", date, date.AddYears(1).AddDays(7));
            if (userUpdated) // If the user is successfully updated.
            {
                // Then let's do the same to the User's respective card.
                var cards = atrium.GetCardsByUserID(users[0]["userID"]);
                cardUpdated = atrium.UpdateCard(cards[0]["objectID"], "Test User via Atrium API Card", date, date.AddYears(1));
            }

            // Finally, print to the User (of the program) the results if the User and Card has been updated successfully.
            Console.WriteLine($"User updated: {userUpdated}, Card updated: {cardUpdated}.");
        }
    }
}
