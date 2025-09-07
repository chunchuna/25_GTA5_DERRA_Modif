Imports GTA
Imports GTA.Math
Imports GTA.Native

Namespace OnlinePlayer
    Public Class PlayerRegenerateScript
        Inherits Script
        Public Sub New()
            Interval = 100
        End Sub

        Private Sub PlayerRegenerateScript_Tick(sender As Object, e As EventArgs) Handles Me.Tick
            If OnlinePlayerOption.EnableMPMode.Enabled AndAlso PlayerPed.IsDead Then
                Dim dead_location As Vector3 = PlayerPed.Position
                'Game.TimeScale = 1
                Wait(3000)
                [Function].Call(Hash.FORCE_CLEANUP_FOR_ALL_THREADS_WITH_THIS_NAME, "respawn_controller", 3)
                PlayerPed.Resurrect()
                PlayerPed.IsCollisionEnabled = True
                [Function].Call(Hash.FORCE_GAME_STATE_PLAYING)
                Game.Player.SetControlState(True, SetPlayerControlFlags.ReenableControlOnDeath Or SetPlayerControlFlags.AllowPlayerDamage)
                Game.Player.WantedLevel = 0
                Dim location As Vector3 = Nothing
                If World.GetSafePositionForPed(dead_location.Around(100), location, GetSafePositionFlags.OnlyNetworkSpawn) Then
                    PlayerPed.Position = location
                Else
                    location = World.GetNextPositionOnStreet(dead_location.Around(100), True)
                    PlayerPed.Position = location
                End If
                'Game.Player.IsInvincible = True
                'Wait(2000)
                'Game.Player.IsInvincible = False
                'PlayerPed.Position = location
                '[Function].Call(Hash.NETWORK_RESURRECT_LOCAL_PLAYER, location.X, location.Y, location.Z, PlayerPed.Heading, 2000, True)
            End If
        End Sub
    End Class

End Namespace
