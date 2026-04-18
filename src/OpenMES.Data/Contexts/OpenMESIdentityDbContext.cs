using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Entities;

namespace OpenMES.Data.Contexts;

public class OpenMESIdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
	public OpenMESIdentityDbContext(DbContextOptions<OpenMESIdentityDbContext> options) : base(options)
	{
	}

}
