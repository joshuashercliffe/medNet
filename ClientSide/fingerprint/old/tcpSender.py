import socket 

#HOST = '192.168.0.17' #jacob's computer
#HOST = '35.155.4.38' #cnode
HOST = '127.0.0.1'
PORT = 5555

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s: 
    s.connect((HOST, PORT)) 
    s.sendall(b'alooo') 
    data = s.recv(1024) 

print('Received', repr(data))