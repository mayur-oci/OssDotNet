

# Quickstart with OCI .NET SDK for OSS

This quickstart shows how to produce messages to and consume messages from an [**Oracle Streaming Service**](https://docs.oracle.com/en-us/iaas/Content/Streaming/Concepts/streamingoverview.htm) using the [OCI .NET SDK](https://github.com/oracle/oci-dotnet-sdk). We are going to use C# language for these examples. 

## Prerequisites

1. You need have OCI account subscription or free account. typical links @jb
2. Follow [these steps](https://github.com/mayur-oci/OssJs/blob/main/JavaScript/CreateStream.md) to create Streampool and Stream in OCI. If you do  already have stream created, refer step 3 [here](https://github.com/mayur-oci/OssJs/blob/main/JavaScript/CreateStream.md) to capture/record message endpoint and OCID of the stream. We need this Information for upcoming steps.
3. The  [.NET 5.0 SDK or later](https://dotnet.microsoft.com/download). Make sure *dotnet* is in your *PATH* environment variable.
5. VS code(recommended) with with the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) installed. For information about how to install extensions on Visual Studio Code, see [VS Code Extension Marketplace](https://code.visualstudio.com/docs/editor/extension-gallery). In this tutorial we create and run a simple .NET console application by using Visual Studio Code and the .NET CLI,  as quick demonstration of how to use OCI .NET SDK for OSS. Project tasks, such as creating, compiling, and running a project are done by using the .NET CLI. You can follow this tutorial with a different IDE and run commands in a terminal if you prefer. 
6. Make sure you have [SDK and CLI Configuration File](https://docs.oracle.com/en-us/iaas/Content/API/Concepts/sdkconfig.htm#SDK_and_CLI_Configuration_File) setup. For production, you should use [Instance Principle Authentication](https://docs.oracle.com/en-us/iaas/Content/Identity/Tasks/callingservicesfrominstances.htm).

## Producing messages to OSS
1. Open your favorite editor, such as [Visual Studio Code](https://code.visualstudio.com) from the empty working directory *wd*. 
2. Open the terminal and *cd* into *wd* directory. 
3. Create C# .NET console application by running the following command on the terminal
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

                await PublishExampleMessages(streamClient, ociStreamOcid);
           }
           catch (Exception e)
            {
                Console.WriteLine($"Streaming example failed: {e}");
            }
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
    }
}

```
3.   Run the code on the terminal(from the same directory *wd*) follows 
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

 1. [Github for OCI .NET SDK](https://github.com/oracle/oci-dotnet-sdk)
 2. [Streaming Examples with Admin and Client APIs from OCI](https://github.com/oracle/oci-dotnet-sdk/blob/master/Examples/StreamingExample.cs)
