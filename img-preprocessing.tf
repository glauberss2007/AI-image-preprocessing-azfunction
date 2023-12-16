provider "azurerm" {
  features {}
}

# Resource Group
resource "azurerm_resource_group" "example" {
  name     = "image-preprocessing-rg"
  location = "East US"
}

# Storage Account
resource "azurerm_storage_account" "example" {
  name                     = "imagestorageacc"
  resource_group_name      = azurerm_resource_group.example.name
  location                 = azurerm_resource_group.example.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    environment = "production"
  }
}

# Blob Container 1
resource "azurerm_storage_container" "container1" {
  name                  = "user-images"
  storage_account_name  = azurerm_storage_account.example.name
  container_access_type = "private"
}

# Blob Container 2
resource "azurerm_storage_container" "container2" {
  name                  = "processed-images"
  storage_account_name  = azurerm_storage_account.example.name
  container_access_type = "private"
}

# Function App
resource "azurerm_function_app" "example" {
  name                      = "image-processing-function"
  location                  = azurerm_resource_group.example.location
  resource_group_name       = azurerm_resource_group.example.name
  app_service_plan_id       = azurerm_app_service_plan.example.id
  storage_account_name      = azurerm_storage_account.example.name
  storage_account_access_key = azurerm_storage_account.example.primary_access_key

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
    "AzureWebJobsStorage"      = azurerm_storage_account.example.primary_connection_string
  }
}

# App Service Plan
resource "azurerm_app_service_plan" "example" {
  name                = "function-app-service-plan"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  kind                = "FunctionApp"

  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

# Access Policy for Blob Trigger
resource "azurerm_storage_blob_data_contributor" "example" {
  role_definition_name = "Storage Blob Data Contributor"
  scope                = azurerm_storage_container.container1.id
  storage_account_id   = azurerm_storage_account.example.id
}

# Blob Trigger
resource "azurerm_function_blob_trigger" "example" {
  name                     = "blob-trigger"
  resource_group_name      = azurerm_resource_group.example.name
  function_app_name        = azurerm_function_app.example.name
  storage_account_name     = azurerm_storage_account.example.name
  storage_container_name   = azurerm_storage_container.container1.name
  connection_string_setting = "AzureWebJobsStorage"
  event_grid_endpoint      = azurerm_function_app.example.event_grid_publishing_password
}
