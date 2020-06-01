import fp_tools as fp

import serial
import hashlib
import time
import tempfile
import serial.tools.list_ports as lsp
from pyfingerprint.pyfingerprint import PyFingerprint

# initialize fingerprint sensor
[f, com_dev] = fp.init()

# enroll fingerprint functionality
fp.enroll(f)