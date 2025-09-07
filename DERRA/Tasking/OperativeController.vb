Imports GTA

Namespace Tasking
    Public Class OperativeController
        Implements IEntityController
        Private ReadOnly ped As Ped
        Private ReadOnly enermy As Ped
        Public Sub New(ped As Ped, enermy As Ped)
            Me.ped = ped
            Me.enermy = enermy
            ped.Weapons.Give(WeaponHash.SMG, 900, True, True).Components.GetFlashLightComponent().Active = True
        End Sub

        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return ped
            End Get
        End Property

        Public Sub OnTick() Implements IEntityController.OnTick
            If Not ped.IsInCombat Then
                If enermy.IsAlive Then
                    ped.Task.Combat(enermy)
                Else
                    Dim new_enermy As Ped = ThreatDetector.GetNearlistTreat(ped.Position, 50, AddressOf ThreatDetector.IsCommonThreat)
                    If new_enermy IsNot Nothing Then
                        ped.Task.Combat(new_enermy)
                    Else
                        If Not ped.CurrentScriptTaskNameHash = ScriptTaskNameHash.SmartFleePoint Then
                            ped.Task.FleeFrom(ped.Position)
                        End If
                    End If
                End If
            Else
                Dim combat_target As Ped = ped.CombatTarget
                If combat_target IsNot Nothing Then
                    If ped.IsInVehicle() AndAlso combat_target.IsInVehicle() Then
                        If ped.Weapons.Current.Hash <> WeaponHash.CombatPistol Then
                            ped.Weapons.Give(WeaponHash.CombatPistol, 900, True, True).Components.GetFlashLightComponent().Active = True
                        End If
                    ElseIf ped.IsOnFoot Then
                        If ped.Weapons.Current.Hash <> WeaponHash.SMG Then
                            ped.Weapons.Select(WeaponHash.SMG)
                        End If
                    End If
                End If
            End If
        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing
            'ped.Task.ForceMotionState(PedMotionState.Aiming)
        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            If ped.IsDead Then
                Return True
            ElseIf enermy.IsDead AndAlso Not ped.IsInCombat AndAlso Not ThreatDetector.FindThreat(ped.Position, 50, AddressOf ThreatDetector.IsCommonThreat) Then
                If ped.Position.DistanceTo(PlayerPed.Position) > 100 Then
                    Return True
                Else
                    Return False
                End If
            ElseIf enermy.IsAlive AndAlso enermy.IsInFlyingVehicle Then
                Return True
            Else
                Return False
            End If
        End Function
    End Class
End Namespace

