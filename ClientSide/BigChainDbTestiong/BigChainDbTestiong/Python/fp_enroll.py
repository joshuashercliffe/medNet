import fp_tools as fp

import serial
import hashlib
import time
import tempfile
import serial.tools.list_ports as lsp
from pyfingerprint.pyfingerprint import PyFingerprint

[f, com_dev] = fp.init()

## Gets some sensor information
# print('Currently used templates: ' + str(f.getTemplateCount()) +'/'+ str(f.getStorageCapacity()))

## Tries to enroll new finger
try:
    # print('Waiting for finger...')

    ## Wait that finger is read
    while ( f.readImage() == False ):
        pass

    ## Converts read image to characteristics and stores it in charbuffer 1
    f.convertImage(0x01)

    ## Checks if finger is already enrolled
    result = f.searchTemplate()
    positionNumber = result[0]

    if ( positionNumber >= 0 ):
        print('Template already exists at position' + str(positionNumber))
        exit(0)

    # print('Remove finger...')
    time.sleep(2)

    # print('Waiting for same finger again...')

    ## Wait that finger is read again
    while ( f.readImage() == False ):
        pass

    ## Converts read image to characteristics and stores it in charbuffer 2
    f.convertImage(0x02)

    ## Compares the charbuffers
    if ( f.compareCharacteristics() == 0 ):
        print('RESULT:FAILED:RESULT')
        raise Exception('ERROR: Fingers do not match')

    ## Creates a template
    f.createTemplate()

    ## Saves template at new position number
    positionNumber = f.storeTemplate()

    print('ID:{0}:ID'.format(positionNumber))
    # print('New template position #' + str(positionNumber))

except Exception as e:
    print('ERROR: Operation failed!')
    print('ERROR: Exception message: ' + str(e))
    print('RESULT:FAILED:RESULT')
    exit(1)

characterics = str(f.downloadCharacteristics(0x01)).encode('utf-8')

## Hashes characteristics of template
print('HEX:' + hashlib.sha256(characterics).hexdigest() + ':HEX')