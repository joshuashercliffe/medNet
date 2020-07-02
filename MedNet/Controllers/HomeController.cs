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

namespace MedNet.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            string[] nodes = Globals.nodes;
            _bigChainDbService = new BigChainDbService(nodes);
            _random = new Random();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult TestFingerprintButton(TestFingerprintButton model)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            return View(model);
        }

        public IActionResult TriggerFingerprint()
        {
            // DEBUG:FP
            // Description: Using for DEBUG. URL: https://lifeblocks.site/home/testfingerprintbutton 
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            
            // Retrieve the Public IP of the Client Computer using the browser
            var ip = HttpContext.Connection.RemoteIpAddress;
            string ipAddress = ip.ToString();

            // Do fingerprint fetch from windows service here 
            List<byte[]> fpBytes = FingerprintService.scanMultiFP(ipAddress, 5, out _);
            Bitmap fpBmp = null;
            for(int i = 0; i < fpBytes.Count; i++)
            {
                var fpStr = Convert.ToBase64String(fpBytes[i]);
                var debugByte = Convert.FromBase64String(fpStr);
                fpBmp = FingerprintService.byteToBmp(debugByte);
                //fpBmp.Save(i.ToString() + ".bmp");
            }
            byte[] fpData = FingerprintService.bmpToByte(fpBmp);
            Bitmap test = FingerprintService.byteToBmp(fpData);
            //test.Save("test.bmp");
            // do fingerprint comparison
            var isMatch = FingerprintService.compareFP(fpData, fpBytes);
            bool compare = fpBytes[0] == fpData;

            List<byte[]> fpList = new List<byte[]> {fpBytes[0], fpData };
            //FingerprintService.saveFP(fpList);

            // Write the Public IP of the client computer on the window
            var model = new TestFingerprintButton()
            {
                //message = "The Public IP address of the client is: " + ipAddress
                message = "Fingerprints match?" + compare.ToString()
            };
            return RedirectToAction("TestFingerprintButton", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
