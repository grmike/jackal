﻿namespace JackalWebHost2.Controllers.Models.Lobby;

public class LobbyMemberModel
{
    public long UserId { get; set; }
    
    public string UserName { get; set; }
    
    public long? TeamId { get; set; }
}