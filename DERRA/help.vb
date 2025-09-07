Imports DERRA.Interactive
Imports GTA.UI

Public Module help
    Private Shared ReadOnly Rng As New System.Random()

    Public Sub HelloWorld()
        Notification.PostTicker("Hello World", False)
    End Sub

    Public Function CreateVehicle(model As VehicleHash?, distance As Single) As Vehicle
        Dim v As Vehicle = World.CreateVehicle(If(model, CType(Pick(Vehicle.GetAllModels()), UInteger)), PlayerPed.Position.Around(distance))
        v.PlaceOnNextStreet()
        ' If the vehicle supports liveries, apply a random one.
        If v.Mods.LiveryCount > 1 Then
            v.Mods.Livery = Rng.Next(0, v.Mods.LiveryCount)
        End If
        Return v
    End Function
    Public Function PlayerPed As Ped
        Return Game.Player.Character
    End Function
End Module
