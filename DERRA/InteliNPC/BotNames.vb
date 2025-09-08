Public Class BotNames
    Public Shared ReadOnly RandomNames As String() = {
    "Nightfall", "gpk", "Larl", "Pure", "DM", "Save-", "Miposhka",
    "skiter", "Nine", "33", "Sneyking", "Aramis", "Kordan", "Moo", "Fly", "Cr1t-",
    "SaberLight", "Fata", "Ceb", "Topson", "benjyfishy", "Bugha", "EpikWhale", "Clix",
    "Aqua", "Nyhrox", "MrSavage", "Mongraal", "Arkhram", "Zayt", "Saf", "ZexRow", "Khanada", "Reverse2k",
    "Deyy", "Mero", "JannisZ", "Tayson", "Vadeal", "Kami", "3xPO", "Brax1n", "xii", "ReaL", "yomamx", "Jazz",
    "wipeer", "LiiLii", "zYK", "Revenge", "Davi", "xvx", "Mystik", "Rembrandt", "Sya", "Axiyo", "Lei", "Dvl", "M1ka",
    "Mendoza", "westside", "N4RRATE", "QQ_578806739", "Ju0000", "Scump", "Simp", "aBeZy", "Shotzzy",
    "Cellium", "Arcitys32", "96qqqqqqClayster", "Dashy", "Envoy", "Huke", "digg2live", "itzXexia", "digg2live",
    "degte3poshub_", "QAQ_a", "0X0X0X0X0X0X0X0", "digg2lives", "Shae_Baeee", "Gensters", "Kwebbelkop", "OfficialDuckJones",
    "Hobbit", "Crystalst", "Spaceboy", "Penta", "xQcWOWOWOWO", "AnthonyZ", "Vader", "DasMehdi", "LordKebun", "Blaustoise",
    "DisbeArex", "Kyle", "Lirik", "Summit1g", "TimtheTatman"
}
    Public Shared ReadOnly Property PickOne As String
        Get
            Return Pick(RandomNames)
        End Get
    End Property
End Class
