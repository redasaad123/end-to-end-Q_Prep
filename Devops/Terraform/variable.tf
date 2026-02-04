variable "access_key" {
  description = "AWS Access Key"
  type        = string
}

variable "secret_key" {
  description = "AWS Secret Key"
  type        = string
}

variable "public_key" {
  description = "SSH Public Key for EC2 Instance"
  type        = string
}


variable "aws_ami" {
    description = "AMI ID for the EC2 Instance"
    type        = string
    default = "ami-073130f74f5ffb161"
}