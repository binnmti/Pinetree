# Database 
dotnet tool update --global dotnet-ef
cd .\Pinetree
dotnet ef migrations remove
dotnet ef migrations add IsPublished
dotnet ef database update --configuration Release
CREATE UNIQUE NONCLUSTERED INDEX [EmailIndex]
    ON [dbo].[AspNetUsers]([NormalizedEmail] ASC) WHERE ([NormalizedEmail] IS NOT NULL);

dotnet ef database update 20250503032012_AddDatabase

# Azure CLI
az login --tenant mailadress
az ad sp create-for-rbac --name "Pinetree_SPN" --role contributor --scopes /subscriptions/tenantID --sdk-auth

