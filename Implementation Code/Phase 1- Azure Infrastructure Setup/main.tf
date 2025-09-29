terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "sttfstate"
    container_name       = "tfstate"
    key                  = "automotive.tfstate"
  }
}

provider "azurerm" {
  features {}
}

# Virtual Network
resource "azurerm_virtual_network" "main" {
  name                = "vnet-automotive-platform"
  address_space       = ["10.0.0.0/16"]
  location            = var.location
  resource_group_name = var.resource_group_name
}

# AKS Subnet
resource "azurerm_subnet" "aks" {
  name                 = "snet-aks"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Azure Kubernetes Service
resource "azurerm_kubernetes_cluster" "main" {
  name                = "aks-automotive-platform"
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = "automotive"

  default_node_pool {
    name       = "default"
    node_count = 3
    vm_size    = "Standard_D4s_v3"
    vnet_subnet_id = azurerm_subnet.aks.id
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin = "azure"
    network_policy = "calico"
  }
}

# Azure SQL Database
resource "azurerm_mssql_server" "main" {
  name                         = "sql-automotive-platform"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
}

resource "azurerm_mssql_database" "manufacturing" {
  name           = "sqldb-manufacturing"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  sku_name       = "S2"
}

# Cosmos DB
resource "azurerm_cosmosdb_account" "main" {
  name                = "cosmos-automotive-iot"
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }
}

# IoT Hub
resource "azurerm_iothub" "main" {
  name                = "iothub-automotive-platform"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku {
    name     = "S2"
    capacity = 2
  }
}

# Redis Cache
resource "azurerm_redis_cache" "main" {
  name                = "redis-automotive-cache"
  location            = var.location
  resource_group_name = var.resource_group_name
  capacity            = 1
  family              = "P"
  sku_name            = "Premium"
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "appi-automotive-platform"
  location            = var.location
  resource_group_name = var.resource_group_name
  application_type    = "web"
}