Imports System.Windows.Forms
Imports DERRA.Tasking
Imports GTA
Imports GTA.Math
Imports GTA.Native

Namespace Movie
    ''' <summary>
    ''' 提供创建演员的方法。
    ''' </summary>
    Public Class Actors
        Public Shared ReadOnly cal As Actors = New Actors()
        ''' <summary>
        ''' 创建一个专业的保镖。
        ''' </summary>
        Public Sub CreateRedHatBodyGuard()
            Game.Player.Money -= 2500
            Dim agent As Ped = World.CreatePed(PedHash.FreemodeMale01, PlayerPed.Position.Around(100).Above(100), PlayerPed.Heading) 'World.CreatePed(PedHash.Highsec02SMM, PlayerPed.Position.Around(1), PlayerPed.Heading)
            agent.Weapons.Give(WeaponHash.SMGMk2, 600, True, True).Components.GetFlashLightComponent().Active = True
            'agent.Weapons.Give(WeaponHash.PumpShotgunMk2, 100, True, True)
            HawkMan(agent)
            agent.OpenParachute()
            agent.Task.ParachuteTo(PlayerPed.Position.Around(5))
            agent.RelationshipGroup = PrivateSecurityController.SecurityRelationshipGroup
            agent.Accuracy = 100
            agent.Armor = 100
            agent.CombatAbility = CombatAbility.Professional
            agent.CombatMovement = CombatMovement.WillAdvance
            agent.FiringPattern = FiringPattern.FullAuto
            agent.TargetLossResponse = TargetLossResponse.SearchForTarget
            agent.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, True)
            agent.SetCombatAttribute(CombatAttributes.CanUseCover, True)
            agent.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, True)
            agent.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, True)
            agent.SetCombatAttribute(CombatAttributes.DisableBulletReactions, True)
            agent.SetCombatAttribute(CombatAttributes.DisableInjuredOnGround, True)
            agent.SetCombatAttribute(CombatAttributes.PerfectAccuracy, True)
            agent.SetCombatAttribute(CombatAttributes.CanUseCover, False)
            agent.SetCombatAttribute(CombatAttributes.CanUseDynamicStrafeDecisions, True)
            agent.Health = 300
            [Function].Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, agent, True)
            [Function].Call(Hash.SET_PED_CONFIG_FLAG, agent, 188, True)
            'agent.IsInvincible = True
            agent.IsPriorityTargetForEnemies = True
            With agent.AddBlip
                .IsFriendly = True
                .Name = "士兵"
                .Scale = 0.7
            End With
            PlayerPed.PedGroup.Add(agent, False)
            EntityManagement.AddController(New PrivateSecurityController(agent) With {.Name = "士兵"})
        End Sub
        Public Model As Ped
        Public OriginalModel As Ped
        ''' <summary>
        ''' 将<see cref="Model"/>设为当前玩家模型。
        ''' </summary>
        Public Sub MarkCloneBodyguard()
            Model?.Delete()
            Model = PlayerPed.Clone(False)
            Model.Position = New Math.Vector3(Model.Position.X, Model.Position.Y, Model.Position.Z + 100)
            Model.IsVisible = False
            Model.IsPositionFrozen = True
            Model.IsInvincible = True
        End Sub
        ''' <summary>
        ''' 将玩家的皮肤转换为<see cref="Model"/>。
        ''' </summary>
        Public Sub ChangePlayerToClone()
            If Model IsNot Nothing Then
                OriginalModel = PlayerPed.Clone(False)
                OriginalModel.Position = New Vector3(OriginalModel.Position.X, OriginalModel.Position.Y, OriginalModel.Position.Z + 200)
                OriginalModel.IsVisible = False
                OriginalModel.IsPositionFrozen = True
                OriginalModel.IsInvincible = True
                Dim weapon As WeaponHash = PlayerPed.Weapons.Current.Hash
                If Game.Player.ChangeModel(Model.Model) Then
                    Model.CloneToTarget(PlayerPed)
                    If weapon = WeaponHash.Unarmed Then
                        PlayerPed.Weapons.Give(WeaponHash.SMGMk2, 300, False, True)
                    Else
                        PlayerPed.Weapons.Give(weapon, 600, True, True)
                    End If
                Else
                    UI.Screen.ShowSubtitle("克隆失败")
                End If
            Else
                UI.Screen.ShowSubtitle("未指定模板")
            End If
        End Sub
        Public Sub RecoverOriginalModel()
            If OriginalModel IsNot Nothing Then
                Game.Player.ChangeModel(OriginalModel.Model)
                OriginalModel.CloneToTarget(PlayerPed)
                OriginalModel = Nothing
            Else
                UI.Screen.ShowSubtitle("无需恢复模型")
            End If
        End Sub
        ''' <summary>
        ''' 创建一个<see cref="Model"/>演员，同步玩家的所有动作。
        ''' </summary>
        Public Sub CreateSyncActorFromModel()
            If Model IsNot Nothing Then
                Dim actor As Ped = Model.Clone(True)
                actor.Position = PlayerPed.Position
                'actor.AttachTo(PlayerPed)
                actor.SetNoCollision(PlayerPed, True)
                PlayerPed.IsVisible = False
                actor.IsVisible = True
                actor.Euphoria.BodyRelax.Start()
                UI.Screen.ShowSubtitle("按T退出")
                While PlayerPed.IsAlive
                    Script.Wait(100)
                    For Each actor_bone As PedBone In actor.Bones
                        Dim player_bone As PedBone = PlayerPed.Bones.Item(actor_bone.Tag)
                        If player_bone.IsValid Then
                            actor_bone.RelativePosition = player_bone.RelativePosition
                            actor_bone.RelativeRotation = player_bone.RelativeRotation
                            actor_bone.RelativeMatrix = player_bone.RelativeMatrix
                            actor_bone.RelativeQuaternion = player_bone.RelativeQuaternion
                        End If
                    Next
                    actor.Position = PlayerPed.Position
                    actor.Heading = PlayerPed.Heading
                    actor.Rotation = PlayerPed.Rotation
                    If Game.IsKeyPressed(Keys.T) Then
                        Exit While
                    End If
                End While
                actor.Delete()
                PlayerPed.IsVisible = True
            End If
        End Sub
        ''' <summary>
        ''' 创建一个模型wei<see cref="Model"/>的保镖。
        ''' </summary>
        Public Sub CreateCloneBodyguard()
            If model IsNot Nothing Then
                Dim agent As Ped = model.Clone(False)
                agent.Position = PlayerPed.Position.Around(2)
                agent.Weapons.Give(WeaponHash.TacticalSMG, 600, True, True).Components.GetFlashLightComponent().Active = True
                'agent.Weapons.Give(WeaponHash.PumpShotgunMk2, 100, True, True)
                agent.RelationshipGroup = PrivateSecurityController.SecurityRelationshipGroup
                agent.Accuracy = 100
                agent.Armor = 100
                agent.CombatAbility = CombatAbility.Professional
                agent.CombatMovement = CombatMovement.WillAdvance
                agent.FiringPattern = FiringPattern.FullAuto
                agent.TargetLossResponse = TargetLossResponse.SearchForTarget
                agent.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, True)
                agent.SetCombatAttribute(CombatAttributes.CanUseCover, True)
                agent.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, True)
                agent.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, True)
                agent.SetCombatAttribute(CombatAttributes.DisableBulletReactions, True)
                agent.SetCombatAttribute(CombatAttributes.DisableInjuredOnGround, True)
                agent.SetCombatAttribute(CombatAttributes.PerfectAccuracy, True)
                agent.SetCombatAttribute(CombatAttributes.CanUseCover, False)
                agent.SetCombatAttribute(CombatAttributes.CanUseDynamicStrafeDecisions, True)
                agent.Health = 300
                agent.IsInvincible = False
                [Function].Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, agent, True)
                [Function].Call(Hash.SET_PED_CONFIG_FLAG, agent, 188, True)
                'agent.IsInvincible = True
                agent.IsPriorityTargetForEnemies = True
                With agent.AddBlip()
                    .IsFriendly = True
                    .Name = "克隆人"
                End With
                EntityManagement.AddController(New PrivateSecurityController(agent) With {.Name = "克隆人"})
            Else
                UI.Screen.ShowSubtitle("未设置模板")
            End If
        End Sub
        ''' <summary>
        ''' 在玩家附近创建龙套保镖。
        ''' </summary>
        Public Sub CreateWeakBodyGuardNearby(style As Action(Of Ped))
            Dim agent As Ped = World.CreatePed(PedHash.FreemodeMale01, PlayerPed.Position.Around(1), PlayerPed.Heading)
            style.Invoke(agent)
            agent.Weapons.Give(WeaponHash.SMG, 600, True, True).Components.GetFlashLightComponent().Active = True
            agent.Weapons.Select(WeaponHash.SMG)
            agent.CanSwitchWeapons = False
            agent.RelationshipGroup = PlayerPed.RelationshipGroup
            agent.Accuracy = 0
            agent.Armor = 100
            agent.CombatAbility = CombatAbility.Professional
            agent.CombatMovement = CombatMovement.WillAdvance
            agent.FiringPattern = FiringPattern.FullAuto
            agent.TargetLossResponse = TargetLossResponse.SearchForTarget
            agent.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, False)
            agent.SetCombatAttribute(CombatAttributes.CanUseCover, False)
            agent.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, True)
            agent.IsPriorityTargetForEnemies = True
            EntityManagement.AddBodyGuard(agent, "鸟人")
        End Sub
        ''' <summary>
        ''' 在玩家附近创建保镖。
        ''' </summary>
        ''' <param name="style">保镖属性配置委托。</param>
        ''' <param name="weapon">保镖武器。</param>
        ''' <param name="name">保镖名称。</param>
        Public Sub CreateWeakGuardNearby(style As Action(Of Ped), weapon As WeaponHash, name As String)
            Dim agent As Ped = World.CreatePed(PedHash.FreemodeMale01, PlayerPed.Position.Around(1), PlayerPed.Heading)
            style.Invoke(agent)
            agent.Weapons.Give(weapon, 600, True, True) '.Components.GetFlashLightComponent().Active = True
            agent.Weapons.Select(weapon)
            'agent.CanSwitchWeapons = False
            agent.RelationshipGroup = PlayerPed.RelationshipGroup
            agent.Accuracy = 0
            agent.Armor = 100
            agent.CombatAbility = CombatAbility.Professional
            agent.CombatMovement = CombatMovement.Stationary
            agent.FiringPattern = FiringPattern.FullAuto
            agent.TargetLossResponse = TargetLossResponse.SearchForTarget
            agent.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, False)
            agent.SetCombatAttribute(CombatAttributes.CanUseCover, False)
            agent.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, True)
            agent.IsPriorityTargetForEnemies = True
            EntityManagement.AddBodyGuard(agent, name)
        End Sub
        ''' <summary>
        ''' 创建精英保镖。
        ''' </summary>
        Public Sub CreateEliteClone()
            Dim agent As Ped = PlayerPed.Clone(True)
            agent.Weapons.Give(PlayerPed.Weapons.Current.Hash, 9999, True, True)
            agent.RelationshipGroup = PlayerPed.RelationshipGroup
            agent.Armor = 9999
            agent.CombatAbility = CombatAbility.Professional
            agent.IsPriorityTargetForEnemies = True
            agent.SetCombatAttribute(CombatAttributes.PerfectAccuracy, True)
            Dim modelName As String = "玩家克隆"
            EntityManagement.AddBodyGuard(agent, modelName)
        End Sub
    End Class
End Namespace

