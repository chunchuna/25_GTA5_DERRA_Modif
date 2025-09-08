Imports System.Drawing
Imports GTA
Imports GTA.Math
Imports GTA.Native
Imports GTA.UI
Imports System.Windows.Forms
Imports DERRA.InteliNPC.AI

''' <summary>
''' Displays a GTA Online-style player list when L key is pressed
''' </summary>
Public Class OnlinePlayerList
    Inherits Script

    ' Constants for UI positioning and sizing
    Private Const LIST_WIDTH As Single = 350.0F
    Private Const PLAYER_HEIGHT As Single = 20.0F ' Reduced height
    Private Const HEADER_HEIGHT As Single = 30.0F
    Private Const MARGIN As Single = 10.0F
    Private Const ICON_SIZE As Single = 25.0F ' Reduced icon size
    Private Const PLAYERS_PER_PAGE As Integer = 20 ' Maximum players per page
    
    ' Colors
    Private ReadOnly BACKGROUND_COLOR As Color = Color.FromArgb(200, 0, 0, 0)
    Private ReadOnly HEADER_COLOR As Color = Color.FromArgb(200, 0, 120, 200)
    Private ReadOnly ALLY_COLOR As Color = Color.FromArgb(255, 87, 166, 230)
    Private ReadOnly ENEMY_COLOR As Color = Color.FromArgb(255, 230, 87, 87)
    Private ReadOnly TEXT_COLOR As Color = Color.White
    Private ReadOnly RANK_COLOR As Color = Color.FromArgb(255, 255, 180, 0)
    
    ' State
    Private isListVisible As Boolean = False
    Private currentPage As Integer = 0
    
    ' Random for generating player levels and stats
    Private ReadOnly rand As New Random()
    
    ' Player stats cache (to avoid regenerating random values every frame)
    Private ReadOnly playerLevels As New Dictionary(Of String, Integer)()
    Private ReadOnly playerKDRatios As New Dictionary(Of String, Single)()
    Private ReadOnly playerStatuses As New Dictionary(Of String, Integer)()
    
    ' Flag to show initial notification
    Private showInitialNotification As Boolean = True
    Private initialNotificationTime As DateTime = DateTime.Now
    
    ' Player status enum values
    Private Const STATUS_ONLINE As Integer = 0
    Private Const STATUS_IN_VEHICLE As Integer = 1
    Private Const STATUS_ON_MISSION As Integer = 2
    Private Const STATUS_SHOPPING As Integer = 3
    Private Const STATUS_PASSIVE As Integer = 4
    
    ' Constructor
    Public Sub New()
        ' Set up event handlers
        AddHandler Tick, AddressOf OnTick
        AddHandler KeyDown, AddressOf OnKeyDown
        
        ' Initialize player stats for the local player
        playerLevels(Game.Player.Name) = rand.Next(1, 1000)
        playerKDRatios(Game.Player.Name) = CSng(System.Math.Round(rand.NextDouble() * 5, 2))
        playerStatuses(Game.Player.Name) = STATUS_ONLINE
        
        ' Set initial notification time to 5 seconds from now
        initialNotificationTime = DateTime.Now.AddSeconds(5)
    End Sub
    
    ' Handle key presses
    Private Sub OnKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.L Then
            ' Toggle player list visibility
            isListVisible = Not isListVisible
            
            ' Reset to first page when opening
            If isListVisible Then
                currentPage = 0
            End If
            
            ' Play sound when toggling
            Native.Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Menu_Accept", "Phone_SoundSet_Default", False)
        ElseIf isListVisible Then
            ' Handle page navigation
            If e.KeyCode = Keys.Up OrElse e.KeyCode = Keys.Left Then
                ' Previous page
                If currentPage > 0 Then
                    currentPage -= 1
                    Native.Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", False)
                End If
            ElseIf e.KeyCode = Keys.Down OrElse e.KeyCode = Keys.Right Then
                ' Next page
                Dim totalPlayers As Integer = BotFactory.Pool.Count + 1
                Dim totalPages As Integer = CInt(System.Math.Ceiling(totalPlayers / CDbl(PLAYERS_PER_PAGE)))
                
                If currentPage < totalPages - 1 Then
                    currentPage += 1
                    Native.Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", False)
                End If
            End If
        End If
    End Sub
    
    ' Main update loop
    Private Sub OnTick(sender As Object, e As EventArgs)
        ' Show initial notification after 5 seconds
        If showInitialNotification AndAlso DateTime.Now >= initialNotificationTime Then
            UI.Screen.ShowSubtitle("按L键查看在线玩家列表", 5000)
            showInitialNotification = False
        End If
        
        If isListVisible Then
            DrawPlayerList()
        End If
    End Sub
    
    ' Draw the player list UI
    Private Sub DrawPlayerList()
        ' Update the bot list
        BotFactory.Pool.Update()
        
        ' Calculate positions
        Dim screenRes As SizeF = UI.Screen.Resolution
        Dim startX As Single = MARGIN
        Dim startY As Single = MARGIN
        
        ' Get all players (current player + bots)
        Dim allPlayers As New List(Of Tuple(Of Ped, String, Boolean, Integer, Single, Integer))()
        
        ' Add current player first
        allPlayers.Add(New Tuple(Of Ped, String, Boolean, Integer, Single, Integer)(
            Game.Player.Character, 
            Game.Player.Name, 
            True, 
            GetPlayerLevel(Game.Player.Name), 
            GetPlayerKDRatio(Game.Player.Name), 
            GetPlayerStatus(Game.Player.Name)))
        
        ' Add all bots
        For Each bot As Bot In BotFactory.Pool
            If bot IsNot Nothing AndAlso bot.Ped.Exists() Then
                allPlayers.Add(New Tuple(Of Ped, String, Boolean, Integer, Single, Integer)(
                    bot.Ped, 
                    bot.Name, 
                    bot.IsAlly, 
                    GetPlayerLevel(bot.Name), 
                    GetPlayerKDRatio(bot.Name), 
                    GetPlayerStatus(bot.Name)))
            End If
        Next
        
        ' Calculate total players and pages
        Dim totalPlayers As Integer = allPlayers.Count
        Dim totalPages As Integer = CInt(System.Math.Ceiling(totalPlayers / CDbl(PLAYERS_PER_PAGE)))
        
        ' Ensure current page is valid
        If currentPage >= totalPages Then
            currentPage = totalPages - 1
        End If
        If currentPage < 0 Then
            currentPage = 0
        End If
        
        ' Calculate players for current page
        Dim startIndex As Integer = currentPage * PLAYERS_PER_PAGE
        Dim endIndex As Integer = System.Math.Min(startIndex + PLAYERS_PER_PAGE - 1, totalPlayers - 1)
        Dim playersOnCurrentPage As Integer = endIndex - startIndex + 1
        
        ' Calculate list height based on number of players on current page
        Dim listHeight As Single = HEADER_HEIGHT + (playersOnCurrentPage * PLAYER_HEIGHT)
        
        ' Draw background
        DrawRect(startX, startY, LIST_WIDTH, listHeight, BACKGROUND_COLOR)
        
        ' Draw header
        DrawRect(startX, startY, LIST_WIDTH, HEADER_HEIGHT, HEADER_COLOR)
        
        ' Draw title and player count in header
        Dim headerTextY As Single = startY + (HEADER_HEIGHT / 2) - 7
        DrawText("PLAYERS", startX + (LIST_WIDTH / 2), headerTextY, 0.5F, TEXT_COLOR, True, True)
        
        ' Draw page info and player count
        Dim pageInfo As String = $"PAGE {currentPage + 1}/{totalPages}"
        DrawText(pageInfo, startX + MARGIN, headerTextY + 15, 0.3F, TEXT_COLOR, False, True)
        DrawText($"{totalPlayers} PLAYERS", startX + LIST_WIDTH - MARGIN * 6, headerTextY + 15, 0.3F, TEXT_COLOR, False, True)
        
        ' Draw players for current page
        Dim currentY As Single = startY + HEADER_HEIGHT
        
        For i As Integer = startIndex To endIndex
            Dim playerInfo As Tuple(Of Ped, String, Boolean, Integer, Single, Integer) = allPlayers(i)
            Dim rank As Integer = i
            
            DrawPlayerEntry(playerInfo.Item1, playerInfo.Item2, rank, playerInfo.Item3, 
                            currentY, playerInfo.Item4, playerInfo.Item5, playerInfo.Item6)
            
            currentY += PLAYER_HEIGHT
        Next
        
        ' Draw navigation hint if multiple pages
        If totalPages > 1 Then
            Dim navHint As String = "Use ← → or ↑ ↓ to navigate pages"
            DrawText(navHint, startX + (LIST_WIDTH / 2), startY + listHeight + 5, 0.3F, Color.LightGray, True, True)
        End If
    End Sub
    
    ' Get or generate player level
    Private Function GetPlayerLevel(playerName As String) As Integer
        If Not playerLevels.ContainsKey(playerName) Then
            playerLevels(playerName) = rand.Next(1, 1000)
        End If
        Return playerLevels(playerName)
    End Function
    
    ' Get or generate player K/D ratio
    Private Function GetPlayerKDRatio(playerName As String) As Single
        If Not playerKDRatios.ContainsKey(playerName) Then
            playerKDRatios(playerName) = CSng(System.Math.Round(rand.NextDouble() * 5, 2))
        End If
        Return playerKDRatios(playerName)
    End Function
    
    ' Get or generate player status
    Private Function GetPlayerStatus(playerName As String) As Integer
        If Not playerStatuses.ContainsKey(playerName) Then
            playerStatuses(playerName) = rand.Next(0, 5) ' 0-4 for different statuses
        End If
        
        ' Update status based on actual state for bots
        For Each bot As Bot In BotFactory.Pool
            If bot.Name = playerName Then
                If bot.Ped.IsInVehicle() Then
                    playerStatuses(playerName) = STATUS_IN_VEHICLE
                ElseIf bot.CurrentActionName.Contains("mission") OrElse bot.CurrentActionName.Contains("task") Then
                    playerStatuses(playerName) = STATUS_ON_MISSION
                End If
                Exit For
            End If
        Next
        
        Return playerStatuses(playerName)
    End Function
    
    ' Get status text based on player status
    Private Function GetStatusText(status As Integer) As String
        Select Case status
            Case STATUS_ONLINE
                Return "ONLINE"
            Case STATUS_IN_VEHICLE
                Return "IN VEHICLE"
            Case STATUS_ON_MISSION
                Return "ON MISSION"
            Case STATUS_SHOPPING
                Return "SHOPPING"
            Case STATUS_PASSIVE
                Return "PASSIVE MODE"
            Case Else
                Return "ONLINE"
        End Select
    End Function
    
    ' Draw a single player entry in the list
    Private Sub DrawPlayerEntry(ped As Ped, name As String, rank As Integer, isAlly As Boolean, y As Single, level As Integer, kdRatio As Single, status As Integer)
        Dim x As Single = MARGIN
        
        ' Draw player entry background (slightly transparent)
        DrawRect(x, y, LIST_WIDTH, PLAYER_HEIGHT, Color.FromArgb(100, 0, 0, 0))
        
        ' Draw rank
        DrawText(rank.ToString(), x + MARGIN, y + (PLAYER_HEIGHT / 2) - 5, 0.35F, TEXT_COLOR, False, True)
        
        ' Draw player icon/avatar at the left
        Dim iconX As Single = x + MARGIN * 3
        Dim iconY As Single = y + (PLAYER_HEIGHT / 2) - (ICON_SIZE / 2)
        
        ' Draw player avatar (use ped head shot if possible)
        If ped IsNot Nothing AndAlso ped.Exists() Then
            ' Try to get ped mugshot/headshot
            Dim handle As Integer = 0
            Dim textureDict As String = ""
            Dim textureName As String = ""
            
            ' Request ped mugshot
            Try
                handle = Native.Function.Call(Of Integer)(Hash.REGISTER_PEDHEADSHOT, ped.Handle)
                
                ' Wait for the mugshot to load (up to 50ms)
                Dim startTime As DateTime = DateTime.Now
                While Not Native.Function.Call(Of Boolean)(Hash.IS_PEDHEADSHOT_READY, handle)
                    If (DateTime.Now - startTime).TotalMilliseconds > 50 Then
                        Exit While ' Timeout after 50ms
                    End If
                End While
                
                If Native.Function.Call(Of Boolean)(Hash.IS_PEDHEADSHOT_READY, handle) Then
                    textureName = Native.Function.Call(Of String)(Hash.GET_PEDHEADSHOT_TXD_STRING, handle)
                    
                    ' If we got a valid texture name, draw it
                    If Not String.IsNullOrEmpty(textureName) Then
                        ' Request the texture dictionary
                        Native.Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, textureName, False)
                        
                        ' Wait for texture to load (up to 50ms)
                        startTime = DateTime.Now
                        While Not Native.Function.Call(Of Boolean)(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, textureName)
                            If (DateTime.Now - startTime).TotalMilliseconds > 50 Then
                                Exit While ' Timeout after 50ms
                            End If
                        End While
                        
                        ' Draw the texture if loaded
                        If Native.Function.Call(Of Boolean)(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, textureName) Then
                            ' Convert screen coordinates to relative (0.0-1.0)
                            Dim res As SizeF = UI.Screen.Resolution
                            Dim relX As Single = (iconX + (ICON_SIZE / 2)) / res.Width
                            Dim relY As Single = (iconY + (ICON_SIZE / 2)) / res.Height
                            Dim relWidth As Single = ICON_SIZE / res.Width
                            Dim relHeight As Single = ICON_SIZE / res.Height
                            
                            ' Draw the mugshot texture
                            Native.Function.Call(Hash.DRAW_SPRITE, textureName, "MUGSHOT_TAKEN_PHOTO", 
                                               relX, relY, relWidth, relHeight, 0.0F, 255, 255, 255, 255)
                            
                            ' Draw a border around the mugshot
                            Dim borderColor As Color = Color.FromArgb(255, 255, 255, 255)
                            DrawRect(iconX, iconY, ICON_SIZE, 1, borderColor) ' Top
                            DrawRect(iconX, iconY + ICON_SIZE, ICON_SIZE, 1, borderColor) ' Bottom
                            DrawRect(iconX, iconY, 1, ICON_SIZE, borderColor) ' Left
                            DrawRect(iconX + ICON_SIZE, iconY, 1, ICON_SIZE, borderColor) ' Right
                            
                            ' Release the texture dictionary
                            Native.Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, textureName)
                        Else
                            ' Fallback to colored icon with letter
                            DrawFallbackIcon(iconX, iconY, ICON_SIZE, ICON_SIZE, isAlly, name)
                        End If
                    Else
                        ' Fallback to colored icon with letter
                        DrawFallbackIcon(iconX, iconY, ICON_SIZE, ICON_SIZE, isAlly, name)
                    End If
                Else
                    ' Fallback to colored icon with letter
                    DrawFallbackIcon(iconX, iconY, ICON_SIZE, ICON_SIZE, isAlly, name)
                End If
                
                ' Unregister the pedheadshot
                If handle <> 0 Then
                    Native.Function.Call(Hash.UNREGISTER_PEDHEADSHOT, handle)
                End If
            Catch ex As Exception
                ' If any error occurs, use fallback
                DrawFallbackIcon(iconX, iconY, ICON_SIZE, ICON_SIZE, isAlly, name)
            End Try
        Else
            ' Fallback to colored icon with letter
            DrawFallbackIcon(iconX, iconY, ICON_SIZE, ICON_SIZE, isAlly, name)
        End If
        
        ' Draw player name
        Dim nameColor As Color = If(isAlly, ALLY_COLOR, TEXT_COLOR)
        DrawText(name, iconX + ICON_SIZE + MARGIN, y + (PLAYER_HEIGHT / 2) - 5, 0.35F, nameColor, False, True)
        
        ' Draw player level (RP rank)
        DrawText("RP " + level.ToString(), iconX + ICON_SIZE + MARGIN + 130, y + (PLAYER_HEIGHT / 2) - 5, 0.3F, RANK_COLOR, False, True)
        
        ' Draw K/D ratio
        DrawText("K/D " + kdRatio.ToString("0.00"), iconX + ICON_SIZE + MARGIN + 190, y + (PLAYER_HEIGHT / 2) - 5, 0.3F, Color.LightGray, False, True)
        
        ' Draw status icon (based on status)
        DrawStatusIcon(x + LIST_WIDTH - MARGIN * 3, y + (PLAYER_HEIGHT / 2), status)
    End Sub
    
    ' Draw fallback icon with first letter
    Private Sub DrawFallbackIcon(x As Single, y As Single, width As Single, height As Single, isAlly As Boolean, name As String)
        ' Draw colored background for the icon
        Dim iconColor As Color = If(isAlly, ALLY_COLOR, ENEMY_COLOR)
        DrawRect(x, y, width, height, iconColor)
        
        ' Draw a border around the icon
        Dim borderColor As Color = Color.FromArgb(255, 255, 255, 255)
        DrawRect(x, y, width, 1, borderColor) ' Top
        DrawRect(x, y + height, width, 1, borderColor) ' Bottom
        DrawRect(x, y, 1, height, borderColor) ' Left
        DrawRect(x + width, y, 1, height, borderColor) ' Right
        
        ' Draw first letter of name in icon
        If name.Length > 0 Then
            Dim firstLetter As String = name.Substring(0, 1).ToUpper()
            DrawText(firstLetter, x + (width / 2), y + (height / 2) - 5, 0.4F, Color.White, True, True)
        End If
    End Sub
    
    ' Draw status icon
    Private Sub DrawStatusIcon(x As Single, y As Single, status As Integer)
        Dim iconColor As Color
        
        Select Case status
            Case STATUS_ONLINE
                iconColor = Color.LimeGreen
            Case STATUS_IN_VEHICLE
                iconColor = Color.SkyBlue
            Case STATUS_ON_MISSION
                iconColor = Color.Orange
            Case STATUS_SHOPPING
                iconColor = Color.Yellow
            Case STATUS_PASSIVE
                iconColor = Color.White
            Case Else
                iconColor = Color.LimeGreen
        End Select
        
        ' Draw a colored circle for the status
        DrawRect(x - 5, y - 5, 10, 10, iconColor)
    End Sub
    
    ' Draw a rectangle on screen
    Private Sub DrawRect(x As Single, y As Single, width As Single, height As Single, color As Color)
        Dim res As SizeF = UI.Screen.Resolution
        
        ' Convert to screen coordinates (0.0-1.0)
        Dim relX As Single = (x + (width / 2)) / res.Width
        Dim relY As Single = (y + (height / 2)) / res.Height
        Dim relWidth As Single = width / res.Width
        Dim relHeight As Single = height / res.Height
        
        Native.Function.Call(Hash.DRAW_RECT, relX, relY, relWidth, relHeight, color.R, color.G, color.B, color.A)
    End Sub
    
    ' Draw text on screen
    Private Sub DrawText(text As String, x As Single, y As Single, scale As Single, color As Color, centered As Boolean, shadow As Boolean)
        ' Create a TextElement and draw it
        Dim alignment As UI.Alignment = If(centered, UI.Alignment.Center, UI.Alignment.Left)
        Dim textElement As New UI.TextElement(text, New PointF(x, y), scale, color, UI.Font.ChaletLondon, alignment)
        textElement.Draw()
    End Sub
End Class