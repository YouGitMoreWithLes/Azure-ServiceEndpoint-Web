using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Azure_ServiceEndpoint_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IConfiguration Configuration;

        // Endpoint of Cosmos DB Account
        private const string EndpointUrl = "https://westus-sep-test-cosmos-db.documents.azure.com:443/";
        //<your-account>.documents.azure.com:443/";

        // Primary Key for Cosmos DB account
        private const string AuthorizationKey = "X4gv9KTA1U14A6hG2aweXV8DuzWadHhkfXyfwjE5Mos2Q44iMJGW9LnvYsieXxC4fER2zEASGCVQlQLxjupQ2A==";

        // The name of database
        private const string DatabaseId = "testDB";

        // The name of container
        private const string ContainerId = "config";

        public ConfigController(IConfiguration configuration, IHttpContextAccessor accessor)
        {
            _accessor = accessor;
            Configuration = configuration;
        }

        /// <summary>
        /// SecretName (Name in Key Vault: 'SecretName')
        /// Obtained from Configuration with Configuration[""SecretName""]
        /// Value: {Configuration["SecretName"]}
        /// Section:SecretName (Name in Key Vault: 'Section--SecretName')
        /// Obtained from Configuration with Configuration[""Section:SecretName""]
        /// Value: {Configuration["Section:SecretName"]}
        /// Section:SecretName (Name in Key Vault: 'Section--SecretName')
        /// Obtained from Configuration with Configuration.GetSection(""Section"")[""SecretName""]
        /// Value: {Configuration.GetSection("Section")["SecretName"]}";
        /// </summary>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            List<string> results = new List<string>() 
            { 
                DateTime.Now.ToString(),
                $"Configuration[\"mySecretName\"] : {Configuration["mySecretName"]}",
                $"Configuration[\"mySection:mySecretName\"] : {Configuration["mySection:mySecretName"]}",
                $"Configuration.GetmySection(\"mySection\")[\"mySecretName\"] : {Configuration.GetSection("mySection")["mySecretName"]}"
            };
            // {"mySection":"mySecretName"}
            return results;
        }

        // GET: api/Config/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            string result = "started";
            try
            {
                System.Threading.CancellationTokenSource cs = new System.Threading.CancellationTokenSource();
                using (CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey))
                {
                    Database database = cosmosClient.GetDatabase(DatabaseId);
                    Container container = database.GetContainer(ContainerId);

                    string sqlA = $"SELECT * FROM config c WHERE c.id = '{id}'";
                    FeedIterator<Models.Config> queryA = container.GetItemQueryIterator<Models.Config>(new QueryDefinition(sqlA), requestOptions: new QueryRequestOptions { MaxConcurrency = 1 });
                    if(queryA.HasMoreResults)
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        };
                        result = JsonSerializer.Serialize(queryA.ReadNextAsync().Result.First(), options);
                    }
                }
            }
            catch (Exception e)
            {
                result =  e.GetBaseException().Message;
            }

            try
            {
                result = $"{{{result}, \"ip\": \"{_accessor.HttpContext.Connection.RemoteIpAddress.ToString()}\" }}";
            }
            catch(Exception e)
            {
                result = $"{{{result}, \"error\": \"{e.GetBaseException().Message}\" }}";
            }

            return result;
        }
    }
}
