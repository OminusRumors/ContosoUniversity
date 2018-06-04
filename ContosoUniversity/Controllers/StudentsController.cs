using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Models.SchoolViewModels;

namespace ContosoUniversity.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SchoolContext _context;

        public StudentsController(SchoolContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index(
            string sortOrder,
            string searchString,
            string currentFilter,
            int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParam"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var students = from s in _context.Students
                           select s;

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString)
                  || s.FirstMidName.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    students = students.OrderByDescending(s => s.LastName);
                    break;
                case "Date":
                    students = students.OrderBy(s => s.EnrollmentDate);
                    break;
                case "date_desc":
                    students.OrderByDescending(s => s.EnrollmentDate);
                    break;
                default:
                    students = students.OrderBy(s => s.LastName);
                    break;
            }

            int pageSize = 3;
            return View(await PaginatedList<Student>.CreateAsync(students.AsNoTracking(), page ?? 1, pageSize));
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.
                Include(s => s.Enrollments).ThenInclude(e => e.Course).AsNoTracking().
                SingleOrDefaultAsync(m => m.ID == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            Student student = new Student();
            student.Enrollments = new List<Enrollment>();
            PopulateAssigedCoursesData(student);
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LastName,FirstMidName,EnrollmentDate")] Student student,
            string[] selectedCourses)
        {
            if (selectedCourses != null)
            {
                student.Enrollments = new List<Enrollment>();

                foreach (var c in selectedCourses)
                {
                    var courseToAdd = new Enrollment
                    {
                        CourseID = int.Parse(c),
                        StudentID = student.ID
                    };
                    student.Enrollments.Add(courseToAdd);
                }
            }

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. " + "Try again, and if the problem persists "
                    + "see your system admin.");
            }
            return View(student);

        }

        private void PopulateAssigedCoursesData(Student student)
        {
            var allCourses = _context.Courses;

            var studentCourses = new HashSet<int>(student.Enrollments.Select(c => c.CourseID));

            var viewModel = new List<AssignedCourseData>();

            foreach (var c in allCourses)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseID = c.CourseID,
                    Title = c.Title,
                    Assigned = studentCourses.Contains(c.CourseID)
                });
            }

            ViewData["Courses"] = viewModel;
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(e => e.Enrollments).ThenInclude(e => e.Course)
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);
            if (student == null)
            {
                return NotFound();
            }
            PopulateAssigedCoursesData(student);
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, string[] selectedCourses)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentToUpdate = await _context.Students
                .Include(e => e.Enrollments).ThenInclude(e => e.Course)
                .SingleOrDefaultAsync(s => s.ID == id);

            if (await TryUpdateModelAsync<Student>(studentToUpdate, "",
                s => s.FirstMidName, s => s.LastName, s => s.EnrollmentDate))
            {
                UpdateStudentCourses(selectedCourses, studentToUpdate);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. " + "Try again, and if the problem persists "
                    + "see your system admin.");
                }
                return RedirectToAction("Index");
            }
            UpdateStudentCourses(selectedCourses, studentToUpdate);
            PopulateAssigedCoursesData(studentToUpdate);
            return View(studentToUpdate);
        }

        private void UpdateStudentCourses(string[] selectedCourses, Student studentToUpdate)
        {
            if (selectedCourses == null)
            {
                studentToUpdate.Enrollments = new List<Enrollment>();
                return;
            }

            var selectedCoursesHS = new HashSet<string>(selectedCourses);
            var studentCourses = new HashSet<int>(studentToUpdate.Enrollments.Select(c => c.CourseID));

            foreach (var c in _context.Courses)
            {
                if (selectedCoursesHS.Contains(c.CourseID.ToString()))
                {
                    if (!studentCourses.Contains(c.CourseID))
                    {
                        studentToUpdate.Enrollments.Add(new Enrollment
                        {
                            StudentID = studentToUpdate.ID,
                            CourseID = c.CourseID
                        });
                    }
                }
                else
                {
                    if (studentCourses.Contains(c.CourseID))
                    {
                        Enrollment enrollmentToRemove = studentToUpdate.Enrollments.SingleOrDefault(e => e.CourseID == c.CourseID);
                        _context.Remove(enrollmentToRemove);
                    }
                }
            }
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.AsNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);

            if (student == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] = "Delete failed. Try again, and if the problem persists " +
                    "see your system administrator.";
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                .Include(e => e.Enrollments)
                .SingleOrDefaultAsync(m => m.ID == id);

            if (student == null)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
            }
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.ID == id);
        }
    }
}
