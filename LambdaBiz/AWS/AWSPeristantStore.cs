using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaBiz.Model;
using Newtonsoft.Json;

namespace LambdaBiz.AWS
{
    internal class AWSPeristantStore : IPersistantStore
    {
        private AmazonDynamoDBClient _amazonDynamoDbClient;
        internal AWSPeristantStore(string awsAccessKey, string awsSecretAccessKey, string awsRegion)
        {
            _amazonDynamoDbClient = new AmazonDynamoDBClient(awsAccessKey, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));

        }
 
        public async Task<Workflow> GetCurrentStateAsync(string orchestrationId)
        {
            var getItemRequest = new GetItemRequest();
            getItemRequest.TableName = Constants.LAMBDA_BIZ_DYNAMODB_TABLE;
            getItemRequest.Key = new Dictionary<string, AttributeValue>();
            getItemRequest.Key.Add(Constants.LAMBDA_BIZ_ORCHESTRATION_ID, new AttributeValue
            {
                S = orchestrationId
            });

            var getItemResponse = await _amazonDynamoDbClient.GetItemAsync(getItemRequest);

            var attributes = getItemResponse.Item[Constants.LAMBDA_BIZ_WF_ATTRIBUTES];
            var workflow = JsonConvert.DeserializeObject<Workflow>(attributes.S);
            workflow.Activities = new List<Activity>();

            foreach (var attribute in getItemResponse.Item)
            {
                if(attribute.Key != Constants.LAMBDA_BIZ_WF_ATTRIBUTES
                    && attribute.Key != Constants.LAMBDA_BIZ_ORCHESTRATION_ID)
                {
                    (workflow.Activities as IList<Activity>).Add(JsonConvert.DeserializeObject<Activity>(attribute.Value.S));
                }
            }

            return workflow;
        }

        public async Task CreateStoreAsync()
        {
            await _amazonDynamoDbClient.CreateTableAsync(new CreateTableRequest
            {
                KeySchema = new List<KeySchemaElement>()
                {
                    new KeySchemaElement
                    {
                        AttributeName = Constants.LAMBDA_BIZ_ORCHESTRATION_ID,
                        KeyType = KeyType.HASH                        
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new AttributeDefinition
                    {
                        AttributeName = Constants.LAMBDA_BIZ_ORCHESTRATION_ID,
                        AttributeType = ScalarAttributeType.S
                    }
                },
                BillingMode= BillingMode.PAY_PER_REQUEST,
                TableName = Constants.LAMBDA_BIZ_DYNAMODB_TABLE
            });
        }

        public async Task LogStateAsync(Workflow workflow)
        {
            var putItemRequest = new PutItemRequest();
            putItemRequest.Item = new Dictionary<string, AttributeValue>();
            putItemRequest.Item = new Dictionary<string, AttributeValue>();
            putItemRequest.Item.Add(Constants.LAMBDA_BIZ_ORCHESTRATION_ID, new AttributeValue
            {
                S = workflow.OrchestrationId
            });


            foreach (var activity in workflow.Activities)
            {
                if (activity.Name != Constants.LAMBDA_BIZ_EVENT)
                {
                    putItemRequest.Item.Add(activity.UniqueId, new AttributeValue
                    {
                        S = JsonConvert.SerializeObject(activity)
                    });

                    
                }

            }

            putItemRequest.Item.Add(Constants.LAMBDA_BIZ_WF_ATTRIBUTES, new AttributeValue
            {
                S = workflow.ToString()
            });

            putItemRequest.TableName = Constants.LAMBDA_BIZ_DYNAMODB_TABLE;

            await _amazonDynamoDbClient.PutItemAsync(putItemRequest);
        }

        public async Task<bool> StoreExistsAsync()
        {
            bool exists = false;
            var tables = await _amazonDynamoDbClient.ListTablesAsync();

            if(tables != null && tables.TableNames != null)
            {
                if (tables.TableNames.Contains(Constants.LAMBDA_BIZ_DYNAMODB_TABLE))
                    exists = true;
            }

            return exists;
        }
    }
}
