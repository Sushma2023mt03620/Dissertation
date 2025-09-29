variable "location" {
  default = "eastus"
}

variable "resource_group_name" {
  default = "rg-automotive-platform"
}

variable "sql_admin_username" {
  sensitive = true
}

variable "sql_admin_password" {
  sensitive = true
}