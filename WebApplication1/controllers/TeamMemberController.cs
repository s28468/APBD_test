using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using WebApplication1.models;
using System.ComponentModel.DataAnnotations;

namespace task7.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskMemberController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TaskMemberController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("{id}")]
        public IActionResult GetTeamMemberTasks(int id)
        {
            var tasks = new List<dynamic>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    string sql = @"
                    SELECT tm.FirstName, tm.LastName, tm.Email, t.Name, t.Description, t.Deadline, p.Name as ProjectName, tt.Name as TaskTypeName, 
                        CASE WHEN t.IdAssignedTo = @id THEN 'Assigned' ELSE 'Created' END as TaskRole
                    FROM master.dbo.TeamMember tm
                    JOIN master.dbo.Task t ON tm.IdTeamMember = t.IdAssignedTo OR tm.IdTeamMember = t.IdCreator
                    JOIN master.dbo.Project p ON t.IdProject = p.IdProject
                    JOIN master.dbo.TaskType tt ON t.IdTaskType = tt.IdTaskType
                    WHERE tm.IdTeamMember = @id
                    ORDER BY t.Deadline DESC";

                    using (var command = new SqlCommand(sql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var task = new
                                {
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    TaskName = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Deadline = DateTime.Parse(reader["Deadline"].ToString()).ToString("yyyy-MM-dd"),
                                    ProjectName = reader["ProjectName"].ToString(),
                                    TaskTypeName = reader["TaskTypeName"].ToString(),
                                    TaskRole = reader["TaskRole"].ToString()
                                };
                                tasks.Add(task);
                            }
                        }
                    }
                    transaction.Commit();
                    var assignedTasks = tasks.Where(t => t.TaskRole == "Assigned").ToList();
                    var createdTasks = tasks.Where(t => t.TaskRole == "Created").ToList();

                    var response = new
                    {
                        FirstName = tasks.FirstOrDefault()?.FirstName,
                        LastName = tasks.FirstOrDefault()?.LastName,
                        Email = tasks.FirstOrDefault()?.Email,
                        AssignedTasks = assignedTasks,
                        CreatedTasks = createdTasks
                    };

                    return Ok(response);
                }
                catch (SqlException sqlEx)
                {
                    transaction.Rollback();
                    return StatusCode(500, "A database error occurred: " + sqlEx.Message);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, "An error occurred while processing your request: " + ex.Message);
                }

            }
            
        }
        [HttpDelete("{id}")]
        public IActionResult DeleteTeamMember(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    string deleteTasksSql = @"
                        DELETE FROM master.dbo.Task
                        WHERE IdAssignedTo = @id OR IdCreator = @id";

                    using (var deleteTasksCmd = new SqlCommand(deleteTasksSql, connection, transaction))
                    {
                        deleteTasksCmd.Parameters.AddWithValue("@id", id);
                        deleteTasksCmd.ExecuteNonQuery();  // Execute the deletion of tasks
                    }

                    string deleteMemberSql = @"
                        DELETE FROM master.dbo.TeamMember
                        WHERE IdTeamMember = @id";

                    using (var deleteMemberCmd = new SqlCommand(deleteMemberSql, connection, transaction))
                    {
                        deleteMemberCmd.Parameters.AddWithValue("@id", id);
                        int result = deleteMemberCmd.ExecuteNonQuery(); 
                        if (result == 0)
                        {
                            transaction.Rollback();
                            return NotFound($"Team member with ID {id} not found.");
                        }
                    }

                    transaction.Commit();  
                    return Ok($"Team member with ID {id} and all related tasks have been deleted successfully.");
                }
                catch (SqlException sqlEx)
                {
                    transaction.Rollback();  
                    return StatusCode(500, $"A database error occurred: {sqlEx.Message}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); 
                    return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
                }
            }
        }

    }
}