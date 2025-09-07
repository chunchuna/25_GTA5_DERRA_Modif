Imports GTA

Public Module MissionHelper
    Private m_enermyRelationship As RelationshipGroup?
    Private random As Random = New Random()
    Public Function PickEnum(Of T)(ParamArray except As T()) As T
        Dim values As Array = [Enum].GetValues(GetType(T))
        Dim list As List(Of T) = New List(Of T)(values.Length)
        For Each value As T In values
            If Not except.Contains(value) Then
                list.Add(value)
            End If
        Next
        Return Pick(list)
    End Function
    Public Function Pick(Of T)(list As IList(Of T)) As T
        Return list.Item(random.Next(list.Count))
    End Function
    Public Function Pick(max_value As Integer) As Integer
        Return random.Next(max_value + 1)
    End Function
    Public Function Pick(max_value As Integer, min_value As Integer) As Integer
        Return min_value + System.Math.Round((max_value - min_value) * random.NextDouble())
    End Function
    Public ReadOnly Property EnermyRelationshipGroup As RelationshipGroup
        Get
            If Not m_enermyRelationship.HasValue Then
                m_enermyRelationship = World.AddRelationshipGroup("derras_enermy")
                PlayerPed.RelationshipGroup.SetRelationshipBetweenGroups(m_enermyRelationship, Relationship.Hate)
            End If
            Return m_enermyRelationship
        End Get
    End Property
    Public Function CanPlayerSee(entity As Entity) As Boolean
        Return entity.IsOnScreen AndAlso entity.Position.DistanceTo(PlayerPed.Position) < 200
    End Function
    ''' <summary>
    ''' 生成梅里韦瑟打手
    ''' </summary>
    Public Sub DispatchGoons()
        Dim vehicle1 As Vehicle = CreateVehicle(VehicleHash.Baller6, 200)
        For Each seat As VehicleSeat In {VehicleSeat.Driver, VehicleSeat.RightFront, VehicleSeat.LeftRear, VehicleSeat.RightRear}
            Dim cop As Ped = vehicle1.CreatePedOnSeat(seat, PedHash.Blackops03SMY)
            cop.RelationshipGroup = EnermyRelationshipGroup
            cop.CombatAbility = CombatAbility.Professional
            cop.CombatMovement = CombatMovement.WillAdvance
            cop.CombatRange = CombatRange.Near
            cop.Weapons.Give(WeaponHash.MiniSMG, 1000, True, True)
            cop.Weapons.Give(WeaponHash.SpecialCarbine, 1000, True, True)
            Dim controller As EnermyController = New EnermyController(cop, "打手")
            EntityManagement.AddController(controller)
            cop.Task.Combat(PlayerPed)
        Next
        vehicle1.MarkAsNoLongerNeeded()
        Dim vehicle2 As Vehicle = World.CreateVehicle(VehicleHash.Baller6, World.GetNextPositionOnStreet(vehicle1.FrontPosition, True))
        vehicle2.PlaceOnNextStreet()
        For Each seat As VehicleSeat In {VehicleSeat.Driver, VehicleSeat.RightFront, VehicleSeat.LeftRear, VehicleSeat.RightRear}
            Dim cop As Ped = vehicle2.CreatePedOnSeat(seat, PedHash.Blackops03SMY)
            cop.Style.RandomizeOutfit()
            cop.Style.RandomizeProps()
            cop.RelationshipGroup = EnermyRelationshipGroup
            cop.CombatAbility = CombatAbility.Average
            cop.CombatMovement = CombatMovement.WillAdvance
            cop.CombatRange = CombatRange.Near
            cop.Weapons.Give(WeaponHash.SpecialCarbine, 1000, True, True)
            cop.Armor = 200
            Dim controller As EnermyController = New EnermyController(cop, "打手")
            EntityManagement.AddController(controller)
            cop.Task.Combat(PlayerPed)
        Next
        vehicle2.MarkAsNoLongerNeeded()
    End Sub
    ''' <summary>
    ''' 刷出来的敌人控制器，拥有不会消失。
    ''' </summary>
    Public Class EnermyController
        Implements IEntityController
        Private ReadOnly enermy As Ped
        Private ReadOnly blipName As String

        Public Sub New(enermy As Ped, blipName As String)
            Me.enermy = enermy
            enermy.IsEnemy = True
            Me.blipName = blipName
        End Sub
        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return enermy
            End Get
        End Property

        Public Sub OnTick() Implements IEntityController.OnTick
            If enermy.AttachedBlip Is Nothing Then
                With enermy.AddBlip()
                    .IsFriendly = False
                    .Scale = 0.7
                    .Name = blipName
                End With
            End If
            If Not enermy.IsInCombat Then
                enermy.Task.Combat(PlayerPed, taskThreatResponseFlags:=TaskThreatResponseFlags.CanFightArmedPedsWhenNotArmed)
            End If
        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing
        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            If enermy.IsDead OrElse PlayerPed.IsDead Then
                Return True
            Else
                Return False
            End If
        End Function
    End Class
End Module
