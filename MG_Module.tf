# Management Group Module
module "management_groups" {
  source = "./modules/management-groups"
  
  root_management_group_id = "automotive-root"
  management_groups = {
    platform = {
      display_name = "Platform Services"
      subscriptions = [var.management_subscription_id]
    }
    landing_zones = {
      display_name = "Application Landing Zones"
      subscriptions = [var.prod_subscription_id, var.dev_subscription_id]
    }
  }
}

# Hub Network Module
module "hub_network" {
  source = "./modules/networking"
  
  resource_group_name = "rg-connectivity-hub"
  location = var.primary_location
  
  vnet_config = {
    name = "vnet-hub"
    address_space = ["10.0.0.0/16"]
    
    subnets = {
      gateway = "10.0.0.0/24"
      firewall = "10.0.1.0/24"
      shared_services = "10.0.2.0/24"
    }
  }
}