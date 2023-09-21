using BitFaster.Caching;
using BitFaster.Caching.Lru;
using Slant.Entity.Demo.DomainModel;
using System;
using System.Threading.Tasks;

namespace Slant.Entity.Demo.Repositories;

public class UserRepositoryCache : IUserRepository
{
    private UserRepository _userRepository;
    
    private ICache<Guid, User> _cache = 
        new ConcurrentLruBuilder<Guid, User>()
            .WithCapacity(1024)
            .WithMetrics()
            .Build();

    public UserRepositoryCache(IAmbientDbContextLocator ambientDbContextLocator)
    {
        _userRepository = new UserRepository(ambientDbContextLocator);
        if (_cache.Events.HasValue)
        {
            _cache.Events.Value.ItemUpdated += (sender, args) => Console.WriteLine("Cache item updated");
            // Console.WriteLine("Events configured");
        }
    }

    public User Get(Guid userId)
    {
        return _cache.GetOrAdd(userId, _userRepository.Get);
    }

    public async ValueTask<User> GetAsync(Guid userId)
    {
        if (_cache.TryGet(userId, out var user))
        {
            return user;
        }
        user = await _userRepository.GetAsync(userId);
        return _cache.GetOrAdd(userId, _ => user);
    }

    public void Add(User user)
    {
        _userRepository.Add(user);
        _cache.AddOrUpdate(user.Id, user);
    }
}