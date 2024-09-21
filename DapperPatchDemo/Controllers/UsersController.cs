using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Threading.Tasks;
using DapperPatchDemo.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using System;

namespace DapperPatchExample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly SqliteConnection _connection;

        public UsersController(SqliteConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// 查询用户
        /// </summary>
        /// <returns>用户列表</returns>
        [HttpGet("")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _connection.QueryAsync<User>("SELECT * FROM Users");
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// 查询用户详情
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>用户详情</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="user">用户对象</param>
        /// <returns>创建结果</returns>
        [HttpPost("")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _connection.ExecuteAsync("INSERT INTO Users (Name, Email, Age, Address) VALUES (@Name, @Email, @Age, @Address)", user);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="patchDoc">更新内容</param>
        /// <returns>更新结果</returns>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] JsonPatchDocument<User> patchDoc)
        {
            try
            {
                var user = await GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound();
                }

                patchDoc.ApplyTo(user, error =>
                {
                    ModelState.AddModelError(error.Operation.path, error.ErrorMessage);
                });

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await UpdateUserAsync(user);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                await _connection.ExecuteAsync("DELETE FROM Users WHERE Id = @Id", new { Id = id });
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<User> GetUserByIdAsync(int id)
        {
            return await _connection.QuerySingleOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
        }

        private async Task UpdateUserAsync(User user)
        {
            await _connection.ExecuteAsync("UPDATE Users SET Name = @Name, Email = @Email, Age = @Age, Address = @Address WHERE Id = @Id", user);
        }
    }
}
