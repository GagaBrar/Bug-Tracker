﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Bug_tracker.Models
{
    public class AssigndeveloperTicket
    {
            public int TicketId { get; set; }
            public SelectList DeveloperList { get; set; }
            public string SelectedDeveloperId { get; set; }
            public string TicketName { get; set; }
    }
}
