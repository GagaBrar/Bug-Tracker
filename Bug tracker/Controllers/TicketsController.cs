using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Bug_tracker.Helpers;
using Bug_tracker.Models;
using Microsoft.AspNet.Identity;
using System.Net.Sockets;

namespace Bug_tracker.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private ApplicationDbContext db { get; set; }
        private UserRoleHelper UserRoleHelper { get; set; }
        public TicketsController()
        {
            db = new ApplicationDbContext();
            UserRoleHelper = new UserRoleHelper();
        }
        // GET: Tickets
        public ActionResult Index(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                return View(db.Tickets.Include(t => t.TicketPriority).Include(t => t.Project).Include(t => t.TicketStatus).Include(t => t.TicketType).Where(p => p.CreaterId == User.Identity.GetUserId()).ToList());
            }
            return View(db.Tickets.Include(t => t.TicketPriority).Include(t => t.Project).Include(t => t.TicketStatus).Include(t => t.TicketType).ToList());
        }

        //Get UserTickets
        public ActionResult UserTickets()
        {
            string userID = User.Identity.GetUserId();
            if (User.IsInRole("Submitter"))
            {
                var tickets = db.Tickets.Where(t => t.CreaterId == userID).Include(t => t.Creater).Include(t => t.Assignee).Include(t => t.Project);
                return View("Index", tickets.ToList());
            }
            if (User.IsInRole("Developer"))
            {
                var tickets = db.Tickets.Where(t => t.AssigneeId == userID).Include(t => t.Comments).Include(t => t.Creater).Include(t => t.Assignee).Include(t => t.Project);
                return View("Index", tickets.ToList());
            }
            if (User.IsInRole("Project Manager"))
            {
                return View(db.Tickets.Include(t => t.TicketPriority).Include(t => t.Project).Include(t => t.Comments).Include(t => t.TicketStatus).Include(t => t.TicketType).Where(p => p.AssigneeId == userID).ToList());
            }
            return View("Index");
        }
        // Project Manger and Developer Tickets
        [Authorize(Roles = "Developer,Project Manager")]
        public ActionResult ProjectManagerOrDeveloperTickets()
        {
            string userId = User.Identity.GetUserId();
            var UserIds = db.Users.Where(p => p.Id == userId).FirstOrDefault();
            var projectsId = UserIds.ProjectsList.Select(p => p.Id).ToList();
            var tickets = db.Tickets.Where(p => projectsId.Contains(p.ProjectId)).ToList();
            return View("Index", tickets);
        }

        public ActionResult AssignDeveloper(int ticketId)
        {
            var model = new AssigndeveloperTicket();
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == ticketId);

            //var users = db.Users.ToList();
            ////var users = userRoleHelper.UsersInRole("Developer");
            //var userAssignedtoTicket = ticket.Users   .Select(p => p.Id).ToList();
            //model.TicketId = ticketId;
            var developers = UserRoleHelper.UsersInRole("Developer");
            model.DeveloperList = new SelectList(developers, "Id", "Name");
            return View(model);
        }


        //var model = new AssignViewModel();
        //model.Id = id;
        //    var project = db.Projects.FirstOrDefault(p => p.Id == id);
        //var users = db.Users.ToList();
        //var userIdsAssignedToProject = project.Users.Select(p => p.Id).ToList();
        //model.UserList = new MultiSelectList(users, "Id", "Name", userIdsAssignedToProject);
        //    return View(model);



        [HttpPost]
        public ActionResult AssignDeveloper(AssigndeveloperTicket model)
        {
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == model.TicketId);
            ticket.AssigneeId = model.SelectedDeveloperId;
            // Plug in your email service here to send an email.
            var user = db.Users.FirstOrDefault(p => p.Id == model.SelectedDeveloperId);
            var personalEmailService = new PersonalEmailService();
            var mailMessage = new MailMessage(
            WebConfigurationManager.AppSettings["emailto"], user.Email
                   );
            mailMessage.Body = "DB Has Some new Changes";
            mailMessage.Subject = "New Assigned Developer";
            mailMessage.IsBodyHtml = true;
            personalEmailService.Send(mailMessage);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Tickets/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tickets tickets = db.Tickets.Find(id);
            if (tickets == null)
            {
                return HttpNotFound();
            }
            return View(tickets);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Project Manager,Developer,Submitter")]
        public ActionResult CreateComment(int id, string body)
        {
            var tickets = db.Tickets.Where(p => p.Id == id).FirstOrDefault();
            var userId = User.Identity.GetUserId();
            var MangerOrDeveloperId = db.Users.Where(p => p.Id == userId).FirstOrDefault();
            var projectsIds = MangerOrDeveloperId.ProjectsList.Select(p => p.Id).ToList();
            var projects = db.Tickets.Where(p => projectsIds.Contains(p.ProjectId)).ToList();
            if (tickets == null)
            {
                return HttpNotFound();
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                ViewBag.ErrorMessage = "Comment is required";
                return View("Details", tickets);
            }
            if ((User.IsInRole("Admin") || User.IsInRole("ProjectManagers")))
            {
                var comment = new TicketComment();
                comment.UserId = User.Identity.GetUserId();
                comment.TicketId = tickets.Id;
                comment.Created = DateTime.Now;
                comment.Comment = body;
                db.TicketComments.Add(comment);
                var user = db.Users.FirstOrDefault(p => p.Id == comment.UserId);
                var personalEmailService = new PersonalEmailService();
                var mailMessage = new MailMessage(
                WebConfigurationManager.AppSettings["emailto"], user.Email
                       );
                mailMessage.Body = "Somebody Commented on the ticket.";
                mailMessage.Subject = "New Comment";
                mailMessage.IsBodyHtml = true;
                personalEmailService.Send(mailMessage);
                db.SaveChanges();
               
            }
            else if (User.Identity.IsAuthenticated)
            {
                ViewBag.ErrorMessage = "Sorry!! you are not allowed to comment.";
                return View("Details", tickets);
            }
            return RedirectToAction("Details", new { id });
        }

        // GET: Tickets/Create
        [Authorize(Roles = "Submitter")]
        public ActionResult Create()
        {
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name");
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Submitter")]
        public ActionResult Create([Bind(Include = "Id,Name,Description,TicketTypeId,TicketPriorityId,ProjectId")] Tickets tickets)
        {
            if (ModelState.IsValid)
            {
                if (tickets == null)
                {
                    return HttpNotFound();
                }
                tickets.TicketStatusId = 3;
                db.Tickets.Add(tickets);

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", tickets.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", tickets.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", tickets.TicketTypeId);
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Submitter")]
        public ActionResult CreateAttachment(int ticketId, [Bind(Include = "Id,Description,TicketTypeId")] TicketAttachment ticketAttachment, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                var tickets = db.Tickets.FirstOrDefault(t => t.Id == ticketId);
                var userId = User.Identity.GetUserId();
                var MangerOrDeveloperId = db.Users.Where(p => p.Id == userId).FirstOrDefault();
                var projectsIds = MangerOrDeveloperId.ProjectsList.Select(p => p.Id).ToList();
                var projects = db.Tickets.Where(p => projectsIds.Contains(p.ProjectId)).ToList();
                if (!ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    ViewBag.ErrorMessage = "Please link the Image";

                }
                if (image == null)
                {
                    return HttpNotFound();
                }
                if ((User.IsInRole("Admin")) 
                    || (User.IsInRole("Project Manager") && projects.Any(p => p.Id ==  ticketId)) 
                    || (User.IsInRole("Submitter") && tickets.CreaterId == userId) || (User.IsInRole("Developer") && tickets.Assignee.Id == userId))
                {
                    var fileName = Path.GetFileName(image.FileName);
                    image.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), fileName));
                    ticketAttachment.FilePath = "/Uploads/" + fileName;
                    ticketAttachment.UserId = User.Identity.GetUserId();
                    ticketAttachment.Created = DateTime.Now;
                    ticketAttachment.TicketId = ticketId;
                    db.TicketAttachments.Add(ticketAttachment);
                    var user = db.Users.FirstOrDefault(p => p.Id == ticketAttachment.UserId);

                    
                    var personalEmailService = new PersonalEmailService();
                    var mailMessage = new MailMessage(
                    WebConfigurationManager.AppSettings["emailto"], user.Email  );
                    mailMessage.Body = "There is a new attachment to the Ticket";
                    mailMessage.Subject = "New Attachment";
                    mailMessage.IsBodyHtml = true;
                    personalEmailService.Send(mailMessage);
                    db.SaveChanges();
                }            
                return RedirectToAction("Details", new { id= ticketId });
            }
            return View(ticketAttachment);
        }
    

        // GET: Tickets/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tickets tickets = db.Tickets.Find(id);
            if (tickets == null)
            {
                return HttpNotFound();
            }


            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "DisplayName", tickets.AssigneeId);
            ViewBag.CreaterId = new SelectList(db.Users, "Id", "DisplayName", tickets.CreaterId);            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", tickets.ProjectId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", tickets.TicketStatusId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", tickets.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", tickets.TicketTypeId);
            return View(tickets);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Description,TicketTypeId,TicketPriorityId,CreaterId,TicketStatusId,ProjectId")] Tickets tickets)
        {
            if (ModelState.IsValid)
            {
                var dbticket = db.Tickets.FirstOrDefault(p => p.Id == tickets.Id);
                dbticket.Name = tickets.Name;
                dbticket.Description = tickets.Description;
                dbticket.Updated = DateTime.Now;
                var dateChanged = DateTimeOffset.Now;
                var changes = new List<TicketHistory>();
                var dbTicket = db.Tickets.FirstOrDefault(p => p.Id == tickets.Id);
                dbTicket.Name = tickets.Name;
                dbTicket.Description = tickets.Description;
                dbTicket.TicketTypeId = tickets.TicketTypeId;
                dbTicket.Updated = dateChanged;
        
                var originalValues = db.Entry(dbTicket).OriginalValues;
                var currentValues = db.Entry(dbTicket).CurrentValues;
                foreach (var property in originalValues.PropertyNames)
                {
                    var originalValue = originalValues[property]?.ToString();
                    var currentValue = currentValues[property]?.ToString();
                    if (originalValue != currentValue)
                    {
                        var history = new TicketHistory();
                        history.NewValue = GetValueFromKey(property, currentValue);
                        history.OldValue = GetValueFromKey(property, originalValue);
                        history.Changed = dateChanged;
                        history.Property = property;
                        history.TicketId = dbTicket.Id;
                        history.UserId = User.Identity.GetUserId();
                        changes.Add(history);
                    }
                }
                db.TicketHistories.AddRange(changes);
                if (dbTicket.AssigneeId != null)
                {
                    var user = db.Users.FirstOrDefault(p => p.Id == dbTicket.AssigneeId);
                    var personalEmailService = new PersonalEmailService();
                    var mailMessage = new MailMessage(
                    WebConfigurationManager.AppSettings["emailto"], user.Email
                           );
                    mailMessage.Body = "Ticket Has Some new Changes";
                    mailMessage.Subject = "Modified Ticket";
                    mailMessage.IsBodyHtml = true;
                    personalEmailService.Send(mailMessage);
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Name", tickets.AssigneeId);
            ViewBag.CreaterId = new SelectList(db.Users, "Id", "Name", tickets.CreaterId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", tickets.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", tickets.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", tickets.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", tickets.TicketTypeId);
            return View(tickets);
        }
        private string GetValueFromKey(string propertyName, string key)
        {
            if (propertyName == "TicketTypeId")
            {
                return db.TicketTypes.Find(Convert.ToInt32(key)).Name;
            }
            return key;
        }


        // GET: Tickets/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tickets tickets = db.Tickets.Find(id);
            if (tickets == null)
            {
                return HttpNotFound();
            }
            return View(tickets);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Tickets tickets = db.Tickets.Find(id);
            db.Tickets.Remove(tickets);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

    
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
