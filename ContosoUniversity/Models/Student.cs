﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Models
{
    public class Student
    {
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last name")]
        [RegularExpression(@"^[A-Z]+[a-zA-Z""'\s-]*$")]
        public string LastName { get; set; }

        [Column("FirstName")]
        [Required]
        [Display(Name = "First name")]
        [StringLength(50, ErrorMessage = "First name cannot be more than 50 characters.")]
        public string FirstMidName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Enrollment date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime EnrollmentDate { get; set; }

        [Display(Name = "Full name")]
        public string FullName
        {
            get
            {
                return LastName + ", " + FirstMidName;
            }
        }

        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
