Imports System.Drawing
Imports GTA
Imports GTA.Math
''' <summary>
''' 反R星补丁，NPC不会故意别车
''' </summary>
Public Class AntiNpcDriverPatch
    Inherits Script
    Public Shared Property Enabled As Boolean = False
    Public Sub New()
        Interval = 50
    End Sub

    Private Sub AntiRockstarPatch_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        If Not Enabled Then Return
        Dim player As Ped = PlayerPed
        If Not Game.IsLoading AndAlso player IsNot Nothing AndAlso player.Exists() Then
            If player.IsInVehicle() Then
                Dim currentVehicle As Vehicle = player.CurrentVehicle
                For Each vehicle As Vehicle In World.GetNearbyVehicles(currentVehicle.Position, 10)
                    If Not vehicle.Equals(currentVehicle) AndAlso vehicle.Driver IsNot Nothing Then
                        Dim vec As Vector3 = vehicle.Position - currentVehicle.Position
                        Dim force As Vector3 = New Vector3(vec.X, vec.Y, vec.Z + 10)
                        vehicle.ApplyForce(force)
                    End If
                Next
            End If
        End If
    End Sub
End Class
