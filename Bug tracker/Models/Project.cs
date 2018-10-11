﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bug_tracker.Models
{
    public class Project

    {
      public Project()
        {
            Users = new HashSet<ApplicationUser>();
            Tickets = new HashSet<Tickets>();

        }

        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<ApplicationUser> Users { get; set; }
        public virtual ICollection<Tickets> Tickets { get; set; }

    }
}