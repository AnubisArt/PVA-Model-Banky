CREATE TABLE IF NOT EXISTS SporiciUcet(
    UserID INTEGER NOT NULL,
    AccID INTEGER PRIMARY KEY AUTOINCREMENT,
    Zustatek INTEGER NOT NULL DEFAULT 0,
    Studentsky BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);