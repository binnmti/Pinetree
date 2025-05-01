# Provider configuration
provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

# Resource group
resource "azurerm_resource_group" "rg" {
  name     = "pinetree-staging"
  location = "japaneast"
}

# App Service Plan
resource "azurerm_service_plan" "asp" {
  name                = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku_name            = "F1"
  os_type             = "Windows"
}

# App Service
resource "azurerm_windows_web_app" "webapp" {
  name                = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  service_plan_id     = azurerm_service_plan.asp.id

  site_config {
    always_on = false
    application_stack {
      dotnet_version = "v9.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.appinsights.instrumentation_key
    "WEBSITE_RUN_FROM_PACKAGE"       = "1"
  }

  connection_string {
    name  = "SupportEmail"
    type  = "Custom"
    value = var.support_email
  }

  connection_string {
    name  = "SenderEmail"
    type  = "Custom"
    value = var.sender_email
  }

  connection_string {
    name  = "AzureCommunicationServicesConnectionString"
    type  = "Custom"
    value = var.azure_communication_services_connection_string
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = var.default_connection
  }

  connection_string {
    name  = "AzureStorage"
    type  = "Custom"
    value = var.azure_storage
  }
}

# SQL Server
resource "azurerm_mssql_server" "sqlserver" {
  name                         = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  resource_group_name          = azurerm_resource_group.rg.name
  administrator_login          = "sqladmin"
  administrator_login_password = var.administrator_login_password
  version                      = "12.0"
  public_network_access_enabled = true
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sqlserver.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# SQL Database
resource "azurerm_mssql_database" "sqldb" {
  name                = azurerm_resource_group.rg.name
  server_id           = azurerm_mssql_server.sqlserver.id
  sku_name            = "Basic"
  max_size_gb         = 2
}

# Storage Account
resource "azurerm_storage_account" "storage" {
  name                     = "pinetreestaging"
  location                 = azurerm_resource_group.rg.location
  resource_group_name      = azurerm_resource_group.rg.name
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "workspace" {
  name                = "${azurerm_resource_group.rg.name}-workspace"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# Application Insights
resource "azurerm_application_insights" "appinsights" {
  name                = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.workspace.id
}

# Outputs
output "app_service_url" {
  value = azurerm_windows_web_app.webapp.default_hostname
}

output "sql_server_name" {
  value = azurerm_mssql_server.sqlserver.name
}

output "storage_account_name" {
  value = azurerm_storage_account.storage.name
}

output "application_insights_instrumentation_key" {
  value     = azurerm_application_insights.appinsights.instrumentation_key
  sensitive = true
}
