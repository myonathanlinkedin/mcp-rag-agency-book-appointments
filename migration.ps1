dotnet ef migrations add InitialMigration --context "IdentityDbContext" --project Identity/Identity.Infrastructure --startup-project SimpleIdentityServer
dotnet ef database update --context "IdentityDbContext" --project Identity/Identity.Infrastructure --startup-project SimpleIdentityServer
dotnet ef migrations add InitialMigration --context "RAGDbContext" --project RAGScanner/RAGScanner.Infrastructure --startup-project SimpleIdentityServer
dotnet ef database update --context "RAGDbContext" --project RAGScanner/RAGScanner.Infrastructure --startup-project SimpleIdentityServer

dotnet ef migrations add InitialMigration --context "AgencyBookingDbContext" --project AgencyBookingSystem/AgencyBookingSystem.Infrastructure --startup-project SimpleIdentityServer
dotnet ef database update --context "AgencyBookingDbContext" --project AgencyBookingSystem/AgencyBookingSystem.Infrastructure --startup-project SimpleIdentityServer