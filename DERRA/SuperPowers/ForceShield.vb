Imports GTA
Imports GTA.Math
Imports GTA.Native

Public Class ForceShield
    Inherits Script
    Private Shared shield As Vehicle
    Public Shared ReadOnly Toggle As Toggle = New Toggle(False) With {.OnEnable = AddressOf Enable, .OnDisable = AddressOf Disable}
    Private Shared Sub Enable()
        Dim l As Vector3 = PlayerPed.Position
        shield = World.CreateVehicle(VehicleHash.Thruster, New Vector3(l.X, l.Y, l.Z + 300))
        shield.AttachTo(PlayerPed)
        [Function].Call(Hash.SET_ENTITY_ALPHA, shield, 0, True)
        shield.IsInvincible = True
        shield.CanBeVisiblyDamaged = False
    End Sub
    Private Shared Sub Disable()
        shield?.Delete()
    End Sub
    Public Sub New()
        MyBase.New()
        Interval = 10
    End Sub

    Private Sub ForceShield_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        If Toggle.Enabled Then
            For Each entity As Entity In World.GetNearbyEntities(PlayerPed.Position, 3)
                If entity.Exists AndAlso entity <> PlayerPed AndAlso entity <> shield Then
                    entity.ApplyForce((entity.Position - PlayerPed.Position) * 50, forceType:=ForceType.Torque)
                    entity.Health = 0

                End If
            Next
            If PlayerPed.IsDead Then
                Toggle.Enabled = False
            End If
        End If

    End Sub
End Class
