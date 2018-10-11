namespace Bug_tracker.Migrations
{
    using Bug_tracker.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "Bug_tracker.Models.ApplicationDbContext";
        }

        protected override void Seed(ApplicationDbContext context)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            if (!context.Roles.Any(p => p.Name == "Admin"))
            {
                var role = new IdentityRole("Admin");

                roleManager.Create(role);
            }
            if (!context.Roles.Any(p => p.Name == "Project Manager"))
            {
                roleManager.Create(new IdentityRole("Project Manager"));
            }

            if (!context.Roles.Any(p => p.Name == "Developer"))
            {
                roleManager.Create(new IdentityRole("Developer"));
            }

            if (!context.Roles.Any(p => p.Name == "Submitter"))
            {
                roleManager.Create(new IdentityRole("Submitter"));
            }

            ApplicationUser adminUser;

            if (!context.Users.Any(p => p.UserName == "admin@bugtracker.com"))
            {
                adminUser = new ApplicationUser();
                adminUser.Email = "admin@bugtracker.com";
                adminUser.UserName = "admin@bugtracker.com";
               
                userManager.Create(adminUser, "Password-1");
            }
            else
            {
                adminUser = context.Users.FirstOrDefault(p => p.UserName == "admin@bugtracker.com");
            }

            if (!userManager.IsInRole(adminUser.Id, "Admin"))
            {
                userManager.AddToRole(adminUser.Id, "Admin");
            }
            context.TicketTypes.AddOrUpdate(x => x.Id,
           new Models.TicketType() { Id = 1, Name = "Bug Fixes" },
           new Models.TicketType() { Id = 2, Name = "Software Update" },
           new Models.TicketType() { Id = 3, Name = "Adding Helpers" },
           new Models.TicketType() { Id = 4, Name = "Database errors" });
            context.TicketPriorities.AddOrUpdate(x => x.Id,
               new Models.TicketPriority() { Id = 1, Name = "High" },
               new Models.TicketPriority() { Id = 2, Name = "Medium" },
               new Models.TicketPriority() { Id = 3, Name = "Low" },
               new Models.TicketPriority() { Id = 4, Name = "Urgent" });
            context.TicketStatuses.AddOrUpdate(x => x.Id,
               new Models.TicketStatus() { Id = 1, Name = "Finished" },
               new Models.TicketStatus() { Id = 2, Name = "Started" },
               new Models.TicketStatus() { Id = 3, Name = "Not Started" },
               new Models.TicketStatus() { Id = 4, Name = "In progress" });
            context.SaveChanges();
        }
    }
}
