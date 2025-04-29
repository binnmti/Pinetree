variable "subscription_id" {
  description = "The Azure subscription ID"
  type        = string
}

variable "administrator_login_password" {
  description = "administrator login password"
  type        = string
}

variable "support_email" {
  type        = string
}

variable "sender_email" {
  type        = string
}

variable "azure_communication_services_connection_string" {
  type        = string
}

variable "azure_storage" {
  type        = string
}

variable "default_connection" {
  type        = string
}

variable "application_insights_workspace_id" {
  description = "The ID of the Log Analytics workspace for Application Insights"
  type        = string
}