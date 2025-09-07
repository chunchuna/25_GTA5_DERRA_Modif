Imports GTA

Public Class RevengeScript
    Inherits Script
    Public Shared ReadOnly Toggle As Toggle = New Toggle(True)
    Public Sub New()
        Interval = 1000
    End Sub
    Private Sub RevengeScript_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        If PlayerPed.IsDead Then
            Dim attaker As Ped = IdentifyAttaker(PlayerPed.Killer)
            If attaker IsNot Nothing Then
                EntityManagement.AddController(New CityzenController(attaker))
            End If
        Else
            Dim records As EntityDamageRecord() = PlayerPed.DamageRecords.GetAllDamageRecords()
            For Each record As EntityDamageRecord In records
                Dim attacker As Ped = IdentifyAttaker(record.Attacker)
                If attacker IsNot Nothing Then
                    EntityManagement.AddController(New CityzenController(attacker))
                End If
            Next
        End If
    End Sub
    Private Function IdentifyAttaker(killer_entity As Entity) As Ped
        If killer_entity IsNot Nothing Then
            Dim actual_killer As Ped
            If TypeOf killer_entity Is Vehicle Then
                actual_killer = CType(killer_entity, Vehicle).Driver
            ElseIf TypeOf killer_entity Is Ped Then
                actual_killer = killer_entity
            Else
                Return Nothing
            End If
            If actual_killer IsNot Nothing AndAlso actual_killer.IsAlive AndAlso Not actual_killer.IsPlayer AndAlso Not EntityManagement.ContainsEntity(actual_killer) Then
                Return actual_killer
            Else
                Return Nothing
            End If
        Else
            Return Nothing
        End If
    End Function
    Private Class CityzenController
        Implements IEntityController
        Private ReadOnly ped As Ped

        Public Sub New(ped As Ped)
            ped.AddBlip.Scale = 0.7
            Me.ped = ped
            ped.SetIsPersistentNoClearTask(True)
        End Sub

        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return ped
            End Get
        End Property

        Public Sub OnTick() Implements IEntityController.OnTick
        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing
            ped.SetIsPersistentNoClearTask(False)
            ped.IsPersistent = False
        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            Return ped.IsDead
        End Function
    End Class
End Class
