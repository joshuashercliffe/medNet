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
using System.Drawing;
using Org.BouncyCastle.Asn1;
using System.Text;
using MongoDB.Driver;
using System.Net.Sockets;

namespace MedNet.Controllers
{
    public class HomeController : Controller
    {
        private const string currentDSPriK = "currentDoctorSignPrivateKey";
        private const string currentDAPriK = "currentDoctorAgreePrivateKey";
        private const string currentPSPriK = "currentPatientSignPrivateKey";
        private const string currentPAPriK = "currentPatientAgreePrivateKey";
        private const string currentPSPubK = "currentPatientSignPrivateKey";
        private const string currentPAPubK = "currentPatientAgreePrivateKey";
        private const string currentPPHN = "currentPatientPHN";
        private const string currentDoctorName = "currentDoctorName";
        private const string currentDoctorMinc = "currentDoctorMinsc";

        private readonly ILogger<HomeController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            string[] nodes = { "anode.lifeblocks.site", "bnode.lifeblocks.site", 
                "cnode.lifeblocks.site", "dnode.lifeblocks.site", "enode.lifeblocks.site" };
            _bigChainDbService = new BigChainDbService(nodes);
            _random = new Random();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(IndexViewModel indexViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
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
                ModelState.AddModelError("", "Keyword may be incorrect");
                return View(indexViewModel);
            }
            UserCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<UserCredMetadata>(userAsset.id, EncryptionService.getSignPublicKeyStringFromPrivate(signPrivateKey));
            var password = indexViewModel.password;
            if (EncryptionService.verifyPassword(password, userMetadata.hashedPassword))
            {
                HttpContext.Session.SetString(currentDSPriK, signPrivateKey);
                HttpContext.Session.SetString(currentDAPriK, agreePrivateKey);
                HttpContext.Session.SetString(currentDoctorName, $"{userAsset.data.Data.FirstName} {userAsset.data.Data.LastName}");
                HttpContext.Session.SetString(currentDoctorMinc, userAsset.data.Data.ID);
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
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            if (HttpContext.Session.GetString(currentDSPriK) == null || HttpContext.Session.GetString(currentDAPriK) == null)
                return RedirectToAction("Index");
            else if (HttpContext.Session.GetString(currentPSPubK) == null || HttpContext.Session.GetString(currentPAPubK) == null)
                return RedirectToAction("patientLookUp");
            else
            {
                Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, HttpContext.Session.GetString(currentPPHN));

                var doctorSignPrivateKey = HttpContext.Session.GetString(currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(currentDAPriK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var patientSignPublicKey = HttpContext.Session.GetString(currentPSPubK);

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
                var patientInfo = userAsset.data.Data;
                var patientAge = DateTime.Now.Year - patientInfo.DateOfBirth.Year;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientName = $"{patientInfo.FirstName} {patientInfo.LastName}",
                    PatientPHN = patientInfo.ID,
                    PatientDOB = patientInfo.DateOfBirth,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    DoctorNotes = doctorNotes.OrderByDescending(d => d.DateOfRecord).ToList(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }

        public IActionResult AddNewPatientRecord()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            if (HttpContext.Session.GetString(currentDSPriK) == null || HttpContext.Session.GetString(currentDAPriK) == null)
                return RedirectToAction("Index");
            else if (HttpContext.Session.GetString(currentPSPubK) == null || HttpContext.Session.GetString(currentPAPubK) == null)
                return RedirectToAction("patientLookUp");
            else
                return View();
        }

        [HttpPost]
        public IActionResult AddNewPatientRecord(AddNewPatientRecordViewModel addNewPatientRecordViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            if (!string.IsNullOrEmpty(addNewPatientRecordViewModel.DoctorsNote.PurposeOfVisit))
            {
                var noteViewModel = addNewPatientRecordViewModel.DoctorsNote;
                var doctorNote = new DoctorNote
                {
                    PurposeOfVisit = noteViewModel.PurposeOfVisit,
                    Description = noteViewModel.Description,
                    FinalDiagnosis = noteViewModel.FinalDiagnosis,
                    FurtherInstructions = noteViewModel.FurtherInstructions,
                    DoctorName = HttpContext.Session.GetString(currentDoctorName),
                    DoctorMinsc = HttpContext.Session.GetString(currentDoctorMinc),
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
                var doctorSignPrivateKey = HttpContext.Session.GetString(currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(currentDAPriK);
                var patientAgreePublicKey = HttpContext.Session.GetString(currentPAPubK);
                var patientSignPublicKey = HttpContext.Session.GetString(currentPSPubK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivateKey);
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
                    DoctorName = HttpContext.Session.GetString(currentDoctorName),
                    DoctorMinsc = HttpContext.Session.GetString(currentDoctorMinc),
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
                var doctorSignPrivateKey = HttpContext.Session.GetString(currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(currentDAPriK);
                var patientAgreePublicKey = HttpContext.Session.GetString(currentPAPubK);
                var patientSignPublicKey = HttpContext.Session.GetString(currentPSPubK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivateKey);
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
            string signPrivateKey = null, agreePrivateKey = null, signPublicKey =null, agreePublicKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Doctor, doctorSignUpViewModel.DoctorMINC);
            if (userAsset != null)
            {
                ModelState.AddModelError("", "A Doctor profile with that MINC already exists");
                return View(doctorSignUpViewModel);
            }
            var passphrase = doctorSignUpViewModel.DoctorKeyWord;
            var password = doctorSignUpViewModel.Password;
            EncryptionService.getNewBlockchainUser(out signPrivateKey, out signPublicKey, out agreePrivateKey, out agreePublicKey);
            var userAssetData = new UserCredAssetData
            {
                FirstName = doctorSignUpViewModel.FirstName,
                LastName = doctorSignUpViewModel.LastName,
                ID = doctorSignUpViewModel.DoctorMINC,
                Email = doctorSignUpViewModel.Email,
                PrivateKeys = EncryptionService.encryptPrivateKeys(doctorSignUpViewModel.DoctorMINC, passphrase, signPrivateKey, agreePrivateKey),
                DateOfRecord = DateTime.Now,
                SignPublicKey = signPublicKey,
                AgreePublicKey = agreePublicKey,
                FingerprintData = new List<string>(),
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
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            if (HttpContext.Session.GetString(currentDSPriK) == null 
                || HttpContext.Session.GetString(currentDAPriK) == null)
                return RedirectToAction("Index");
            else
                return View();
        }

        [HttpPost]
        public IActionResult PatientLookUp(PatientLookupViewModel patientLookupViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            if (!ModelState.IsValid)
                return View(patientLookupViewModel);
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, patientLookupViewModel.PHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not find a matching user");
                return View(patientLookupViewModel);
            }
            HttpContext.Session.SetString(currentPSPubK, userAsset.data.Data.SignPublicKey);
            HttpContext.Session.SetString(currentPAPubK, userAsset.data.Data.AgreePublicKey);
            HttpContext.Session.SetString(currentPPHN, userAsset.data.Data.ID);
            return RedirectToAction("PatientOverview");
        }

        public IActionResult Feedback()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
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

        public IActionResult RequestAccess()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            if (HttpContext.Session.GetString(currentDSPriK) == null || HttpContext.Session.GetString(currentDAPriK) == null)
                return RedirectToAction("Index");
            else if (HttpContext.Session.GetString(currentPSPubK) == null || HttpContext.Session.GetString(currentPAPubK) == null)
                return RedirectToAction("patientLookUp");
            else
                return View();
        }

        [HttpPost]
        public IActionResult RequestAccess(RequestAccessViewModel requestAccessViewModel)
        {
            // Description: Authenticates a patient's identity when a Doctor requests access to their medical information
            // Get's the Doctor's information for current session
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            if (!ModelState.IsValid)
                return View(requestAccessViewModel);
            string PHN = HttpContext.Session.GetString(currentPPHN);
            string patientSignPublicKey = HttpContext.Session.GetString(currentPSPubK);
            string doctorSignPrivatekey = HttpContext.Session.GetString(currentDSPriK);
            string doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivatekey);
            string doctorAgreePrivatekey = HttpContext.Session.GetString(currentDAPriK);
            string doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivatekey);
            string keyword = requestAccessViewModel.keyword;

            // Searches for a patient with the specified PHN
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, PHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "Could not find a patient profile with PHN: " + PHN);
                return View(requestAccessViewModel);
            }

            // Send request to the Client Computer to authenticate with fingerprint
            List<Image> fpList = FingerprintService.authenticateFP("24.84.225.22", 1); // JW
            Image fpScanned = fpList[0];
            //byte[] fpData = FingerprintService.bmpToByte(fpBmp);

            // Check if fingerprint data is valid
            if (fpList.Count == 0)
            {
                ModelState.AddModelError("", "Something went wrong with the fingerprint scan, try again.");
                return View(requestAccessViewModel);
            }

            // Decrypt the patient's fingerprint data stored in the Blockchain
            byte[] dbFpData = null;
            string patientSignPrivateKey, patientAgreePrivateKey;
            List<Image> fpdbList = new List<Image>();
            List<string> dbList = userAsset.data.Data.FingerprintData;
            //string dbList = userAsset.data.Data.FingerprintData;
            try
            {
                foreach (string db in dbList)
                {
                    EncryptionService.decryptFingerprintData(PHN, keyword, db, out dbFpData);
                    fpdbList.Add(FingerprintService.byteToImg(dbFpData));
                }
                EncryptionService.getPrivateKeyFromIDKeyword(PHN,keyword, userAsset.data.Data.PrivateKeys, out patientSignPrivateKey, out patientAgreePrivateKey);
            }
            catch
            {
                ModelState.AddModelError("", "Keyword may be incorrect");
                return View(requestAccessViewModel);
            }

            // Compare the scanned fingerprint with the one saved in the database
            // debug: errors happen here during published version
            if (!FingerprintService.compareFP(fpScanned, fpdbList))
            {
                ModelState.AddModelError("", "The fingerprint did not match, try again.");
                return View(requestAccessViewModel);
            }

            // Choose the types of records we want to get
            AssetType[] typeList = { AssetType.DoctorNote, AssetType.Prescription };
            var recordList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                (typeList, patientSignPublicKey);
            foreach (var record in recordList)
            {
                MetaDataSaved<Dictionary<string, string>> metadata = record.metadata;
                if (!metadata.data.Keys.Contains(doctorSignPublicKey))
                {
                    var hashedKey = metadata.data[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var newHash = EncryptionService.getEncryptedEncryptionKey(dataDecryptionKey, patientAgreePrivateKey, doctorAgreePublicKey);
                    metadata.data[doctorSignPublicKey] = newHash;
                    _bigChainDbService.SendTransferTransactionToDataBase(record.id, metadata,
                        patientSignPrivateKey, patientSignPublicKey, record.transID);
                }
            }
            return RedirectToAction("PatientOverview");
        }

        public IActionResult TestFingerprintButton(TestFingerprintButton model)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            return View(model);
        }

        public IActionResult TriggerFingerprint()
        {
            // DEBUG:JW
            // Description: Using for DEBUG. URL: https://lifeblocks.site/home/testfingerprintbutton 
            ViewBag.DoctorName = HttpContext.Session.GetString(currentDoctorName);
            
            // Retrieve the Public IP of the Client Computer using the browser
            var ip = HttpContext.Connection.RemoteIpAddress;
            string ipAddress = ip.ToString();
            bool debug = true; // true for DEBUG
            string status = "entered function";
            TcpClient tcpClient = new TcpClient();

            List<Image> fpList = FingerprintService.authenticateFP("24.84.225.22", 3);

            // Do fingerprint fetch from windows service here
            //List<Image> fpList = FingerprintService.scanMultiFP(ipAddress, 3, out _);
            Image fpImg = null;
            for (int i = 0; i < fpList.Count; i++)
            {
                var debugByte = FingerprintService.imgToByte(fpList[i]);
                fpImg = FingerprintService.byteToImg(debugByte);
                fpImg.Save(i.ToString() + ".bmp");
            }

            //bool compare = FingerprintService.compareFP(fpImg, fpList);
            // Write the Public IP of the client computer on the window
            var model = new TestFingerprintButton()
            {
                //message = "The Public IP address of the client is: " + ipAddress
                message = status
            };

            return RedirectToAction("TestFingerprintButton", model);
        }

        [HttpPost]
        public IActionResult PatientSignUp(PatientSignUpViewModel patientSignUpViewModel)
        {
            // Description: Registers a patient up for a MedNet account
            string signPrivateKey = null, agreePrivateKey = null, signPublicKey = null, agreePublicKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, patientSignUpViewModel.PHN);
            
            // Check if PHN is already in use
            if (userAsset != null)
            {
                ModelState.AddModelError("", "A Patient profile with that PHN already exists");
                return View(patientSignUpViewModel);
            }

            // Register fingerprint information 
            List<Image> fpList = FingerprintService.authenticateFP("24.84.225.22", 5);

            if (fpList.Count == 0)
            {
                ModelState.AddModelError("", "Something went wrong with the fingerprint scan, try again.");
                return View(patientSignUpViewModel);
            }
            
            // Parse the input data for user registration 
            var passphrase = patientSignUpViewModel.KeyWord;
            var password = patientSignUpViewModel.Password;

            // Encrypt fingerprint data
            List<string> encrList = new List<string>();
            foreach(Image fp in fpList)
            {
                byte[] fpByte = FingerprintService.imgToByte(fp);
                string encrStr = EncryptionService.encryptFingerprintData(patientSignUpViewModel.PHN, passphrase, fpByte);
                encrList.Add(encrStr);
            }
            //string encrFpData = EncryptionService.encryptFingerprintData(patientSignUpViewModel.PHN, passphrase, fpdb[0]);

            // Create a user for the Blockchain 
            EncryptionService.getNewBlockchainUser(out signPrivateKey, out signPublicKey, out agreePrivateKey, out agreePublicKey);

            // Create the user Asset 
            var userAssetData = new UserCredAssetData
            {
                FirstName = patientSignUpViewModel.FirstName,
                LastName = patientSignUpViewModel.LastName,
                ID = patientSignUpViewModel.PHN,
                Email = patientSignUpViewModel.Email,
                DateOfBirth = patientSignUpViewModel.DateOfBirth,
                PrivateKeys = EncryptionService.encryptPrivateKeys(patientSignUpViewModel.PHN, passphrase, signPrivateKey, agreePrivateKey),
                DateOfRecord = DateTime.Now,
                SignPublicKey = signPublicKey,
                AgreePublicKey = agreePublicKey,
                FingerprintData = encrList,
            };
            
            // Encrypt the user's password in the metadata
            var userMetadata = new UserCredMetadata
            {
                hashedPassword = EncryptionService.hashPassword(password)
            };
            
            // Save the user Asset and Metadata
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
           
            // Send the user's information to the Blockchain database
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
