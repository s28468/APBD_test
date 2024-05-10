using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.ComponentModel.DataAnnotations;
using WebApplication1.models;


namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public MainController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Add CPU")]
        public IActionResult Post([FromBody] CPUAdd request)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    var productCpu = InsertCPU(connection, transaction, request);
                    if (!productCpu.HasValue)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "An error occurred while inserting into CPU.");
                    }

                    transaction.Commit();
                    return Ok("CPU added");
                }
                catch
                {
                    transaction.Rollback();
                    return StatusCode(500, "An error occurred while processing your request.");
                }
            }
        }
        
        [HttpPost("Add Videocard")]
        public IActionResult Post([FromBody] VideocardAdd request)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    var productCpu = InsertVideocard(connection, transaction, request);
                    if (!productCpu.HasValue)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "An error occurred while inserting into Videocard.");
                    }

                    transaction.Commit();
                    return Ok("Videocard added");
                }
                catch
                {
                    transaction.Rollback();
                    return StatusCode(500, "An error occurred while processing your request.");
                }
            }
        }

        [HttpPost("{CPUName}, {VideocardName}, {PCName}")]
        public IActionResult Post([FromBody] PCAdd request)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    if (!CheckIfExists(connection, transaction, "CPU", "Name", request.CPUName))
                    {
                        transaction.Rollback();
                        return NotFound("CPU not found.");
                    }
                    if (!CheckIfExists(connection, transaction, "Videocard", "Name", request.VideocardName))
                    {
                        transaction.Rollback();
                        return NotFound("Videocard not found.");
                    }
                    
                    var productCpu = InsertPC(connection, transaction, request);
                    if (!productCpu.HasValue)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "An error occurred while inserting into PC.");
                    }

                    transaction.Commit();
                    return Ok("PC added");
                }
                catch
                {
                    transaction.Rollback();
                    return StatusCode(500, "An error occurred while processing your request.");
                }
            }
        }

        [HttpDelete("{Name}")]
        public IActionResult DeleteTeamMember(string Name)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    string deletePC = @"
                        DELETE FROM Computer
                        WHERE Name = @Name";

                    using (var deleteTasksCmd = new SqlCommand(deletePC, connection, transaction))
                    {
                        deleteTasksCmd.Parameters.AddWithValue("@Name", Name);
                        deleteTasksCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    return Ok($"PC with name {Name} have been deleted successfully.");
                }
                catch (SqlException sqlEx)
                {
                    transaction.Rollback();
                    return StatusCode(500, $"A database error occurred: {sqlEx.Message}");
                }
            }
        }

        private int? InsertCPU(SqlConnection connection, SqlTransaction transaction, CPUAdd request)
        {
            var query = @"
        INSERT INTO CPU (Name, Frequency , Cores) 
        VALUES (@Name, @Frequency, @Cores);
        SELECT CAST(SCOPE_IDENTITY() as int);
    ";

            using (var command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Name", request.Name);
                command.Parameters.AddWithValue("@Frequency", request.Frequency);
                command.Parameters.AddWithValue("@Cores", request.Cores);

                try
                {
                    var result = command.ExecuteScalar();
                    return (int?)result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred during CPU insert: {ex.Message}");
                    return null;
                }
            }
        }
        private int? InsertVideocard(SqlConnection connection, SqlTransaction transaction, VideocardAdd request)
        {
            var query = @"
        INSERT INTO Videocard (Name, Frequency , Memory) 
        VALUES (@Name, @Frequency, @Memory);
        SELECT CAST(SCOPE_IDENTITY() as int);
    ";

            using (var command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Name", request.Name);
                command.Parameters.AddWithValue("@Frequency", request.Frequency);
                command.Parameters.AddWithValue("@Memory", request.Memory);

                try
                {
                    var result = command.ExecuteScalar();
                    return (int?)result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred during Videocard insert: {ex.Message}");
                    return null;
                }
            }
        }
        private bool CheckIfExists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, object value)
        {
            using (var command = new SqlCommand($"SELECT COUNT(1) FROM {tableName} WHERE {columnName} = @value", connection, transaction))
            {
                command.Parameters.AddWithValue("@value", value);
                try
                {
                    var result = command.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CheckIfExists: {ex.Message}");
                    return false;
                }
            }
        }
        private int? InsertPC(SqlConnection connection, SqlTransaction transaction, PCAdd request)
        {
            var CPUId = GetID(connection, transaction, "CPU", request.CPUName);
            var VideocardId = GetID(connection, transaction, "Videocard", request.VideocardName);
            var query = @"
        INSERT INTO Computer (Name, CPUId , VideocardId) 
        VALUES (@Name, @Frequency, @Memory);
        SELECT CAST(SCOPE_IDENTITY() as int);
    ";

            using (var command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Name", request.Name);
                command.Parameters.AddWithValue("@CPUId", CPUId);
                command.Parameters.AddWithValue("@VideocardId", VideocardId);

                try
                {
                    var result = command.ExecuteScalar();
                    return (int?)result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred during PC insert: {ex.Message}");
                    return null;
                }
            }
        }

        private decimal? GetID(SqlConnection connection, SqlTransaction transaction, string table ,string Name)
        {
            using (var command = new SqlCommand(@"SELECT Id FROM @table WHERE Name = @Name", connection, transaction))
            {
                command.Parameters.AddWithValue("@Name", Name);

                try
                {
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return (decimal)result;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetProductPrice: {ex.Message}");
                    return null;
                }
            }
        }

        public class CPUAdd
        {
            [Required] public string Name { get; set; }
            [Required] public int Frequency { get; set; }
            [Required] public int Cores { get; set; }
        }
        public class VideocardAdd
        {
            [Required] public string Name { get; set; }
            [Required] public int Frequency { get; set; }
            [Required] public int Memory { get; set; }
        }
        public class PCAdd
        {
            [Required] public string Name { get; set; }
            [Required] public string CPUName { get; set; }
            [Required] public string VideocardName { get; set; }
        }
    }
}