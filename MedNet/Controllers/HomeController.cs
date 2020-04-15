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
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace MedNet.Controllers
{
    public class HomeController : Controller
    {
        private const string currentDSPK = "currentDoctorSignPrivateKey";
        private const string currentDAPK = "currentDoctorAgreePrivateKey";
        private const string currentPSPK = "currentPatientSignPrivateKey";
        private const string currentPAPK = "currentPatientAgreePrivateKey";
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
                HttpContext.Session.SetString(currentDSPK, signPrivateKey);
                HttpContext.Session.SetString(currentDAPK, agreePrivateKey);
                return RedirectToAction("patientLookUp");
            }
            else
            {
                ModelState.AddModelError("","Password or Keyword incorrect.");
                return View(indexViewModel);
            }
        }

        public IActionResult PatientOverview()
        {
            if (HttpContext.Session.GetString(currentDSPK) == null || HttpContext.Session.GetString(currentDAPK) == null)
                return RedirectToAction("Index");
            else if (HttpContext.Session.GetString(currentPSPK) == null || HttpContext.Session.GetString(currentPAPK) == null)
                return RedirectToAction("patientLookUp");
            else
            {
                var doctorSignPrivateKey = HttpContext.Session.GetString(currentDSPK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(currentDAPK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var patientSignPrivateKey = HttpContext.Session.GetString(currentPSPK);
                var patientSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(patientSignPrivateKey);

                var doctorNotesList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string>
                    (AssetType.DoctorNote,doctorSignPublicKey,patientSignPublicKey);
                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string>
                    (AssetType.Prescription, doctorSignPublicKey, patientSignPublicKey);
                var doctorNotes = new List<DoctorNote>();
                var prescriptions = new List<Prescription>();
                foreach (var doctorNote in doctorNotesList)
                {
                    var hashedKey = doctorNote.metadata.data[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(doctorNote.data.Data,dataDecryptionKey);
                    doctorNotes.Add(JsonConvert.DeserializeObject<DoctorNote>(data));
                }
                foreach (var prescription in prescriptionsList)
                {
                    var hashedKey = prescription.metadata.data[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                    prescriptions.Add(JsonConvert.DeserializeObject<Prescription>(data));
                }
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    DoctorNotes = doctorNotes.OrderByDescending(d => d.DateOfRecord).ToList(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }

        public IActionResult AddNewPatientRecord()
        {
            if (HttpContext.Session.GetString(currentDSPK) == null || HttpContext.Session.GetString(currentDAPK) == null)
                return RedirectToAction("Index");
            else if (HttpContext.Session.GetString(currentPSPK) == null || HttpContext.Session.GetString(currentPAPK) == null)
                return RedirectToAction("patientLookUp");
            else
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
                string encryptionKey;
                var encryptedData = EncryptionService.getEncryptedAssetData(JsonConvert.SerializeObject(doctorNote), out encryptionKey);

                var asset = new AssetSaved<string>
                {
                    Data = encryptedData,
                    RandomId = _random.Next(0, 100000),
                    Type = AssetType.DoctorNote
                };

                var metadata = new MetaDataSaved<Dictionary<string,string>>();
                metadata.data = new Dictionary<string, string>();

                //store the data encryption key in metadata encrypted with sender and reciever agree key
                var doctorSignPrivateKey = HttpContext.Session.GetString(currentDSPK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(currentDAPK);
                var patientAgreePrivateKey = HttpContext.Session.GetString(currentPAPK);
                var patientSignPrivateKey = HttpContext.Session.GetString(currentPSPK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var patientSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(patientSignPrivateKey);
                var doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivateKey);
                var patientAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(patientAgreePrivateKey);
                metadata.data[doctorSignPublicKey] = 
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey,doctorAgreePrivateKey,doctorAgreePublicKey);
                metadata.data[patientSignPublicKey] =
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, patientAgreePublicKey);
                
                _bigChainDbService.SendCreateTransferTransactionToDataBase<string,Dictionary<string,string>>(asset, metadata, doctorSignPrivateKey, patientSignPublicKey);
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

                string encryptionKey;
                var encryptedData = EncryptionService.getEncryptedAssetData(JsonConvert.SerializeObject(prescription), out encryptionKey);

                var asset = new AssetSaved<string>
                {
                    Data = encryptedData,
                    RandomId = _random.Next(0, 100000),
                    Type = AssetType.Prescription
                };

                var metadata = new MetaDataSaved<Dictionary<string, string>>();
                metadata.data = new Dictionary<string, string>();

                //store the data encryption key in metadata encrypted with sender and reciever agree key
                var doctorSignPrivateKey = HttpContext.Session.GetString(currentDSPK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(currentDAPK);
                var patientAgreePrivateKey = HttpContext.Session.GetString(currentPAPK);
                var patientSignPrivateKey = HttpContext.Session.GetString(currentPSPK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var patientSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(patientSignPrivateKey);
                var doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivateKey);
                var patientAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(patientAgreePrivateKey);
                metadata.data[doctorSignPublicKey] =
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, doctorAgreePublicKey);
                metadata.data[patientSignPublicKey] =
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, patientAgreePublicKey);

                _bigChainDbService.SendCreateTransferTransactionToDataBase<string, Dictionary<string, string>>(asset, metadata, doctorSignPrivateKey, patientSignPublicKey);
            }

            return RedirectToAction("PatientOverview");
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

            _bigChainDbService.SendCreateTransactionToDataBase(asset, metadata, signPrivateKey);
            return RedirectToAction("Index");
        }

        public IActionResult DoctorForgotPassword()
        {
            return View();
        }

        public IActionResult PatientLookUp()
        {
            if (HttpContext.Session.GetString(currentDSPK) == null 
                || HttpContext.Session.GetString(currentDAPK) == null)
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
            HttpContext.Session.SetString(currentPSPK, signPrivateKey);
            HttpContext.Session.SetString(currentPAPK, agreePrivateKey);
            return RedirectToAction("PatientOverview");
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
            HttpContext.Session.Clear();
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

            _bigChainDbService.SendCreateTransactionToDataBase(asset, metadata, signPrivateKey);
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
