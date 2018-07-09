# Automated Tag Synchronization for Azure Resources

This is a proof-of-concept application that synchronizes mandatory tags set on Resource Groups to the resources they contain. Synchronization happens automatically based on a user-defined schedule. The application is broken down into two Functions:

- **AuditSubscriptions:** Executes on a user-defined schedule and reads in subscription configuration data from an Azure Storage table. This configuration includes a list of tags that a given resource group in that subscription must implement. The function iterates through each resource group, inspects the tag values, and determines if changes are required (new or modify) on resources. If so, a message is added to a queue with the details required to add / modify the tag(s) for that resource.
- **AddTags:** Processes each message added to the queue by the AuditSubscriptions function and modifies the tags as necessary. If there are any issues with the tagging, an error is logged in the *InvalidTagResources* table. Any resource type listed in this table are exempted from future runs by *AuditSubscriptionTags*.

## Deployment

### Prerequisites

The application is written in C# (.Net Core) and can be developed and tested using a PC, Mac, or Linux using the Functions 2.0 runtime. As such, it is highly recommended the user complete all setup and installation instructions documented here: [Code and test Azure Functions locally](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local).
It is assumed you are working with an updated version of [Visual Studio Code](https://code.visualstudio.com/) with the [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions) installed.

You must also have a recent version of [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) installed.

### Create and configure the Function App

In the Azure Portal, create a new Function App by navigating to App Services and selecting the **Add** button in the upper left. In the Marketplace section, select *Function App*. In the create dialogue, enter the desired name, resource group, and storage account. Be sure to keep Windows as the OS type. It is recommended (but not required) to enable the *Application Insights* option.

Once the Function App is created, a few follow-up configurations are necessary:

#### Upgrade function runtime

In the Azure Portal, navigate to the Function App and select *Function App Settings*. Switch the runtime version to **beta**.

NOTE: At the time of this writing, there are issues with the latest beta runtime. For this applicaiton to work, you will need to navigate to Application Settings and set the value of FUNCTIONS_EXTENSION_VERSION to ```2.0.11651-alpha```.

#### Enable Managed Service Identity (MSI)

This application makes use of Managed Service Identity for authenticated access to your subscriptions. This is a great security feature that removes the burden of credential management for services running in Azure. Enable MSI by navigating to **Plafrom features** and selecting **Managed Service Identity (Preview)**. At the next screen, toggle **Register with Azure Active Directory** to **On** and click **Save**.

Next, you must assign the application permissions to each subscription you wish to enable for tag synchronization. This is achieved in the Azure Portal by using the search bar and entering **Subscriptions**. Click the suggested link and select a subscription displayed on the next page. In the settings blade, select **Access Control (IAM)** and click the **Add** button. On the Add Permissions blade at the right, select **Contributor** as the Role and enter the name of your function in the select bar. You should see your function app suggested as shown here:

<img src="images/add-msi.png" width=40%>

Select the fucntion app and click **Save**. Repeat this process for any additional subscriptions.

#### Create the configuration tables

Currently, two tables must be manually created within the storage account associated to the Function App before the solution can run. The easiest way to locate the table is by examining the resource group the function was deployed into.

Use Azure Storage Explorer to create tables with the following names: *AuditConfig* and *InvalidTagResources*

#### Create the queue

In the same storage account, use Storage Explorer to create a new queue with the following name: *resources-to-tag*

### Deploy the Functions
Finally, clone the repository to your workstation and open the root folder in VS Code. Assuming you have the Azure Functions extension installed in VS Code, you can deploy directly to your Function App by navigating to the **TagSync.Functions** folder and right-clicking. In the dialogue menu, select *Deploy to Function App*. You will be prompted to select a valid function app in your subscription. Use the one you created earlier.

<img src="images/deploy-function.png" width=30%>

## Configuration and Operation

By default, the ```AuditSubscriptionTags``` function runs once every 4 hours. The ```AddTags``` function is triggered by messages placed into the ```resources-to-tag``` queue created earler. You can manually initialize the process by clicking the Run button on ```AuditSubscriptionTags``` the portal. It is recommended to do this once after the deployment has been completed so that the columns for the ```AuditConfigTable``` are created.

When in operation, the solution works by interacting with the tables hosted in the Storage Account.

### AuditConfig

For each subscription you wish to audit, you must define a configuration item for it in the AuditConfig table. Configuration currently consists of two columns.

- ```SubscriptionId```: The GUID (ID) that represents the subscription.

- ```RequiredTags```: A comma separated list of tags that must be synchronized from the resource group to the resource item.

Once configuraiton is completed, your table shoud look like the following:

<img src="images/audit-config-table.png" width=75%>

### AuditStats

Information about every subscription audit performed by ```AuditSubscriptionTags``` is recorded in this table.

### InvalidTagResources

Azure does not currently have a unified API for resource tagging. There are cases where exeptions will be thrown when attempting to tag certain resource types. These exceptions are handled by the ```AddTags``` function and written to this table.

 <img src="images/invalid-tag-resources.png" width=75%>

 When invoked, ```AuditSubscriptionTags``` reads all items from this table and skips any resource that matches a given Type. That way, repeated API calls that are known to fail are not made. Developers can use the information to update the code to better handle these specific resources. Administrators can use this table to tag these resources either via a script or manually.

## Local Development / Debugging

### Service Principal

If you wish to run this function app locally, you must create a [service principal ID](https://docs.microsoft.com/en-us/powershell/azure/create-azure-service-principal-azureps?view=azurermps-6.0.0). Run one of the following commands to create one:

Azure CLI

```
az ad sp create-for-rbac --name ServicePrincipalName
```

PowerShell

```
New-AzureRmADServicePrincipal -DisplayName ServicePrincipalName
```

When the Service Principal is created, document the ID, key, and the AAD tenant ID for use in the next step. Finally ensure the Service Principal has permissions to modify objects in your subscription(s).

### local.settings.json
 In Azure, connection information that enables the Functions runtime to bind to services like Azure Storage (blobs, tables, and queues) is hosted in Application Settings. When the Functions runtime is running locally, this information is provided by a file named **local.settings.json**. This file contains sensitive information and is excluded from the repositroy, so you must download it from the Azure Portal 

 To do this, navigate to the Function App in the Azure Portal and click the **Download app content** link at the top of the **Overview** page. At the prompt, be sure to select ***Include app settings in the download***.  Oncde the download completes, unzip the contents and copy **local.settings.json** to the root of the **TagSync.Functions** folder.

 Finally, add ```appId```, ```appSecret```, and ```tenantId``` items to the JSON file and set the values to match the Service Principal you created earlier. The end result should look something like this:

<img src="images/local-settings-json.png" width=60%>

Assuming you have the [Functions 2.0 runtime installed and configured from your workstation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#v2), you should now be able to successfuly run and debug this application locally.

 ## FAQ

 TBD