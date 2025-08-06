using System.Collections.Generic;
using System.Linq;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public class UserService : IUserService
    {
        private readonly Dictionary<int, User> _users = new();
        private int _nextId = 1;
    
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
    }
}