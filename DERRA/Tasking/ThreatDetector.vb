Imports DERRA.Structs
Imports GTA
Imports GTA.Math

Namespace Tasking
    ''' <summary>
    ''' 用于检测附近的威胁。
    ''' </summary>
    Public Class ThreatDetector
        ''' <summary>
        ''' 是否启用<see cref="IsCommonThreat(Ped)"/>敌我识别。
        ''' </summary>
        Public Shared ReadOnly Detection As Toggle = New Toggle(True)
        ''' <summary>
        ''' 获取一个<see cref="Boolean"/>值，指示附近是否有威胁。
        ''' </summary>
        ''' <param name="center">中心位置。</param>
        ''' <param name="radius">半径。</param>
        ''' <param name="detect">检测函数（无需包括<see cref="Ped.IsAlive"/>）。</param>
        ''' <returns>附近是否有威胁。</returns>
        Public Shared Function FindThreat(center As Vector3, radius As Single, detect As Predicate(Of Ped)) As Boolean
            For Each ped As Ped In World.GetNearbyPeds(center, radius)
                If ped.IsDead Then
                    Continue For
                End If
                If detect(ped) Then
                    Return True
                End If
            Next
            Return False
        End Function
        ''' <summary>
        ''' 获取距离<paramref name="center"/>最近的威胁。
        ''' </summary>
        ''' <param name="center">中心位置。</param>
        ''' <param name="radius">半径。</param>
        ''' <param name="detect">检测函数（无需包含<see cref="Ped.IsAlive"/>）</param>
        ''' <returns>附近是否有威胁。</returns>
        Public Shared Function GetNearlistTreat(center As Vector3, radius As Single, detect As Predicate(Of Ped)) As Ped
            Dim target As Minimize(Of Ped) = New Minimize(Of Ped)()
            For Each ped As Ped In World.GetNearbyPeds(center, radius)
                If ped.IsDead Then
                    Continue For
                End If
                If detect(ped) Then
                    target.TryNext(ped.Position.DistanceTo(center), ped)
                End If
            Next
            Return target.Selected
        End Function
        Public Shared Function IsCommonThreat(ped As Ped) As Boolean
            If Not Detection.Enabled Then
                Return False
            End If
            If ped.RelationshipGroup <> PlayerPed.RelationshipGroup Then
                Dim relation As Relationship = ped.RelationshipGroup.GetRelationshipBetweenGroups(PlayerPed.RelationshipGroup)
                If relation = Relationship.Hate AndAlso ped.IsHuman Then
                    Return True
                ElseIf ped.IsInCombat Then
                    If relation = Relationship.Companion OrElse relation = Relationship.Like OrElse relation = Relationship.Respect Then
                        Return False
                    Else
                        Return True
                    End If
                ElseIf ped.PedType = PedType.Cop OrElse ped.PedType = PedType.Army OrElse ped.PedType = PedType.Swat OrElse ped.PedType = PedType.Criminal Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return False '友方士兵
            End If
        End Function
    End Class
End Namespace

