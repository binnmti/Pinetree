dotnet tool update --global dotnet-ef
cd .\Pinetree
dotnet ef migrations remove
dotnet ef migrations add AddPinecone
dotnet ef database update

CREATE UNIQUE NONCLUSTERED INDEX [EmailIndex]
    ON [dbo].[AspNetUsers]([NormalizedEmail] ASC) WHERE ([NormalizedEmail] IS NOT NULL);

