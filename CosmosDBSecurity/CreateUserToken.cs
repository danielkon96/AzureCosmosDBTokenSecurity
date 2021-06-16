using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Cosmosdb.Security.Demo
{
    public static class CreateUserToken
    {
        [FunctionName("CreateUserToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Get the preferred username, and the container you want to create a user for.
            string name = req.Query["Username"];
            string containerName = req.Query["ContainerName"];
            string permissionModeStr = req.Query["PermissionMode"];


            PermissionMode pm = PermissionMode.Read; //Read is the default.
            // Set the permission mode to all if the user requested all.
            if (permissionModeStr.Equals("all")) 
            {
                pm = PermissionMode.All;
            }

            using (CosmosClient client = new CosmosClient("https://cosmosdbsecurity.documents.azure.com:443/", "paUrHVSzpHpxcbnq4smm8ofwUgd2VktGVzwSsaTEz2EBvzsFjPY0LfKgAtd6Y5NjcY4UqihNIIWlpJy9GDy0jg==")) 
            {
                // Set up database.
                Database database = client.GetDatabase("SalesDatabase");

                // To delete a user in code.
                //database.GetUser("Username").DeleteAsync();

                // Get the container you want to create a user for.
                Container container = client.GetContainer("SalesDatabase", containerName);

                // Create a user with the passed name
                User user = await database.CreateUserAsync(name);

                // Give the user read permissions on our container.
                int TTLinSeconds = 601;
                PermissionProperties readContainerPermission = await user.CreatePermissionAsync(
                                                                new PermissionProperties(
                                                                    id: "permissionUser1Orders",
                                                                    permissionMode: pm,     // read or r/w
                                                                    container: container),
                                                                    tokenExpiryInSeconds: TTLinSeconds
                                                                    //resourcePartitionKey: new PartitionKey("012345"), // Allows users to query DB Container under this partition key.
                                                                    //itemId: "coolbeans3") // Allows users to query specific item with this id only.
                                                                );

                // Create the document and insert it into a UserTokens container.
                var userTokenDoc = new {
                    id = Guid.NewGuid(),
                    containerName = containerName,
                    userToken = readContainerPermission.Token,
                    userPermissionType = permissionModeStr
                };

                // Insert the user token into the database.
                Container containerTokens = client.GetContainer("SalesDatabase", "UserTokensContainer");
                await containerTokens.CreateItemAsync(userTokenDoc);

                return new OkObjectResult("Your user token is: " + readContainerPermission.Token);
            }
        }
    }
}
