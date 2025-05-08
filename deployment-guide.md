# Azure Deployment Guide for Agency Appointments Application

This guide provides step-by-step instructions to deploy your Agency Appointments application to Azure using CI/CD pipelines.

## Prerequisites

- Azure account with an active subscription
- GitHub account with your repository at `https://github.com/myonathanlinkedin/mcp-rag-agency-book-appointments`
- Azure CLI installed (for local Azure resource deployments)
- .NET 9.0 SDK installed on your development machine

## Option 1: Deploy Using Azure DevOps

### Step 1: Create an Azure DevOps Project

1. Sign in to [Azure DevOps](https://dev.azure.com/)
2. Create a new project or use an existing one
3. Go to "Repos" and connect your GitHub repository:
   - Select "Import" repository
   - Enter your GitHub repository URL: `https://github.com/myonathanlinkedin/mcp-rag-agency-book-appointments`
   - Alternatively, you can use GitHub integration to connect your repository

### Step 2: Set Up Azure DevOps Pipeline

1. In your Azure DevOps project, go to "Pipelines" and select "Create Pipeline"
2. Choose "GitHub" as the source
3. Select your repository
4. Choose "Existing Azure Pipelines YAML file"
5. Select the path to your pipeline file: `/.azuredevops/azure-pipelines.yml`
6. Save and run the pipeline

### Step 3: Configure Azure Service Connection

1. Go to "Project Settings" > "Service Connections"
2. Create a new "Azure Resource Manager" service connection
3. Follow the wizard to authorize Azure DevOps to access your Azure subscription
4. Name your service connection (e.g., "Agency-Azure-Connection")
5. Update the `azureServiceConnection` variable in your pipeline YAML file

### Step 4: Configure Pipeline Variables

1. Go to your pipeline and select "Edit"
2. Update these variables in the YAML file to match your desired Azure resource names:
   - `webAppName`: Your web application name
   - `azureServiceConnection`: The service connection name you created
3. Save the pipeline

### Step 5: Deploy Azure Resources

1. Go to the Azure Portal
2. Open Azure Cloud Shell or use Azure CLI locally
3. Create a resource group (if it doesn't exist already):
   ```bash
   az group create --name AgencyAppointments-RG --location eastus
   ```
4. Deploy the ARM template:
   ```bash
   az deployment group create --resource-group AgencyAppointments-RG --template-file azure-resources.json --parameters administratorLogin=adminuser administratorLoginPassword=YourComplexPassword123!
   ```

## Option 2: Deploy Using GitHub Actions

### Step 1: Configure GitHub Repository

1. In your GitHub repository, go to "Settings" > "Secrets and variables" > "Actions"
2. Add the following secrets:
   - `AZURE_WEBAPP_PUBLISH_PROFILE`: The publish profile credentials from your Azure Web App
   - `AZURE_CREDENTIALS`: Azure service principal credentials (optional for advanced scenarios)

### Step 2: Set Up GitHub Actions

1. In your repository, create a directory `.github/workflows` (if it doesn't exist)
2. Copy the `github-workflow.yml` file to this directory
3. Commit and push the changes to your repository

### Step 3: Deploy Azure Resources

Same as Step 5 in Option 1.

## Option 3: Manual Deployment Using Azure Portal

### Step 1: Create Azure Resources

1. Log in to the [Azure Portal](https://portal.azure.com)
2. Create a resource group for your application
3. Create the following resources:
   - App Service Plan
   - Web App (configured for .NET 9.0)
   - SQL Server and Database
   - Event Hubs namespace (for Kafka functionality)
   - Application Insights

### Step 2: Configure Connection Strings and Application Settings

1. In your Web App, go to "Configuration"
2. Add these Application Settings:
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `KafkaTopic`: Your Event Hub name
   - `KafkaServer`: `your-namespace.servicebus.windows.net:9093`
3. Add Connection Strings:
   - `DefaultConnection`: Your SQL Database connection string

### Step 3: Deploy Your Application

1. In Visual Studio, right-click on your API project
2. Select "Publish"
3. Choose "Azure" as the target
4. Select your existing Web App and follow the wizard

## Post-Deployment Configuration

### Configure CORS (if needed)

1. In your Web App, go to "CORS" under "API"
2. Add any origins that need to access your API

### Set Up Monitoring

1. In the Azure Portal, navigate to your Application Insights resource
2. Explore the monitoring dashboards and alerts

### Database Migrations

1. Connect to your database using the Entity Framework Core CLI:
   ```bash
   dotnet ef database update --connection "Your-Connection-String"
   ```
   
## Troubleshooting

### Common Issues

1. **Deployment Failures**:
   - Check the logs in Azure DevOps or GitHub Actions
   - Ensure your application builds successfully locally
   - Verify that the correct .NET version is specified in deployment configurations

2. **Database Connection Errors**:
   - Verify SQL Server firewall rules
   - Check connection string format and credentials
   - Ensure migration scripts run without errors

3. **Kafka/Event Hubs Connectivity**:
   - Check connection strings and access policies
   - Verify that the client configuration matches the Event Hubs namespace

### Getting Help

For additional assistance:
- Check Azure documentation
- Review specific error messages in the App Service Logs
- Post issues to the repository for community help

## Next Steps

- Set up custom domains and SSL certificates
- Implement staging slots for blue-green deployments
- Configure autoscaling rules
- Implement Azure Monitor alerts for performance and availability