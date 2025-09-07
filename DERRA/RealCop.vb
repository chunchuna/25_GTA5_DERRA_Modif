Imports GTA
Imports GTA.Native

Public Class RealCop
    Inherits Script
    Private ReadOnly killed_cops As HashSet(Of Ped) = New HashSet(Of Ped)()
    Private wanted As Boolean
    Private delete_cop_virus As Boolean
    Private ReadOnly known_cops As HashSet(Of Ped) = New HashSet(Of Ped)()
    Public Shared ReadOnly Enable As Toggle = New Toggle(False)
    Public Sub New()
        Interval = 100
    End Sub
    Private Sub Log(message As String)
        'UI.Notification.PostTicker(message, False)
    End Sub
    Public WriteOnly Property DispatchCops As Boolean
        Set(value As Boolean)
            Dim indeces As Integer() = {1, 2, 4, 6, 7, 8, 9, 10, 13, 14}
            For Each index As Integer In indeces
                [Function].Call(Hash.ENABLE_DISPATCH_SERVICE, index, value)
            Next
            delete_cop_virus = Not value
            If delete_cop_virus Then
                For Each cop As Ped In World.GetAllPeds()
                    If cop.IsAlive AndAlso (cop.PedType = PedType.Cop OrElse cop.PedType = PedType.Swat) AndAlso Not EntityManagement.ContainsEntity(cop) Then
                        known_cops.Add(cop)
                    End If
                Next
            Else
                known_cops.Clear()
            End If
        End Set
    End Property
    Public Function IsCopDispatchedByGame(cop As Ped) As Boolean
        If cop.IsAlive AndAlso (cop.PedType = PedType.Cop OrElse cop.PedType = PedType.Swat) AndAlso Not EntityManagement.ContainsEntity(cop) Then
            Return True
        Else
            Return False
        End If
    End Function
    Private Sub RealCop_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        On Error Resume Next
        If Not Enable Then
            Return
        End If
        If Game.Player.WantedLevel > 0 AndAlso Not Game.IsMissionActive Then
            For Each ped As Ped In World.GetNearbyPeds(PlayerPed, 100)
                If ped.Exists() AndAlso ped.IsDead AndAlso ped.PedType = PedType.Cop AndAlso ped.Killer?.Equals(PlayerPed) Then
                    killed_cops.Add(ped)
                End If
            Next
            wanted = True

            If killed_cops.Count < 8 Then
                Game.MaxWantedLevel = 2
                If Game.Player.WantedLevel > 2 Then
                    Game.Player.WantedLevel = 2
                End If
            Else '能够触发三星通缉
                Game.MaxWantedLevel = 5
            End If

            If PlayerPed.Position.DistanceTo(Game.Player.WantedCenterPosition) > 100 AndAlso Game.Player.AreWantedStarsGrayedOut Then
                DispatchCops = False
            Else
                DispatchCops = True
            End If

            If delete_cop_virus Then
                For Each ped As Ped In World.GetAllPeds
                    If IsCopDispatchedByGame(ped) AndAlso Not known_cops.Contains(ped) Then
                        Log("~b~已删除 " + Hex(ped.Handle))
                        ped.Delete()
                    End If
                Next
            End If
        Else
            If wanted Then
                DispatchCops = True
            End If
            killed_cops.Clear()
            wanted = False
            known_cops.Clear()
        End If

    End Sub
End Class
