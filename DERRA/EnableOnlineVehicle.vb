Imports GTA
Imports GTA.Native

Public Class EnableOnlineVehicle
    Inherits Script
    Public Sub New()
        GlobalVariable.Get(4269479).Write(1)
    End Sub
End Class
