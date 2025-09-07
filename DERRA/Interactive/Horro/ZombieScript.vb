Imports GTA
Imports GTA.Chrono

Namespace Interactive.Horro
    Public Class ZombieScript
        Inherits Script
        Public Shared ReadOnly Infection As Toggle = New Toggle(False) With {.OnEnable = AddressOf StartInfection, .OnDisable = AddressOf StopInfection}
        Private ReadOnly zombies As EntityCollection(Of Ped) = New EntityCollection(Of Ped)()
        Public Sub New()
            Interval = 10
        End Sub
        Private Shared Sub StartInfection()
            World.Blackout = True
            World.Weather = Weather.ThunderStorm
            Game.MaxWantedLevel = 0
        End Sub
        Private Shared Sub StopInfection()
            World.Blackout = False
            World.Weather = Weather.ExtraSunny
            Game.MaxWantedLevel = 5
        End Sub
        Private Sub ZombieScript_Tick(sender As Object, e As EventArgs) Handles Me.Tick
            If Infection.Enabled Then
                GameClock.TimeOfDay = GameClockTime.FromHms(20, 0, 0)
                Game.MaxWantedLevel = 0
                Game.Player.WantedLevel = 0
                World.SetAmbientVehicleDensityMultiplierThisFrame(0)
                'World.SetAmbientPedDensityMultiplierThisFrame(3)
                zombies.Keep(KeepRule.MustAlive)
                For Each ped As Ped In World.GetNearbyPeds(PlayerPed, 500)
                    If ped.IsAlive AndAlso ped.IsHuman AndAlso Not EntityManagement.ContainsEntity(ped) Then
                        Dim zombie As ZombieController = New ZombieController(ped)
                        EntityManagement.AddController(zombie)
                        zombies.Add(ped)
                    End If
                Next
                UI.Screen.ShowSubtitle("丧尸数量:" + CStr(zombies.Count))
            End If
        End Sub
    End Class
End Namespace

