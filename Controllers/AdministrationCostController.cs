﻿using Alliance_for_Life.Models;
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
    public class AdministrationCostController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: AdministrationCost
        public ActionResult Index(string sortOrder, Guid? searchString, string Month, int? Year, string currentFilter, int? page, int? pgSize)
        {
            ViewBag.Sub = searchString;
            ViewBag.Yr = Year;
            ViewBag.Mnth = Month;

            //paged view
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

            ViewBag.CurrentFilter = searchString;

            var adminSearch = db.AdminCosts
            .Include(a => a.Subcontractor)
            .Where(a => a.SubcontractorId == a.Subcontractor.SubcontractorId);

            if (!User.IsInRole("Admin"))
            {
                var organization = "";
                var id = User.Identity.GetUserId();
                var usr = db.Users.Find(id);
                organization = db.SubContractors.Find(usr.SubcontractorId).OrgName;
                var usersubid = db.Users.Find(id).SubcontractorId;

                adminSearch = from s in adminSearch
                              where usersubid == s.SubcontractorId
                              select s;

                ViewBag.Subcontractor = organization;
            }

            var year_search = "";

            if ((Year != null) && Year.ToString().Length > 1)
            {
                year_search = Year.ToString();
            }

            if (!String.IsNullOrEmpty(Month) || !String.IsNullOrEmpty(Year.ToString()) || !String.IsNullOrEmpty(searchString.ToString()))
            {
                var yearSearch = (Year);

                // if fields Month and Year are empty
                if (String.IsNullOrEmpty(Month) && String.IsNullOrEmpty(Year.ToString()))
                {
                    adminSearch = adminSearch.Where(r => r.Subcontractor.SubcontractorId == searchString);
                }
                // if fields Year and Org are empty
                else if (String.IsNullOrEmpty(Year.ToString()) && String.IsNullOrEmpty(searchString.ToString()))
                {
                    var regionSearch = Enum.Parse(typeof(Months), Month);
                    adminSearch = adminSearch.Where(r => r.Month == (Months)regionSearch);
                }
                // if fields Org and Month are empty
                else if (String.IsNullOrEmpty(searchString.ToString()) && String.IsNullOrEmpty(Month))
                {
                    adminSearch = adminSearch.Where(r => r.Year == yearSearch);
                }

                // if Month field is empty
                else if (String.IsNullOrEmpty(Month))
                {
                    adminSearch = adminSearch.Where(r => r.Year == yearSearch && r.Subcontractor.SubcontractorId == searchString);
                }
                //if Year is empty
                else if (String.IsNullOrEmpty(Year.ToString()))
                {
                    var regionSearch = Enum.Parse(typeof(Months), Month);
                    adminSearch = adminSearch.Where(r => r.Month == (Months)regionSearch && r.Subcontractor.SubcontractorId == searchString);
                }
                // if Org is empty
                else if (String.IsNullOrEmpty(searchString.ToString()))
                {
                    var regionSearch = Enum.Parse(typeof(Months), Month);
                    adminSearch = adminSearch.Where(r => r.Month == (Months)regionSearch && r.Year == yearSearch);
                }
                // if none are empty
                else
                {
                    var regionSearch = Enum.Parse(typeof(Months), Month);
                    adminSearch = adminSearch.Where(r => r.Month == (Months)regionSearch && r.Year == yearSearch && r.Subcontractor.SubcontractorId == searchString);
                }
            }

            switch (sortOrder)
            {
                case "name_desc":
                    adminSearch = adminSearch.OrderByDescending(s => s.Subcontractor.OrgName);
                    break;
                case "Date":
                    adminSearch = adminSearch.OrderBy(s => s.SubmittedDate);
                    break;
                case "date_desc":
                    adminSearch = adminSearch.OrderByDescending(s => s.SubmittedDate);
                    break;
                default:
                    adminSearch = adminSearch.OrderBy(s => s.Subcontractor.OrgName);
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

            ViewBag.TotalSum = adminSearch.Sum(m => m.ATotCosts).ToString("C");
            return View(adminSearch.ToPagedList(pageNumber, defaSize));
        }

        // GET: AdministrationCost/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AdminCosts adminCosts = db.AdminCosts
                .Include(a => a.Subcontractor)
                .SingleOrDefault(a => a.AdminCostId == id);

            if (adminCosts == null)
            {
                return HttpNotFound();
            }
            return View(adminCosts);
        }

        // GET: AdministrationCost/Create
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
            ViewBag.SubcontractorId = new SelectList(list.OrderBy(a => a.OrgName), "SubcontractorId", "OrgName");

            return View();
        }

        // POST: AdministrationCost/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AdminCostId,AOtherInput,AOtherInput2,AOtherInput3,ASalandWages,AflBillable,AEmpBenefits,AEmpTravel,AEmpTraining,AOfficeRent,AOfficeUtilities,AFacilityIns,AOfficeSupplies,AEquipment,AOfficeCommunications,AOfficeMaint,AConsulting,AJanitorServices,ADepreciation,ATechSupport,ASecurityServices,AOther,AOther2,AOther3,ATotCosts,Region,Month,SubcontractorId,Year,SubmittedDate")] AdminCosts adminCosts)
        {
            if (ModelState.IsValid)
            {
                var dataexist = from s in db.AdminCosts
                                where s.SubcontractorId == adminCosts.SubcontractorId &&
                                s.Year == adminCosts.Year &&
                                s.Month == adminCosts.Month
                                select s;
                if (dataexist.Count() >= 1)
                {
                    ViewBag.error = "Data already exists. Please change the params or search in the Reports tab for the current Record.";
                }
                else
                {
                    adminCosts.AdminCostId = Guid.NewGuid();
                    adminCosts.SubmittedDate = System.DateTime.Now;
                    adminCosts.AflBillable = 0;
                    adminCosts.Region = db.SubContractors.Where(A => A.SubcontractorId == adminCosts.SubcontractorId).FirstOrDefault().Region;
                    db.AdminCosts.Add(adminCosts);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();

            ViewBag.Year = new SelectList(datelist);
            ViewBag.SubcontractorId = new SelectList(db.SubContractors.OrderBy(a => a.OrgName), "SubcontractorId", "OrgName", adminCosts.SubcontractorId);

            return View(adminCosts);
        }

        // GET: AdministrationCost/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AdminCosts adminCosts = db.AdminCosts.Find(id);
            if (adminCosts == null)
            {
                return HttpNotFound();
            }

            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();
            ViewBag.Year = new SelectList(datelist);
            ViewBag.SubcontractorId = new SelectList(db.SubContractors.Where(a => a.SubcontractorId == adminCosts.SubcontractorId).OrderBy(a => a.OrgName), "SubcontractorId", "OrgName");

            return View(adminCosts);
        }

        // POST: AdministrationCost/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AdminCostId,AOtherInput,AOtherInput2,AOtherInput3,ASalandWages,AEmpBenefits,AflBillable,AEmpTravel,AEmpTraining,AOfficeRent,AOfficeUtilities,AFacilityIns,AOfficeSupplies,AEquipment,AOfficeCommunications,AOfficeMaint,AConsulting,AJanitorServices,ADepreciation,ATechSupport,ASecurityServices,AOther,AOther2,AOther3,ATotCosts,Region,Month,SubcontractorId,Year,SubmittedDate")] AdminCosts adminCosts)
        {
            if (ModelState.IsValid)
            {
                adminCosts.SubmittedDate = System.DateTime.Now;
                
                db.Entry(adminCosts).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();


            ViewBag.Year = new SelectList(datelist);
            ViewBag.SubcontractorId = new SelectList(db.SubContractors.Where(s => s.SubcontractorId == adminCosts.SubcontractorId).OrderBy(a => a.OrgName), "SubcontractorId", "OrgName");
            return View(adminCosts);
        }

        // GET: AdministrationCost/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AdminCosts adminCosts = db.AdminCosts

                .Include(a => a.Subcontractor)
                .SingleOrDefault(a => a.AdminCostId == id);

            if (adminCosts == null)
            {
                return HttpNotFound();
            }
            return View(adminCosts);
        }

        // POST: AdministrationCost/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            AdminCosts adminCosts = db.AdminCosts

                .Include(a => a.Subcontractor)
                .SingleOrDefault(a => a.AdminCostId == id);

            db.AdminCosts.Remove(adminCosts);
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

        [HttpPost]
        public FileResult Export()
        {
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[26]
            {

                new DataColumn ("Organization"),
                new DataColumn ("Month"),
                new DataColumn ("Region"),
                new DataColumn ("Year"),
                new DataColumn ("AflBillable"),
                new DataColumn ("Salary/Wages"),
                new DataColumn ("Employee Benefits"),
                new DataColumn ("Employee Travel"),
                new DataColumn ("Employee Training"),
                new DataColumn ("Office Rent"),
                new DataColumn ("Office Utilities"),
                new DataColumn ("Facility Insurance"),
                new DataColumn ("Office Supplies"),
                new DataColumn ("Equipment"),
                new DataColumn ("Office Communications"),
                new DataColumn ("Office Maintenance"),
                new DataColumn ("Consulting Fees"),
                new DataColumn ("Janitor Services"),
                new DataColumn ("Depreciation"),
                new DataColumn ("Technical Support"),
                new DataColumn ("Security Services"),
                new DataColumn ("Other"),
                new DataColumn ("Other 2"),
                new DataColumn ("Other 3"),
                new DataColumn ("Total Costs"),
                 new DataColumn ("Date Submitted"),
            });

            var costs = from a in db.AdminCosts
                        join s in db.SubContractors on a.SubcontractorId equals s.SubcontractorId
                        join us in db.Users on s.SubcontractorId equals us.SubcontractorId
                        where a.SubcontractorId == s.SubcontractorId
                        select new AdminReport
                        {
                            SubmittedDate = a.SubmittedDate,
                            OrgName = s.OrgName,
                            MonthName = a.Month.ToString(),
                            YearName = a.Year,
                            AflBillable = "$" + a.AflBillable.ToString(),
                            ASalandWages = "$" + a.ASalandWages.ToString(),
                            AEmpBenefits = "$" + a.AEmpBenefits.ToString(),
                            AEmpTravel = "$" + a.AEmpTravel.ToString(),
                            AEmpTraining = "$" + a.AEmpTraining.ToString(),
                            AOfficeRent = "$" + a.AOfficeRent.ToString(),
                            AOfficeUtilities = "$" + a.AOfficeUtilities.ToString(),
                            AFacilityIns = "$" + a.AFacilityIns.ToString(),
                            AOfficeSupplies = "$" + a.AOfficeSupplies.ToString(),
                            AEquipment = "$" + a.AEquipment.ToString(),
                            AOfficeCommunications = "$" + a.AOfficeCommunications.ToString(),
                            AOfficeMaint = "$" + a.AOfficeMaint.ToString(),
                            AConsulting = "$" + a.AConsulting.ToString(),
                            AJanitorServices = "$" + a.AJanitorServices.ToString(),
                            ADepreciation = "$" + a.ADepreciation.ToString(),
                            ATechSupport = "$" + a.ATechSupport.ToString(),
                            ASecurityServices = "$" + a.ASecurityServices.ToString(),
                            AOther = a.AOtherInput + ": $" + a.AOther.ToString(),
                            AOther2 = a.AOtherInput2 + ": $" + a.AOther2.ToString(),
                            AOther3 = a.AOtherInput3 + ": $" + a.AOther3.ToString(),
                            ATotCosts = "$" + a.ATotCosts.ToString()
                        };

            if (!User.IsInRole("Admin"))
            {
                var id = User.Identity.GetUserId();


                costs = from a in db.AdminCosts
                        join s in db.SubContractors on a.SubcontractorId equals s.SubcontractorId
                        join us in db.Users on s.SubcontractorId equals us.SubcontractorId
                        where a.SubcontractorId == s.SubcontractorId && us.Id == id
                        select new AdminReport
                        {
                            SubmittedDate = a.SubmittedDate,
                            OrgName = s.OrgName,
                            MonthName = a.Month.ToString(),
                            YearName = a.Year,
                            AflBillable = "$" + a.AflBillable.ToString(),
                            ASalandWages = "$" + a.ASalandWages.ToString(),
                            AEmpBenefits = "$" + a.AEmpBenefits.ToString(),
                            AEmpTravel = "$" + a.AEmpTravel.ToString(),
                            AEmpTraining = "$" + a.AEmpTraining.ToString(),
                            AOfficeRent = "$" + a.AOfficeRent.ToString(),
                            AOfficeUtilities = "$" + a.AOfficeUtilities.ToString(),
                            AFacilityIns = "$" + a.AFacilityIns.ToString(),
                            AOfficeSupplies = "$" + a.AOfficeSupplies.ToString(),
                            AEquipment = "$" + a.AEquipment.ToString(),
                            AOfficeCommunications = "$" + a.AOfficeCommunications.ToString(),
                            AOfficeMaint = "$" + a.AOfficeMaint.ToString(),
                            AConsulting = "$" + a.AConsulting.ToString(),
                            AJanitorServices = "$" + a.AJanitorServices.ToString(),
                            ADepreciation = "$" + a.ADepreciation.ToString(),
                            ATechSupport = "$" + a.ATechSupport.ToString(),
                            ASecurityServices = "$" + a.ASecurityServices.ToString(),
                            AOther = a.AOtherInput + ": $" + a.AOther.ToString(),
                            AOther2 = a.AOtherInput2 + ": $" + a.AOther2.ToString(),
                            AOther3 = a.AOtherInput3 + ": $" + a.AOther3.ToString(),
                            ATotCosts = "$" + a.ATotCosts.ToString()

                        };

            }
            foreach (var item in costs)
            {
                dt.Rows.Add(item.OrgName, item.MonthName, item.RegionName, item.YearName, item.AflBillable, item.ASalandWages, item.AEmpBenefits,
                    item.AEmpTravel, item.AEmpTraining, item.AOfficeRent, item.AOfficeUtilities, item.AFacilityIns, item.AOfficeSupplies, item.AEquipment,
                    item.AOfficeCommunications, item.AOfficeMaint, item.AConsulting, item.AJanitorServices, item.ADepreciation,
                    item.ATechSupport, item.ASecurityServices, item.AOther, item.AOther2, item.AOther3, item.ATotCosts, item.SubmittedDate);
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AdministrationCosts.xlsx");
                }
            }
        }
    }
}
