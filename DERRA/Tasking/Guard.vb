Imports DERRA.Tasking
Imports GTA
Imports GTA.Math
Imports GTA.UI

Public Class Guard
    Implements IEntityController
    Private ReadOnly guard As Ped
    Private ReadOnly guarding_position As Vector3
    Private ReadOnly area_blip As Blip
    Private ReadOnly area_name As String
    Private ReadOnly vehicle As Vehicle
    Public Sub New(guard As Ped, vehicle As Vehicle)
        Me.guard = guard
        Me.vehicle = vehicle
        guarding_position = guard.Position
        area_name = World.GetZoneLocalizedName(guarding_position) + ", " + World.GetStreetName(guarding_position)
        area_blip = World.CreateBlip(guarding_position, 150)
        area_blip.Alpha = 100
        area_blip.Name = area_name + " 安全区"
        With guard.AddBlip
            .Name = area_name + " 的守卫"
            .IsFriendly = True
            .IsFlashing = True
            .Scale = 0.7
            .Sprite = BlipSprite.RCTank
            .ShowsHeadingIndicator = True
        End With
    End Sub
    Public Shared Sub Deploy()
        Dim guading_position As Vector3 = GetNextPositionOnStreet()
        Dim tank As Vehicle = World.CreateVehicle(VehicleHash.MiniTank, guading_position)
        Dim guard As Ped = tank.CreatePedOnSeat(VehicleSeat.Driver, PedHash.Agent)
        guard.RelationshipGroup = PrivateSecurityController.SecurityRelationshipGroup
        Dim controller As Guard = New Guard(guard, tank)
        EntityManagement.AddController(controller)
    End Sub
    Public ReadOnly Property Target As Entity Implements IEntityController.Target
        Get
            Return guard
        End Get
    End Property

    Public Sub OnTick() Implements IEntityController.OnTick
        If Not guard.IsInCombat Then
            Dim enermy As Ped = ThreatDetector.GetNearlistTreat(guarding_position, 150, AddressOf ThreatDetector.IsCommonThreat)
            If enermy IsNot Nothing Then
                guard.Task.Combat(enermy)
                Notification.PostTicker(area_name + " ~o~发现入侵者", True)
            Else
                guard.Task.CruiseWithVehicle(vehicle, 10, VehicleDrivingFlags.DrivingModeAvoidVehiclesStopForPedsObeyLights)
            End If
        End If

    End Sub

    Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
        If guard.IsDead Then
            Disposing()
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub Disposing() Implements IEntityController.Disposing
        vehicle.MarkAsNoLongerNeeded()
        area_blip.Delete()
        Notification.PostTicker(area_name + " ~r~已陷落", True)
    End Sub
End Class
