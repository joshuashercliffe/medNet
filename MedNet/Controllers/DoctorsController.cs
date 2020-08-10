using MedNet.Data.Models;
using MedNet.Data.Models.Models;
using MedNet.Data.Services;
using MedNet.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MedNet.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class DoctorsController : Controller
    {
        private readonly ILogger<DoctorsController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;

        public DoctorsController(ILogger<DoctorsController> logger)
        {
            _logger = logger;
            string[] nodes = Globals.nodes;
            _bigChainDbService = new BigChainDbService(nodes);
            _random = new Random();
        }

        public IActionResult AddNewPatientRecord()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentPPHN));

                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);
                ViewBag.PatientName = userMetadata.FirstName + " " + userMetadata.LastName;
                ViewBag.PatientID = userAsset.data.Data.ID;
                return View();
            }
        }

        [HttpPost]
        public IActionResult AddNewPatientRecord(AddNewPatientRecordViewModel addNewPatientRecordViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!string.IsNullOrEmpty(addNewPatientRecordViewModel.DoctorsNote.PurposeOfVisit))
            {
                var noteViewModel = addNewPatientRecordViewModel.DoctorsNote;
                var doctorNote = new DoctorNote
                {
                    PurposeOfVisit = noteViewModel.PurposeOfVisit,
                    Description = noteViewModel.Description,
                    FinalDiagnosis = noteViewModel.FinalDiagnosis,
                    FurtherInstructions = noteViewModel.FurtherInstructions,
                    DoctorName = HttpContext.Session.GetString(Globals.currentUserName),
                    DoctorMinsc = HttpContext.Session.GetString(Globals.currentUserID),
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

                var metadata = new MetaDataSaved<double>();
                metadata.AccessList = new Dictionary<string, string>();

                //store the data encryption key in metadata encrypted with sender and reciever agree key
                var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
                var patientAgreePublicKey = HttpContext.Session.GetString(Globals.currentPAPubK);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivateKey);
                metadata.AccessList[doctorSignPublicKey] =
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, doctorAgreePublicKey);
                metadata.AccessList[patientSignPublicKey] =
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, patientAgreePublicKey);

                _bigChainDbService.SendCreateTransferTransactionToDataBase<string, double>(asset, metadata, doctorSignPrivateKey, patientSignPublicKey);
            }

            if (!string.IsNullOrEmpty(addNewPatientRecordViewModel.Prescription.DrugName))
            {
                var prescriptionViewModel = addNewPatientRecordViewModel.Prescription;
                var prescription = new Prescription
                {
                    PrescribingDate = prescriptionViewModel.PrescribingDate,
                    Superscription = prescriptionViewModel.Superscription,
                    DrugName = prescriptionViewModel.DrugName,
                    Dosage = prescriptionViewModel.Dosage,
                    //StartDate = prescriptionViewModel.StartDate,
                    EndDate = prescriptionViewModel.EndDate,
                    Refill = prescriptionViewModel.Refill,
                    Substitution = prescriptionViewModel.Substitution,
                    DoctorName = HttpContext.Session.GetString(Globals.currentUserName),
                    DoctorMinsc = HttpContext.Session.GetString(Globals.currentUserID),
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

                var metadata = new MetaDataSaved<PrescriptionMetadata>
                {
                    AccessList = new Dictionary<string, string>(),
                    data = new PrescriptionMetadata
                    {
                        RefillRemaining = prescription.Refill,
                        LastIssueQty = -1
                    }
                };

                //store the data encryption key in metadata encrypted with sender and reciever agree key
                var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
                var patientAgreePublicKey = HttpContext.Session.GetString(Globals.currentPAPubK);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivateKey);
                metadata.AccessList[doctorSignPublicKey] =
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, doctorAgreePublicKey);
                metadata.AccessList[patientSignPublicKey] =
                    EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, patientAgreePublicKey);

                _bigChainDbService.SendCreateTransferTransactionToDataBase<string, PrescriptionMetadata>(asset, metadata, doctorSignPrivateKey, patientSignPublicKey);
            }

            //There is a test result that exists
            if (!string.IsNullOrEmpty(addNewPatientRecordViewModel.TestRequisition.ReasonForTest))
            {
                //File exists
                if (addNewPatientRecordViewModel.TestRequisition.AttachedFile != null)
                {
                    var file = addNewPatientRecordViewModel.TestRequisition.AttachedFile;
                    string base64FileString = "";

                    // Convert "file" into base64 string "base64FileString" to save into database
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        base64FileString = Convert.ToBase64String(fileBytes);
                    }
                    //encrypt file and store in ipfs
                    var encryptionKey = EncryptionService.getNewAESEncryptionKey();
                    var encryptedFile = EncryptionService.getEncryptedAssetDataKey(base64FileString, encryptionKey);
                    var id = _bigChainDbService.UploadTextToIPFS(encryptedFile);

                    var testRequisition = new TestRequisitionAsset
                    {
                        AttachedFile = new FileData
                        {
                            Data = id,
                            Type = file.ContentType,
                            Extension = file.ContentType.Split('/').Last(),
                            Name = file.FileName
                        },
                        ReasonForTest = addNewPatientRecordViewModel.TestRequisition.ReasonForTest,
                        TestType = addNewPatientRecordViewModel.TestRequisition.TestType,
                        DateOrdered = DateTime.Now
                    };
                    var encryptedData = EncryptionService.getEncryptedAssetDataKey(JsonConvert.SerializeObject(testRequisition), encryptionKey);

                    var asset = new AssetSaved<string>
                    {
                        Data = encryptedData,
                        RandomId = _random.Next(0, 100000),
                        Type = AssetType.TestRequisition
                    };

                    var metadata = new MetaDataSaved<double>();
                    metadata.AccessList = new Dictionary<string, string>();

                    //store the data encryption key in metadata encrypted with sender and reciever agree key
                    var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
                    var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
                    var patientAgreePublicKey = HttpContext.Session.GetString(Globals.currentPAPubK);
                    var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
                    var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                    var doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivateKey);
                    metadata.AccessList[doctorSignPublicKey] =
                        EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, doctorAgreePublicKey);
                    metadata.AccessList[patientSignPublicKey] =
                        EncryptionService.getEncryptedEncryptionKey(encryptionKey, doctorAgreePrivateKey, patientAgreePublicKey);

                    _bigChainDbService.SendCreateTransferTransactionToDataBase<string, double>(asset, metadata, doctorSignPrivateKey, patientSignPublicKey);
                }
            }

            return RedirectToAction("PatientOverview");
        }

        public JsonResult GetAllPatientIDs()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return Json("{}");
            else
            {
                var phns = _bigChainDbService.GetAllTypeIDs(AssetType.Patient);
                return Json(phns);
            }
        }

        public IActionResult GetRequisitionFile(string transID)
        {
            var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, double>(transID);
            var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
            var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
            var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
            if (result.metadata.AccessList.Keys.Contains(doctorSignPublicKey))
            {
                var hashedKey = result.metadata.AccessList[doctorSignPublicKey];
                var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                var data = EncryptionService.getDecryptedAssetData(result.data.Data, dataDecryptionKey);
                var asset = JsonConvert.DeserializeObject<TestRequisitionAsset>(data);
                //get encrypted file from ipfs
                string encryptedFileData = _bigChainDbService.GetTextFromIPFS(asset.AttachedFile.Data);
                string fileData = EncryptionService.getDecryptedAssetData(encryptedFileData, dataDecryptionKey);

                byte[] fileBytes = Convert.FromBase64String(fileData);
                return File(fileBytes, asset.AttachedFile.Type, asset.AttachedFile.Name);
            }
            return new EmptyResult();
        }

        public IActionResult GetResultFile(string transID)
        {
            var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, double>(transID);
            var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
            var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
            var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
            if (result.metadata.AccessList.Keys.Contains(doctorSignPublicKey))
            {
                var hashedKey = result.metadata.AccessList[doctorSignPublicKey];
                var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                var encryptedFile = _bigChainDbService.GetAssociatedTestResultFile(result.id);
                if (encryptedFile != "")
                {
                    var data = EncryptionService.getDecryptedAssetData(encryptedFile, dataDecryptionKey);
                    var asset = JsonConvert.DeserializeObject<FileData>(data);
                    //get encrypted file from ipfs
                    string encryptedFileData = _bigChainDbService.GetTextFromIPFS(asset.Data);
                    string fileData = EncryptionService.getDecryptedAssetData(encryptedFileData, dataDecryptionKey);

                    byte[] fileBytes = Convert.FromBase64String(fileData);
                    return File(fileBytes, asset.Type, asset.Name);
                }
            }
            return new EmptyResult();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(DoctorLoginViewModel indexViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
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
                HttpContext.Session.SetString(Globals.currentDSPriK, signPrivateKey);
                HttpContext.Session.SetString(Globals.currentDAPriK, agreePrivateKey);
                HttpContext.Session.SetString(Globals.currentUserName, $"{userAsset.data.Data.FirstName} {userAsset.data.Data.LastName}");
                HttpContext.Session.SetString(Globals.currentUserID, userAsset.data.Data.ID);
                return RedirectToAction("PatientLookUp");
            }
            else
            {
                ModelState.AddModelError("", "Password or Keyword incorrect.");
                return View(indexViewModel);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View();
        }

        public IActionResult PatientLookUp()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null
                || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else
                return View();
        }

        [HttpPost]
        public IActionResult PatientLookUp(PatientLookupViewModel patientLookupViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(patientLookupViewModel);
            Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(patientLookupViewModel.PHN);
            if (userAsset == null)
            {
                var sugg_phn = _bigChainDbService.GetSuggPatientPHN(patientLookupViewModel.PHN);
                ModelState.AddModelError("", "We could not find a matching user.");
                if (sugg_phn != "")
                {
                    ModelState.AddModelError("", "Did you mean: " + sugg_phn + "?");
                }

                return View(patientLookupViewModel);
            }
            HttpContext.Session.SetString(Globals.currentPSPubK, userAsset.data.Data.SignPublicKey);
            HttpContext.Session.SetString(Globals.currentPAPubK, userAsset.data.Data.AgreePublicKey);
            HttpContext.Session.SetString(Globals.currentPPHN, userAsset.data.Data.ID);
            return RedirectToAction("PatientOverview");
        }

        public IActionResult PatientOverview()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentPPHN));

                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge()
                };

                return View(patientOverviewViewModel);
            }
        }

        public IActionResult PatientRecords()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentPPHN));

                var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                var doctorNotesList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string, double>
                    (AssetType.DoctorNote, doctorSignPublicKey, patientSignPublicKey);
                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string, PrescriptionMetadata>
                    (AssetType.Prescription, doctorSignPublicKey, patientSignPublicKey);
                var testRequisitionList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string, double>
                    (AssetType.TestRequisition, doctorSignPublicKey, patientSignPublicKey);
                Dictionary<string, string> testResults = null;
                if (testRequisitionList.Any())
                {
                    testResults = _bigChainDbService.GetAssociatedTestResults(testRequisitionList);
                }
                var doctorNotes = new List<DoctorNoteFullData>();
                var prescriptions = new List<PrescriptionFullData>();
                var testRequisitions = new List<TestRequisitionFullData>();
                foreach (var doctorNote in doctorNotesList)
                {
                    var hashedKey = doctorNote.metadata.AccessList[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(doctorNote.data.Data, dataDecryptionKey);
                    var newEntry = new DoctorNoteFullData
                    {
                        assetData = JsonConvert.DeserializeObject<DoctorNote>(data),
                        Metadata = doctorNote.metadata.data
                    };
                    doctorNotes.Add(newEntry);
                }
                foreach (var prescription in prescriptionsList)
                {
                    var hashedKey = prescription.metadata.AccessList[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                    var newEntry = new PrescriptionFullData
                    {
                        assetData = JsonConvert.DeserializeObject<Prescription>(data),
                        Metadata = prescription.metadata.data
                    };
                    prescriptions.Add(newEntry);
                }
                foreach (var testrequisition in testRequisitionList)
                {
                    var hashedKey = testrequisition.metadata.AccessList[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(testrequisition.data.Data, dataDecryptionKey);
                    var newEntry = new TestRequisitionFullData
                    {
                        assetData = JsonConvert.DeserializeObject<TestRequisitionAsset>(data),
                        Metadata = testrequisition.metadata.data,
                        assetID = testrequisition.id,
                        transID = testrequisition.transID
                    };
                    if (testResults != null && testResults.Keys.Contains(testrequisition.id))
                    {
                        var decryptedResultFile = EncryptionService.getDecryptedAssetData(testResults[testrequisition.id], dataDecryptionKey);
                        newEntry.ResultFile = JsonConvert.DeserializeObject<FileData>(decryptedResultFile);
                    }
                    testRequisitions.Add(newEntry);
                }
                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    DoctorNotes = doctorNotes.OrderByDescending(d => d.assetData.DateOfRecord).ToList(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.assetData.PrescribingDate).ToList(),
                    TestRequisitions = testRequisitions.OrderByDescending(p => p.assetData.DateOrdered).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }

        public IActionResult PatientSignUp()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            return View();
        }

        [HttpPost]
        public IActionResult PatientSignUp(PatientSignUpViewModel patientSignUpViewModel)
        {
            // Description: Registers a patient up for a MedNet account
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            string signPrivateKey = null, agreePrivateKey = null, signPublicKey = null, agreePublicKey = null;
            Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(patientSignUpViewModel.PHN);

            // Check if PHN is already in use
            if (userAsset != null)
            {
                ModelState.AddModelError("", "A Patient profile with that PHN already exists");
                return View(patientSignUpViewModel);
            }

            // Register fingerprint information
            int numScans = 5;
            List<Image> fpList = FingerprintService.authenticateFP("24.84.225.22", numScans);
            List<byte[]> fpdb = new List<byte[]>();

            if (fpList.Count > numScans)
            {
                ModelState.AddModelError("", "Something went wrong with the fingerprint scan, try again.");
                return View(patientSignUpViewModel);
            }

            // Parse the input data for user registration
            var passphrase = patientSignUpViewModel.KeyWord;
            var password = patientSignUpViewModel.Password;

            // Encrypt fingerprint data
            List<string> encrList = new List<string>();
            foreach (byte[] fp in fpdb)
            {
                string encrStr = EncryptionService.encryptFingerprintData(patientSignUpViewModel.PHN, passphrase, fp);
                encrList.Add(encrStr);
            }

            // Create a user for the Blockchain
            EncryptionService.getNewBlockchainUser(out signPrivateKey, out signPublicKey, out agreePrivateKey, out agreePublicKey);

            // Create the user Asset
            var userAssetData = new PatientCredAssetData
            {
                ID = patientSignUpViewModel.PHN,
                DateOfBirth = patientSignUpViewModel.DateOfBirth,
                PrivateKeys = EncryptionService.encryptPrivateKeys(patientSignUpViewModel.PHN, passphrase, signPrivateKey, agreePrivateKey),
                DateOfRecord = DateTime.Now,
                SignPublicKey = signPublicKey,
                AgreePublicKey = agreePublicKey,
                FingerprintData = encrList,
            };

            // Encrypt the user's password in the metadata
            var userMetadata = new PatientCredMetadata
            {
                FirstName = patientSignUpViewModel.FirstName,
                LastName = patientSignUpViewModel.LastName,
                Email = patientSignUpViewModel.Email,
                hashedPassword = EncryptionService.hashPassword(password)
            };

            // Save the user Asset and Metadata
            var asset = new AssetSaved<PatientCredAssetData>
            {
                Type = AssetType.Patient,
                Data = userAssetData,
                RandomId = _random.Next(0, 100000)
            };
            var metadata = new MetaDataSaved<PatientCredMetadata>
            {
                data = userMetadata
            };

            // Send the user's information to the Blockchain database
            _bigChainDbService.SendCreateTransactionToDataBase(asset, metadata, signPrivateKey);
            return RedirectToAction("PatientLookUp");
        }

        public IActionResult RequestAccess()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentPPHN));

                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);
                ViewBag.PatientName = userMetadata.FirstName + " " + userMetadata.LastName;
                ViewBag.PatientID = userAsset.data.Data.ID;
                return View();
            }
        }

        [HttpPost]
        public IActionResult RequestAccess(RequestAccessViewModel requestAccessViewModel)
        {
            // Description: Authenticates a patient's identity when a Doctor requests access to their medical information
            // Get's the Doctor's information for current session
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(requestAccessViewModel);
            string PHN = HttpContext.Session.GetString(Globals.currentPPHN);
            string patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            string doctorSignPrivatekey = HttpContext.Session.GetString(Globals.currentDSPriK);
            string doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivatekey);
            string doctorAgreePrivatekey = HttpContext.Session.GetString(Globals.currentDAPriK);
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
            int numScans = 1;
            List<Image> fpList = FingerprintService.authenticateFP("24.84.225.22", numScans); // DEBUG: Jacob's Computer

            // Check if fingerprint data is valid
            if (fpList.Count < numScans)
            {
                ModelState.AddModelError("", "Something went wrong with the fingerprint scan, try again.");
                return View(requestAccessViewModel);
            }
            Image fpImg = fpList[0];

            // Decrypt the patient's fingerprint data stored in the Blockchain
            byte[] dbFpData = null;
            string patientSignPrivateKey, patientAgreePrivateKey;
            List<string> dbList = userAsset.data.Data.FingerprintData;
            List<Image> dbfpList = new List<Image>();
            try
            {
                foreach (string db in dbList)
                {
                    EncryptionService.decryptFingerprintData(PHN, keyword, db, out dbFpData);
                    dbfpList.Add(FingerprintService.byteToImg(dbFpData));
                }
                EncryptionService.getPrivateKeyFromIDKeyword(PHN, keyword, userAsset.data.Data.PrivateKeys, out patientSignPrivateKey, out patientAgreePrivateKey);
            }
            catch
            {
                ModelState.AddModelError("", "Keyword may be incorrect");
                return View(requestAccessViewModel);
            }

            // Compare the scanned fingerprint with the one saved in the database
            if (!FingerprintService.compareFP(fpImg, fpList))
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
                MetaDataSaved<object> metadata = record.metadata;
                if (!metadata.AccessList.Keys.Contains(doctorSignPublicKey))
                {
                    var hashedKey = metadata.AccessList[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var newHash = EncryptionService.getEncryptedEncryptionKey(dataDecryptionKey, patientAgreePrivateKey, doctorAgreePublicKey);
                    metadata.AccessList[doctorSignPublicKey] = newHash;
                    _bigChainDbService.SendTransferTransactionToDataBase(record.id, metadata,
                        patientSignPrivateKey, patientSignPublicKey, record.transID);
                }
            }
            return RedirectToAction("PatientOverview");
        }

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SignUp(doctorSignUpViewModel doctorSignUpViewModel)
        {
            string signPrivateKey = null, agreePrivateKey = null, signPublicKey = null, agreePublicKey = null;
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
            return RedirectToAction("Login");
        }
    }
}