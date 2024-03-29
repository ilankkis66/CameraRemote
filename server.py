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
SIZE_OF_SIZE = 10


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
    with open(my_dir + "/photos/" + name + "/number " + str(num) + " " + device + ".png", "wb") as f:
        f.write(data)
        users_db.add_photo(name, f.name)


def accept(server_socket):
    global connected_devices, users_db
    try:
        cs, a = server_socket.accept()
        data = receive(cs)

        # add him to the connected devices dictionary
        connected_devices[data] = [cs, a]
        print(data, a)

        # insert the client to the db
        if not users_db.check_exist(data):
            users_db.insert(data)

        # create his folder for saving his pictures
        if not os.path.exists(my_dir + "/photos/" + data):
            os.mkdir(my_dir + "/photos/" + data)

        # send him all the connected clients
        to_send = ""
        for key in get_keys_list(connected_devices):
            if key != data and len(connected_devices[device]) < 3:
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
    global users_db, connected_devices
    data = receive(client_socket)
    name = get_key_by_address(client_socket)

    if data and name != -1:  # if data was received and name is on the dictionary
        data = data.split(SEPARATOR)
        command = data[0]

        if command == "COND":
            device = data[1]
            if device in get_keys_list(connected_devices):
                device_ip = str(connected_devices[device][1][0])
                if len(connected_devices[device]) < 3:
                    # send each device the address of the other
                    send(client_socket, "DADR" + SEPARATOR + device_ip + SEPARATOR + "server")
                    send(connected_devices[device][0], "DADR" + SEPARATOR +
                         str(connected_devices[name][1][0]) + SEPARATOR + "client" + SEPARATOR + name)
            else:
                send(client_socket, "DUAA" + SEPARATOR)
        elif command == "SPIC":
            device = data[1]
            device_ip = str(connected_devices[device][1][0])
            try:
                Lock.acquire()
                url = "http://" + device_ip + ":8080/photo.jpg"
                data = requests.get(url)
                add_photo(data.content, name, device)
                add_photo(data.content, device, name)
                Lock.release()
            except Exception as e:
                print(e)
        elif command == "GPCL":
            file_list = users_db.get_files(name)
            file_list = str(file_list[0]).split("\n")
            file_list.remove("")
            for i in range(len(file_list)):
                file_list[i] = file_list[i][file_list[i].find("/n")+1:]
            to_send = str(file_list)
            send(client_socket, to_send[1:len(to_send)-1])
        elif command == "GPIC":
            file = data[1]
            with open(my_dir + "/photos/" + name + "/"+file, "rb") as f:
                byte_data = f.read()
                to_send = str(len(byte_data)).zfill(SIZE_OF_SIZE).encode() + SEPARATOR.encode()
                client_socket.send(to_send + byte_data)
        elif command == "DSCT":
            del connected_devices[name]
        print(data)


if __name__ == '__main__':
    main()
