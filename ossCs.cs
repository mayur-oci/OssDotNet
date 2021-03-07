/*
 * Copyright (c) 2020, 2021, Oracle and/or its affiliates. All rights reserved.
 * This software is dual-licensed to you under the Universal Permissive License (UPL) 1.0 as shown at https://oss.oracle.com/licenses/upl or Apache License 2.0 as shown at http://www.apache.org/licenses/LICENSE-2.0. You may choose either license.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Oci.Common.Auth;
using Oci.Common.Waiters;
using Oci.StreamingService;
using Oci.StreamingService.Models;
using Oci.StreamingService.Requests;
using Oci.StreamingService.Responses;

namespace Oci.Examples
{
    /**
    * This class provides an example of basic streaming usage.
    * - List streams
    * - Get a stream
    * - Create a stream
    * - Delete a stream
    * - Publish to a stream
    * - Consume from a stream, using a partition cursor
    * - Consume from a stream, using a group cursor
    */

    public class StreamsExample
    {
    
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static string STREAM_NAME = "dotnet-sdk-stream-example";
        private static int PARTITIONS = 1;

        public static async Task Main()
        {
            Console.WriteLine("Starting example");
            
            var provider = new ConfigFileAuthenticationDetailsProvider("DEFAULT");
            string compartmentId = "ocid1.tenancy.oc1..aaaaaaaaopbu45aomik7sswe4nzzll3f6ii6pipd5ttw4ayoozez37qqmh3a";
            //Environment.GetEnvironmentVariable("OCI_COMPARTMENT_ID");

            StreamAdminClient streamAdminClient = new StreamAdminClient(provider);
            StreamClient streamClient = new StreamClient(provider);
            string streamId = null;

            try
            {
                Stream stream = await GetOrCreateStream(streamAdminClient, compartmentId, STREAM_NAME, PARTITIONS);
                streamId = stream.Id;

                // Streams are assigned a specific endpoint url based on where they are provisioned.
                // Create a stream client using the provided message endpoint.
                streamClient.SetEndpoint(stream.MessagesEndpoint);

                // publish some messages to the stream
                await PublishExampleMessages(streamClient, streamId);

                // give the streaming service a second to propagate messages
                await Task.Delay(1000);

                // Use a cursor for getting messages; each getMessages call will return a next-cursor for iteration.
                // There are a couple kinds of cursors.

                // A cursor can be created at a given partition/offset.
                // This gives explicit offset management control to the consumer.
                Console.WriteLine("Starting a simple message loop with a partition cursor");
                string partitionCursor = await GetCursorByPartition(streamClient, streamId, "0");
                await SimpleMessageLoop(streamClient, streamId, partitionCursor);

                // A cursor can be created as part of a consumer group.
                // Committed offsets are managed for the group, and partitions
                // are dynamically balanced amongst consumers in the group.
                Console.WriteLine("Starting a simple message loop with a group cursor");
                string groupCursor = await GetCursorByGroup(streamClient, streamId, "exampleGroup", "exampleInstance-1");
                await SimpleMessageLoop(streamClient, streamId, groupCursor);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Streaming example failed: {e}");
            }
            finally
            {
                // Cleanup; remember to delete streams which are not in use.
                Console.WriteLine("Deleting the stream");
                await DeleteStream(streamAdminClient, streamId);

                // Stream deletion is an asynchronous operation, give it some time to complete.
                GetStreamRequest getStreamRequest = new GetStreamRequest
                {
                    StreamId = streamId
                };
                Console.WriteLine("Waiting to confirm the deletion the stream");
                streamAdminClient.Waiters.ForStream(getStreamRequest, Stream.LifecycleStateEnum.Deleted).Execute();

                streamAdminClient.Dispose();

                Console.WriteLine("End example");
            } 
        }

        private static async Task<Stream> GetOrCreateStream(StreamAdminClient client, string compartmentId, string streamName, int partitions)
        {

            ListStreamsRequest listRequest = new ListStreamsRequest
            {
                CompartmentId = compartmentId,
                LifecycleState = Stream.LifecycleStateEnum.Active,
                Name = streamName
            };
            ListStreamsResponse listStreamsResponse = await client.ListStreams(listRequest);

            if (listStreamsResponse.Items.Count != 0)
            {
                // if we find an active stream with the correct name, we'll use it.
                Console.WriteLine($"An active stream named {streamName} was found");

                string streamId = listStreamsResponse.Items[0].Id;
                return await GetStream(client, streamId);
            }

            Console.WriteLine($"No active stream named {streamName} was found; creating it now");
            Stream createdStream = await CreateStream(client, compartmentId, streamName, partitions);

            // GetStream provides details about a specific stream.
            // Since stream creation is asynchronous; we need to wait for the stream to become active.
            WaiterConfiguration waiterConfiguration = new WaiterConfiguration
            {
                MaxAttempts = 20,
                GetNextDelayInSeconds = DelayStrategy.GetExponentialDelayInSeconds
            };
            GetStreamRequest streamRequest = new GetStreamRequest
            {
                StreamId = createdStream.Id
            };
            Stream activeStream = client.Waiters.ForStream(streamRequest, waiterConfiguration, Stream.LifecycleStateEnum.Active).Execute().Stream;

            // Give a little time for the stream to be ready.
            await Task.Delay(1000);
            return activeStream;
        }

        private static async Task<Stream> GetStream(StreamAdminClient adminClient, string streamId)
        {
            GetStreamRequest getStreamRequest = new GetStreamRequest
            {
                StreamId = streamId
            };
            GetStreamResponse getStreamResponse = await adminClient.GetStream(getStreamRequest);

            return getStreamResponse.Stream;
        }

        private static async Task<Stream> CreateStream(StreamAdminClient client, string compartmentId, string streamName, int partitions)
        {
            Console.WriteLine($"Creating stream {streamName} with {partitions} partitions");

            CreateStreamDetails createStreamDetails = new CreateStreamDetails
            {
                CompartmentId = compartmentId,
                Name = streamName,
                Partitions = partitions
            };
            CreateStreamRequest createStreamRequest = new CreateStreamRequest
            {
                CreateStreamDetails = createStreamDetails
            };
            CreateStreamResponse createStreamResponse = await client.CreateStream(createStreamRequest);

            return createStreamResponse.Stream;
        }

        private static async Task<string> GetCursorByPartition(StreamClient streamClient, string streamId, string partition)
        {
            Console.WriteLine($"Creating a cursor for partition {partition}");

            CreateCursorDetails createCursorDetails = new CreateCursorDetails
            {
                Partition = partition,
                Type = CreateCursorDetails.TypeEnum.TrimHorizon
            };
            CreateCursorRequest createCursorRequest = new CreateCursorRequest
            {
                StreamId = streamId,
                CreateCursorDetails = createCursorDetails
            };
            CreateCursorResponse createCursorResponse = await streamClient.CreateCursor(createCursorRequest);

            return createCursorResponse.Cursor.Value;
        }

        private static async Task<string> GetCursorByGroup(StreamClient streamClient, string streamId, string groupName, string instanceName)
        {
            Console.WriteLine($"Creating a cursor for group {groupName}, instance {instanceName}");

            CreateGroupCursorDetails createGroupCursorDetails = new CreateGroupCursorDetails
            {
                GroupName = groupName,
                InstanceName = instanceName,
                Type = CreateGroupCursorDetails.TypeEnum.TrimHorizon,
                CommitOnGet = true
            };
            CreateGroupCursorRequest createCursorRequest = new CreateGroupCursorRequest
            {
                StreamId = streamId,
                CreateGroupCursorDetails = createGroupCursorDetails
            };
            CreateGroupCursorResponse groupCursorResponse = await streamClient.CreateGroupCursor(createCursorRequest);

            return groupCursorResponse.Cursor.Value;
        }

        private static async Task PublishExampleMessages(StreamClient streamClient, string streamId)
        {
            // build up a putRequest and publish some messages to the stream
            List<PutMessagesDetailsEntry> messages = new List<PutMessagesDetailsEntry>();
            for (int i = 0; i < 100; i++)
            {
                PutMessagesDetailsEntry detailsEntry = new PutMessagesDetailsEntry
                {
                    Key = Encoding.UTF8.GetBytes($"messagekey-{i}"),
                    Value = Encoding.UTF8.GetBytes($"messageValue-{i}")
                };
                messages.Add(detailsEntry);
            }

            Console.WriteLine($"Publishing {messages.Count} messages to stream {streamId}");
            PutMessagesDetails messagesDetails = new PutMessagesDetails
            {
                Messages = messages
            };
            PutMessagesRequest putRequest = new PutMessagesRequest
            {
                StreamId = streamId,
                PutMessagesDetails = messagesDetails
            };
            PutMessagesResponse putResponse = await streamClient.PutMessages(putRequest);

            // the putResponse can contain some useful metadata for handling failures
            foreach (PutMessagesResultEntry entry in putResponse.PutMessagesResult.Entries)
            {
                if (entry.Error != null)
                {
                    Console.WriteLine($"Error({entry.Error}): {entry.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine($"Published message to partition {entry.Partition}, offset {entry.Offset}");
                }
            }
        }

        private static async Task SimpleMessageLoop(StreamClient streamClient, string streamId, string initialCursor)
        {
            string cursor = initialCursor;
            for (int i = 0; i < 10; i++)
            {

                GetMessagesRequest getMessagesRequest = new GetMessagesRequest
                {
                    StreamId = streamId,
                    Cursor = cursor,
                    Limit = 10
                };
                GetMessagesResponse getResponse = await streamClient.GetMessages(getMessagesRequest);

                // process the messages
                Console.WriteLine($"Read {getResponse.Items.Count}");
                foreach (Message message in getResponse.Items)
                {
                    Console.WriteLine($"{Encoding.UTF8.GetString(message.Key)} : {Encoding.UTF8.GetString(message.Value)}");
                }

                // getMessages is a throttled method; clients should retrieve sufficiently large message
                // batches, as to avoid too many http requests.
                await Task.Delay(1000);

                // use the next-cursor for iteration
                cursor = getResponse.OpcNextCursor;
            }
        }

        private static async Task DeleteStream(StreamAdminClient adminClient, String streamId)
        {
            Console.WriteLine($"Deleting stream {streamId}");
            DeleteStreamRequest deleteStreamRequest = new DeleteStreamRequest
            {
                StreamId = streamId
            };
            await adminClient.DeleteStream(deleteStreamRequest);

            WaiterConfiguration waiterConfiguration = new WaiterConfiguration
            {
                MaxAttempts = 20,
                GetNextDelayInSeconds = DelayStrategy.GetExponentialDelayInSeconds
            };
            GetStreamRequest getStreamRequest = new GetStreamRequest
            {
                StreamId = streamId
            };
            adminClient.Waiters.ForStream(getStreamRequest, waiterConfiguration, Stream.LifecycleStateEnum.Deleted).Execute();
        }
    }
}