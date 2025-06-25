using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;

namespace SparkUp.MVC.Controllers
{
    public class DatabaseSyncController : Controller
    {
        private readonly AppDbContext _context;

        public DatabaseSyncController(AppDbContext context)
        {
            _context = context;
        }

        // WARNING: This is for development only - remove in production!
        [HttpGet]
        public async Task<IActionResult> SyncDatabase()
        {
            try
            {
                var result = new List<string>();

                // 1. Fix WorkerProfiles -> TaskTypes relationship
                result.Add("Starting database schema sync...");

                await _context.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkerProfiles_TaskTypes_TaskTypeId')
                    BEGIN
                        ALTER TABLE WorkerProfiles DROP CONSTRAINT FK_WorkerProfiles_TaskTypes_TaskTypeId
                    END
                ");
                result.Add("Dropped existing WorkerProfiles FK...");

                await _context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE WorkerProfiles 
                    ADD CONSTRAINT FK_WorkerProfiles_TaskTypes_TaskTypeId 
                        FOREIGN KEY (TaskTypeId) REFERENCES TaskTypes(Id) ON DELETE CASCADE
                ");
                result.Add("Created WorkerProfiles FK with CASCADE...");

                // 2. Fix ChatMessages -> Users relationship
                await _context.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ChatMessages_Users_SenderId')
                    BEGIN
                        ALTER TABLE ChatMessages DROP CONSTRAINT FK_ChatMessages_Users_SenderId
                    END
                ");

                await _context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE ChatMessages 
                    ADD CONSTRAINT FK_ChatMessages_Users_SenderId 
                        FOREIGN KEY (SenderId) REFERENCES Users(Id) ON DELETE SET NULL
                ");
                result.Add("Fixed ChatMessages FK with SET NULL...");

                result.Add("Database schema sync completed successfully!");

                return Json(new { success = true, messages = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
