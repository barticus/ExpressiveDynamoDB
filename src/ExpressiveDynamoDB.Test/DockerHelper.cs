using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Docker.DotNet;
using Docker.DotNet.Models;
using NUnit.Framework;

namespace ExpressiveDynamoDB.Test
{
    public static class DockerHelper
    {
        private static DockerClient? Client = null;
        private static string ImageName = "amazon/dynamodb-local";
        private static string Tag = "latest";
        private static string Port = "8000";
        private static string EndpointUrl = "http://localhost:8000";

        public static async Task<string> StartDynamoDBAsync()
        {
            if (Client != null) return EndpointUrl;

            Client = new DockerClientConfiguration().CreateClient();

            // look for container
            var container = (await Client.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    Limit = 10,
                    Filters = new Dictionary<string, IDictionary<string, bool>> {
                        {"ancestor", new Dictionary<string, bool> {
                            { ImageName, true }
                        }}
                    }
                })
            ).FirstOrDefault();

            if (container?.State == "running")
            {
                return EndpointUrl;
            }

            //look for image
            // var image = (await Client.Images.ListImagesAsync(new ImagesListParameters()
            // {
            //     MatchName = $"{ImageName}:{Tag}",
            // }, CancellationToken.None)).FirstOrDefault();

            // if (image == null)
            //     throw new Exception($"Docker image for {ImageName}:{Tag} not found.");

            var containerId = container?.ID;

            //create container from image
            if (container == null)
            {
                var newContainer = await Client.Containers.CreateContainerAsync(new CreateContainerParameters()
                {

                    ExposedPorts = new Dictionary<string, EmptyStruct>()
                    {
                        [Port] = new EmptyStruct()
                    },
                    HostConfig = new HostConfig()
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>()
                        {
                            [Port] = new List<PortBinding>()
                            {new PortBinding() {HostIP = "127.0.0.1", HostPort = Port}}
                        }
                    },
                    Image = $"{ImageName}:{Tag}",
                }, CancellationToken.None);
                containerId = newContainer.ID;
            }

            if (!await Client.Containers.StartContainerAsync(containerId, new ContainerStartParameters()
            {
                DetachKeys = $"d={ImageName}"
            }, CancellationToken.None))
            {
                throw new Exception($"Could not start container: {containerId}");
            }

            var count = 10;
            Thread.Sleep(5000);
            var containerStat = await Client.Containers.InspectContainerAsync(containerId, CancellationToken.None);
            while (!containerStat.State.Running && count-- > 0)
            {
                Thread.Sleep(1000);
                containerStat = await Client.Containers.InspectContainerAsync(containerId, CancellationToken.None);
            }

            return EndpointUrl;
        }

        public static async Task<bool> CreateTableIfNotExists(
            IAmazonDynamoDB client, 
            string tableName, 
            List<KeySchemaElement> keySchema,
            List<AttributeDefinition> attributeDefinitions, 
            ProvisionedThroughput provisionedThroughput
        )
        {
            var existingTables = await client.ListTablesAsync();
            if(existingTables.TableNames.Contains(tableName))
            {
                //await client.DeleteTableAsync(tableName);
                return false;
            }
            await client.CreateTableAsync(tableName, keySchema, attributeDefinitions, provisionedThroughput);
            return true;
        }
    }
}