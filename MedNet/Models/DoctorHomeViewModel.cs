﻿using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class DoctorHomeViewModel
    {
        [Required(ErrorMessage = "PHN is required.")]
        public string PHN { get; set; }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> jacob-test
