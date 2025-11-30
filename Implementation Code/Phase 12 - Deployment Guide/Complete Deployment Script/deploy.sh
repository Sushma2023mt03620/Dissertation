#!/bin/bash

set -e

echo "========================================="
echo "Automotive Platform Deployment Script" 
echo "========================================="

# Configuration
RESOURCE_GROUP="rg-automotive-platform"
LOCATION="eastus"
AKS_CLUSTER="aks-automotive-platform"
ACR_NAME="acrautomotiveplatform"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

# Step 1: Azure Login
echo ""
print_status "Logging into Azure..."
az login

# Step 2: Deploy Infrastructure
echo ""
print_status "Deploying infrastructure with Terraform..."
cd terraform
terraform init
terraform plan -out=tfplan
terraform apply tfplan
cd ..

# Step 3: Get AKS Credentials
echo ""
print_status "Getting AKS credentials..."
az aks get-credentials \
    --resource-group $RESOURCE_GROUP \
    --name $AKS_CLUSTER \
    --overwrite-existing

# Step 4: Create Kubernetes Namespaces
echo ""
print_status "Creating Kubernetes namespaces..."
kubectl create namespace production --dry-run=client -o yaml | kubectl apply -f -
kubectl create namespace staging --dry-run=client -o yaml | kubectl apply -f -
kubectl create namespace monitoring --dry-run=client -o yaml | kubectl apply -f -

# Step 5: Create Secrets
echo ""
print_status "Creating Kubernetes secrets..."

# Get connection strings from Azure
SQL_CONNECTION=$(az sql db show-connection-string \
    --client ado.net \
    --server sql-automotive-platform \
    --name sqldb-manufacturing \
    --query connectionString -o tsv)

REDIS_KEY=$(az redis list-keys \
    --name redis-automotive-cache \
    --resource-group $RESOURCE_GROUP \
    --query primaryKey -o tsv)

# Create secrets
kubectl create secret generic db-secrets \
    --from-literal=connection-string="$SQL_CONNECTION" \
    --namespace=production \
    --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret generic redis-secrets \
    --from-literal=connection-string="redis-automotive-cache.redis.cache.windows.net:6380,password=$REDIS_KEY,ssl=True" \
    --namespace=production \
    --dry-run=client -o yaml | kubectl apply -f -

# Step 6: Build and Push Docker Images
echo ""
print_status "Building and pushing Docker images..."

# Login to ACR
az acr login --name $ACR_NAME

# Build Production Line Service
print_status "Building Production Line Service..."
docker build -t $ACR_NAME.azurecr.io/production-line-service:latest \
    -f src/ProductionLineService/Dockerfile \
    src/ProductionLineService
docker push $ACR_NAME.azurecr.io/production-line-service:latest

# Build Telematics Service
print_status "Building Telematics Service..."
docker build -t $ACR_NAME.azurecr.io/telematics-service:latest \
    -f src/TelematicsService/Dockerfile \
    src/TelematicsService
docker push $ACR_NAME.azurecr.io/telematics-service:latest

# Build ML Service
print_status "Building ML Service..."
docker build -t $ACR_NAME.azurecr.io/ml-service:latest \
    -f src/MLServices/Dockerfile \
    src/MLServices
docker push $ACR_NAME.azurecr.io/ml-service:latest

# Step 7: Deploy to Kubernetes
echo ""
print_status "Deploying applications to Kubernetes..."
kubectl apply -f k8s/production-line-service/ -n production
kubectl apply -f k8s/telematics-service/ -n production
kubectl apply -f k8s/ml-service/ -n production

# Step 8: Deploy Monitoring
echo ""
print_status "Deploying monitoring stack..."
kubectl apply -f k8s/monitoring/ -n monitoring

# Step 9: Wait for Deployments
echo ""
print_status "Waiting for deployments to be ready..."
kubectl wait --for=condition=available --timeout=300s \
    deployment/production-line-service -n production
kubectl wait --for=condition=available --timeout=300s \
    deployment/telematics-service -n production
kubectl wait --for=condition=available --timeout=300s \
    deployment/ml-service -n production

# Step 10: Get Service Endpoints
echo ""
print_status "Deployment complete! Service endpoints:"
kubectl get services -n production

echo ""
print_status "========================================="
print_status "Deployment completed successfully!"
print_status "========================================="
