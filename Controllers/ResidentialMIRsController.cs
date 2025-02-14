﻿using Alliance_for_Life.Models;
using Microsoft.AspNet.Identity;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;


namespace Alliance_for_Life.Controllers
{
    [Authorize]
    public class ResidentialMIRsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: ResidentialMIRs
        public ActionResult Index(string sortOrder, int? Year, string Month, Guid? searchString, string currentFilter, int? page, int? pgSize)
        {
            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();
            ViewBag.Year = new SelectList(datelist);
            ViewBag.Month = new SelectList(Enum.GetValues(typeof(Months)).Cast<Months>());
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.Subcontractor = new SelectList(db.SubContractors.OrderBy(a => a.OrgName), "SubcontractorId", "OrgName");
            //assign default values if Year is empty
            if (String.IsNullOrEmpty(Year.ToString()))
            {
                Year = DateTime.Now.Year;
            }

            //assign default values if Month is empty
            if (String.IsNullOrEmpty(Month))
            {
                Month = DateTime.Now.Month.ToString();
                ViewBag.Title = "Monthly Service Report for " + DateTime.Now.ToString("MMMM") + "-" + Year;
            }
            else
            {
                ViewBag.Title = "Monthly Service Report for " + Month + "-" + Year;
            }
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

            var ressearch = db.ResidentialMIRs.Include(a => a.Subcontractor);

            var nonresidental = db.NonResidentialMIRs.Include(a => a.Subcontractor)
                .Where(a => a.SubcontractorId == a.Subcontractor.SubcontractorId).ToList();
           

            //if year is not null
            if (Year != null)
            {
                ressearch = ressearch.Where(a => a.Year == Year);

                nonresidental = nonresidental.Where(a => a.Year == Year).ToList();
            }
            //if month is not null
            if (!String.IsNullOrEmpty(Month))
            {
                ressearch = ressearch.Where(a => a.Month.ToString() == Month);

                nonresidental = nonresidental.Where(a => a.Months.ToString() == Month).ToList();
            }

            if (!User.IsInRole("Admin"))
            {
                var organization = "";
                var id = User.Identity.GetUserId();
                var usr = db.Users.Find(id);
                organization = db.SubContractors.Find(usr.SubcontractorId).OrgName;
                var usersubid = db.Users.Find(id).SubcontractorId;

                ressearch = from s in ressearch
                            where usersubid == s.SubcontractorId
                            select s;

                nonresidental = nonresidental.Where(t => t.SubcontractorId == usersubid).ToList();

                ViewBag.Subcontractor = organization;
            }


            if (!String.IsNullOrEmpty(searchString.ToString()))
            {
                ressearch = ressearch.Where(a => a.Subcontractor.SubcontractorId == searchString);

                nonresidental = nonresidental.Where(a => a.Subcontractor.SubcontractorId == searchString).ToList();
            }

            switch (sortOrder)
            {
                case "name_desc":
                    ressearch = ressearch.OrderByDescending(s => s.Subcontractor.OrgName);
                    nonresidental = nonresidental.OrderByDescending(s => s.Subcontractor.OrgName).ToList();
                    break;
                case "Date":
                    ressearch = ressearch.OrderBy(s => s.SubmittedDate);
                    nonresidental = nonresidental.OrderBy(s => s.SubmittedDate).ToList();
                    break;
                case "date_desc":
                    ressearch = ressearch.OrderByDescending(s => s.SubmittedDate);
                    nonresidental = nonresidental.OrderByDescending(s => s.SubmittedDate).ToList();
                    break;
                default:
                    ressearch = ressearch.OrderBy(s => s.Subcontractor.OrgName);
                    nonresidental = nonresidental.OrderBy(s => s.Subcontractor.OrgName).ToList();
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

            ViewBag.nonResidentialMIRs = nonresidental.ToList();
            return View(ressearch.ToPagedList(pageNumber, defaSize));
        }

        // GET: ResidentialMIRs/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ResidentialMIR residentialMIR = db.ResidentialMIRs
                 .Include(s => s.Subcontractor)
                 .SingleOrDefault(a => a.Id == id);

            if (residentialMIR == null)
            {
                return HttpNotFound();
            }
            return View(residentialMIR);
        }

        // GET: ResidentialMIRs/Create
        public ActionResult Create()
        {
            var list = db.SubContractors.ToList();
            if (!User.IsInRole("Admin"))
            {
                var id = User.Identity.GetUserId();
                var usersubid = db.Users.Find(id).SubcontractorId;

                list = list.Where(s => s.SubcontractorId == usersubid).ToList();
            }

            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();
            ViewBag.Year = new SelectList(datelist);
            ViewBag.SubcontractorId = new SelectList(list, "SubcontractorId", "OrgName");

            return View();
        }

        // POST: ResidentialMIRs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,SubmittedDate,SubcontractorId,TotOverallServed,TotBedNights,TotA2AEnrollment,TotA2ABedNights,MA2Apercent,ClientsJobEduServ,ParticipatingFathers,TotEduClasses,TotClientsinEduClasses,TotCaseHrs,TotClientsCaseHrs,TotOtherClasses,Year,Month")] ResidentialMIR residentialMIR)
        {
            if (ModelState.IsValid)
            {
                var dataexist = from s in db.ResidentialMIRs
                                where s.SubcontractorId == residentialMIR.SubcontractorId &&
                                s.Year == residentialMIR.Year &&
                                s.Month == residentialMIR.Month
                                select s;
                if (dataexist.Count() >= 1)
                {
                    ViewBag.error = "Data already exists. Please change the params or search in the Reports tab for the current Record.";
                }
                else
                {
                    residentialMIR.SubmittedDate = DateTime.Now;
                    residentialMIR.Id = Guid.NewGuid();
                    db.ResidentialMIRs.Add(residentialMIR);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();

            ViewBag.Year = new SelectList(datelist);
            ViewBag.SubcontractorId = new SelectList(db.SubContractors.OrderBy(a=>a.OrgName), "SubcontractorId", "OrgName", residentialMIR.SubcontractorId);

            return View(residentialMIR);
        }

        // GET: ResidentialMIRs/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ResidentialMIR residentialMIR = db.ResidentialMIRs.Find(id);
            if (residentialMIR == null)
            {
                return HttpNotFound();
            }

            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();

            ViewBag.Year = new SelectList(datelist);
            ViewBag.SubcontractorId = new SelectList(db.SubContractors.Where(a => a.SubcontractorId == residentialMIR.SubcontractorId).OrderBy(a=>a.OrgName), "SubcontractorId", "OrgName");

            return View(residentialMIR);
        }

        // POST: ResidentialMIRs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,SubmittedDate,SubcontractorId,TotBedNights,TotOverallServed,TotA2AEnrollment,TotA2ABedNights,MA2Apercent,ClientsJobEduServ,ParticipatingFathers,TotEduClasses,TotClientsinEduClasses,TotCaseHrs,TotClientsCaseHrs,TotOtherClasses,Year,Month")] ResidentialMIR residentialMIR)
        {
            if (ModelState.IsValid)
            {
                residentialMIR.SubmittedDate = DateTime.Now;
                db.Entry(residentialMIR).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();

            ViewBag.Year = new SelectList(datelist);
            ViewBag.SubcontractorId = new SelectList(db.SubContractors.OrderBy(a=>a.OrgName), "SubcontractorId", "OrgName", residentialMIR.SubcontractorId);

            return View(residentialMIR);
        }

        // GET: ResidentialMIRs/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ResidentialMIR residentialMIR = db.ResidentialMIRs
                .Include(s => s.Subcontractor)
                .SingleOrDefault();

            if (residentialMIR == null)
            {
                return HttpNotFound();
            }
            return View(residentialMIR);
        }

        // POST: ResidentialMIRs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            ResidentialMIR residentialMIR = db.ResidentialMIRs
                .Include(s => s.Subcontractor)
                .SingleOrDefault();

            db.ResidentialMIRs.Remove(residentialMIR);
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
