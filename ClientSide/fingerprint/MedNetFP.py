import time, random, serial, hashlib, time, tempfile, pdb, socket, io
from pathlib import Path
from SMWinservice import SMWinservice
import serial.tools.list_ports as lsp
from pyfingerprint import PyFingerprint
from PIL import Image
import _thread as thread
from requests import get 
import fpScan

class FPService(SMWinservice):
    # Creates a subclass of SMWinservice (a subclass of pywin32)
    _svc_name_ = "MedNetFP" # Service Name
    _svc_display_name_ = "MedNet Fingerprint" # Service Display Name
    _svc_description_ = "Enables Fingerprint communication to MedNet's Server" # Service Description

    def start(self):
        self.isrunning = True

    def stop(self):
        self.isrunning = False

    def main(self):
        # Service functionality. Socket Communication + Fingerprint
        fpScan.main()
        
if __name__ == '__main__':
    FPService.parse_command_line()