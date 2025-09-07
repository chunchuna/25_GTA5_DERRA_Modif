Imports GTA
Imports GTA.Native
Imports GTA.UI

Namespace Movie
    ''' <summary>
    ''' 环境功能。
    ''' </summary>
    Public Class Envrn
        Public Shared Sub PlayerAttackedByPeds()
            For Each ped As Ped In World.GetNearbyPeds(PlayerPed, 10)
                If ped.IsAlive AndAlso Not EntityManagement.ContainsEntity(ped) AndAlso ped.IsOnScreen Then
                    ped.Task.Combat(PlayerPed, TaskThreatResponseFlags.CanFightArmedPedsWhenNotArmed)
                End If
            Next
        End Sub
    End Class
End Namespace

