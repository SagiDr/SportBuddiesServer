using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SportBuddiesServer.Models;

public partial class SportBuddiesDbContext : DbContext
{
    public User? GetUser(string email)
    {
        return this.Users.Where(u => u.Email == email)
                            .FirstOrDefault();
    }
}

