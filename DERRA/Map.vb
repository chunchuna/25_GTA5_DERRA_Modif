Imports GTA
Imports GTA.Math

Public Class Map
    Private Shared ReadOnly gun_shops As List(Of Vector3) = New List(Of Vector3)()
    Private Shared ReadOnly cloth_shops As List(Of Vector3) = New List(Of Vector3)()
    Private Shared ReadOnly lofts As List(Of Vector3) = New List(Of Vector3)()
    Private Shared ReadOnly m_shops As List(Of Vector3) = New List(Of Vector3)()
    Private Shared ReadOnly airports As List(Of Vector3) = New List(Of Vector3)()
    Shared Sub New()
        gun_shops.Add(New Vector3(250.1518, -47.50141, 69.94107))
        gun_shops.Add(New Vector3(-1305.547, -394.7312, 36.69576))
        gun_shops.Add(New Vector3(842.5546, -1033.872, 28.19487))
        cloth_shops.Add(New Vector3(425.5158, -806.191, 29.49115))
        cloth_shops.Add(New Vector3(125.8335, -223.4805, 54.55782))
        cloth_shops.Add(New Vector3(-709.8701, -153.0631, 37.41513))
        lofts.Add(New Vector3(-632.6221, 56.53967, 43.72516))
        lofts.Add(New Vector3(-763.2294, 239.9446, 75.67195))
        airports.Add(New Vector3(1394.749, 1441.551, 105.2647))
        airports.Add(New Vector3(-1045.197, -2919.331, 13.953))
        airports.Add(New Vector3(1482.547, 3063.308, 40.53369))
    End Sub

    Public Shared ReadOnly Property GunShops As Vector3()
        Get
            Return gun_shops.ToArray()
        End Get
    End Property
    Public Shared ReadOnly Property HighLofts As Vector3()
        Get
            Return lofts.ToArray()
        End Get
    End Property
    Public Shared ReadOnly Property ClothesShops As Vector3()
        Get
            Return cloth_shops.ToArray()
        End Get
    End Property
    Public Shared ReadOnly Property AirportLocations As Vector3()
        Get
            Return airports.ToArray()
        End Get
    End Property
End Class
