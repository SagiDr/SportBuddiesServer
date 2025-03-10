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
    public List<string> GetAllEmails()//returns a list of emails of all of the users in the app
    {
        return this.Users.Select(u => u.Email).ToList();
    }
}

