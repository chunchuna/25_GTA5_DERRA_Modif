Imports GTA
Imports GTA.Math
Imports GTA.Native

Namespace Interactive.Horro
    Public Class ZombieController
        Implements IEntityController
        Private ReadOnly ped As Ped
        Public Sub New(ped As Ped)
            Me.ped = ped
            SetAttributes()

            'ped.MarkAsMissionEntity(True)
        End Sub
        Public ReadOnly Property Target2 As Entity Implements IEntityController.Target
            Get
                Return ped
            End Get
        End Property
        Private Sub SetAttributes()
            [Function].Call(Hash.APPLY_PED_DAMAGE_PACK, ped, "BigHitByVehicle", 0.0F, 1.0F)
            [Function].Call(Hash.APPLY_PED_DAMAGE_PACK, ped, "SCR_Torture", 0.0F, 1.0F)
            [Function].Call(Hash.APPLY_PED_DAMAGE_PACK, ped, "Explosion_Med", 0.0F, 1.0F)
            If Not [Function].Call(Of Boolean)(Hash.HAS_CLIP_SET_LOADED, "move_m@drunk@verydrunk") Then
                [Function].Call(Hash.REQUEST_CLIP_SET, "move_m@drunk@verydrunk")
            Else
                [Function].Call(Hash.SET_PED_MOVEMENT_CLIPSET, ped, "move_m@drunk@verydrunk", 1.0F)
            End If
            '[Function].Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, ped, 0.3F)
            '[Function].Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, ped.Handle, 0.3F)
            ped.Weapons.RemoveAll()
            [Function].Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped, 0, False)
            [Function].Call(Hash.SET_PED_CAN_RAGDOLL, ped, True)
            [Function].Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, ped, True)
            [Function].Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped, True)
            [Function].Call(Hash.SET_PED_CAN_EVASIVE_DIVE, ped, False)
            [Function].Call(Hash.SET_PED_SHOOT_RATE, ped, 1000)
            [Function].Call(Hash.SET_PED_SEEING_RANGE, ped, 1000.0F)
            [Function].Call(Hash.SET_PED_HEARING_RANGE, ped, 1000.0F)
            ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, False)
            ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, True)
            ped.SetConfigFlag(PedConfigFlagToggles.RunFromFiresAndExplosions, False)
            ped.SetConfigFlag(PedConfigFlagToggles.DisableExplosionReactions, True)
            ped.SetConfigFlag(PedConfigFlagToggles.DisableHurt, True)
            ped.SetCombatAttribute(CombatAttributes.DisableBulletReactions, True)
            ped.SetCombatAttribute(CombatAttributes.DisableInjuredOnGround, True)

            [Function].Call(Hash.STOP_PED_SPEAKING, ped, True)
            [Function].Call(Hash.DISABLE_PED_PAIN_AUDIO, ped, True)
        End Sub
        Public Sub OnTick() Implements IEntityController.OnTick
            SetAttributes()
            ped.Health = 99999
            Dim playerPed As Ped = Sys.PlayerPed
            Dim distance As Single = ped.Position.DistanceTo(playerPed.Position)
            If ped.HasClearLineOfSightToInFront(playerPed) OrElse distance < 50 Then '发现玩
                If ped.CurrentScriptTaskNameHash <> ScriptTaskNameHash.FollowToOffsetOfEntity Then
                    ped.Task.FollowToOffsetFromEntity(playerPed, Vector3.Zero, Pick({0.3, 1, 2}), distanceToFollow:=0)
                End If
                If Not ped.IsRagdoll AndAlso Not ped.IsGettingUp AndAlso Not ped.IsClimbing AndAlso Not ped.IsFleeing Then
                    If distance < 1.2 Then
                        If Not playerPed.IsGettingUp AndAlso Not playerPed.IsRagdoll Then
                            playerPed.ApplyDamage(15)
                            [Function].Call(Hash.SET_PED_TO_RAGDOLL, playerPed, 1, 9000, 9000, 1, 1, 1)
                            [Function].Call(Hash.SET_PED_TO_RAGDOLL, ped, 1, 9000, 9000, 1, 1, 1)
                            Dim direction As Vector3 = playerPed.Position - ped.Position
                            ped.ApplyForceRelative(direction.Above(-30))
                            playerPed.ApplyForceRelative(direction.Above(-30))
                        End If
                    End If
                End If

            Else
                If ped.CurrentScriptTaskNameHash <> ScriptTaskNameHash.WanderStandard Then
                    ped.Task.Wander()
                End If
            End If
        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing
            ped.Kill()
        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            Return ped.IsDead
        End Function
    End Class
End Namespace

