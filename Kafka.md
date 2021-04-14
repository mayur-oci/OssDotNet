


# Quickstart with Kafka .NET Client for OSS

This quickstart shows how to produce messages to and consume messages from an [**Oracle Streaming Service**](https://docs.oracle.com/en-us/iaas/Content/Streaming/Concepts/streamingoverview.htm) using the [Kafka .NET  Client](https://docs.confluent.io/clients-confluent-kafka-dotnet/current/overview.html). We are going to use C# language for these examples.
Please note, OSS is API compatible with Apache Kafka. Hence developers who are already familiar with Kafka need to make only few minimal changes to their Kafka client code, like config values like endpoint for Kafka brokers!  

## Prerequisites

1. You need have OCI account subscription or free account. typical links @jb
2. Follow [these steps](https://github.com/mayur-oci/OssJs/blob/main/JavaScript/CreateStream.md) to create Streampool and Stream in OCI. If you do  already have stream created, refer step 4 [here](https://github.com/mayur-oci/OssJs/blob/main/JavaScript/CreateStream.md) to capture information related to `Kafka Connection Settings`. We need this Information for upcoming steps.
3. The  [.NET 5.0 SDK or later](https://dotnet.microsoft.com/download). Make sure *dotnet* is in your *PATH* environment variable.
4. VS code(recommended) with with the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) installed. For information about how to install extensions on Visual Studio Code, see [VS Code Extension Marketplace](https://code.visualstudio.com/docs/editor/extension-gallery). In this tutorial we create and run a simple .NET console application by using Visual Studio Code and the .NET CLI,  as quick demonstration of how to use OCI .NET SDK for OSS. Project tasks, such as creating, compiling, and running a project are done by using the .NET CLI. You can follow this tutorial with a different IDE and run commands in a terminal if you prefer. 
5. Authentication with the Kafka protocol uses auth-tokens and the SASL/PLAIN mechanism. Follow [Working with Auth Tokens](https://docs.oracle.com/en-us/iaas/Content/Identity/Tasks/managingcredentials.htm#Working) for auth-token generation. 
Since you have created the stream(aka Kafka Topic) and Streampool in OCI, you are already authorized to use this stream as per OCI IAM.  Hence create auth-token for your user in OCI. These `OCI user auth-tokens` are visible only once at the time of creation. Hence please copy it and keep it somewhere safe, as we are going to need it later.
6. You need to install the CA root certificates on your host(where you are going developing and running this quickstart). These are required for secure access OSS. For [Windows](https://docs.confluent.io/platform/current/tutorials/examples/clients/docs/csharp.html#prerequisites), please download `cacert.pem` file distributed with curl ([download cacert.pm](https://curl.haxx.se/ca/cacert.pem)). For other platforms, please refer to [Root CA for other platforms](https://docs.confluent.io/platform/current/tutorials/examples/clients/docs/csharp.html#configure-ssl-trust-store)


## Producing messages to OSS
1. Open your favorite editor, such as [Visual Studio Code](https://code.visualstudio.com) from the empty working directory `wd`. 
2. Open the terminal and `cd` into `wd` directory. 
3. Create C# .NET console application by running the following command on the terminal
```Shell
  $:/path/to/wd/directory>dotnet new console
    The template "Console Application" was created successfully.
```
This will create Program.cs file with C# code for simple HellowWorld application.

4. To reference confluent-kafka-dotnet library in your just created .NET Core project, execute the following command in your project’s directory `wd`
```Shell
  $:/path/to/wd/directory>dotnet add package Confluent.Kafka
``` 
5. Replace the code in *Program.cs* in directory *wd* with following code.
You also need to replace after you replace values of config variables in the map`ProducerConfig` and the name of `topic` is the name of stream you created. You should already have all the `Kafka config info` and topic name(stream name) from the step 2 of the *Prerequisites* section of this tutorial. 
```C#
using System;
using Confluent.Kafka;

namespace OssProducerWithKafkaApi
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Demo for using Kafka APIs seamlessly with OSS");

            var config = new ProducerConfig {
                            BootstrapServers = "[end point of the bootstrap servers]", //usually of the form cell-1.streaming.[region code].oci.oraclecloud.com:9092
                            SslCaLocation = "path\to\root\ca\certificate\*.pem",
                            SecurityProtocol = SecurityProtocol.SaslSsl,
                            SaslMechanism = SaslMechanism.Plain,
                            SaslUsername = "[OCI_TENANCY_NAME]/[YOUR_OCI_USERNAME]/[OCID_FOR_STREAMPOOL_YOU_CREATED]",
                            SaslPassword = "[Your OCI User Auth-Token]", // use the auth-token you created step 5 of Prerequisites section 
                            };

            Produce("topicName", config); // use the name of the stream you created

        }

        static void Produce(string topic, ClientConfig config)
        {
            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                int numProduced = 0;
                int numMessages = 10;
                for (int i=0; i<numMessages; ++i)
                {
                    var key = "messageKey" + i;
                    var val = "messageVal" + i;

                    Console.WriteLine($"Producing record: {key} {val}");

                    producer.Produce(topic, new Message<string, string> { Key = key, Value = val },
                        (deliveryReport) =>
                        {
                            if (deliveryReport.Error.Code != ErrorCode.NoError)
                            {
                                Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
                            }
                            else
                            {
                                Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
                                numProduced += 1;
                            }
                        });
                }

                producer.Flush(TimeSpan.FromSeconds(10));

                Console.WriteLine($"{numProduced} messages were produced to topic {topic}");
            }
        }
    }
}
```

3.   Run the code on the terminal(from the same directory `wd`) follows 
```Shell
  $:/path/to/wd/directory>dotnet run
```
This will put messages in your OSS stream.
4. In the OCI Web Console, quickly go to your Stream Page and click on *Load Messages* button. You should see the messages we just produced as below.
![See Produced Messages in OCI Wb Console](https://github.com/mayur-oci/OssJs/blob/main/JavaScript/StreamExampleLoadMessages.png?raw=true)

  
## Consuming messages from OSS
1. First produce messages to the stream you want to consumer message from unless you already have messages in the stream. You can produce message easily from *OCI Web Console* using simple *Produce Test Message* button as shown below
![Produce Test Message Button](https://github.com/mayur-oci/OssJs/blob/main/JavaScript/ProduceButton.png?raw=true)
 
 You can produce multiple test messages by clicking *Produce* button back to back, as shown below
![Produce multiple test message by clicking Produce button](https://github.com/mayur-oci/OssJs/blob/main/JavaScript/ActualProduceMessagePopUp.png?raw=true)


2. Open your favorite editor, such as [Visual Studio Code](https://code.visualstudio.com) from the empty working directory *wd*. 
3. Open the terminal and *cd* into *wd* directory. 
4. Create C# .NET console application by running the following command on the terminal
```Shell
  $:/path/to/wd/directory>dotnet new console
    The template "Console Application" was created successfully.
```
This will create Program.cs file with C# code for simple HellowWorld application.

4. Add OCI SDK packages for basic IAM authentication and OSS to your C# project as follows.
```Shell
  $:/path/to/wd/directory>dotnet add package OCI.DotNetSDK.Common
  $:/path/to/wd/directory>dotnet add package OCI.DotNetSDK.Streaming
``` 
6. Replace the code in *Program.cs* in directory *wd* with following code after you replace values of variables *configurationFilePath, profile ,ociStreamOcid and ociMessageEndpoint* in the follwing code snippet with values applicable for your tenancy. 
```C#
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

namespace OssProducer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting example for OSS Producer");
            string configurationFilePath = "C:\\.oci\\config";
            string profile = "DEFAULT";
            string ociStreamOcid = "ocid1.stream.oc1.ap-mumbai-1.amaaaaaauwpiejqaxcfc2ht67wwohfg7mxcstfkh2kp3hweeenb3zxtr5khq";
            string ociMessageEndpoint = "https://cell-1.streaming.ap-mumbai-1.oci.oraclecloud.com";

           try{
                var provider = new ConfigFileAuthenticationDetailsProvider(configurationFilePath, profile);
                
                StreamClient streamClient = new StreamClient(provider);
                streamClient.SetEndpoint(ociMessageEndpoint);

                // A cursor can be created as part of a consumer group.
                // Committed offsets are managed for the group, and partitions
                // are dynamically balanced amongst consumers in the group.
                Console.WriteLine("Starting a simple message loop with a group cursor");
                string groupCursor = await GetCursorByGroup(streamClient, ociStreamOcid, "exampleGroup", "exampleInstance-1");
                await SimpleMessageLoop(streamClient, ociStreamOcid, groupCursor);
           }
           catch (Exception e)
            {
                Console.WriteLine($"Streaming example failed: {e}");
            }
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
                    string key = message.Key != null ? Encoding.UTF8.GetString(message.Key) : "Null";     
                    Console.WriteLine($"{key} : {Encoding.UTF8.GetString(message.Value)}");
                }

                // getMessages is a throttled method; clients should retrieve sufficiently large message
                // batches, as to avoid too many http requests.
                await Task.Delay(1000);

                // use the next-cursor for iteration
                cursor = getResponse.OpcNextCursor;
            }
        }
    }
}

```
4. Run the code on the terminal(from the same directory *wd*) follows 
  Run the code on the terminal(from the same directory *wd*) follows 
```Shell
  $:/path/to/wd/directory>dotnet run
```
5. You should see the messages similar to shown below. Note when we produce message from OCI Web Console(as described above in first step), the Key for each message is *Null*
```
$:/path/to/wd/directory>dotnet run
 [INFO related maven compiling and building the Java code]
Starting a simple message loop with a group cursor
Creating a cursor for group exampleGroup, instance exampleInstance-1.
Read 25 messages.
Null: Example Test Message 0
Null: Example Test Message 0
 Read 2 messages
Null: Example Test Message 0
Null: Example Test Message 0
 Read 1 messages
Null: Example Test Message 0
 Read 10 messages
key 0: value 0
key 1: value 1

```

## Next Steps
Please refer to

 1. [Oracle Streaming Service And Kafka API compatibilty](https://docs.oracle.com/en-us/iaas/Content/Streaming/Tasks/kafkacompatibility_topic-Configuration.htm)
 2. [Streaming Examples with Admin and Client APIs from OCI](https://github.com/oracle/oci-dotnet-sdk/blob/master/Examples/StreamingExample.cs)
