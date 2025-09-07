Imports System.IO
Imports System.Runtime.CompilerServices
Imports GTA
Imports GTA.Graphics
Imports GTA.Math
Imports GTA.Native

Public Module Sys
    <Extension()>
    Public Function Above(location As Vector3, distance As Single) As Vector3
        Return New Vector3(location.X, location.Y, location.Z + distance)
    End Function
    Public Sub Pass()
        Script.Yield()
    End Sub
    ''' <summary>
    ''' 获取当前玩家<see cref="Ped"/>。
    ''' </summary>
    ''' <returns>当前玩家<see cref="Ped"/>。</returns>
    Public ReadOnly Property PlayerPed As Ped
        Get
            Return Game.Player.Character
        End Get
    End Property
    Public Function GetNextPositionOnStreet() As Vector3
        Return World.GetNextPositionOnStreet(PlayerPed.Position, True)
    End Function
    Public Function CreateVehicle(model As Model?, distance As Single, Optional center As Ped = Nothing) As Vehicle
        Dim location As Vector3 = World.GetNextPositionOnStreet(If(center, Game.LocalPlayerPed).Position.Around(distance))
        Dim vehicle As Vehicle
        If model.HasValue Then
            vehicle = World.CreateVehicle(model, location)
        Else
            vehicle = World.CreateRandomVehicle(location)
        End If
        vehicle.PlaceOnNextStreet()
        Return vehicle
    End Function
    Public Sub GotoPosition(location As Vector3, Optional min_distance As Single = 10)
        Dim blip As Blip = World.CreateBlip(location)
        blip.ShowRoute = True
        Dim street As String = World.GetZoneLocalizedName(location)
        While PlayerPed.Position.DistanceTo(location) > min_distance
            UI.Screen.ShowSubtitle("前往~y~" + street, 1000)
            Script.Wait(1000)
        End While
        blip.Delete()
    End Sub
    ''' <summary>
    ''' 从调用堆栈中提取代码文件的位置信息。
    ''' </summary>
    ''' <param name="str">调用堆栈。</param>
    ''' <returns>代码文件位置信息。</returns>
    Public Function ExtractCodeFileInfoFromStackTrace(str As String) As String
        Dim reader As StringReader = New StringReader(str)
        While True
            Dim line As String = reader.ReadLine()
            If line Is Nothing Then
                Return Nothing
            End If
            If line.Contains(":\") OrElse line.Contains(":/") Then
                Return line
            End If
        End While
        Return Nothing
    End Function
    Public Function GetPedFace(ped As Ped) As PedFace
        Dim id As Integer = [Function].Call(Of Integer)(Hash.REGISTER_PEDHEADSHOT, ped)
        While Not [Function].Call(Of Boolean)(Hash.IS_PEDHEADSHOT_READY, id) OrElse Not [Function].Call(Of Boolean)(Hash.IS_PEDHEADSHOT_VALID, id)
            Script.Wait(1)
        End While
        Dim face As String = [Function].Call(Of String)(Hash.GET_PEDHEADSHOT_TXD_STRING, id)
        Dim asset As TextureAsset = New TextureAsset(face, face)
        Return New PedFace(id, asset)
    End Function
    Public Function IsInSameVehicle(x As Ped, y As Ped) As Boolean
        Dim v_x As Vehicle = x.CurrentVehicle, v_y As Vehicle = y.CurrentVehicle
        Return v_x IsNot Nothing AndAlso v_y IsNot Nothing AndAlso v_x = v_y
    End Function
    ''' <summary>
    ''' 随机修改载具武器。
    ''' </summary>
    ''' <param name="vehicle">需要修改武器的载具。</param>
    Public Sub RandomlyMotifyVehicleWeapons(vehicle As Vehicle, Optional change_list As IList(Of String) = Nothing)
        vehicle.Mods.InstallModKit()
        For Each part As VehicleModType In {VehicleModType.ArchCover, VehicleModType.RightFender, VehicleModType.Tank, VehicleModType.Roof}
            If vehicle.Mods.Contains(part) Then
                Dim [mod] As VehicleMod = vehicle.Mods.Item(part)
                [mod].Index = Pick([mod].Count - 1)
                change_list?.Add([mod].LocalizedTypeName + " 设置为 " + [mod].LocalizedName)
            End If
        Next
    End Sub
    ''' <summary>
    ''' 控制台快速修改玩家载具武器。
    ''' </summary>
    Public Sub vw()
        Dim v As Vehicle = PlayerPed.CurrentVehicle
        If v IsNot Nothing Then
            Dim change_list As List(Of String) = New List(Of String)()
            RandomlyMotifyVehicleWeapons(v, change_list)
            If change_list.Count = 0 Then
                UI.Screen.ShowSubtitle("当前载具似乎没用任何可用的武器", 5000)
            Else
                UI.Screen.ShowSubtitle(String.Join(",", change_list), 5000)
            End If
        Else
            UI.Screen.ShowSubtitle("玩家没有乘坐载具")
        End If


    End Sub
    Public Class PedFace
        Implements IDisposable
        Private ReadOnly id As Integer
        Public ReadOnly Asset As TextureAsset

        Public Sub New(id As Integer, asset As TextureAsset)
            Me.id = id
            Me.Asset = asset
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            [Function].Call(Hash.UNREGISTER_PEDHEADSHOT, id)
        End Sub
    End Class
End Module
