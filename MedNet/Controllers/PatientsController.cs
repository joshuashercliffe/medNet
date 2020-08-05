using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MedNet.Models;
using MedNet.Data.Models.Models;
using System;
using MedNet.Data.Services;
using MedNet.Data.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.AspNetCore.Authentication;

namespace MedNet.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PatientsController : Controller
    {
        private readonly ILogger<PatientsController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;
        public IActionResult Index()
        {
            return View();
        }

        public PatientsController(ILogger<PatientsController> logger)
        {
            _logger = logger;
            string[] nodes = Globals.nodes;
            _bigChainDbService = new BigChainDbService(nodes);
            _random = new Random();
        }

        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(PatientLoginViewModel indexViewModel)
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(indexViewModel);
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID( indexViewModel.PatientPHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not fin-d a matching user");
                return View(indexViewModel);
            }
            var hashedKeys = userAsset.data.Data.PrivateKeys;
            try
            {
                EncryptionService.getPrivateKeyFromIDKeyword(indexViewModel.PatientPHN, indexViewModel.PatientKeyword, hashedKeys, out signPrivateKey, out agreePrivateKey);
            }
            catch
            {
                ModelState.AddModelError("", "Keyword may be incorrect");
                return View(indexViewModel);
            }
            PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, userAsset.data.Data.SignPublicKey);
            var password = indexViewModel.password;
            if (EncryptionService.verifyPassword(password, userMetadata.hashedPassword))
            {
                HttpContext.Session.SetString(Globals.currentPSPriK, signPrivateKey);
                HttpContext.Session.SetString(Globals.currentPAPriK, agreePrivateKey);
                HttpContext.Session.SetString(Globals.currentPSPubK, userAsset.data.Data.SignPublicKey);
                HttpContext.Session.SetString(Globals.currentPAPubK, userAsset.data.Data.AgreePublicKey);
                HttpContext.Session.SetString(Globals.currentUserName, $"{userMetadata.FirstName} {userMetadata.LastName}");
                HttpContext.Session.SetString(Globals.currentUserID, userAsset.data.Data.ID);
                return RedirectToAction("PatientOverview");
            }
            else
            {
                ModelState.AddModelError("", "Password or Keyword incorrect.");
                return View(indexViewModel);
            }
        }

        public IActionResult PatientOverview()
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("Login");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));

                var patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
                //var patientAgreePrivateKey = HttpContext.Session.GetString(Globals.currentPAPriK);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                /*var doctorNotesList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                    (AssetType.DoctorNote, patientSignPublicKey);
                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                    (AssetType.Prescription, patientSignPublicKey);
                var doctorNotes = new List<DoctorNote>();
                var prescriptions = new List<Prescription>();
                foreach (var doctorNote in doctorNotesList)
                {
                    var hashedKey = doctorNote.metadata.data[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(doctorNote.data.Data, dataDecryptionKey);
                    doctorNotes.Add(JsonConvert.DeserializeObject<DoctorNote>(data));
                }
                foreach (var prescription in prescriptionsList)
                {
                    var hashedKey = prescription.metadata.data[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                    prescriptions.Add(JsonConvert.DeserializeObject<Prescription>(data));
                }*/
                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge()
                    //DoctorNotes = doctorNotes.OrderByDescending(d => d.DateOfRecord).ToList(),
                    //Prescriptions = prescriptions.OrderByDescending(p => p.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }
        public IActionResult EditProfile()
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("Login");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));

                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                //Description: page where patient can edit their basic personal information
                return View(userMetadata);
            }
        }

        [HttpPost]
        public IActionResult EditProfile(PatientCredMetadata patientCredMetadata) 
        {
            
            Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));
            var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            var patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
            var transaction = _bigChainDbService.GetMetadataIDFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);
            var transID = transaction.Id ?? userAsset.id;
            patientCredMetadata.hashedPassword = transaction.Metadata.data.hashedPassword;
            var newMetadata = new MetaDataSaved<PatientCredMetadata> { 
                data = patientCredMetadata
            };
            _bigChainDbService.SendTransferTransactionToDataBase(userAsset.id, newMetadata,
    patientSignPrivateKey, patientSignPublicKey, transID);
            return RedirectToAction("PatientOverview");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View();
        }

        public IActionResult PreviousAppointments()
        {
            return View();
        }

        public IActionResult PrescriptionList()
        {
            return View();
        }

        public IActionResult TestResultList()
        {
            return View();
        }

        public IActionResult PatientRecords()
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientOverview");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));

                var patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
                var patientAgreePrivateKey = HttpContext.Session.GetString(Globals.currentPAPriK);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                var doctorNotesList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string,double>
                    (AssetType.DoctorNote, patientSignPublicKey);
                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string,PrescriptionMetadata>
                    (AssetType.Prescription, patientSignPublicKey);
                var doctorNotes = new List<DoctorNote>();
                var prescriptions = new List<PrescriptionFullData>();
                foreach (var doctorNote in doctorNotesList)
                {
                    var hashedKey = doctorNote.metadata.AccessList[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(doctorNote.data.Data, dataDecryptionKey);
                    doctorNotes.Add(JsonConvert.DeserializeObject<DoctorNote>(data));
                }
                foreach (var prescription in prescriptionsList)
                {
                    var hashedKey = prescription.metadata.AccessList[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                    var newEntry = new PrescriptionFullData
                    {
                        assetData = JsonConvert.DeserializeObject<Prescription>(data),
                        Metadata = prescription.metadata.data
                    };
                    prescriptions.Add(newEntry);
                }
                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    DoctorNotes = doctorNotes.OrderByDescending(d => d.DateOfRecord).ToList(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.assetData.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }
    }
}
