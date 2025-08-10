using System.Collections.Generic;
using System.Linq;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public class UserService : IUserService
    {
        private readonly Dictionary<int, User> _users = new()
        {
            { 1, new User { 
                Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", 
                Department = "IT", Username = "john.doe", Password = "password123", Role = "Employee" 
            }},
            { 2, new User { 
                Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", 
                Department = "HR", Username = "jane.smith", Password = "password123", Role = "Manager" 
            }},
            { 3, new User { 
                Id = 3, FirstName = "Admin", LastName = "User", Email = "admin@example.com", 
                Department = "IT", Username = "admin", Password = "admin123", Role = "Admin" 
            }},
            { 4, new User { 
                Id = 4, FirstName = "Bob", LastName = "Developer", Email = "bob@example.com", 
                Department = "Engineering", Username = "developer", Password = "dev123", Role = "Employee" 
            }}
        };
        private int _nextId = 5;
    
        public IEnumerable<User> GetAll() => _users.Values;

        public User? GetById(int id) => _users.TryGetValue(id, out var user) ? user : null;

        public User Add(User user)
        {
            user.Id = _nextId++;
            _users.Add(user.Id, user);
            return user;
        }

        public bool Update(int id, User user)
        {
            if (!_users.TryGetValue(id, out var existing)) 
                return false;
            
            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.Email = user.Email;
            existing.Department = user.Department;
            return true;
        }

        public bool Delete(int id)
        {
            return _users.Remove(id);
        }

        // Add method to find user by username
        public User? GetByUsername(string username)
        {
            return _users.Values.FirstOrDefault(u => u.Username == username);
        }
    }
}