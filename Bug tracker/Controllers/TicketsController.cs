﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Bug_tracker.Helpers;
using Bug_tracker.Models;
using Microsoft.AspNet.Identity;

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

        //Get UsreTickets
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
                var tickets = db.Tickets.Where(t => t.AssigneeId == userID).Include(t => t.Creater).Include(t => t.Assignee).Include(t => t.Project);
                return View("Index", tickets.ToList());
            }
            return View("Index");
        }
        // Project Manger and Developer Tickets
        [Authorize(Roles = "Project Manager,Developer")]
        public ActionResult ProjectManagerOrDeveloperTickets()
        {
            string userId = User.Identity.GetUserId();
            var ProjectId = db.Users.Where(p => p.Id == userId).FirstOrDefault();
            var Project = ProjectId.ProjectsList.Select(p => p.Id).FirstOrDefault();
            var tickets = db.Tickets.Where(p => p.Id == Project).ToList();
            return View("Index", tickets);
        }

        public ActionResult AssignDeveloper(int ticketId)
        {
            var model = new AssigndeveloperTicket();
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == ticketId);
            var userRoleHelper = new UserRoleHelper();
            var users = userRoleHelper.UsersInRole("Developer");
            model.TicketId = ticketId;
            model.DeveloperList = new SelectList(users, "Id", "Name");
            return View(model);
        }
        [HttpPost]
        public ActionResult AssignDeveloper(AssigndeveloperTicket model)
        {
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == model.TicketId);
            ticket.AssigneeId = model.SelectedDeveloperId;
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
                tickets.CreaterId = User.Identity.GetUserId();
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
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", tickets.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", tickets.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", tickets.TicketTypeId);
            return View(tickets);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Description,TicketTypeId,TicketPriorityId,CreaterId,TicketStatusId,AssigneeId,ProjectId")] Tickets tickets)
        {
            if (ModelState.IsValid)
            {
                var dbsticket = db.Tickets.FirstOrDefault(p => p.Id == tickets.Id);
                dbsticket.Name = tickets.Name;
                dbsticket.Description = tickets.Description;
                dbsticket.Updated = DateTime.Now;
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
