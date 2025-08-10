using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public interface IUserService
    {
        IEnumerable<User> GetAll();
        User? GetById(int id);
        User Add(User user);
        bool Update(int id, User user);
        bool Delete(int id);
        User? GetByUsername(string username);
    }
}