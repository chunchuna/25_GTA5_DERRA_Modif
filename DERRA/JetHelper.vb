Imports System.Windows.Forms
Imports GTA
Imports GTA.Math

Public Class JetHelper
    Inherits Script
    Private Shared jet As Vehicle
    Private Shared last_time As Date?
    Private Sub JetHelper_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If jet IsNot Nothing AndAlso PlayerPed.IsInVehicle(jet) Then
            If e.KeyCode = Keys.E Then
                Game.IsThermalVisionActive = Not Game.IsThermalVisionActive
            End If
        End If
    End Sub
    Public Shared Sub CreateJet(model As VehicleHash)
        If Not last_time.HasValue OrElse (Now - last_time.Value).TotalMinutes > 2 Then
            UI.Screen.FadeOut(2000)
            Wait(2000)
            jet?.Delete()
            Dim position As Vector3 = PlayerPed.Position
            jet = World.CreateVehicle(model, New Vector3(position.X, position.Y, position.Z + 300), PlayerPed.Heading)
            jet.IsEngineRunning = True
            jet.Speed = 70
            PlayerPed.SetIntoVehicle(jet, VehicleSeat.Driver)
            Wait(1000)
            UI.Screen.FadeIn(1000)
            UI.Screen.ShowSubtitle("按 ~h~E~h~ 启动或关闭热成像")
        Else
            UI.Screen.ShowHelpText("操作频繁")
        End If


    End Sub
End Class
