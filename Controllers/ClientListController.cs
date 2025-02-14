﻿using Alliance_for_Life.Models;
using Alliance_for_Life.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Alliance_for_Life.Controllers
{
    [Authorize]
    public class ClientListController : Controller
    {
        private readonly ApplicationDbContext db;

        public ClientListController()
        {
            db = new ApplicationDbContext();
        }

        [Authorize]
        public ActionResult Create()
        {
            var subcontractorlist = new ClientListFormViewModel
            {
                Subcontractors = from s in db.SubContractors
                                 join a in db.Users on s.AdministratorId equals a.Id
                                 where s.SubcontractorId == a.SubcontractorId
                                 select s,
                Heading = "Add Client Information"
            };

            if (!User.IsInRole("Admin"))
            {

                var id = User.Identity.GetUserId();
                var usersubid = db.Users.Find(id).SubcontractorId;
                var viewModel = new ClientListFormViewModel
                {
                    Subcontractors = db.SubContractors.ToList().Where(s => s.SubcontractorId == usersubid).ToList(),
                    Heading = "Add Client Information"
                };
                return View("ClientListForm", viewModel);
            }


            return View("ClientListForm", subcontractorlist);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ClientListFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Subcontractors = db.SubContractors.ToList();
                return View("ClientListForm", viewModel);
            }

            var client = new ClientList
            {
                Id = Guid.NewGuid(),
                SubcontractorId = viewModel.SubcontractorId,
                FirstName = viewModel.FirstName,
                LastName = viewModel.LastName,
                DOB = viewModel.DOB,
                DueDate = viewModel.DueDate,
                Active = viewModel.Active,
                SubmittedDate = DateTime.Now
            };

            db.ClientLists.Add(client);
            db.SaveChanges();

            return RedirectToAction("Create", "ContractorForm");
        }

        // GET: ClientList/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ClientList client = db.ClientLists

                .SingleOrDefault(a => a.Id == id);
            if (client == null)
            {
                return HttpNotFound();
            }
            return View(client);
        }

        // GET: AdministrationCost/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ClientList client = db.ClientLists

                .Include(a => a.Subcontractor)
                .SingleOrDefault(a => a.Id == id);

            if (client == null)
            {
                return HttpNotFound();
            }
            return View(client);
        }

        // POST: AdministrationCost/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            ClientList client = db.ClientLists

                .Include(a => a.Subcontractor)
                .SingleOrDefault(a => a.Id == id);

            db.ClientLists.Remove(client);
            db.SaveChanges();
            return RedirectToAction("AllActiveClients");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(ClientListFormViewModel viewModel)
        {
            //if (!ModelState.IsValid)
            //{
            //    viewModel.Subcontractors = db.SubContractors.ToList();
            //    return View("ClientListForm", viewModel);
            //}

            var client = db.ClientLists.SingleOrDefault(s => s.Id == viewModel.Id);
            {
                client.SubcontractorId = new Guid(Request["SubcontractorId"]);
                client.FirstName = viewModel.FirstName;
                client.LastName = viewModel.LastName;
                client.DOB = viewModel.DOB;
                client.DueDate = viewModel.DueDate;
                client.Active = viewModel.Active;
            };

            db.SaveChanges();
            if (!User.IsInRole("Admin"))
            {
                if (client.Active)
                {
                    return RedirectToAction("ActiveClients", "ClientList");
                }
                return RedirectToAction("NonActiveClients", "ClientList");
            }
            else
            {
                if (client.Active)
                {
                    return RedirectToAction("AllActiveClients", "ClientList");
                }
                return RedirectToAction("AllNonActiveClients", "ClientList");
            }

        }

        [Authorize]
        public ActionResult Edit(Guid? id)
        {
            var client = db.ClientLists.Find(id);
            var viewModel = new ClientListFormViewModel
            {
                Heading = "Edit Client Information",
                Id = client.Id,
                Subcontractors = db.SubContractors.ToList().Where(s => s.SubcontractorId == client.SubcontractorId).ToList(),
                FirstName = client.FirstName,
                LastName = client.LastName,
                DOB = client.DOB,
                DueDate = client.DueDate,
                Active = client.Active,
                SubmittedDate = DateTime.Now
            };
            viewModel.SubcontractorId = client.SubcontractorId;
            viewModel.DOB = client.DOB;
            viewModel.DueDate = client.DueDate;

            return View("ClientListForm", viewModel);
        }

        public ViewResult AllActiveClients(string sortOrder, Guid? searchString, string currentFilter, int? page, int? pgSize)
        {
            ViewBag.Sub = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.LastNameSortParm = sortOrder == "LastName" ? "last_desc" : "LastName";
            ViewBag.Subcontractor = new SelectList(db.SubContractors.OrderBy(a => a.OrgName), "SubcontractorId", "OrgName");

            // looking for the searchstring
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                currentFilter = searchString.ToString();
            }
            ViewBag.CurrentFilter = searchString;

            var clients = db.ClientLists.Include(s => s.Subcontractor)
                .Where(s => s.Active);

            if (!User.IsInRole("Admin"))
            {
                var id = User.Identity.GetUserId();
                var usersubid = db.Users.Find(id).SubcontractorId;

                clients = from s in clients
                          where usersubid == s.SubcontractorId
                          select s;
            }

            if (!String.IsNullOrEmpty(searchString.ToString()))
            {
                clients = clients.Where(a => a.Subcontractor.SubcontractorId == searchString);
            }

            switch (sortOrder)
            {
                case "name_desc":
                    clients = clients.OrderByDescending(s => s.Subcontractor.OrgName);
                    break;
                case "Date":
                    clients = clients.OrderBy(s => s.DueDate);
                    break;
                case "date_desc":
                    clients = clients.OrderByDescending(s => s.DueDate);
                    break;
                case "LastName":
                    clients = clients.OrderBy(s => s.LastName);
                    break;
                case "last_desc":
                    clients = clients.OrderByDescending(s => s.LastName);
                    break;
                default:
                    clients = clients.OrderBy(s => s.Subcontractor.OrgName);
                    break;
            }

            int pageNumber = (page ?? 1);
            int defaSize = (pgSize ?? 15);

            ViewBag.psize = defaSize;

            ViewBag.PageSize = new List<SelectListItem>()
            {
                new SelectListItem() { Value="10", Text= "10" },
                new SelectListItem() { Value="20", Text= "20" },
                new SelectListItem() { Value="30", Text= "30" },
                new SelectListItem() { Value="40", Text= "40" },
            };

            ViewBag.ActiveClients = clients.Count();
            return View(clients.ToPagedList(pageNumber, defaSize));
        }

        public FileResult ExportAllActive()
        {
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[6]
            {
                new DataColumn ("Client ID"),
                new DataColumn ("Organization"),
                new DataColumn ("First Name"),
                new DataColumn ("Last Name"),
                new DataColumn ("Birthday"),
                new DataColumn ("Due Date"),
            });

            var user1 = User.Identity.GetUserId();
            var query = from cl in db.ClientLists
                        join su in db.SubContractors on cl.SubcontractorId equals su.SubcontractorId
                        where cl.Active
                        select new ClientListReport
                        {
                            Id = cl.Id,
                            Orgname = su.OrgName,
                            FirstName = cl.FirstName,
                            LastName = cl.LastName,
                            DOB = cl.DOB,
                            DueDate = cl.DueDate,
                            SubmittedDate = cl.SubmittedDate
                        };

            foreach (var item in query)
            {
                dt.Rows.Add(item.Id, item.Orgname, item.FirstName, item.LastName, item.DOB.ToShortDateString(), item.DueDate.ToShortDateString());
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Grid.xlsx");
                }
            }
        }

        public ActionResult ActiveClients(string sortOrder, string searchString, string currentFilter, int? page, int? pgSize)
        {
            ViewBag.Sub = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.LastNameSortParm = sortOrder == "LastName" ? "last_desc" : "LastName";

            //looking for the searchstring
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var clients = db.ClientLists.Include(s => s.Subcontractor)
                .Where(s => s.SubcontractorId == s.Subcontractor.SubcontractorId)
                .Where(s => s.Active);

            if (!User.IsInRole("Admin"))
            {
                var id = User.Identity.GetUserId();

                var usersubid = from s in db.Users
                                where s.Id == id
                                select s.SubcontractorId;

                foreach (var items in usersubid)
                {
                    clients = db.ClientLists.Include(s => s.Subcontractor)
                                .Where(s => s.SubcontractorId == items)
                                .Where(s => s.Active);
                }

            }


            if (!String.IsNullOrEmpty(searchString))
            {
                clients = clients.Where(a => a.Subcontractor.OrgName.Contains(searchString)
                || a.LastName.ToString().Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    clients = clients.OrderByDescending(s => s.Subcontractor.OrgName);
                    break;
                case "Date":
                    clients = clients.OrderBy(s => s.DueDate);
                    break;
                case "date_desc":
                    clients = clients.OrderByDescending(s => s.DueDate);
                    break;
                case "LastName":
                    clients = clients.OrderBy(s => s.LastName);
                    break;
                case "last_desc":
                    clients = clients.OrderByDescending(s => s.LastName);
                    break;
                default:
                    clients = clients.OrderBy(s => s.Subcontractor.OrgName);
                    break;
            }

            int pageNumber = (page ?? 1);
            int defaSize = (pgSize ?? 15);

            ViewBag.psize = defaSize;

            ViewBag.PageSize = new List<SelectListItem>()
            {
                new SelectListItem() { Value="10", Text= "10" },
                new SelectListItem() { Value="20", Text= "20" },
                new SelectListItem() { Value="30", Text= "30" },
                new SelectListItem() { Value="40", Text= "40" },
            };
            ViewBag.ActiveClients = clients.Count();
            return View(clients.ToPagedList(pageNumber, defaSize));
        }

        [HttpPost]
        public FileResult ExportActive()
        {
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[6]
            {
                new DataColumn ("Client ID"),
                new DataColumn ("Organization"),
                new DataColumn ("First Name"),
                new DataColumn ("Last Name"),
                new DataColumn ("Birthday"),
                new DataColumn ("Due Date")
            });

            var user1 = User.Identity.GetUserId();
            var query = from cl in db.ClientLists
                        join su in db.SubContractors on cl.SubcontractorId equals su.SubcontractorId
                        join us in db.Users on su.SubcontractorId equals us.SubcontractorId
                        where cl.Active && us.Id == user1
                        select new ClientListReport
                        {
                            Id = cl.Id,
                            Orgname = su.OrgName,
                            FirstName = cl.FirstName,
                            LastName = cl.LastName,
                            DOB = cl.DOB,
                            DueDate = cl.DueDate
                        };

            foreach (var item in query)
            {
                dt.Rows.Add(item.Id, item.Orgname, item.FirstName, item.LastName, item.DOB.ToShortDateString(), item.DueDate.ToShortDateString());
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Grid.xlsx");
                }
            }
        }

        public ActionResult AllNonActiveClients(string sortOrder, Guid? searchString, string currentFilter, int? page, int? pgSize)
        {
            ViewBag.Sub = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.LastNameSortParm = sortOrder == "LastName" ? "last_desc" : "LastName";
            ViewBag.Subcontractor = new SelectList(db.SubContractors.OrderBy(a => a.OrgName), "SubcontractorId", "OrgName");

            //looking for the searchstring
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                currentFilter = searchString.ToString();
            }
            ViewBag.CurrentFilter = searchString;

            var clients = db.ClientLists.Include(s => s.Subcontractor)
                .Where(s => s.Active == false);

            if (!String.IsNullOrEmpty(searchString.ToString()))
            {
                clients = clients.Where(a => a.Subcontractor.SubcontractorId == searchString);
            }

            //counting the number of non-active clients

            switch (sortOrder)
            {
                case "name_desc":
                    clients = clients.OrderByDescending(s => s.Subcontractor.OrgName);
                    break;
                case "Date":
                    clients = clients.OrderBy(s => s.DueDate);
                    break;
                case "date_desc":
                    clients = clients.OrderByDescending(s => s.DueDate);
                    break;
                case "LastName":
                    clients = clients.OrderBy(s => s.LastName);
                    break;
                case "last_desc":
                    clients = clients.OrderByDescending(s => s.LastName);
                    break;
                default:
                    clients = clients.OrderBy(s => s.Subcontractor.OrgName);
                    break;
            }

            int pageNumber = (page ?? 1);
            int defaSize = (pgSize ?? 15);

            ViewBag.psize = defaSize;

            ViewBag.PageSize = new List<SelectListItem>()
            {
                new SelectListItem() { Value="10", Text= "10" },
                new SelectListItem() { Value="20", Text= "20" },
                new SelectListItem() { Value="30", Text= "30" },
                new SelectListItem() { Value="40", Text= "40" },
            };

            ViewBag.NonActive = clients.Count();

            return View(clients.ToPagedList(pageNumber, defaSize));
        }

        [HttpPost]
        public FileResult ExportAllNonActive()
        {
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[6]
            {
                new DataColumn ("Client ID"),
                new DataColumn ("Organization"),
                new DataColumn ("First Name"),
                new DataColumn ("Last Name"),
                new DataColumn ("Birthday"),
                new DataColumn ("Due Date")
            });

            var user1 = User.Identity.GetUserId();
            var query = from cl in db.ClientLists
                        join su in db.SubContractors on cl.SubcontractorId equals su.SubcontractorId
                        where cl.Active == false
                        select new ClientListReport
                        {
                            Id = cl.Id,
                            Orgname = su.OrgName,
                            FirstName = cl.FirstName,
                            LastName = cl.LastName,
                            DOB = cl.DOB,
                            DueDate = cl.DueDate
                        };

            foreach (var item in query)
            {
                dt.Rows.Add(item.Id, item.Orgname, item.FirstName, item.LastName, item.DOB.ToShortDateString(), item.DueDate.ToShortDateString());
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Grid.xlsx");
                }
            }
        }

        public ActionResult NonActiveClients(string sortOrder, string searchString, string currentFilter, int? page, int? pgSize)
        {
            ViewBag.Sub = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.LastNameSortParm = sortOrder == "LastName" ? "last_desc" : "LastName";

            //looking for the searchstring
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;

            var clients = db.ClientLists.Include(s => s.Subcontractor)
                .Where(s => s.SubcontractorId == s.Subcontractor.SubcontractorId)
                .Where(s => s.Active == false);

            if (!User.IsInRole("Admin"))
            {
                var id = User.Identity.GetUserId();

                var usersubid = from s in db.Users
                                where s.Id == id
                                select s.SubcontractorId;

                foreach (var items in usersubid)
                {
                    clients = db.ClientLists.Include(s => s.Subcontractor)
                                .Where(s => s.SubcontractorId == items)
                                 .Where(s => s.Active == false);
                    ;
                }

            }

            if (!String.IsNullOrEmpty(searchString))
            {
                clients = clients.Where(a => a.Subcontractor.OrgName.Contains(searchString)
                || a.LastName.ToString().Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    clients = clients.OrderByDescending(s => s.Subcontractor.OrgName);
                    break;
                case "Date":
                    clients = clients.OrderBy(s => s.DueDate);
                    break;
                case "date_desc":
                    clients = clients.OrderByDescending(s => s.DueDate);
                    break;
                case "LastName":
                    clients = clients.OrderBy(s => s.LastName);
                    break;
                case "last_desc":
                    clients = clients.OrderByDescending(s => s.LastName);
                    break;
                default:
                    clients = clients.OrderBy(s => s.Subcontractor.OrgName);
                    break;
            }

            int pageNumber = (page ?? 1);
            int defaSize = (pgSize ?? 15);

            ViewBag.psize = defaSize;

            ViewBag.PageSize = new List<SelectListItem>()
            {
                new SelectListItem() { Value="10", Text= "10" },
                new SelectListItem() { Value="20", Text= "20" },
                new SelectListItem() { Value="30", Text= "30" },
                new SelectListItem() { Value="40", Text= "40" },
            };

            ViewBag.NonActive = clients.Count();
            return View(clients.ToPagedList(pageNumber, defaSize));
        }

        [HttpPost]
        public FileResult ExportNonActive()
        {
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[6]
            {
                new DataColumn ("Client ID"),
                new DataColumn ("Organization"),
                new DataColumn ("First Name"),
                new DataColumn ("Last Name"),
                new DataColumn ("Birthday"),
                new DataColumn ("Due Date")
            });

            var user1 = User.Identity.GetUserId();
            var query = from cl in db.ClientLists
                        join su in db.SubContractors on cl.SubcontractorId equals su.SubcontractorId
                        join us in db.Users on su.SubcontractorId equals us.SubcontractorId
                        where cl.Active == false && us.Id == user1
                        select new ClientListReport
                        {
                            Id = cl.Id,
                            Orgname = su.OrgName,
                            FirstName = cl.FirstName,
                            LastName = cl.LastName,
                            DOB = cl.DOB,
                            DueDate = cl.DueDate
                        };

            foreach (var item in query)
            {
                dt.Rows.Add(item.Id, item.Orgname, item.FirstName, item.LastName, item.DOB.ToShortDateString(), item.DueDate.ToShortDateString());
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Grid.xlsx");
                }
            }
        }
    }
}