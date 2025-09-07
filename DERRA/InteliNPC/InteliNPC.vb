Imports System.IO
Imports System.Reflection
Imports DERRA.Structs
Imports DERRA.Tasking
Imports GTA
Imports GTA.Math
Imports GTA.UI

Namespace InteliNPC
    Public Class VehicleCollection
        Private ReadOnly dict As Dictionary(Of VehicleHash, Boolean) = New Dictionary(Of VehicleHash, Boolean)()
        ''' <summary>
        ''' 初始化<see cref="VehicleCollection"/>类的新实例。
        ''' </summary>
        Public Sub New()
        End Sub
        Public ReadOnly Property Count As Integer
            Get
                Return dict.Count
            End Get
        End Property
        Public Sub CloneTo(other As VehicleCollection)
            other.dict.Clear()
            For Each item In dict
                other.dict.Add(item.Key, item.Value)
            Next
        End Sub
        Public Function Add(model As VehicleHash, is_weaponized As Boolean) As Boolean
            If dict.ContainsKey(model) Then
                Return False
            Else
                dict.Add(model, is_weaponized)
                Return True
            End If
        End Function
        Public Function GetRandomVehicleModel(must_be_weapon As Boolean) As VehicleHash?
            Dim choices As List(Of VehicleHash) = New List(Of VehicleHash)()
            For Each model As VehicleHash In dict.Keys
                If must_be_weapon Then
                    If dict.Item(model) Then
                        choices.Add(model)
                    End If
                Else
                    choices.Add(model)
                End If
            Next
            If choices.Count = 0 Then
                If dict.Count > 0 Then
                    Return Pick(dict.Keys.ToArray())
                Else
                    Return Nothing
                End If

            Else
                Return Pick(choices)
            End If
        End Function
        Public Function Contains(hash As VehicleHash) As Boolean
            Return dict.ContainsKey(hash)
        End Function
        Public Function IsWeaponized(hash As VehicleHash) As Boolean
            Return Contains(hash) AndAlso dict.Item(hash)
        End Function
    End Class
    Public Class NpcWeaponCollection
        Private ReadOnly dict As Dictionary(Of WeaponHash, Single) = New Dictionary(Of WeaponHash, Single)()
        Private ReadOnly owner As Ped
        Private ReadOnly m_rocketWeapons As HashSet(Of WeaponHash) = New HashSet(Of WeaponHash)()
        Public Sub New(owner As Ped)
            dict.Add(WeaponHash.Pistol, 9999) '基本上不在载具上是不会选的
            Me.owner = owner
        End Sub
        Public ReadOnly Property Hashes As ICollection(Of WeaponHash)
            Get
                Return dict.Keys
            End Get
        End Property
        Public ReadOnly Property Count As Integer
            Get
                Return dict.Count
            End Get
        End Property
        Public ReadOnly Property KnownRocketWeapons As HashSet(Of WeaponHash)
            Get
                Return m_rocketWeapons
            End Get
        End Property
        Public ReadOnly Property HasRocketWeapon As Boolean
            Get
                For Each key As WeaponHash In dict.Keys
                    If KnownRocketWeapons.Contains(key) Then
                        Return True
                    End If
                Next
                Return False
            End Get
        End Property
        Public Sub SafeRemove(weapon As WeaponHash)
            dict.Remove(weapon)
        End Sub
        Public Sub CloneTo(other As NpcWeaponCollection)
            other.dict.Clear()
            For Each item In dict
                other.dict.Add(item.Key, item.Value)
            Next
            other.m_rocketWeapons.Clear()
            For Each weapon As WeaponHash In m_rocketWeapons
                other.m_rocketWeapons.Add(weapon)
            Next
        End Sub
        Public Function Contains(weaponHash As WeaponHash) As Boolean
            Return dict.ContainsKey(weaponHash)
        End Function
        Public Sub Give(weapon As WeaponHash, best_target_distance As Single)
            owner.Weapons.Give(weapon, 10, False, False)
            dict.Item(weapon) = best_target_distance
        End Sub
        Public Function GetBestWeapon(target_distance As Single, need_rocket_weapon As Boolean) As WeaponHash
            Dim selected As Minimize(Of WeaponHash) = New Minimize(Of WeaponHash)()
            For Each weapon As WeaponHash In dict.Keys
                If HasRocketWeapon AndAlso need_rocket_weapon AndAlso Not KnownRocketWeapons.Contains(weapon) Then
                    Continue For
                End If
                Dim distance_loss As Single = System.Math.Abs(dict.Item(weapon) - target_distance)
                selected.TryNext(distance_loss, weapon)
            Next

            Return selected.Selected
        End Function
    End Class
    Public Structure BlipDisplay
        Public ReadOnly Sprite As BlipSprite
        Public ReadOnly UpdateRotation As Boolean

        Public Sub New(sprite As BlipSprite, updateRotation As Boolean)
            Me.Sprite = sprite
            Me.UpdateRotation = updateRotation
        End Sub
    End Structure
    Public Class VehicleBlipDisplay
        Public Shared ReadOnly Table As Dictionary(Of VehicleHash, BlipDisplay) = New Dictionary(Of VehicleHash, BlipDisplay)()
    End Class
End Namespace

