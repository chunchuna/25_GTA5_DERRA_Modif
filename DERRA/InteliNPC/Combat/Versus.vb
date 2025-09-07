Imports DERRA.InteliNPC.AI
Imports GTA
Imports GTA.Graphics
Imports GTA.Native
Imports GTA.UI

Namespace InteliNPC.Combat
    Public Class Versus
        Inherits Script
        Public Shared ReadOnly CauseOfDeath As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)()
        Public Shared ReadOnly KillRecord As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)()
        Private Shared Function GetValue(dict As Dictionary(Of String, Integer), key As String) As Integer
            Dim value As Integer = 0
            dict.TryGetValue(key, value)
            Return value
        End Function


        Public Shared Property EnermyScore(enermyName As String) As Integer
            Get
                Return GetValue(CauseOfDeath, enermyName)
            End Get
            Set(value As Integer)
                CauseOfDeath.Item(enermyName) = value
            End Set
        End Property
        Public Shared Property PlayerScore(enermyName As String) As Integer
            Get
                Return GetValue(KillRecord, enermyName)
            End Get
            Set(value As Integer)
                KillRecord.Item(enermyName) = value
            End Set
        End Property
        Private Shared Function GetPedHeadShotId(ped As Ped) As Integer
            Dim id As Integer = [Function].Call(Of Integer)(Hash.REGISTER_PEDHEADSHOT, ped)
            While Not [Function].Call(Of Boolean)(Hash.IS_PEDHEADSHOT_READY, id) OrElse Not [Function].Call(Of Boolean)(Hash.IS_PEDHEADSHOT_VALID, id)
                Script.Wait(1)
            End While
            Return id
        End Function
        Public Shared Sub ShowScore(enermy As Ped, enermyName As String)
            Dim player_id As Integer = GetPedHeadShotId(PlayerPed)
            Dim enermy_id As Integer = GetPedHeadShotId(enermy)
            Dim player_face As String = [Function].Call(Of String)(Hash.GET_PEDHEADSHOT_TXD_STRING, player_id)
            Dim bot_face As String = [Function].Call(Of String)(Hash.GET_PEDHEADSHOT_TXD_STRING, enermy_id)


            Dim player_texture As TextureAsset = New TextureAsset(player_face, player_face)
            Dim bot_texture As TextureAsset = New TextureAsset(bot_face, bot_face)
            Notification.PostVersusTitleUpdate(player_texture, Versus.PlayerScore(enermyName), bot_texture, Versus.EnermyScore(enermyName), HudColor.BlueLight, HudColor.Red)
            [Function].Call(Hash.UNREGISTER_PEDHEADSHOT, player_id)
            [Function].Call(Hash.UNREGISTER_PEDHEADSHOT, enermy_id)
        End Sub
        Private Sub Versus_Tick(sender As Object, e As EventArgs) Handles Me.Tick
            While PlayerPed?.IsAlive
                Wait(1000)
            End While
            Dim killer As Entity = PlayerPed.Killer
            If killer IsNot Nothing AndAlso killer.EntityType = EntityType.Vehicle Then
                killer = TryCast(killer, Vehicle)?.Driver
            End If
            For Each bot As Bot In BotFactory.Pool
                If killer = bot.Ped Then
                    EnermyScore(bot.Name) += 1
                    Notification.PostTicker($"~h~{bot.Name}~h~ <font color='rgba(255,255,255,0.8)'>使用 {Weapon.GetHumanNameFromHash(PlayerPed.CauseOfDeath)} 杀死了您</font>", False)
                    ShowScore(killer, bot.Name)
                    Exit For
                End If
            Next
            While PlayerPed?.IsDead
                Wait(1000)
            End While
        End Sub
    End Class
End Namespace

