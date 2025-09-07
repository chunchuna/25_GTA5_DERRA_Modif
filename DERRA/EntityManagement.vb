Imports System.IO
Imports GTA
Imports GTA.UI
''' <summary>
''' <see cref="Entity"/>管理器。
''' 不要在<see cref="EntityManagement"/>外部调用<see cref="Entity.Delete()"/>或<see cref="Entity.MarkAsNoLongerNeeded()"/>。
''' </summary>
Public Class EntityManagement
    Inherits Script
    Private Shared ReadOnly list As List(Of IEntityController) = New List(Of IEntityController)()
    Public Shared DebugInfo As String = ""
    Private Shared is_scanning As Boolean = False
    Private Shared current_task As String
    ''' <summary>
    ''' 获取或设置一个<see cref="Boolean"/>值，指示<see cref="Tick"/>是否暂停处理<see cref="Controllers"/>。
    ''' </summary>
    Public Shared Property PauseProcessing As Boolean = False
    Public Sub New()
        Interval = 2000
    End Sub
    Private Shared Sub WaitForScanningCompeleted()
        While is_scanning
            Wait(10)
            UI.Screen.ShowSubtitle("~g~" + NameOf(EntityManagement) + "~w~类型正在处理~o~Tick()~w~事件:" + current_task, 1000)
        End While
    End Sub
    Public Shared Function ContainsEntity(entity As Entity) As Boolean
        For Each controller As IEntityController In Controllers
            If controller.Target = entity Then
                Return True
            End If
        Next

        Return False
    End Function
    Public Shared ReadOnly Property Controllers As IEntityController()
        Get
            WaitForScanningCompeleted()
            Return list.ToArray()
        End Get
    End Property
    Public Shared Sub AddController(controller As IEntityController)
        WaitForScanningCompeleted()
        list.Add(controller)
    End Sub
    ''' <summary>
    ''' 释放所以托管的资源。
    ''' </summary>
    Public Shared Sub Reset()
        WaitForScanningCompeleted()
        ResetImediately()
    End Sub
    Private Shared Sub ResetImediately()
        For Each controller As IEntityController In list.ToArray()
            controller.Disposing()
            If controller.Target.AttachedBlip IsNot Nothing AndAlso controller.Target.AttachedBlip.Exists() Then
                controller.Target.AttachedBlip.Delete()
            End If
            controller.Target.MarkAsNoLongerNeeded()
        Next
        list.Clear()
    End Sub
    Private Sub PedManagement_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        If PauseProcessing OrElse Game.IsPaused Then
            Return
        End If
        is_scanning = True
        Dim array As IEntityController() = list.ToArray()
        is_scanning = False
        For Each controller As IEntityController In array
            Dim can_dispose As Boolean
            Try
                current_task = "获取 " + TypeName(controller) + ".CanDispose()"
                can_dispose = controller.CanDispose()
            Catch ex As Exception
                can_dispose = False
                Notification.PostTicker("~y~CanDispose()~w~异常 " + GetErrorInfo(ex), True)
            End Try
            If can_dispose Then
                current_task = "执行 " + NameOf(DisposeController) + "(controller)"
                DisposeController(controller)
            Else
                Try
                    current_task = "执行 " + TypeName(controller) + ".OnTick()"
                    controller.OnTick()
                Catch ex As Exception
                    Notification.PostTicker("~y~OnTick()~w~异常 " + GetErrorInfo(ex), True)
                    DisposeController(controller)
                End Try
            End If
            current_task = "准备下一个循环"
        Next
        is_scanning = False
    End Sub
    Private Sub DisposeController(controller As IEntityController)
        controller.Disposing()
        Try
            For Each blip As Blip In controller.Target.AttachedBlips
                blip.Delete()
            Next
            controller.Target.MarkAsNoLongerNeeded()
            is_scanning = True
            list.Remove(controller)
            is_scanning = False
        Catch ex As Exception
            Notification.PostTicker($"释放{TypeName(controller)}时发生异常 " + GetErrorInfo(ex), True)
        End Try

    End Sub
    Private Function GetErrorInfo(ex As Exception) As String
        Return $"~r~{TypeName(ex)}:{ex.Message}{vbNewLine}{ExtractCodeFileInfoFromStackTrace(ex.StackTrace)}"
    End Function
    Public Shared Sub AddBodyGuard(ped As Ped, Optional name As String = "DERRA安保人员")
        Dim controller As BodyGuardController = New BodyGuardController(ped, name)
        AddController(controller)
    End Sub
    Public Shared Sub AddTempAgent(ped As Ped, Optional name As String = "DERRA士兵")
        ped.SetCombatAttribute(CombatAttributes.CanInvestigate, True)
        ped.SetCombatAttribute(CombatAttributes.CanThrowSmokeGrenade, True)
        'ped.SetCombatAttribute(CombatAttributes.DisableInjuredOnGround, True)
        ped.SetCombatAttribute(CombatAttributes.HasRadio, True)
        ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, True)
        ped.SetCombatAttribute(CombatAttributes.UseMaxSenseRangeWhenReceivingEvents, True)
        ped.SetCombatAttribute(CombatAttributes.WillGenerateDeadPedSeenScriptEvents, True)
        Dim controller As AgentController = New AgentController(ped, name)
        AddController(controller)
    End Sub
    Public Shared Sub AddAgentWithBlipAndWeapon(ped As Ped, weapon As WeaponHash)
        ped.Weapons.Give(weapon, 1000, True, True)
        'AddBodyGuard(ped)
        AddTempAgent(ped)
    End Sub
    Public Shared Sub AddPersonalVehicle(vehicle As Vehicle, Optional sprite As BlipSprite = BlipSprite.PersonalVehicleCar, Optional name As String = Nothing)
        vehicle.AddBlip().Sprite = sprite
        If name IsNot Nothing Then
            vehicle.AttachedBlip.Name = name
        End If
        Dim controller As PersonalVehicleController = New PersonalVehicleController(vehicle)
        AddController(controller)
    End Sub

    Private Sub EntityManagement_Aborted(sender As Object, e As EventArgs) Handles Me.Aborted
        Reset()
    End Sub

    ''' <summary>
    ''' 突击队控制器。
    ''' </summary>
    Private Class AgentController
        Implements IEntityController
        Private ReadOnly m_target As Ped
        Private ReadOnly name As String
        Public Sub New(ped As Ped, name As String)
            m_target = ped
            Me.name = name
            ped.SetConfigFlag(PedConfigFlagToggles.ForcedAim, True)
        End Sub
        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return m_target
            End Get
        End Property

        Public Sub OnTick() Implements IEntityController.OnTick
            If PlayerPed.IsInVehicle() AndAlso m_target.IsInVehicle(PlayerPed.CurrentVehicle) Then
                '删除图标
                If m_target.AttachedBlip IsNot Nothing AndAlso m_target.AttachedBlip.Exists() Then
                    m_target.AttachedBlip.Delete()
                End If
            Else
                '如果没有就创建一个图标
                If m_target.AttachedBlip Is Nothing OrElse Not m_target.AttachedBlip.Exists() Then
                    With m_target.AddBlip()
                        .Color = BlipColor.White
                        .Scale = 0.7
                        .Name = name
                    End With
                End If
            End If
            If m_target.IsAlive AndAlso Not m_target.IsInGroup Then
                If Not m_target.IsInVehicle() Then
                    If Not m_target.IsInCombat Then
                        Dim target_found As Boolean = False
                        For Each ped As Ped In World.GetNearbyPeds(Target, 30)
                            If ped.IsAlive AndAlso ped.RelationshipGroup <> m_target.RelationshipGroup Then
                                If ped.RelationshipGroup.GetRelationshipBetweenGroups(m_target.RelationshipGroup) = Relationship.Hate AndAlso ped.IsHuman Then
                                    m_target.Task.Combat(ped)
                                    target_found = True
                                    Exit For
                                ElseIf ped.RelationshipGroup.GetRelationshipBetweenGroups(m_target.RelationshipGroup) = Relationship.Dislike AndAlso ped.IsHuman Then
                                    m_target.Task.Combat(ped)
                                    target_found = True
                                    Exit For
                                ElseIf ped.IsInCombat Then
                                    m_target.Task.Combat(ped)
                                    target_found = True
                                    Exit For
                                End If
                            End If
                        Next
                        If Not target_found Then
                            If Not m_target.IsWalking Then
                                m_target.Weapons.Select(m_target.Weapons.BestWeapon)
                                m_target.Task.WanderAround(m_target.Position, 10)
                            End If
                        End If
                    Else 'this is in combat


                    End If
                Else
                    If m_target.CurrentVehicle.Driver Is Nothing Then
                        m_target.Task.LeaveVehicle()
                    End If
                End If


            End If

        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing

        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            If Target.IsDead Then
                Notification.PostTicker("编号" + CStr(Target.Handle) + " " + name + " ~r~已死亡", True, False)
                Return True
            ElseIf Target.Position.DistanceTo(PlayerPed.Position) > 200 AndAlso Not m_target.IsInVehicle() Then
                Notification.PostTicker("编号" + CStr(Target.Handle) + " " + name + " 已解散", True, False)
                Return True
            Else
                Return False
            End If
        End Function
    End Class
    ''' <summary>
    ''' 保镖控制器
    ''' </summary>
    Private Class BodyGuardController
        Implements IEntityController
        Private ReadOnly m_target As Ped
        Private ReadOnly name As String
        Public Sub New(ped As Ped, name As String)
            m_target = ped
            Me.name = name
        End Sub
        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return m_target
            End Get
        End Property

        Public Sub OnTick() Implements IEntityController.OnTick
            If PlayerPed.IsInVehicle() AndAlso m_target.IsInVehicle(PlayerPed.CurrentVehicle) Then
                '删除图标
                If m_target.AttachedBlip IsNot Nothing AndAlso m_target.AttachedBlip.Exists() Then
                    m_target.AttachedBlip.Delete()
                End If
            Else
                '如果没有就创建一个图标
                If m_target.AttachedBlip Is Nothing OrElse Not m_target.AttachedBlip.Exists() Then
                    With m_target.AddBlip()
                        .IsFriendly = True
                        .Scale = 0.7
                        .Name = name
                    End With
                End If
            End If
            If m_target.IsAlive AndAlso Not m_target.IsInGroup AndAlso Not m_target.IsInCombat AndAlso Not m_target.IsInVehicle() Then
                For Each ped As Ped In World.GetNearbyPeds(Target, 30)
                    If ped.IsAlive AndAlso ped.RelationshipGroup <> m_target.RelationshipGroup Then
                        If ped.RelationshipGroup.GetRelationshipBetweenGroups(m_target.RelationshipGroup) = Relationship.Hate AndAlso ped.IsHuman Then
                            m_target.Task.Combat(ped)
                        ElseIf ped.RelationshipGroup.GetRelationshipBetweenGroups(m_target.RelationshipGroup) = Relationship.Dislike AndAlso ped.IsHuman Then
                            m_target.Task.Combat(ped)
                        ElseIf ped.IsInCombat Then
                            m_target.Task.Combat(ped)
                        End If
                    End If
                Next
            End If

        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing

        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            If Target.IsDead Then
                Notification.PostTicker("编号" + CStr(Target.Handle) + " " + name + " 已死亡", True, False)
                Return True
            Else
                Return False
            End If
        End Function
    End Class

    Private Class PersonalVehicleController
        Implements IEntityController
        Private ReadOnly vehicle As Vehicle
        Public Sub New(vehicle As Vehicle)
            Me.vehicle = vehicle
        End Sub
        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return vehicle
            End Get
        End Property

        Public Sub OnTick() Implements IEntityController.OnTick
            If vehicle.Exists() Then
                If Game.Player.Character.IsInVehicle(vehicle) Then
                    '删除图标
                    If vehicle.AttachedBlip IsNot Nothing AndAlso vehicle.AttachedBlip.Exists() Then
                        vehicle.AttachedBlip.DisplayType = BlipDisplayType.NoDisplay
                    End If
                Else
                    '如果没有就创建一个图标
                    If vehicle.AttachedBlip Is Nothing AndAlso vehicle.AttachedBlip.Exists() Then
                        vehicle.AttachedBlip.DisplayType = BlipDisplayType.Default
                    End If
                End If
            End If
        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing

        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            Dim distance As Single = vehicle.Position.DistanceTo(PlayerPed.Position)
            If vehicle.IsDead Then
                Screen.ShowHelpText("您的个人载具已被摧毁")
                Return True
            ElseIf distance > 300 Then
                Screen.ShowHelpText($"您的个人载具距离当前位置太远（{System.Math.Round(distance)}m），已收回")
                Return True
            Else
                Return False
            End If
        End Function
    End Class
End Class
