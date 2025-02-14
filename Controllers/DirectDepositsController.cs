﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Alliance_for_Life.Models;
using Alliance_for_Life.ViewModels;
using ClosedXML.Excel;
using PagedList;

namespace Alliance_for_Life.Controllers
{
    public class DirectDepositsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: DirectDeposits
        public ActionResult Index(string sortOrder, Guid? searchString, string Month, int? Year, string currentFilter, int? page, int? pgSize)
        {
           
            DirectDeposits directdepo = new DirectDeposits();

            ViewBag.Sub = searchString;
            ViewBag.Yr = Year;
            ViewBag.Mnth = Month;

            ViewBag.CurrentSort = sortOrder;
            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();
            ViewBag.Year = new SelectList(datelist);
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.YearSortParm = sortOrder == "Year" ? "year_desc" : "Year";
            ViewBag.Subcontractor = new SelectList(db.SubContractors.OrderBy(a => a.OrgName), "SubcontractorId", "OrgName");
            ViewBag.Month = new SelectList(Enum.GetValues(typeof(Months)).Cast<Months>());


           
            //looking for the searchstring
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                currentFilter = searchString.ToString();
            }
           
           var directdeposit = from a in db.AdminCosts
                                join p in db.ParticipationServices on a.SubcontractorId equals p.SubcontractorId
                                where (a.Year ==  p.Year ) && (a.Month ==  p.Month)
                                select new DirectDepositView
                                {
                                    AdminCost = a,
                                    ParticipationService = p
                                };


            //filter based on the month and year selected
            directdeposit = directdeposit.Where(a => a.AdminCost.Year == Year && a.ParticipationService.Year == Year).Where(b => b.AdminCost.Month.ToString() == Month && b.ParticipationService.Month.ToString() == Month);
            //check to see if there is data stored on the database

            foreach (var items in directdeposit.ToList())
            {
                if (checkdeposit(items.AdminCost.AdminCostId, items.ParticipationService.PSId) == true)
                {
                    //create if the data does not exists
                    Create(directdepo, items.AdminCost.AdminCostId, items.ParticipationService.PSId, items.AdminCost.Year, items.AdminCost.Month.ToString());
                }
            }

            //return data from the database
            var depodb = db.DirectDeposit.OrderBy(a=>a.AdminCost.Subcontractor.OrgName).ToList();

            //checking for contrac
            if(!String.IsNullOrEmpty(searchString.ToString()))
            {
                 depodb = depodb.Where(r => r.AdminCost.SubcontractorId == searchString ).ToList();
                
            }
            if (!String.IsNullOrEmpty(Month))
            {
                depodb = depodb.Where(r => r.Month.ToString() == Month).ToList();
            }

            if (!String.IsNullOrEmpty(Year.ToString()))
            {
                depodb = depodb.Where(r => r.Year == Year).ToList();
            }

            depodb = depodb.OrderBy(b => b.Year).ThenBy(c => c.Month).ToList();
            //sorting
            switch (sortOrder)
            {
                case "name_desc":
                    depodb = depodb.OrderByDescending(s => s.AdminCost.Subcontractor.OrgName).ToList();
                    break;
                default:
                    break;
            }

            ViewBag.CurrentFilter = searchString;
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

            return View(depodb.ToPagedList(pageNumber, defaSize));
        }

        //checking to see if the data exists on the table
        public Boolean checkdeposit(Guid AdminID, Guid PatriID )
        {
            var adddeposit = false;
            var deposit = db.DirectDeposit
                .Where(a => a.AdminCost.AdminCostId == AdminID)
                .Where(a => a.ParticipationService.PSId == PatriID).ToList();

            if(deposit.Count() == 0)
            {
                adddeposit = true;
            }

            return adddeposit;
        }

        // GET: DirectDeposits/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DirectDeposits/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DirectDeposits directDeposits, Guid AdminId, Guid PartiID, int year, string month)
        {
            directDeposits = new DirectDeposits();

            if (ModelState.IsValid)
            {
                directDeposits.DirectDepositsId = Guid.NewGuid();
                directDeposits.AdminCostId = AdminId;
                directDeposits.PSId = PartiID;
                directDeposits.Year = year;
                directDeposits.Month = (Months)Enum.Parse(typeof(Months), month);
                db.DirectDeposit.Add(directDeposits);
                db.SaveChanges();
                return View();
            }
            return View();
        }

        // GET: DirectDeposits/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DirectDeposits directDeposits = db.DirectDeposit.Find(id);
            if (directDeposits == null)
            {
                return HttpNotFound();
            }
            ViewBag.Subcontractor = db.SubContractors.Find( directDeposits.AdminCost.SubcontractorId).OrgName;
                                 
            return View(directDeposits);
        }

        // POST: DirectDeposits/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DirectDepositsId,IsCheked")] DirectDeposits directDeposits)
        {
            if (ModelState.IsValid)
            {
                directDeposits.AdminCostId = new Guid(Request["AdminCostId"]);
                directDeposits.PSId = new Guid(Request["PSId"]);
                db.Entry(directDeposits).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(directDeposits);
        }

        // GET: DirectDeposits/Delete/5
       
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        //export excel sheets
        
        [HttpPost]
        public FileResult Export(string Month, int? Year)
        {
           
            DataTable dt = new DataTable("Direct Deposit");
            dt.Columns.AddRange(new DataColumn[8]
            {
                new DataColumn ("EIN"),
                new DataColumn ("Organization"),
                 new DataColumn ("Year"),
                  new DataColumn ("Month"),
                new DataColumn ("Amount"),
                new DataColumn ("Direct Admin Cost"),
                //admincost
                new DataColumn ("3%"),
                new DataColumn ("Subcontractor Direct Deposit Total"),
            });
            
            var directdeposit = from a in db.AdminCosts
                                join p in db.ParticipationServices on a.SubcontractorId equals p.SubcontractorId
                                where (a.Year == p.Year) && (a.Month == p.Month)
                                select new DirectDepositView
                                {
                                    AdminCost = a,
                                    ParticipationService = p
                                };

            if (!String.IsNullOrEmpty(Month))
            {
                directdeposit = directdeposit.Where(b => b.AdminCost.Month.ToString() == Month && b.ParticipationService.Month.ToString() == Month);
            }
            if (!String.IsNullOrEmpty(Year.ToString()))
            {
                directdeposit = directdeposit.Where(b => b.AdminCost.Year == Year && b.ParticipationService.Year == Year);
            }
         //   directdeposit = directdeposit.Where(a => a.AdminCost.Year == Year && a.ParticipationService.Year == Year).Where(b => b.AdminCost.Month.ToString() == Month && b.ParticipationService.Month.ToString() == Month);

            var subs = db.SubContractors.ToList();

            //orderby year / month / orgname
            foreach (var item in directdeposit.OrderBy(a=>a.AdminCost.Year).OrderBy(a=>a.AdminCost.Month).ThenBy(a=>a.AdminCost.Subcontractor.OrgName))
            {
               
                //finding subcontractor information
                dt.Rows.Add(subs.Where(a=>a.SubcontractorId == item.AdminCost.SubcontractorId).FirstOrDefault().EIN, subs.Where(a => a.SubcontractorId == item.AdminCost.SubcontractorId).FirstOrDefault().OrgName,item.AdminCost.Year,item.AdminCost.Month ,(item.AdminCost.ATotCosts + item.ParticipationService.PTotals).ToString("C"), item.AdminCost.ATotCosts.ToString("C")
                   , (item.AdminCost.ATotCosts * 0.03).ToString("C"), (item.AdminCost.ATotCosts + item.ParticipationService.PTotals - item.AdminCost.ATotCosts * 0.03).ToString("C") );
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Direct Deposit.xlsx");
                }
            }
        }
    }
}
