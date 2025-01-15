dotnet tool update --global dotnet-ef
cd .\Pinetree
dotnet ef migrations remove
dotnet ef migrations add AddPinecone
dotnet ef database update
