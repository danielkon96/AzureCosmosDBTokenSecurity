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

namespace Microsoft.Cosmosdb.test.insert
{
    public static class InsertWithUserToken
    {
        [FunctionName("InsertWithUserToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string containerName = req.Query["ContainerName"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TokenClass tokenClass = JsonConvert.DeserializeObject<TokenClass>(requestBody);
            string token = tokenClass.token;

            using (CosmosClient client = new CosmosClient("https://cosmosdbsecurity.documents.azure.com:443/", token)) {
                // Set up database and container
                Database database = client.GetDatabase("SalesDatabase");
                Container container = client.GetContainer("SalesDatabase", containerName);

                // Insert into the database and see that I only have read permissions.
                var newItem = new {
                    id = Guid.NewGuid(),
                    containerName = containerName
                };
                await container.CreateItemAsync(newItem);
            }

            return new OkObjectResult("We successfully inserted into the container.");
        }
    }
}
