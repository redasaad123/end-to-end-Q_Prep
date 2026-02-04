terraform {
  backend "s3" {
    bucket         = "recomind-devops-terraform-state-461703429082"
    key            = "my/terraform.tfstate"
    region         = "eu-central-1"
    encrypt        = true

  }

   required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  
  required_version = ">= 1.0"
}

