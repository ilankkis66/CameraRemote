import sqlite3


class Users:
    def __init__(self):
        self.conn = None  # will store the DB connection
        self.current = None  # will store the DB connection cursor

    def open_DB(self):
        """
        will open DB file and put value in:
        self.conn (need DB file name)
        and self.current
        """
        self.conn = sqlite3.connect("users.db")
        self.current = self.conn.cursor()
        self.current.execute("CREATE TABLE IF NOT EXISTS users(Name text, PhotosNumber int, "
                             "files string, UNIQUE (Name));")

    def check_exist(self, Name):
        self.open_DB()
        sql = self.current.execute(
            "SELECT * FROM users WHERE Name = '" + Name + "';").fetchone()
        self.close_DB()
        return sql is not None

    def get_photos_number(self, Name):
        self.open_DB()
        res = self.current.execute(
            "SELECT PhotosNumber FROM users WHERE Name = '" + Name + "';").fetchone()
        self.close_DB()
        return res

    def get_files(self, Name):
        self.open_DB()
        res = self.current.execute(
            "SELECT Files FROM users WHERE Name = '" + Name + "';").fetchone()
        self.close_DB()
        return res

    def insert(self, Name):
        self.open_DB()
        sql = "INSERT INTO users(Name, PhotosNumber, files) "
        sql += "VALUES('" + Name + "','" + str(0) + "','""');"
        try:
            res = self.current.execute(sql)
            self.commit()
        except Exception as e:
            print("insert" + str(e))
        self.close_DB()

    def add_photo(self, Name, file):
        sql = "update users set PhotosNumber = '" + str(int(self.get_photos_number(Name)[0]) + 1) + \
              "',Files = '" + str(self.get_files(Name)[0]) + file + "\n" + "'where Name = '" + Name + "';"
        self.open_DB()
        try:
            res = self.current.execute(sql)
            self.commit()
        except Exception as e:
            print("add photo" + str(e))
        self.close_DB()

    def close_DB(self):
        self.conn.close()

    def commit(self):
        self.conn.commit()

    def print_sql(self):
        sql = "SELECT * FROM users;"
        res = self.current.execute(sql).fetchall()
        self.commit()
        return res
