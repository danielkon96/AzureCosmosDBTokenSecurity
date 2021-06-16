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
using System.Collections.Generic;

namespace Microsoft.Cosmosdb.Security.Demo.Test
{
    public class CustomClass {
        public string Id { get;set; }
    }

    public static class TestUserToken
    {
        [FunctionName("TestUserToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Get the container you created a user for and the token associated with that user.
            string containerName = req.Query["ContainerName"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TokenClass tokenClass = JsonConvert.DeserializeObject<TokenClass>(requestBody);
            string token = tokenClass.token;

            Console.WriteLine(token);

            using (CosmosClient client = new CosmosClient("https://cosmosdbsecurity.documents.azure.com:443/", token)) {

                // Set up database.
                Database database = client.GetDatabase("SalesDatabase");

                // In one scenario, we read from the container we created a user for.
                // In the second secnario, we read from a container that doesn't have user access.
                Container container = client.GetContainer("SalesDatabase", containerName);

                // Try to delete an item
                //var result = await container.DeleteItemAsync<CustomClass>("coolbeans3", new PartitionKey("012345"));
                
                // How to read a specific item and the partition key.
                /*CustomClass ok = await container.ReadItemAsync<CustomClass>("coolbeans2", new PartitionKey("012345"));
                Console.WriteLine("\tRead {0}\n", ok.Id);*/

                // We query the entire container.
                var sqlQueryText = "SELECT * FROM c";
                Console.WriteLine("Running query: {0}\n", sqlQueryText);

                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<CustomClass> queryResultSetIterator = container.GetItemQueryIterator<CustomClass>(
                    queryDefinition
                    /*
                    // Add requestOptions if you are querying with a partition key.
                    requestOptions: new QueryRequestOptions() 
                    {
                        PartitionKey = new PartitionKey("012345")
                    }
                    */
                    );
                List<CustomClass> customRequests = new List<CustomClass>();

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<CustomClass> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (CustomClass customClass in currentResultSet)
                    {
                        customRequests.Add(customClass);
                        Console.WriteLine("\tRead {0}\n", customClass.Id);
                    }
                }
        
                return new OkObjectResult("We successfully read from the container in the database.");
            }
        }
    }
}
