using Microsoft.Data.Sqlite;

enum Role{
    Admin,
    User,
    Banker
}

enum AccountType{
    Bezny,
    Sporici,
    Kreditni
}

class Config{
    public int _maxDebt;
    public int _urok;

    public Config(){
        // Zkontroluje jestli slozka ./config existuje
        if(!Directory.Exists("./config")){
            Directory.CreateDirectory("./config");
        }
        // Zkusi existenci log.txt, debt.txt a uroky.txt
        if(!File.Exists("./config/log.txt")){
            File.Create("./config/log.txt").Close();
        }
        if(!File.Exists("./config/debt.txt")){
            File.Create("./config/debt.txt").Close();
            // Nastavi defaultni hodnotu
            File.WriteAllText("./config/debt.txt", "2000");
            _maxDebt = 2000;
        }
        else{
            _maxDebt = int.Parse(File.ReadAllText("./config/debt.txt"));
        }
        if(!File.Exists("./config/uroky.txt")){
            File.Create("./config/uroky.txt").Close();
            // Nastavi defaultni hodnotu
            File.WriteAllText("./config/uroky.txt", "10");
            _urok = 10;
        }
        else{
            _urok = int.Parse(File.ReadAllText("./config/uroky.txt"));
        }
    }

    public static void WriteLog(string message){
        using(StreamWriter sw = File.AppendText("./config/log.txt")){
            sw.WriteLine(message);
        }
    }
    public static void ReadLog(){
        using(StreamReader sr = File.OpenText("./config/log.txt")){
            string? line = "";
            while((line = sr.ReadLine()) != null){
                Console.WriteLine(line);
            }
        }
    }
    public static void FilterLog(List<string> FilterString){
        using(StreamReader sr = File.OpenText("./config/log.txt")){
            string? line = "";
            while((line = sr.ReadLine()) != null){
                foreach(string filter in FilterString){
                    if(line.Contains(filter)){
                        Console.WriteLine(line);
                    }
                }
            }
        }
    }
    public int GetMaxDebt(){
        return _maxDebt;
    }

    public int GetUrok(){
        return _urok;
    }
}

class HesloManager
{
    // Hashing the password
    public static string Hash(string heslo)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(heslo);
    }

    // Verifying the password
    public static bool Verify(string hashedHeslo, string userHeslo)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(userHeslo, hashedHeslo);
    }
}

class Database{
    private string _connectionString = "Data Source=./Databaze/Banka.db";
    private SqliteConnection _connection;

    // V konstruktoru zjisti, zda Banka.db existuje, pokud ne, vytvori ji a pomoci scriptu v ./Databaze/SQL/ vytvori tabulky
    public Database(){
        if(!Directory.Exists("./Databaze")){
            throw new Exception("Databazovy adresar neexistuje! Tudiz ani scripty pro vytvoreni databaze neexistuji!");
        }
        if(!File.Exists("./Databaze/Banka.db")){
            File.Create("./Databaze/Banka.db").Close();
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
            
            string[] scripts = Directory.GetFiles("./Databaze/SQL/", "*.sql");
            foreach(string script in scripts){
                string sql = File.ReadAllText(script);
                SqliteCommand command = new SqliteCommand(sql, _connection);
                command.ExecuteNonQuery();
            }
        }
        else{
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
    }

    public void CloseConnection(){
        _connection.Close();
    }
    

    // Funkce pro praci s databazi
    public BeznyUcet VytvorBeznyUcet(int userID){
        string sql = "INSERT INTO BeznyUcet (UserID, Zustatek) VALUES (@userID, 0)";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        command.ExecuteNonQuery();

        sql = "SELECT last_insert_rowid()";
        command = new SqliteCommand(sql, _connection);
        int accountID = Convert.ToInt32(command.ExecuteScalar());

        return new BeznyUcet(accountID, userID, 0);
    }
    public BeznyUcet? GetBeznyUcet(int userID){
        string sql = "SELECT * FROM BeznyUcet WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        SqliteDataReader reader = command.ExecuteReader();
        if(reader.Read()){
            return new BeznyUcet(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
        }
        return null;
    }
    public SporiciUcet VytvorSporiciUcet(int userID, bool studentsky){
        string sql = "INSERT INTO SporiciUcet (UserID, Zustatek, Studentsky) VALUES (@userID, 0, @studentsky)";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        command.Parameters.AddWithValue("@studentsky", studentsky);
        command.ExecuteNonQuery();

        sql = "SELECT last_insert_rowid()";
        command = new SqliteCommand(sql, _connection);
        int accountID = Convert.ToInt32(command.ExecuteScalar());

        return new SporiciUcet(accountID, userID, 0, studentsky);
    }
    public SporiciUcet? GetSporiciUcet(int userID){
        string sql = "SELECT * FROM SporiciUcet WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        SqliteDataReader reader = command.ExecuteReader();
        if(reader.Read()){
            return new SporiciUcet(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetBoolean(3));
        }
        return null;
    }
    public KreditniUcet VytvorKreditniUcet(int userID, DateTime datumSplaceni){
        string sql = "INSERT INTO KreditniUcet (UserID, Zustatek, termin_splatnosti) VALUES (@userID, 0, @termin_splatnosti)";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        command.Parameters.AddWithValue("@termin_splatnosti", datumSplaceni);
        command.ExecuteNonQuery();

        sql = "SELECT last_insert_rowid()";
        command = new SqliteCommand(sql, _connection);
        int accountID = Convert.ToInt32(command.ExecuteScalar());

        return new KreditniUcet(accountID, userID, 0, datumSplaceni);
    }
    public KreditniUcet? GetKreditniUcet(int userID){
        string sql = "SELECT * FROM KreditniUcet WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        SqliteDataReader reader = command.ExecuteReader();
        if(reader.Read()){
            return new KreditniUcet(reader.GetInt32(1), reader.GetInt32(0), reader.GetInt32(2), reader.GetDateTime(3));
        }
        return null;
    }
    public Uzivatel VytvorUzivatele(string jmeno, string prijmeni, string heslo, string role){
        string hashPassword = HesloManager.Hash(heslo);
        string sql = "INSERT INTO Users (Jmeno, Prijmeni, Heslo, Role) VALUES (@jmeno, @prijmeni, @heslo, @role)";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@jmeno", jmeno);
        command.Parameters.AddWithValue("@prijmeni", prijmeni);
        command.Parameters.AddWithValue("@heslo", hashPassword);
        command.Parameters.AddWithValue("@role", role);
        command.ExecuteNonQuery();

        // Get the last inserted ID
        sql = "SELECT last_insert_rowid()";
        command = new SqliteCommand(sql, _connection);
        int userID = Convert.ToInt32(command.ExecuteScalar());

        BeznyUcet beznyUcet = VytvorBeznyUcet(userID);

        return new Uzivatel(userID, jmeno, prijmeni, hashPassword, beznyUcet);
    }
    public Uzivatel? LogIn(string jmeno, string heslo){
        string sql = "SELECT * FROM Users WHERE Jmeno = @jmeno";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@jmeno", jmeno);
        SqliteDataReader reader = command.ExecuteReader();
        if(reader.Read()){
            if(HesloManager.Verify(reader.GetString(3), heslo)){
                Role role = (Role)Enum.Parse(typeof(Role), reader.GetString(4));
                switch(role){
                    case Role.Admin:
                        return new Administrator(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
                    case Role.User:
                        BeznyUcet? beznyUcet = GetBeznyUcet(reader.GetInt32(0));
                        SporiciUcet? sporiciUcet = GetSporiciUcet(reader.GetInt32(0));
                        KreditniUcet? kreditniUcet = GetKreditniUcet(reader.GetInt32(0));
                        return new Uzivatel(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), beznyUcet, sporiciUcet, kreditniUcet);
                    case Role.Banker:
                        return new Banker(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
                }
            }
        }
        return null;
    }
    public bool CheckAccountExistence(int accountID, AccountType accountType){
        string sql = "";
        switch(accountType){
            case AccountType.Bezny:
                sql = "SELECT COUNT(*) FROM BeznyUcet WHERE AccID = @accountID";
                break;
            case AccountType.Sporici:
                sql = "SELECT COUNT(*) FROM SporiciUcet WHERE AccID = @accountID";
                break;
            case AccountType.Kreditni:
                sql = "SELECT COUNT(*) FROM KreditniUcet WHERE AccID = @accountID";
                break;
        }
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@accountID", accountID);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
    public bool CheckUserExistence(int userID){
        string sql = "SELECT COUNT(*) FROM Users WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
    public int GetZustatek(int accountID, AccountType accountType){
        string sql = "";
        switch(accountType){
            case AccountType.Bezny:
                sql = "SELECT Zustatek FROM BeznyUcet WHERE AccID = @accountID";
                break;
            case AccountType.Sporici:
                sql = "SELECT Zustatek FROM SporiciUcet WHERE AccID = @accountID";
                break;
            case AccountType.Kreditni:
                sql = "SELECT Zustatek FROM KreditniUcet WHERE AccID = @accountID";
                break;
        }
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@accountID", accountID);
        return Convert.ToInt32(command.ExecuteScalar());
    }
    public bool Transakce(int srcAccountID, AccountType srcType, int destAccountID, AccountType destType, int amount, Config config){
        if (!CheckAccountExistence(srcAccountID, srcType)){
            Console.WriteLine("Ucet ze ktereho se penize posilaji neexistuje!");
            Config.WriteLog("Transakce: " + srcAccountID + " (" + srcType.ToString() + ") -> " + destAccountID + " (" + destType.ToString() + ") (" + amount + "), status: FAILED");
            return false;
        }
        if (!CheckAccountExistence(destAccountID, destType)){
            Console.WriteLine("Ucet na ktery se penize posilaji neexistuje!");
            Config.WriteLog("Transakce: " + srcAccountID + " (" + srcType.ToString() + ") -> " + destAccountID + " (" + destType.ToString() + ") (" + amount + "), status: FAILED");
            return false;
        }
        if(srcAccountID == destAccountID && srcType == destType){
            Console.WriteLine("Nelze provest transakci na stejny ucet!");
            Config.WriteLog("Transakce: " + srcAccountID + " (" + srcType.ToString() + ") -> " + destAccountID + " (" + destType.ToString() + ") (" + amount + "), status: FAILED");
            return false;
        }
        int srcZustatek = GetZustatek(srcAccountID, srcType);
        int destZustatek = GetZustatek(destAccountID, destType);
        if(srcZustatek - amount < 0 && srcType != AccountType.Kreditni){
            Console.WriteLine("Nedostatek penez na uctu!");
            Config.WriteLog("Transakce: " + srcAccountID + " (" + srcType.ToString() + ") -> " + destAccountID + " (" + destType.ToString() + ") (" + amount + "), status: FAILED");
            return false;
        }
        if(srcType == AccountType.Kreditni){
            if(srcZustatek - amount < -config.GetMaxDebt()){
                Console.WriteLine("Nelze mit dluh vetsi nez " + config.GetMaxDebt() + "!");
                return false;
            }
        }
        srcZustatek -= amount;
        destZustatek += amount;

        string sql = "";
        switch(srcType){
            case AccountType.Bezny:
                sql = "UPDATE BeznyUcet SET Zustatek = @zustatek WHERE AccID = @accountID";
                break;
            case AccountType.Sporici:
                sql = "UPDATE SporiciUcet SET Zustatek = @zustatek WHERE AccID = @accountID";
                break;
            case AccountType.Kreditni:
                sql = "UPDATE KreditniUcet SET Zustatek = @zustatek WHERE AccID = @accountID";
                break;
        }
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@zustatek", srcZustatek);
        command.Parameters.AddWithValue("@accountID", srcAccountID);
        command.ExecuteNonQuery();

        switch(destType){
            case AccountType.Bezny:
                sql = "UPDATE BeznyUcet SET Zustatek = @zustatek WHERE AccID = @accountID";
                break;
            case AccountType.Sporici:
                sql = "UPDATE SporiciUcet SET Zustatek = @zustatek WHERE AccID = @accountID";
                break;
            case AccountType.Kreditni:
                sql = "UPDATE KreditniUcet SET Zustatek = @zustatek WHERE AccID = @accountID";
                break;
        }
        command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@zustatek", destZustatek);
        command.Parameters.AddWithValue("@accountID", destAccountID);
        command.ExecuteNonQuery();

        Config.WriteLog("Transakce: " + srcAccountID + " (" + srcType.ToString() + ") -> " + destAccountID + " (" + destType.ToString() + ") (" + amount + "), status: SUCCESS");

        return true;
    }
    public bool IsAdmin(int userID){
        string sql = "SELECT Role FROM Users WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        return command.ExecuteScalar()?.ToString() == "Admin";
    }
    public bool DeleteUser(int userID){
        if(IsAdmin(userID)){
            Console.WriteLine("Nelze smazat admina!");
            return false;
        }
        string sql = @"
            DELETE FROM BeznyUcet WHERE UserID = @userID;
            DELETE FROM SporiciUcet WHERE UserID = @userID;
            DELETE FROM KreditniUcet WHERE UserID = @userID;
            DELETE FROM Users WHERE UserID = @userID;
        ";        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        command.ExecuteNonQuery();
        return true;
    }
    public bool ChangeRole(int userID, Role role){
        string sql = "UPDATE Users SET Role = @role WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@role", role.ToString());
        command.Parameters.AddWithValue("@userID", userID);
        command.ExecuteNonQuery();
        return true;
    }
    public List<object?> GetZustatky(int userID){
        List<object?> zustatky = new List<object?>();
        string sql = "SELECT * FROM BeznyUcet WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        SqliteDataReader reader = command.ExecuteReader();
        if(reader.Read()){
            zustatky.Add(reader.GetInt32(2));
        }
        else{
            zustatky.Add(null);
        }
        sql = "SELECT * FROM SporiciUcet WHERE UserID = @userID";
        command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        reader = command.ExecuteReader();
        if(reader.Read()){
            zustatky.Add(reader.GetInt32(2));
            // Studentsky
            zustatky.Add(reader.GetBoolean(3));
        }
        else{
            zustatky.Add(null);
            zustatky.Add(null);
        }
        sql = "SELECT * FROM KreditniUcet WHERE UserID = @userID";
        command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        reader = command.ExecuteReader();
        if(reader.Read()){
            zustatky.Add(reader.GetInt32(2));
            // Datum splaceni
            zustatky.Add(reader.GetDateTime(3));
        }
        else{
            zustatky.Add(null);
            zustatky.Add(null);
        }
        return zustatky;
    }
    public List<int> GetCislaUctu(int userID){
        List<int> cislaUctu = new List<int>();
        string sql = "SELECT * FROM BeznyUcet WHERE UserID = @userID";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        SqliteDataReader reader = command.ExecuteReader();
        if(reader.Read()){
            cislaUctu.Add(reader.GetInt32(0));
        }
        sql = "SELECT * FROM SporiciUcet WHERE UserID = @userID";
        command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        reader = command.ExecuteReader();
        if(reader.Read()){
            cislaUctu.Add(reader.GetInt32(0));
        }
        sql = "SELECT * FROM KreditniUcet WHERE UserID = @userID";
        command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@userID", userID);
        reader = command.ExecuteReader();
        if(reader.Read()){
            cislaUctu.Add(reader.GetInt32(0));
        }
        return cislaUctu;
    }
    public Dictionary<string, int> GetListUzivatelu(Role role){
        // Vrati jmeno a ID uzivatelu s danym opravnenim
        Dictionary<string, int> uzivatele = new Dictionary<string, int>();
        string sql = "SELECT Jmeno, Prijmeni, UserID FROM Users WHERE Role = @role";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@role", role.ToString());
        SqliteDataReader reader = command.ExecuteReader();
        while(reader.Read()){
            uzivatele.Add(reader.GetString(0) + " " + reader.GetString(1), reader.GetInt32(2));
        }
        return uzivatele;
    }
    public void AddUrok(Config config){
        // Ke vsem penezum se pricte % urok, ktery je v configu, na sporicim uctu
        string sql = "UPDATE SporiciUcet SET Zustatek = Zustatek + (Zustatek * @urok / 100)";
        SqliteCommand command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@urok", config.GetUrok());
        command.ExecuteNonQuery();
        return;
    }
}

class Uzivatel{
    private int _userID;
    private string _jmeno;
    private string _prijmeni;
    private string _heslo;
    private BeznyUcet? _beznyUcet;
    private SporiciUcet? _sporiciUcet;
    private KreditniUcet? _kreditniUcet;

    public Uzivatel(int userID, string jmeno, string prijmeni, string heslo, BeznyUcet? beznyUcet = null, SporiciUcet? sporiciUcet = null, KreditniUcet? kreditniUcet = null){
        _userID = userID;
        _jmeno = jmeno;
        _prijmeni = prijmeni;
        _heslo = heslo;
        _beznyUcet = beznyUcet;
        _sporiciUcet = sporiciUcet;
        _kreditniUcet = kreditniUcet;
    }

    public void Help(){
        Console.WriteLine("|---------------------------------------------------------------|");
        Console.WriteLine("| Dostupne prikazy:                                             |");
        Console.WriteLine("| 1. st - Vypise vsechny transakce o ktere se uzivatel pokusil  |");
        Console.WriteLine("| 2. p - Provede platbu na ucet                                 |");
        Console.WriteLine("| 3. a - Vypise zustatky na uctech s dalsimi informacemi        |");
        Console.WriteLine("| 4. n - Vypise cisla uctu                                      |");
        Console.WriteLine("| 5. q - Odhlasi uzivatele a ukonci program                     |");
        Console.WriteLine("| 6. h - Vypise napovedu                                        |");
        Console.WriteLine("|---------------------------------------------------------------|");

    }
    public void LogOut(){
        Console.WriteLine("Uzivatel byl odhlasen!");
        Environment.Exit(0);
    }
    public void StavUctu(){
        if (_beznyUcet != null){
            Console.WriteLine("Bezny ucet: " + _beznyUcet.GetZustatek());
        }
        if (_sporiciUcet != null){
            Console.WriteLine("Sporici ucet: " + _sporiciUcet.GetZustatek());
            Console.WriteLine("Studentsky: " + _sporiciUcet.GetStudentsky().ToString());
        }
        if (_kreditniUcet != null){
            Console.WriteLine("Kreditni ucet: " + _kreditniUcet.GetZustatek());
            Console.WriteLine("Datum splaceni: " + _kreditniUcet.GetDatumSplaceni().ToString());
        }
    }
    public void CislaUctu(){
        if (_beznyUcet != null){
            Console.WriteLine("Bezny ucet: " + _beznyUcet.GetAccountID());
        }
        if (_sporiciUcet != null){
            Console.WriteLine("Sporici ucet: " + _sporiciUcet.GetAccountID());
        }
        if (_kreditniUcet != null){
            Console.WriteLine("Kreditni ucet: " + _kreditniUcet.GetAccountID());
        }
    }
    public void Platba(int accountID, AccountType accountType, int amount, Database database, Config config){
        int srcAccountID = 0;
        if (accountType == AccountType.Bezny){
            if (_beznyUcet == null){
                Console.WriteLine("Uzivatel nema bezny ucet!");
                return;
            }
            srcAccountID = _beznyUcet.GetAccountID();
        }
        else if(accountType == AccountType.Sporici){
            if (_sporiciUcet == null){
                Console.WriteLine("Uzivatel nema sporici ucet!");
                return;
            }
            srcAccountID = _sporiciUcet.GetAccountID();
        }
        else if(accountType == AccountType.Kreditni){
            if (_kreditniUcet == null){
                Console.WriteLine("Uzivatel nema kreditni ucet!");
                return;
            }
            srcAccountID = _kreditniUcet.GetAccountID();
        }
        if (srcAccountID == 0){
            Console.WriteLine("Nepodarilo se najit vas ucet!");
            return;
        }
        if(!database.Transakce(srcAccountID, accountType, accountID, AccountType.Bezny, amount, config)){
            Console.WriteLine("Transakce se nezdarila!");
        }
    }
    public void DefaultVypis(Database database, int? userID = null){
        if(userID == null){
            userID = _userID;
        }
        List<object?> zustatky = database.GetZustatky(userID.Value);
        Console.WriteLine(" ----------------------- ");
        if(zustatky[0] != null){
            Console.WriteLine(" Bezny ucet: " + zustatky[0]);
        }
        if(zustatky[1] != null){
            Console.WriteLine(" Sporici ucet: " + zustatky[1]);
            Console.WriteLine(" Studentsky: " + zustatky[2]);
        }
        if(zustatky[3] != null){
            Console.WriteLine(" Kreditni ucet: " + zustatky[3]);
            Console.WriteLine(" Datum splaceni: " + zustatky[4]);
        }
        Console.WriteLine(" ----------------------- ");
    }
    public int GetUserID(){
        return _userID;
    }
}

class Banker : Uzivatel{
    public Banker(int userID, string jmeno, string prijmeni, string heslo) : base(userID, jmeno, prijmeni, heslo){
    }

    public static void CislaUctu(int userID, Database database){
        List<int> cislaUctu = database.GetCislaUctu(userID);
        Console.WriteLine("Bezny ucet: " + cislaUctu[0]);
        Console.WriteLine("Sporici ucet: " + cislaUctu[1]);
        Console.WriteLine("Kreditni ucet: " + cislaUctu[2]);
    }
    public static void AddUser(Database database){
        Console.WriteLine("Zadejte jmeno: ");
        string? jmeno = Console.ReadLine();
        Console.WriteLine("Zadejte prijmeni: ");
        string? prijmeni = Console.ReadLine();
        Console.WriteLine("Zadejte heslo: ");
        string? heslo = Console.ReadLine();
        Console.WriteLine("Zadejte roli: ");
        string? role = Console.ReadLine();

        if(String.IsNullOrEmpty(jmeno) || String.IsNullOrEmpty(prijmeni) || String.IsNullOrEmpty(heslo) || String.IsNullOrEmpty(role)){
            Console.WriteLine("Nepodarilo se vytvorit uzivatele!");
            return;
        }
        Role roleEnum = (Role)Enum.Parse(typeof(Role), role);
        if(!Enum.IsDefined(typeof(Role), roleEnum)){
            Console.WriteLine("Nepodarilo se vytvorit uzivatele!");
            return;
        }
        Uzivatel user = database.VytvorUzivatele(jmeno, prijmeni, heslo, role);
        Console.WriteLine("Uzivatel byl vytvoren!");
        return;
    }
    public static void CreateAccount(Database database){
        Console.WriteLine("Zadejte ID uzivatele: ");
        string? userID = Console.ReadLine();
        Console.WriteLine("Zadejte typ uctu: ");
        string? accountType = Console.ReadLine();
        if(String.IsNullOrEmpty(userID) || String.IsNullOrEmpty(accountType)){
            Console.WriteLine("Nepodarilo se vytvorit ucet!");
            return;
        }
        AccountType accountTypeEnum = (AccountType)Enum.Parse(typeof(AccountType), accountType);
        if(!Enum.IsDefined(typeof(AccountType), accountTypeEnum)){
            Console.WriteLine("Nepodarilo se vytvorit ucet!");
            return;
        }
        if(!database.CheckUserExistence(int.Parse(userID))){
            Console.WriteLine("Uzivatel neexistuje!");
            return;
        }
        switch(accountTypeEnum){
            case AccountType.Bezny:
                BeznyUcet beznyUcet = database.VytvorBeznyUcet(int.Parse(userID));
                Console.WriteLine("Bezny ucet byl vytvoren!");
                break;
            case AccountType.Sporici:
                Console.WriteLine("Zadejte zda je ucet studentsky: ");
                string? studentsky = Console.ReadLine();
                if(String.IsNullOrEmpty(studentsky)){
                    Console.WriteLine("Nepodarilo se vytvorit ucet!");
                    return;
                }
                SporiciUcet sporiciUcet = database.VytvorSporiciUcet(int.Parse(userID), bool.Parse(studentsky));
                Console.WriteLine("Sporici ucet byl vytvoren!");
                break;
            case AccountType.Kreditni:
                Console.WriteLine("Zadejte datum splaceni: ");
                string? datumSplaceni = Console.ReadLine();
                if(String.IsNullOrEmpty(datumSplaceni)){
                    Console.WriteLine("Nepodarilo se vytvorit ucet!");
                    return;
                }
                KreditniUcet kreditniUcet = database.VytvorKreditniUcet(int.Parse(userID), DateTime.Parse(datumSplaceni));
                Console.WriteLine("Kreditni ucet byl vytvoren!");
                break;
        }
    }
    public new void Help(){
        Console.WriteLine("|-----------------------------------------------------------------|");
        Console.WriteLine("| Dostupne prikazy:                                               |");
        Console.WriteLine("| 1. st - Vypise vsechny transakce o ktere se klient pokusil      |");
        Console.WriteLine("| 2. a - Vypise zustatky na uctech klienta s dalsimi informacemi  |");
        Console.WriteLine("| 3. n - Vypise cisla uctu klienta                                |");
        Console.WriteLine("| 4. q - Odhlasi uzivatele a ukonci program                       |");
        Console.WriteLine("| 5. add - Vytvoreni uzivatele                                    |");
        Console.WriteLine("| 6. create - Vytvoreni uctu                                      |");
        Console.WriteLine("| 7. h - Vypise napovedu                                          |");
        Console.WriteLine("|-----------------------------------------------------------------|");

    }
}

class Administrator : Banker{
    public Administrator(int userID, string jmeno, string prijmeni, string heslo) : base(userID, jmeno, prijmeni, heslo){
    }

    public static void ZmenitOpravneni(int userID, Role role, Database database){
        bool res = database.ChangeRole(userID, role);
        if (res){
            Console.WriteLine("Opravneni bylo zmeneno!");
        }
        else{
            Console.WriteLine("Opravneni nebylo zmeneno!");
        }
    }
    public void SmazatUzivatele(int userID, Database database){
        bool res = database.DeleteUser(userID);
        if (res){
            Console.WriteLine("Uzivatel byl smazan!");
        }
        else{
            Console.WriteLine("Uzivatel nebyl smazan!");
        }
    }
    public void SeznamUzivatelu(Role role, Database database){
        Dictionary<string, int> uzivatele = database.GetListUzivatelu(role);
        foreach(KeyValuePair<string, int> uzivatel in uzivatele){
            Console.WriteLine(uzivatel.Key + " " + uzivatel.Value);
        }
    }
    public new void Help(){
        Console.WriteLine("|-----------------------------------------------------|");
        Console.WriteLine("| Dostupne prikazy:                                   |");
        Console.WriteLine("| 1. chmod - Zmeni opravneni uzivatele                |");
        Console.WriteLine("| 2. r - Smaze uzivatele                              |");
        Console.WriteLine("| 3. tab - Vypise seznam uzivatelu s danym typem role |");
        Console.WriteLine("| 4. q - Odhlasi uzivatele a ukonci program           |");
        Console.WriteLine("| 5. add - Vytvoreni uzivatele                        |");
        Console.WriteLine("| 6. create - Vytvoreni uctu                          |");
        Console.WriteLine("| 7. h - Vypise napovedu                              |");
        Console.WriteLine("|-----------------------------------------------------|");

    }
}

class BeznyUcet{
    private int _accountID;
    private int _userID;
    private int _zustatek;

    public BeznyUcet(int accountID, int userID, int zustatek){
        _accountID = accountID;
        _userID = userID;
        _zustatek = zustatek;
    }

    public int GetZustatek(){
        return _zustatek;
    }
    public int GetAccountID(){
        return _accountID;
    }
}

class SporiciUcet : BeznyUcet{
    private bool _studentsky;

    public SporiciUcet(int accountID, int userID, int zustatek, bool studentsky) : base(accountID, userID, zustatek){
        _studentsky = studentsky;
    }

    public bool GetStudentsky(){
        return _studentsky;
    }
}

class KreditniUcet : BeznyUcet{
    private DateTime _datumSplaceni;

    public KreditniUcet(int accountID, int userID, int zustatek, DateTime datumSplaceni) : base(accountID, userID, zustatek){
        _datumSplaceni = datumSplaceni;
    }

    public DateTime GetDatumSplaceni(){
        return _datumSplaceni;
    }
}

class Program{
    static void Main(string[] args){
        Config config = new Config();
        Database database = new Database();

        int counter = 0;

        do{
            //Zkontroluje datum, jestli je prvni den v mesici, pokud ano, pricte urok
            if(DateTime.Now.Day == 1){
                database.AddUrok(config);
            }
            Console.WriteLine("Vitejte v aplikaci banky!");
            Console.WriteLine("Pro napovedu zadejte -h");

            Console.WriteLine("Zadejte jmeno: ");
            string? jmeno = Console.ReadLine();
            Console.WriteLine("Zadejte heslo: ");
            string? heslo = Console.ReadLine();
            if(String.IsNullOrEmpty(jmeno) || String.IsNullOrEmpty(heslo)){
                Console.WriteLine("Nepodarilo se prihlasit!");
                continue;
            }
            Uzivatel? prihlasenyUzivatel = database.LogIn(jmeno, heslo);
            if(prihlasenyUzivatel is Administrator admin){
                Console.Clear();
                while(true){
                    Console.WriteLine("Zadejte prikaz: ");
                    string? prikaz = Console.ReadLine();
                    switch (prikaz){
                        case "h":
                            admin.Help();
                            break;
                        case "chmod":
                            Console.WriteLine("Zadejte ID uzivatele: ");
                            string? id = Console.ReadLine();
                            Console.WriteLine("Zadejte novou roli: ");
                            string? role = Console.ReadLine();
                            if(String.IsNullOrEmpty(id) || String.IsNullOrEmpty(role)){
                                Console.WriteLine("Nepodarilo se zmenit opravneni!");
                                break;
                            }
                            Role newRole = (Role)Enum.Parse(typeof(Role), role);
                            if(!Enum.IsDefined(typeof(Role), newRole)){
                                Console.WriteLine("Nepodarilo se zmenit opravneni!");
                                break;
                            }
                            if(!database.CheckUserExistence(int.Parse(id))){
                                Console.WriteLine("Uzivatel neexistuje!");
                                break;
                            }
                            Administrator.ZmenitOpravneni(int.Parse(id), newRole, database);
                            break;
                        case "r":
                            Console.WriteLine("Zadejte ID uzivatele: ");
                            string? userID = Console.ReadLine();
                            if(String.IsNullOrEmpty(userID)){
                                Console.WriteLine("Nepodarilo se smazat uzivatele!");
                                break;
                            }
                            if(!database.CheckUserExistence(int.Parse(userID))){
                                Console.WriteLine("Uzivatel neexistuje!");
                                break;
                            }
                            admin.SmazatUzivatele(int.Parse(userID), database);
                            break;
                        case "tab":
                            Console.WriteLine("Zadejte roli: ");
                            string? roleTab = Console.ReadLine();
                            if(String.IsNullOrEmpty(roleTab)){
                                Console.WriteLine("Nepodarilo se zobrazit seznam uzivatelu!");
                                break;
                            }
                            Role roleTabEnum = (Role)Enum.Parse(typeof(Role), roleTab);
                            if(!Enum.IsDefined(typeof(Role), roleTabEnum)){
                                Console.WriteLine("Nepodarilo se zobrazit seznam uzivatelu!");
                                break;
                            }
                            admin.SeznamUzivatelu(roleTabEnum, database);
                            break;
                        case "q":
                            admin.LogOut();
                            break;
                        case "add":
                            Administrator.AddUser(database);
                            break;
                        case "create":
                            Banker.CreateAccount(database);
                            break;
                        default:
                            Console.WriteLine("Neplatny prikaz!");
                            break;
                    }
                }
            }
            else if(prihlasenyUzivatel is Banker banker){
                Console.Clear();
                while(true){
                    Console.WriteLine("Zadejte prikaz: ");
                    string? prikaz = Console.ReadLine();
                    switch (prikaz){
                        case "h":
                            banker.Help();
                            break;
                        case "q":
                            banker.LogOut();
                            break;
                        case "st":
                            Console.WriteLine("Zadejte ID klienta: ");
                            string? kliendID = Console.ReadLine();
                            if(String.IsNullOrEmpty(kliendID)){
                                Console.WriteLine("Nepodarilo se zobrazit transakce!");
                                break;
                            }
                            if(!database.CheckUserExistence(int.Parse(kliendID))){
                                Console.WriteLine("Uzivatel neexistuje!");
                                break;
                            }
                            List<string> cislaUctu = new List<string>();
                            cislaUctu.AddRange(database.GetCislaUctu(int.Parse(kliendID)).Select(x => x.ToString()));
                            Config.FilterLog(cislaUctu);
                            break;
                        case "a":
                            Console.WriteLine("Zadejte ID uctu: ");
                            string? accountID = Console.ReadLine();
                            if (String.IsNullOrEmpty(accountID)){
                                Console.WriteLine("Nepodarilo se provest platbu!");
                                break;
                            }
                            if (!database.CheckAccountExistence(int.Parse(accountID), AccountType.Bezny) && !database.CheckAccountExistence(int.Parse(accountID), AccountType.Sporici) && !database.CheckAccountExistence(int.Parse(accountID), AccountType.Kreditni)){
                                Console.WriteLine("Ucet neexistuje!");
                                break;
                            }
                            banker.DefaultVypis(database, int.Parse(accountID));
                            break;
                        case "n":
                            Console.WriteLine("Zadejte ID klienta: ");
                            string? klientID = Console.ReadLine();
                            if(String.IsNullOrEmpty(klientID)){
                                Console.WriteLine("Nepodarilo se zobrazit cisla uctu!");
                                break;
                            }
                            if(!database.CheckUserExistence(int.Parse(klientID))){
                                Console.WriteLine("Uzivatel neexistuje!");
                                break;
                            }
                            Banker.CislaUctu(int.Parse(klientID), database);
                            break;
                        case "add":
                            Banker.AddUser(database);
                            break;
                        case "create":
                            Banker.CreateAccount(database);
                            break;
                    }
                }
            }
            else if(prihlasenyUzivatel is Uzivatel user){
                Console.Clear();
                user.DefaultVypis(database);
                while(true){
                    Console.WriteLine("Zadejte prikaz: ");
                    string? prikaz = Console.ReadLine();
                    switch (prikaz){
                        case "h":
                            user.Help();
                            break;
                        case "q":
                            user.LogOut();
                            break;
                        case "st":
                            List<string> cislaUctu = new List<string>();
                            cislaUctu.AddRange(database.GetCislaUctu(user.GetUserID()).Select(x => x.ToString()));
                            Config.FilterLog(cislaUctu);
                            break;
                        case "p":
                            Console.WriteLine("Zadejte cislo uctu na ktery se poslou penize: ");
                            string? accountID = Console.ReadLine();
                            Console.WriteLine("Zadejte typ uctu ze ktereho se bude platit: ");
                            string? accountType = Console.ReadLine();
                            Console.WriteLine("Zadejte castku: ");
                            string? amount = Console.ReadLine();
                            if(String.IsNullOrEmpty(accountID) || String.IsNullOrEmpty(accountType) || String.IsNullOrEmpty(amount)){
                                Console.WriteLine("Nepodarilo se provest platbu!");
                                break;
                            }
                            AccountType accountTypeEnum = (AccountType)Enum.Parse(typeof(AccountType), accountType);
                            if(!Enum.IsDefined(typeof(AccountType), accountTypeEnum)){
                                Console.WriteLine("Nepodarilo se provest platbu!");
                                break;
                            }
                            user.Platba(int.Parse(accountID), accountTypeEnum, int.Parse(amount), database, config);
                            Console.WriteLine("Platba byla uspesna!");
                            break;
                        case "a":
                            user.DefaultVypis(database);
                            break;
                        case "n":
                            user.CislaUctu();
                            break;
                    }
                }
            }
            else{
                Console.WriteLine("Nepodarilo se prihlasit!");
            }
            counter++;
            Console.Clear();
        }while(counter < 3);
    }
}