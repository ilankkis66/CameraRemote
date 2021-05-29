import socket
import threading
import SqlORM
import os
import requests

IP = "0.0.0.0"
PORT = 8820
CHUNK = 1024
SEPARATOR = "###"
connected_devices = {}
users_db = SqlORM.Users()
my_dir = os.getcwd()
command_len = 4
Lock = threading.Lock()


def receive(client_socket):
    try:
        return client_socket.recv(CHUNK).decode()
    except socket.error:
        return ''


def send(client_socket, send_data):
    """
    :param client_socket:
    :type send_data: string
    """
    client_socket.send(send_data.encode())


def get_keys_list(dic):
    """return list with all the keys of dic"""
    keys = []
    for key in dic.keys():
        keys.append(key)
    return keys


def get_key_by_address(client_address):
    """return key in players by client address = players[key][0]"""
    for key in get_keys_list(connected_devices):
        if connected_devices[key][0] == client_address:
            return key
    return -1


def add_photo(data, name, device):
    global users_db
    if name == "":
        return
    num = users_db.get_photos_number(name)[0]
    with open(my_dir + "/photos/" + name + "/number " + str(num) + " " + device + ".png",
              "wb") as f:
        f.write(data)
        users_db.add_photo(name, f.name)


def accept(server_socket):
    global connected_devices, users_db
    try:
        cs, a = server_socket.accept()
        data = receive(cs)
        if not users_db.check_exist(data):
            users_db.insert(data)
        if not os.path.exists(my_dir + "/photos/" + data):
            os.mkdir(my_dir + "/photos/" + data)
        connected_devices[data] = [cs, a]
        print(data, a)
        to_send = ""
        for key in get_keys_list(connected_devices):
            if key != data:
                to_send += key + SEPARATOR
        to_send += "ENDD" + SEPARATOR + "IMGN" + SEPARATOR + str(users_db.get_photos_number(data)[0])
        send(cs, to_send)
    except socket.error:
        pass


def main():
    server_socket = socket.socket()
    server_socket.bind((IP, PORT))
    server_socket.listen(2)
    server_socket.settimeout(0.0001)
    if not os.path.exists(my_dir + "/photos"):
        os.mkdir(my_dir + "/photos")
    while True:
        accept(server_socket)
        try:
            for key in get_keys_list(connected_devices):
                t = threading.Thread(target=handle_client, args=(connected_devices[key][0],))
                t.start()
        except KeyError:
            pass


def handle_client(client_socket):
    global users_db
    data = receive(client_socket)
    name = get_key_by_address(client_socket)

    if data and name != -1:
        data = data.split(SEPARATOR)
        command, device = data[0], data[1]
        device_ip = str(connected_devices[device][1][0])
        if command == "COND":
            send(client_socket, "DADR" + SEPARATOR + device_ip + SEPARATOR + "server")
            send(connected_devices[device][0],
                 "DADR" + SEPARATOR + str(connected_devices[name][1]) + SEPARATOR + "client" + SEPARATOR + name)
        elif command == "SPIC":
            try:
                Lock.acquire()
                url = "http://" + device_ip + ":8080/photo.jpg"
                data = requests.get(url)
                add_photo(data.content, name, device)
                add_photo(data.content, device, name)
                Lock.release()
            except Exception as e:
                print(e)
        print(data.split(SEPARATOR))


if __name__ == '__main__':
    main()
