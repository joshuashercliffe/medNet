using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MedNet.Models;
using MedNet.Data.Models.Models;
using System;
using MedNet.Data.Services;
using MedNet.Data.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace MedNet.Controllers
{
    public class HomeController : Controller
    {
        private static string _publicKeyString = "302a300506032b657003210033c43dc2180936a2a9138a05f06c892d2fb1cfda4562cbc35373bf13cd8ed373";
        private static string _privateKeyString = "302e020100300506032b6570042204206f6b0cd095f1e83fc5f08bffb79c7c8a30e77a3ab65f4bc659026b76394fcea8";
        private readonly ILogger<HomeController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _bigChainDbService = new BigChainDbService("https://anode.lifeblocks.site");
            _random = new Random();
        }

        public IActionResult Index()
        {
            ViewBag.Title = "test";
            return View();
        }

        public IActionResult PatientOverview()
        {
            var doctorNotesJson = _bigChainDbService.GetAllDoctorNotesFromPublicKey(_publicKeyString);
            var prescriptionsJson = _bigChainDbService.GetAlPrescriptionsFromPublicKey(_publicKeyString);
            var doctorNotes = new List<DoctorNote>();
            var prescriptions = new List<Prescription>();
            foreach (var doctorNote in doctorNotesJson)
            {
                doctorNotes.Add(JsonConvert.DeserializeObject<DoctorNote>(doctorNote));
            }
            foreach(var prescription in prescriptionsJson)
            {
                prescriptions.Add(JsonConvert.DeserializeObject<Prescription>(prescription));
            }
            var patientOverviewViewModel = new PatientOverviewViewModel
            {
                DoctorNotes = doctorNotes.OrderByDescending(d => d.DateOfRecord).ToList(),
                Prescriptions = prescriptions.OrderByDescending(p => p.PrescribingDate).ToList()
            };

            return View(patientOverviewViewModel);
        }

        public IActionResult AddNewPatientRecord()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddNewPatientRecord(AddNewPatientRecordViewModel addNewPatientRecordViewModel)
        {
            
             if (!string.IsNullOrEmpty(addNewPatientRecordViewModel.DoctorsNote.PurposeOfVisit))
            {
                var noteViewModel = addNewPatientRecordViewModel.DoctorsNote;
                var doctorNote = new DoctorNote
                {
                    PurposeOfVisit = noteViewModel.PurposeOfVisit,
                    Description = noteViewModel.Description,
                    FinalDiagnosis = noteViewModel.FinalDiagnosis,
                    FurtherInstructions = noteViewModel.FurtherInstructions,
                    DateOfRecord = DateTime.Now
                };

                var asset = new Asset
                {
                    Data = JsonConvert.SerializeObject(doctorNote),
                    RandomId = _random.Next(0, 100000),
                    Type = AssetType.DoctorNote
                };

                var metadata = new MetaDataSaved();
               metadata.AccessList[_publicKeyString] = "AES symmetric Key for encrypted data stored encrypted by asymmetric public key of user";

                _bigChainDbService.SendTransactionToDataBase(asset, metadata, _publicKeyString, _privateKeyString);

                return View();
            }

            if (!string.IsNullOrEmpty(addNewPatientRecordViewModel.Prescription.DrugNameStrength))
            {
                var prescriptionViewModel = addNewPatientRecordViewModel.Prescription;
                var prescription = new Prescription
                {
                    PrescribingDate = prescriptionViewModel.PrescribingDate,
                    DrugNameStrength = prescriptionViewModel.DrugNameStrength,
                    Dosage = prescriptionViewModel.Dosage,
                    StartDate = prescriptionViewModel.StartDate,
                    EndDate = prescriptionViewModel.EndDate,
                    DirectionForUse = prescriptionViewModel.DirectionForUse
                };

                var asset = new Asset
                {
                    Data = JsonConvert.SerializeObject(prescription),
                    RandomId = _random.Next(0, 100000),
                    Type = AssetType.Prescription
                };

                var metadata = new MetaDataSaved();
                metadata.AccessList[_publicKeyString] = "AES symmetric Key for encrypted data stored encrypted by asymmetric public key of user";

                _bigChainDbService.SendTransactionToDataBase(asset, metadata, _publicKeyString, _privateKeyString);

                return View();
            }

            return View();
        }

        public IActionResult DoctorSignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DoctorSignUp(doctorSignUpViewModel doctorSignUpViewModel)
        {
            var asd = doctorSignUpViewModel;
            return View("Index");
        }

        public IActionResult DoctorForgotPassword()
        {
            return View();
        }

        public IActionResult PatientLookUp()
        {
            return View();
        }
        public IActionResult RecentPatients()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View();
        }

        public IActionResult Logout()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
