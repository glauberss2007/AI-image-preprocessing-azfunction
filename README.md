# AI-image-preprocessing-azfunction
This project is related to an image prepocessing flow to be used before apply an AI.


Typically, images provided by users (such as profile pictures, social media posts, and business documents) require moderation to safeguard businesses and ensure quality. When an application receives a substantial volume of images, manual moderation becomes time-consuming and lacks immediacy. This article outlines the process of implementing automated image moderation through Azure functions and Cognitive Services. Automated moderation leverages machine learning and AI to efficiently manage moderation at a large scale, eliminating the need for human intervention and cost-effectively ensuring quality control.

-> Create an image HERE (image upload -> blob storage -> az functions <-> AZ Cognitive service)

## Stack used

Before delving into the implementation, let's quickly grasp some fundamental terms within the Azure ecosystem:

- **Azure Functions:** Similar to AWS Lambda, this feature embodies a serverless architecture, enabling code execution without concerns about hosting.
  
- **Cognitive Services:** These APIs provide applications with artificial intelligence capabilities, facilitating transformation through AI.

- **Content Moderator:** Equipped with APIs for moderating Text, Image, and Video content, ensuring the filtration of offensive or unwanted material.

- **Azure Blob Storage:** Comparable to Amazon S3, this service offers object storage, primarily utilized for storing unstructured data like documents, media files, or application installers. The blob service hierarchy comprises:
  - Account > Container > Blob (file)
  - Containers serve as groupings for sets of blobs, efficiently organizing related data.

## Workflow Steps:

1. **Image Upload:**
   - An image is uploaded to a Blob storage container by a user or an application.

2. **Trigger Configuration:**
   - Configuration set to trigger an Azure function upon blob addition in the specified container.

3. **Face Detection:**
   - Azure function calls the Face Detection API from Microsoft Cognitive Services to determine face count in the image.
   - Validation ensures only one face is detected, confirming a single person in the photo.

4. **Content Moderation:**
   - The Azure function employs the Image Moderation API's Evaluate operation to predict adult or racy content presence in the image.

5. **Image Processing and Branding:**
   - Upon passing validation, the function adds branding text or a watermark to the image.
   - The modified image is stored in another container (e.g., Container2) for application use.

6. **Archival/Deletion:**
   - Optionally, the original image can be archived or deleted, leaving only automatically rejected or pending review images for manual moderation.

This automated process ensures efficient image moderation, adhering to content guidelines and facilitating streamlined handling within the application's environment.

## Setup Content Moderator

If you have an Azure subscription, start by accessing the Azure portal. Click on "+ New" and type "Content Moderator" into the search bar. Once located, click on "Create" to initialize the service setup within Cognitive Services.

After creating the resource, navigate to the specific resource page and locate the "Keys" section. Here, you'll find two keys: KEY 1 and KEY 2. These keys are essential for the integration process within the Azure function. They'll be utilized to establish the connection and enable the functionalities of the Content Moderator service seamlessly.

Terraform example:

## Azure Functions App and Storage

To configure a Function App in Azure for handling images, follow these steps:

1. **Access Azure Portal:**
   - Navigate to the Azure portal and click on "+ New".

2. **Compute in Azure Marketplace:**
   - Select "Compute" within the Azure Marketplace and then choose "Function App".

3. **Enter Details:**
   - Fill in the necessary details like the Name and other required information for the Function App setup.

4. **Storage Configuration:**
   - For simplicity, opt to create a new Storage account by selecting the "Create New" option and providing a name. This Storage account will be utilized for uploading images.

5. **Initiate Creation:**
   - After entering all required information, click on the "Create" button to begin the setup process.

Once your Storage account and Azure Function app are set up, proceed with creating the necessary containers:

6. **Access Storage Account:**
   - Go to "All Resources" in the Azure portal and select your Storage account.

7. **Create Container 1 (For User Submitted Images):**
   - Inside the Storage account, navigate to "Blobs" and click on "+ Container".
   - Enter the name "container1" and set the Container's access level as "Public". Confirm by clicking "OK".
   - This container will serve as the storage for user-submitted images.

8. **Create Container 2 (For Watermarked or posprocessed Images):**
   - Similarly, create another container by repeating the process.
   - Name this container "container2". Images after moderation will be watermarked and saved here.

Terraform example:

## Triggers and Binding

To set up triggers and bindings for your Azure Function, follow these steps:

1. **Access Azure Functions:**
   - Go to Azure Functions and select the function app that we previously created.

2. **Create a New Function:**
   - Click on the "+" icon for Function and choose "Custom Function" at the bottom.

3. **Select Blob Trigger:**
   - Choose "Blob Trigger" and select "C#" as the language.

4. **Enter Function Details:**
   - Provide a Name for the function (e.g., BlobTriggerCSharp1).
   - Configure the Azure Blob Storage trigger with the following details:
     - Path: `container1/{name}`
     - Storage account connection: Choose the option with the connection string of the storage account.

5. **Click Create:**
   - Confirm by clicking on the "Create" button.

Now, this function is set to be triggered whenever a blob is added to `container1`. By default, the function will include the following method:
```csharp
public static void Run(Stream myBlob, string name, ILogger log)
{
    log.LogInformation($"C# Blob trigger function processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
}
```

6. **Setting Up Azure Blob Output**
  - Access the function and click on "Integrate" to add a new output.
  - Choose "Azure Blob Storage" and configure the following settings:
    - Blob parameter name: "outputBlob".
    - Path: "container2/{name}".
    - Select the appropriate Storage account connection containing the connection string.

7. **Advanced Editor Configuration**
  - Click on "Advanced Editor" located in the top-right corner to access code editing.
  - Use the provided JSON code snippet:

```json
{
  "bindings": [
    {
      "name": "myBlob",
      "type": "blobTrigger",
      "direction": "in",
      "path": "container1/{name}",
      "connection": "AzureWebJobsDashboard"
    },
    {
      "type": "blob",
      "name": "outputBlob",
      "path": "container2/{name}",
      "connection": "AzureWebJobsDashboard",
      "direction": "out"
    }
  ],
  "disabled": false
}
```
8. Click on BlobTriggerCSharp1 to open run.csx file. Add one more argument “outputBlob” in the existing method

```
public static void Run(Stream myBlob, string name, Stream outputBlob, TraceWriter log)
{
    log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
}
```

9. Open Logs window and click on Save. You will get compilation success message.

10. On Save, the function is compiled. Any errors during the build are displayed in the Logs window. Now when any file is added on container1, this function will be called automatically. In the logs, you will be able to see the output logs.






