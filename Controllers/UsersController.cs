using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            try
            {
                return Ok(_userService.GetAll());
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        [HttpGet("{id}")]
        public ActionResult<User> GetUser(int id)
        {
            try
            {
                var user = _userService.GetById(id);
                if (user == null) return NotFound($"User with ID {id} not found");
                return Ok(user);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        [HttpPost]
        public ActionResult<User> CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("User data is required");
                
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check for duplicate email
            var existingUser = _userService.GetAll().FirstOrDefault(u => 
                u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
            
            if (existingUser != null)
                return Conflict($"A user with email '{user.Email}' already exists");

            try
            {
                var created = _userService.Add(user);
                return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] User updatedUser)
        {
            if (updatedUser == null)
                return BadRequest("User data is required");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check for duplicate email (excluding current user)
            var existingUser = _userService.GetAll().FirstOrDefault(u => 
                u.Id != id && u.Email.Equals(updatedUser.Email, StringComparison.OrdinalIgnoreCase));
            
            if (existingUser != null)
                return Conflict($"A user with email '{updatedUser.Email}' already exists");

            try
            {
                if (!_userService.Update(id, updatedUser))
                    return NotFound($"User with ID {id} not found");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                if (!_userService.Delete(id))
                    return NotFound($"User with ID {id} not found");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }
    }
}