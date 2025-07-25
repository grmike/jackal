﻿using JackalWebHost2.Data.Interfaces;
using JackalWebHost2.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JackalWebHost2.Data.Repositories;

public class UserRepositoryInMemory : IUserRepository
{
    private static long Id;
    
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;

    public UserRepositoryInMemory(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
    }

    public async Task<User?> GetUser(long id, CancellationToken token)
    {
        return _memoryCache.TryGetValue<User>(GetKey(id), out var user)
            ? user
            : null;
    }

    public async Task<IList<User>> GetUsers(long[] ids, CancellationToken token)
    {
        var list = new List<User>();
        foreach (var id in ids)
        {
            var user = await GetUser(id, token);
            if (user != null) list.Add(user);
        }
        return list;
    }

    public async Task<User?> GetUser(string login, CancellationToken token)
    {
        return null;
    }

    public Task<User> CreateUser(string login, CancellationToken token)
    {
        var user = new User
        {
            Id = Interlocked.Increment(ref Id),
            Login = login
        };

        _memoryCache.Set(GetKey(user.Id), user, _cacheEntryOptions);
        return Task.FromResult(user);
    }
    
    private static string GetKey(long num) => "user:" + num;
}