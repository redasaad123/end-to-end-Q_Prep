provider "aws" {
    region     = "eu-central-1"
    access_key = var.access_key
    secret_key = var.secret_key
}

data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"]

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd*/ubuntu-*-24.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
    enable_dns_support   = true
    tags = {
      Name    = "Prep-VPC"
      project_name = "Q_Prep_Project"
    }
  
}

resource "aws_subnet" "subnet" {
    vpc_id            = aws_vpc.main.id
    cidr_block        = "10.0.1.0/24"
    availability_zone = "eu-central-1b"
    tags = {
      Name    = "Prep-Subnet"
      project_name = "Q_Prep_Project"
    }
  
}

resource "aws_internet_gateway" "igw" {
    vpc_id = aws_vpc.main.id
    tags = {
      Name    = "Prep-IGW"
      project_name = "Q_Prep_Project"
    }
  
}

resource "aws_route_table" "rt" {
    vpc_id = aws_vpc.main.id
  
    route {
      cidr_block = "0.0.0.0/0"
      gateway_id = aws_internet_gateway.igw.id
    }
    tags = {
      Name    = "Prep-RT"
      project_name = "Q_Prep_Project"
    }
}

resource "aws_route_table_association" "rta" {
    subnet_id      = aws_subnet.subnet.id
    route_table_id = aws_route_table.rt.id
}

resource "aws_security_group" "sg" {
    name = "prep-sg"
    description = "Security group for Q_Prep_Project"
    vpc_id = aws_vpc.main.id
    ingress {
      from_port   = 22
      to_port     = 22
      protocol    = "tcp"
      cidr_blocks = ["0.0.0.0/0"]
    }
    ingress {
    from_port       = 10250
    to_port         = 10250
    protocol        = "tcp"
    self            = true
  }

  # Overlay Network (Flannel)
  ingress {
    from_port       = 8472
    to_port         = 8472
    protocol        = "udp"
    self            = true
  }

  # Public Traffic للـ Ingress
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

    tags = {
        Name = "Prep-SG"
        project_name = "Q_Prep_Project"
    }


}

resource "aws_key_pair" "prep_key" {
  key_name =  "prep-key"
  public_key = var.public_key
}

resource "aws_eip" "nat_eip" {
  domain =  "vpc"
  instance = aws_instance.server.id
  tags = {
    Name = "Prep-NAT-EIP"
    project_name = "Q_Prep_Project"
  }
  depends_on = [ aws_internet_gateway.igw ]
  
} 

resource "aws_instance" "server" {
    ami           = data.aws_ami.ubuntu.id
    instance_type = "c7i-flex.large"
    subnet_id     = aws_subnet.subnet.id
    vpc_security_group_ids = [aws_security_group.sg.id]
    key_name      = aws_key_pair.prep_key.key_name
    
    tags = {
        Name = "Prep-Server"
        project_name = "Q_Prep_Project"
    }

    root_block_device {
      volume_size           = 256
    volume_type           = "gp3"
    delete_on_termination = true
    }
  
}

output "public_ip" {
  description = "Public IP address of the AI Server"
  value       = aws_eip.nat_eip.public_ip
}

output "instance_id" {
  description = "EC2 Instance ID"
  value       = aws_instance.server.id
}

output "vm_admin_username" {
  description = "VM Admin Username"
  value       = "ubuntu"
}






    

    