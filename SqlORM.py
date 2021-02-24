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
        self.current.execute("CREATE TABLE IF NOT EXISTS users(username text, photos number int, files string);")

    def check_exist(self, username):
        self.open_DB()
        sql = self.current.execute(
            "SELECT * FROM users WHERE username = '" + username + ";").fetchone()
        self.close_DB()
        return sql[0] != 0

    def get_photos_number(self, username):
        self.open_DB()
        res = self.current.execute(
            "SELECT photos number FROM users WHERE username = '" + username + ";").fetchone()
        self.close_DB()
        return res

    def insert(self, username):
        self.open_DB()
        sql = "INSERT INTO users(username, photos number, files) "
        sql += "VALUES('" + username + "','" + str(0) + "'+'""');"
        res = self.current.execute(sql)
        self.commit()
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
