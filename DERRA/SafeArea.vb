'Imports DERRA.Structs
'Imports DERRA.Tasking
'Imports DERRA.Tasking.Hunting
'Imports GTA
'Imports GTA.Math
'Imports GTA.Native
'Imports GTA.UI

'Public Class SafeArea
'    Inherits Script
'    Public Const DefendRadius As Single = 100
'    Private Shared ReadOnly location As Vector3 = New Vector3(-1123.922, 370.3236, 70.74055)
'    Private ReadOnly blip As Blip
'    Private ReadOnly guards_on_duty As EntityCollection(Of Ped) = New EntityCollection(Of Ped)()
'    Private ReadOnly air_defence_service As EntityCollection(Of Ped) = New EntityCollection(Of Ped)()
'    Private ReadOnly guard_relationship As Lazy(Of RelationshipGroup) = New Lazy(Of RelationshipGroup)(Function()
'                                                                                                           Dim r As RelationshipGroup = World.AddRelationshipGroup("script_guard")
'                                                                                                           r.SetRelationshipBetweenGroups(PlayerPed.RelationshipGroup, Relationship.Respect)
'                                                                                                           Return r
'                                                                                                       End Function)
'    Public Sub New()
'        '初始化安全区
'        Dim blip As Blip = World.CreateBlip(location)
'        blip.Sprite = BlipSprite.KingOfTheCastle
'        'blip.CategoryType = BlipCategoryType.OwnedProperty
'        blip.Name = World.GetZoneLocalizedName(location) + " 安全区"
'        'blip.DisplayType = BlipDisplayType.BothMapNoSelectable
'        Interval = 1000
'    End Sub

'    Private Sub SafeHouse_Tick(sender As Object, e As EventArgs) Handles Me.Tick
'        guards_on_duty.Keep(KeepRule.MustAlive)
'        air_defence_service.Keep(KeepRule.MustAlive)
'        If PlayerPed.Position.DistanceTo(location) < DefendRadius + 50 Then
'            PlayerPed.Health = Clamp(PlayerPed.Health + 10, 0, PlayerPed.MaxHealth)
'            Dim count As Integer = guards_on_duty.Count
'            If guards_on_duty.Count < 4 Then
'                TryCreateStandingGuard()
'            End If
'            If air_defence_service.Count < 1 Then
'                If World.GetClosestVehicle(location, 5) Is Nothing AndAlso PlayerPed.Position.DistanceTo(location) > 100 Then
'                    Dim defender As Vehicle = World.CreateVehicle(VehicleHash.TrailerSmall2, location)
'                    defender.AddBlip().Sprite = BlipSprite.WeaponizedTrailer
'                    defender.PlaceOnGround()
'                    Dim ped As Ped = defender.CreatePedOnSeat(VehicleSeat.Driver, PedHash.Juggernaut01M)
'                    ped.RelationshipGroup = guard_relationship.Value
'                    ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, False)
'                    EntityManagement.AddController(New AntiAircraftController(ped))
'                    air_defence_service.Add(ped)
'                    defender.MarkAsNoLongerNeeded()
'                End If
'            End If
'        End If
'        For Each ped As Ped In World.GetNearbyPeds(location, DefendRadius)
'            If ped.IsAlive AndAlso ThreatDetector.IsCommonThreat(ped) Then
'                If ped.IsOnFoot Then
'                    World.ShootSingleBullet(ped.AbovePosition, ped.Position, 9999, WeaponHash.StunGun)
'                End If
'            End If
'        Next
'    End Sub
'    Private Sub TryCreateStandingGuard()
'        Dim position As Vector3 = World.GetNextPositionOnSidewalk(location.Around(DefendRadius / 2))
'        Dim guard As Ped = World.CreatePed(PedHash.FreemodeMale01, position, Vector3.RandomXY().ToHeading)
'        If PlayerPed.HasClearLineOfSightToAdjustForCover(guard, IntersectFlags.Everything) AndAlso guard.IsOnScreen Then
'            guard.Delete()
'        Else
'            RedHat(guard)
'            guard.Health = 200
'            guard.Armor = 200
'            'guard.Weapons.Give(WeaponHash.ServiceCarbine, 100, True, True)
'            guard.CombatRange = CombatRange.Near
'            guard.FiringPattern = FiringPattern.FullAuto
'            guard.RelationshipGroup = guard_relationship.Value
'            guard.SetCombatFloatAttribute(CombatFloatAttributes.WeaponDamageModifier, 1000)
'            With guard.AddBlip()
'                .IsFriendly = True
'                .Scale = 0.7
'                .Name = "警卫"
'                .IsShortRange = True
'                .DisplayType = BlipDisplayType.MiniMapOnly
'            End With
'            Dim controller As GuardController = New GuardController(guard)
'            EntityManagement.AddController(controller)
'            guards_on_duty.Add(guard)
'            guard.Task.GuardCurrentPosition()
'            guard.Weapons.Give(WeaponHash.ServiceCarbine, 9000, True, True)
'            guard.Task.AimGunAtPosition(guard.FrontPosition, 2000)
'        End If
'    End Sub
'    Private Sub SafeHouse_Aborted(sender As Object, e As EventArgs) Handles Me.Aborted
'        blip?.Delete()
'    End Sub
'    Private Class AntiAircraftController
'        Implements IEntityController
'        Private ReadOnly ped As Ped

'        Public Sub New(ped As Ped)
'            Me.ped = ped
'        End Sub

'        Public ReadOnly Property Target As Entity Implements IEntityController.Target
'            Get
'                Return ped
'            End Get
'        End Property

'        Public Sub OnTick() Implements IEntityController.OnTick
'            If Not ped.IsInCombat OrElse (ped.CombatTarget?.IsDead).GetValueOrDefault(False) Then
'                Dim enermy As Ped = ThreatDetector.GetNearlistTreat(ped.Position, 150, AddressOf IsThreat)
'                If enermy IsNot Nothing Then
'                    ped.Task.Combat(enermy)
'                End If
'            End If
'        End Sub
'        Public Function IsThreat(ped As Ped) As Boolean
'            Return ThreatDetector.IsCommonThreat(ped) AndAlso Me.ped.HasClearLineOfSightTo(ped)
'        End Function
'        Public Sub Disposing() Implements IEntityController.Disposing
'            Notification.PostTicker("一辆防空拖车被摧毁", False, False)
'        End Sub

'        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
'            Return ped.IsDead
'        End Function
'    End Class
'    Private Class GuardController
'        Implements IEntityController
'        Private ReadOnly guard As Ped

'        Public Sub New(guard As Ped)
'            Me.guard = guard
'        End Sub

'        Public ReadOnly Property Target As Entity Implements IEntityController.Target
'            Get
'                Return guard
'            End Get
'        End Property

'        Public Sub OnTick() Implements IEntityController.OnTick
'            If Not guard.IsInCombat OrElse (guard.CombatTarget?.IsDead).GetValueOrDefault(False) Then
'                Dim enermy As Ped = ThreatDetector.GetNearlistTreat(guard.Position, 50, AddressOf ThreatDetector.IsCommonThreat)
'                If enermy IsNot Nothing Then
'                    guard.Task.Combat(enermy)
'                Else

'                    If guard.CurrentScriptTaskNameHash <> ScriptTaskNameHash.GuardCurrentPosition Then
'                        guard.Task.GuardCurrentPosition()
'                    End If
'                End If
'            End If

'        End Sub

'        Public Sub Disposing() Implements IEntityController.Disposing
'            Notification.PostTicker("警卫" + Hex(guard.Handle) + " 已死亡", False, False)
'        End Sub

'        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
'            If guard.IsDead OrElse guard.Position.DistanceTo(location) > DefendRadius + 30 Then
'                Return True
'            Else
'                Return False
'            End If
'        End Function
'    End Class
'End Class
