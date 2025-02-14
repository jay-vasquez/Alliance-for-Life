﻿using Alliance_for_Life.Models;
using Alliance_for_Life.ViewModels;
using ClosedXML.Excel;
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
    public class BudgetCostsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: BudgetCosts
        public ViewResult Index(string sortOrder, string searchString, string Year, string currentFilter, int? page, int? pgSize)
        {
            //paged view
            ViewBag.CurrentSort = sortOrder;
            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();
            ViewBag.Year = new SelectList(datelist);
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.YearSortParm = sortOrder == "Year" ? "year_desc" : "Year";
            ViewBag.RegionSortParm = sortOrder == "Region" ? "region_desc" : "Region";

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

            var budgetsearch = from s in db.BudgetCosts
                               select s;

            var year_search = "";

            if ((Year != null) && Year.Length > 1)
            {
                year_search = (Year);
            }

            if (!String.IsNullOrEmpty(searchString) || !String.IsNullOrEmpty(Year))
            {
                var yearSearch = (Year);

                if (String.IsNullOrEmpty(searchString))
                {
                    budgetsearch = budgetsearch.Where(r => r.Year.ToString() == Year).OrderBy(r => r.Region);
                }
                else if (String.IsNullOrEmpty(Year.ToString()))
                {
                    var regionSearch = Enum.Parse(typeof(GeoRegion), searchString);
                    budgetsearch = budgetsearch.Where(r => r.Region == (GeoRegion)regionSearch).OrderBy(r => r.Region);
                }
                else
                {
                    var regionSearch = Enum.Parse(typeof(GeoRegion), searchString);
                    budgetsearch = budgetsearch.Where(r => r.Region == (GeoRegion)regionSearch && r.Year.ToString() == Year).OrderBy(r => r.Region);
                }
            }

            switch (sortOrder)
            {
                case "Year":
                    budgetsearch = budgetsearch.OrderBy(s => s.Year);
                    break;
                case "Region":
                    budgetsearch = budgetsearch.OrderBy(s => s.Region);
                    break;
                case "year_desc":
                    budgetsearch = budgetsearch.OrderByDescending(s => s.Year);
                    break;
                case "region_desc":
                    budgetsearch = budgetsearch.OrderByDescending(s => s.Region);
                    break;
                default:
                    budgetsearch = budgetsearch.OrderBy(s => s.Region);
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

            return View(budgetsearch.ToPagedList(pageNumber, defaSize));

        }

        public ViewResult CostAnalysis(string sortOrder, string searchString, string Year, string currentFilter, int? page, int? pgSize)
        {
            //paged view
            ViewBag.CurrentSort = sortOrder;
            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();
            ViewBag.Year = new SelectList(datelist);
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.YearSortParm = sortOrder == "Year" ? "year_desc" : "Year";
            ViewBag.RegionSortParm = sortOrder == "Region" ? "region_desc" : "Region";

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

            var budgetsearch = from s in db.BudgetCosts
                               join a in db.AdminCosts on s.Region equals a.Region
                               join b in db.ParticipationServices on s.Region equals b.Region
                               where s.Year == a.Year && s.Year == b.Year && a.Month == b.Month
                               select new BudgetViewModel { budgetcosts = s, admincost = a, particost = b };

            var year_search = "";

            if ((Year != null) && Year.Length > 1)
            {
                year_search = (Year);
            }

            if (!String.IsNullOrEmpty(searchString) || !String.IsNullOrEmpty(Year))
            {
                if (String.IsNullOrEmpty(searchString))
                {
                    budgetsearch = budgetsearch.Where(r => r.budgetcosts.Year.ToString() == year_search).OrderBy(r => r.budgetcosts.Region);
                }
                else if (String.IsNullOrEmpty(Year))
                {
                    var regionSearch = Enum.Parse(typeof(GeoRegion), searchString);
                    budgetsearch = budgetsearch.Where(r => r.budgetcosts.Region == (GeoRegion)regionSearch).OrderBy(r => r.budgetcosts.Region);
                }
                else
                {
                    var regionSearch = Enum.Parse(typeof(GeoRegion), searchString);
                    budgetsearch = budgetsearch.Where(r => r.budgetcosts.Region == (GeoRegion)regionSearch && r.budgetcosts.Year.ToString() == year_search).OrderBy(r => r.budgetcosts.Region);
                }
            }

            budgetsearch = budgetsearch.OrderByDescending(m => m.budgetcosts.Year);

            switch (sortOrder)
            {
                case "Year":
                    budgetsearch = budgetsearch.OrderBy(s => s.budgetcosts.Year);
                    break;
                case "Region":
                    budgetsearch = budgetsearch.OrderBy(s => s.budgetcosts.Region);
                    break;
                case "year_desc":
                    budgetsearch = budgetsearch.OrderByDescending(s => s.budgetcosts.Year);
                    break;
                case "region_desc":
                    budgetsearch = budgetsearch.OrderByDescending(s => s.budgetcosts.Region);
                    break;
                default:
                    budgetsearch = budgetsearch.OrderBy(s => s.budgetcosts.Region);
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

            return View(budgetsearch.ToPagedList(pageNumber, defaSize));
        }

        // GET: BudgetCosts/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BudgetCosts budgetCosts = db.BudgetCosts

                .SingleOrDefault(a => a.BudgetInvoiceId == id);
            if (budgetCosts == null)
            {
                return HttpNotFound();
            }
            return View(budgetCosts);
        }

        // GET: BudgetCosts/Create
        public ActionResult Create()
        {
            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();
            ViewBag.Year = new SelectList(datelist);
            return View();
        }

        // POST: BudgetCosts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "BudgetInvoiceId,ASalandWages,AEmpBenefits,AEmpTravel,AEmpTraining,AOfficeRent,AOfficeUtilities,AFacilityIns,AOfficeSupplies,AEquipment,AOfficeCommunications,AOfficeMaint,AConsulting,SubConPayCost,BackgrounCheck,Other,AJanitorServices,ADepreciation,ATechSupport,ASecurityServices,ATotCosts,AdminFee,Trasportation,JobTraining,TuitionAssistance,ContractedResidential,UtilityAssistance,EmergencyShelter,HousingAssistance,Childcare,Clothing,Food,Supplies,RFO,BTotal,Maxtot,Region,Month,Year,SubmittedDate")] BudgetCosts budgetCosts)
        {
            if (ModelState.IsValid)
            {
                var dataexist = from s in db.BudgetCosts
                                where
                                s.Year == budgetCosts.Year &&
                                s.Region == budgetCosts.Region
                                select s;
                if (dataexist.Count() >= 1)
                {
                    ViewBag.error = "Data already exists. Please change the params or search in the Reports tab for the current Record.";
                }
                else
                {
                    budgetCosts.BudgetInvoiceId = Guid.NewGuid();
                    budgetCosts.SubmittedDate = DateTime.Now;
                    db.BudgetCosts.Add(budgetCosts);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();

            ViewBag.Year = new SelectList(datelist);

            return View(budgetCosts);
        }

        // GET: BudgetCosts/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BudgetCosts budgetCosts = db.BudgetCosts.Find(id);
            if (budgetCosts == null)
            {
                return HttpNotFound();
            }

            var datelist = Enumerable.Range(System.DateTime.Now.Year-1, 5).ToList();

            ViewBag.Year = new SelectList(datelist);

            return View(budgetCosts);
        }

        // POST: BudgetCosts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BudgetInvoiceId,ASalandWages,AEmpBenefits,AEmpTravel,AEmpTraining,AOfficeRent,AOfficeUtilities,AFacilityIns,AOfficeSupplies,AEquipment,AOfficeCommunications,AOfficeMaint,AConsulting,SubConPayCost,BackgrounCheck,Other,AJanitorServices,ADepreciation,ATechSupport,ASecurityServices,ATotCosts,AdminFee,Trasportation,JobTraining,TuitionAssistance,ContractedResidential,UtilityAssistance,EmergencyShelter,HousingAssistance,Childcare,Clothing,Food,Supplies,RFO,BTotal,Maxtot,Region,Month,Year,SubmittedDate")] BudgetCosts budgetCosts)
        {
            if (ModelState.IsValid)
            {
                budgetCosts.SubmittedDate = DateTime.Now;
                db.Entry(budgetCosts).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var datelist = Enumerable.Range(DateTime.Now.Year, 5).ToList();

            ViewBag.Year = new SelectList(datelist);


            return View(budgetCosts);
        }

        // GET: BudgetCosts/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BudgetCosts budgetCosts = db.BudgetCosts

                .SingleOrDefault(a => a.BudgetInvoiceId == id);

            if (budgetCosts == null)
            {
                return HttpNotFound();
            }
            return View(budgetCosts);
        }

        // POST: BudgetCosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            BudgetCosts budgetCosts = db.BudgetCosts

                .SingleOrDefault(a => a.BudgetInvoiceId == id);

            db.BudgetCosts.Remove(budgetCosts);
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
            //get the invoice id
            var exportid = Request["BudgetInvoiceId"];


            //create datatable
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[39]
            {
                new DataColumn ("Budget Report Id"),
                new DataColumn ("Date Submitted"),
                new DataColumn ("Region"),
                new DataColumn ("Year"),
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
                new DataColumn ("EFT Fees"),
                new DataColumn ("Background Check"),
                new DataColumn ("Other"),
                new DataColumn ("Janitorial Services"),
                new DataColumn ("Depreciation"),
                new DataColumn ("Technical Support"),
                new DataColumn ("Security Services"),
                new DataColumn ("Admininstration Totals"),
                new DataColumn ("Administration Fee"),
                new DataColumn ("Transportation"),
                new DataColumn ("Job Training"),
                new DataColumn ("Tuition Assistance"),
                new DataColumn ("Contracted Residential Care"),
                new DataColumn ("Utility Assistance"),
                new DataColumn ("Emergency Shelter"),
                new DataColumn ("Housing Assistance"),
                new DataColumn ("Childcare"),
                new DataColumn ("Clothing"),
                new DataColumn ("Food"),
                new DataColumn ("Supplies"),
                new DataColumn ("RFO Costs"),
                new DataColumn ("Participation Total Costs"),
                new DataColumn ("Direct and Participation Total Costs")
            });

            var costs = from a in db.BudgetCosts
                        select new BudgetReport
                        {
                            BudgetInvoiceId = a.BudgetInvoiceId,
                            SubmittedDate = a.SubmittedDate,
                            RegionName = a.Region.ToString(),
                            YearName = a.Year,
                            ASalandWages = a.ASalandWages,
                            AEmpBenefits = a.AEmpBenefits,
                            AEmpTravel = a.AEmpTravel,
                            AEmpTraining = a.AEmpTraining,
                            AOfficeRent = a.AOfficeRent,
                            AOfficeUtilities = a.AOfficeUtilities,
                            AFacilityIns = a.AFacilityIns,
                            AOfficeSupplies = a.AOfficeSupplies,
                            AEquipment = a.AEquipment,
                            AOfficeCommunications = a.AOfficeCommunications,
                            AOfficeMaint = a.AOfficeMaint,
                            AConsulting = a.AConsulting,
                            SubConPayCost = a.SubConPayCost,
                            BackgrounCheck = a.BackgrounCheck,
                            Other = a.Other,
                            AJanitorServices = a.AJanitorServices,
                            ADepreciation = a.ADepreciation,
                            ATechSupport = a.ATechSupport,
                            ASecurityServices = a.ASecurityServices,
                            ATotCosts = a.ATotCosts,
                            AdminFee = a.AdminFee,
                            Trasportation = a.Trasportation,
                            JobTraining = a.JobTraining,
                            TuitionAssistance = a.TuitionAssistance,
                            ContractedResidential = a.ContractedResidential,
                            UtilityAssistance = a.UtilityAssistance,
                            EmergencyShelter = a.EmergencyShelter,
                            HousingAssistance = a.HousingAssistance,
                            Childcare = a.Childcare,
                            Clothing = a.Clothing,
                            Food = a.Food,
                            RFO = a.RFO,
                            BTotal = a.BTotal,
                            Maxtot = a.Maxtot
                        };

            if (exportid.Length == 32)
            {
                costs = costs.Where(s => s.BudgetInvoiceId == new Guid(exportid));
            }

            foreach (var item in costs)
            {
                dt.Rows.Add(item.BudgetInvoiceId, item.SubmittedDate, item.RegionName, item.YearName, item.ASalandWages, item.AEmpBenefits,
                    item.AEmpTravel, item.AEmpTraining, item.AOfficeRent, item.AOfficeUtilities, item.AFacilityIns, item.AOfficeSupplies,
                    item.AEquipment, item.AOfficeCommunications, item.AOfficeMaint, item.AConsulting, item.SubConPayCost, item.BackgrounCheck,
                    item.Other, item.AJanitorServices, item.ADepreciation, item.ATechSupport, item.ASecurityServices, item.ATotCosts,
                    item.AdminFee, item.Trasportation, item.JobTraining, item.TuitionAssistance, item.ContractedResidential,
                    item.UtilityAssistance, item.EmergencyShelter, item.HousingAssistance, item.Childcare, item.Clothing,
                    item.Food, item.RFO, item.BTotal, item.Maxtot);
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