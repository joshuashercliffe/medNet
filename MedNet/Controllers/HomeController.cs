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

        [HttpPost]
        public IActionResult Index(IndexViewModel indexViewModel)
        {
            if (!ModelState.IsValid)
                return View(indexViewModel);
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Doctor, indexViewModel.DoctorMINC);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not find a matching user");
                return View(indexViewModel);
            }
            var hashedKeys = userAsset.data.Data.PrivateKeys;
            try
            {
                EncryptionService.getPrivateKeyFromIDKeyword(indexViewModel.DoctorMINC, indexViewModel.DoctorKeyword, hashedKeys, out signPrivateKey, out agreePrivateKey);
            }
            catch 
            {
                ModelState.AddModelError("", "Keyword may be inccorrect");
                return View(indexViewModel);
            }
            UserCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<UserCredMetadata>(userAsset.id, EncryptionService.getSignPublicKeyStringFromPrivate(signPrivateKey));
            var password = indexViewModel.password;
            if (EncryptionService.verifyPassword(password, userMetadata.hashedPassword))
            {
                ViewBag.currentDoctorSignPrivateKey = signPrivateKey;
                ViewBag.currentDoctorAgreePrivateKey = agreePrivateKey;
                return View("patientLookUp");
            }
            else
            {
                ModelState.AddModelError("","Password or Keyword incorrect.");
                return View(indexViewModel);
            }
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
            foreach (var prescription in prescriptionsJson)
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

                var asset = new AssetSaved<string>
                {
                    Data = JsonConvert.SerializeObject(doctorNote),
                    RandomId = _random.Next(0, 100000),
                    Type = AssetType.DoctorNote
                };

                var metadata = new MetaDataSaved<Dictionary<string,string>>();
                metadata.data = new Dictionary<string, string>();
                metadata.data[_publicKeyString] = "AES symmetric Key for encrypted data stored encrypted by asymmetric public key of user";

                _bigChainDbService.SendTransactionToDataBase<string,Dictionary<string,string>>(asset, metadata, _publicKeyString);

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

                var asset = new AssetSaved<string>
                {
                    Data = JsonConvert.SerializeObject(prescription),
                    RandomId = _random.Next(0, 100000),
                    Type = AssetType.Prescription
                };

                var metadata = new MetaDataSaved<Dictionary<string,string>>();
                metadata.data = new Dictionary<string, string>();
                metadata.data[_publicKeyString] = "AES symmetric Key for encrypted data stored encrypted by asymmetric public key of user";

                _bigChainDbService.SendTransactionToDataBase<string,Dictionary<string,string>>(asset, metadata, _publicKeyString);

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
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Doctor, doctorSignUpViewModel.DoctorMINC);
            if (userAsset != null)
            {
                ModelState.AddModelError("", "A Doctor profile with that MINC already exists");
                return View(doctorSignUpViewModel);
            }
            var passphrase = doctorSignUpViewModel.DoctorKeyWord;
            var password = doctorSignUpViewModel.Password;
            EncryptionService.getNewBlockchainUser(out signPrivateKey, out _, out agreePrivateKey, out _);
            var userAssetData = new UserCredAssetData
            {
                FirstName = doctorSignUpViewModel.FirstName,
                LastName = doctorSignUpViewModel.LastName,
                ID = doctorSignUpViewModel.DoctorMINC,
                Email = doctorSignUpViewModel.Email,
                PrivateKeys = EncryptionService.encryptPrivateKeys(doctorSignUpViewModel.DoctorMINC, passphrase, signPrivateKey, agreePrivateKey),
                DateOfRecord = DateTime.Now
            };
            var userMetadata = new UserCredMetadata
            {
                hashedPassword = EncryptionService.hashPassword(password)
            };
            var asset = new AssetSaved<UserCredAssetData>
            {
                Type = AssetType.Doctor,
                Data = userAssetData,
                RandomId = _random.Next(0, 100000)
            };
            var metadata = new MetaDataSaved<UserCredMetadata>
            {
                data = userMetadata
            };

            _bigChainDbService.SendTransactionToDataBase(asset, metadata, signPrivateKey);
            return RedirectToAction("Index");
        }

        public IActionResult DoctorForgotPassword()
        {
            return View();
        }

        public IActionResult PatientLookUp()
        {
            if (ViewBag.currentDoctorSignPrivateKey == null || ViewBag.currentDoctorAgreePrivateKey == null)
                return RedirectToAction("Index");
            else
                return View();
        }

        [HttpPost]
        public IActionResult PatientLookUp(PatientLookupViewModel patientLookupViewModel)
        {
            if (!ModelState.IsValid)
                return View(patientLookupViewModel);
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, patientLookupViewModel.PHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not find a matching user");
                return View(patientLookupViewModel);
            }
            var hashedKeys = userAsset.data.Data.PrivateKeys;
            try
            {
                EncryptionService.getPrivateKeyFromIDKeyword(patientLookupViewModel.PHN, patientLookupViewModel.Keyword, hashedKeys, out signPrivateKey, out agreePrivateKey);
            }
            catch
            {
                ModelState.AddModelError("", "Keyword may be inccorrect");
                return View(patientLookupViewModel);
            }
            ViewBag.currentPatientSignPrivateKey = signPrivateKey;
            ViewBag.currentPatientAgreePrivateKey = agreePrivateKey;
            return View("PatientOverview");
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
            ViewBag.currentDoctorSignPrivateKey = null;
            ViewBag.currentDoctorAgreePrivateKey = null;
            return View();
        }

        public IActionResult PatientSignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult PatientSignUp(PatientSignUpViewModel patientSignUpViewModel)
        {
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, patientSignUpViewModel.PHN);
            if (userAsset != null)
            {
                ModelState.AddModelError("", "A Patient profile with that MINC already exists");
                return View(patientSignUpViewModel);
            }
            var passphrase = patientSignUpViewModel.KeyWord;
            var password = patientSignUpViewModel.Password;
            EncryptionService.getNewBlockchainUser(out signPrivateKey, out _, out agreePrivateKey, out _);
            var userAssetData = new UserCredAssetData
            {
                FirstName = patientSignUpViewModel.FirstName,
                LastName = patientSignUpViewModel.LastName,
                ID = patientSignUpViewModel.PHN,
                Email = patientSignUpViewModel.Email,
                PrivateKeys = EncryptionService.encryptPrivateKeys(patientSignUpViewModel.PHN, passphrase, signPrivateKey, agreePrivateKey),
                DateOfRecord = DateTime.Now
            };
            var userMetadata = new UserCredMetadata
            {
                hashedPassword = EncryptionService.hashPassword(password)
            };
            var asset = new AssetSaved<UserCredAssetData>
            {
                Type = AssetType.Patient,
                Data = userAssetData,
                RandomId = _random.Next(0, 100000)
            };
            var metadata = new MetaDataSaved<UserCredMetadata>
            {
                data = userMetadata
            };

            _bigChainDbService.SendTransactionToDataBase(asset, metadata, signPrivateKey);
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
